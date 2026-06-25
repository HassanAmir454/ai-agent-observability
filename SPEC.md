# AI Agent Observability — Specification

**Status:** Written before any implementation
**Version:** 1.0
**Last updated:** 2026-06-23
**Author:** Hassan Amir
**Assignment:** M-Files — AI Native Engineer (Agentic Development)

---

## 1. What This System Does

Engineering teams use AI coding tools every day — Claude Code, Cursor, GitHub Copilot — but have almost no visibility into how those tools are actually used. How many tokens are consumed? Which agent is used most? How much does it cost? Where is the time spent?

This system answers those questions with a **local-first observability stack**: a collector reads AI agent telemetry from a developer workstation on a schedule, an ingest API validates and stores it, and a live dashboard visualises it. Everything runs locally with one `docker compose up`.

---

## 2. System Architecture

```
Developer Workstation
│
├── AI tools (run normally) → write session files to disk
│     Claude Code → ~/.claude/projects/
│     Cursor      → ~/.cursor/ (sqlite)
│     Codex       → ~/.codex/sessions/
│
└── Collector (CodeBurn extension)
      Runs on a configurable interval (default 60 min)
      Reads data via CodeBurn (already installed in this repo)
      Maps to the AgentEvent schema
      POSTs a batch to the Azure Function (Authorization: Bearer JWT)
            │
            ▼
      Azure Function (C# .NET, Functions v4 isolated)
        POST /auth/token      → exchange API key for a JWT
        POST /events          → validate + persist batch
        GET  /events/summary  → aggregate for dashboard
        GET  /health          → DB connectivity check
            │
            ▼
      Azure SQL Edge (Docker)
        AgentEvents table, indexed on timestamp + agent
            │
            ▼
      React + TypeScript dashboard
        Polls /events/summary, renders charts + feed
```

---

## 3. Data Source — Grounded in Real CodeBurn Output

> This section was written **after running the tool**, not from assumptions. The collector's source of truth is the JSON CodeBurn already produces.

The collector obtains data by invoking CodeBurn's existing JSON output rather than re-parsing session files:

```
codeburn report --format json                 # all providers combined
codeburn report --provider claude --format json
codeburn report --provider cursor --format json
codeburn report --provider codex  --format json
```

A real `report --format json` run on this workstation returns aggregated rollups, not a flat list of fully-detailed events. The top-level shape is:

| Key | Contains |
|---|---|
| `overview` | totals: `cost`, `calls`, `sessions`, `tokens{input,output,cacheRead,cacheWrite}` |
| `daily[]` | per-day rollups: `date`, `cost`, `calls`, `turns`, `editTurns`, `oneShotRate` |
| `projects[]` | `name`, `path`, `cost`, `calls`, `sessions`, `avgCostPerSession` |
| `models[]` | `name`, `calls`, `inputTokens`, `outputTokens`, `cacheReadTokens`, `cacheWriteTokens`, `cost`, `oneShotRate` |
| `activities[]` | `category`, `cost`, `turns`, `editTurns` |
| `tools[]` | `name`, `calls` |
| `topSessions[]` | `project`, `sessionId`, `date`, `cost`, `calls` |

### Per-agent attribution mechanism
CodeBurn's `--provider` flag works on `report`. The collector therefore runs `report` **once per provider** and tags every emitted event with that provider as `agentName`. This gives clean per-agent breakdowns without re-implementing any parsing.

### Honest field-availability note (implementation must verify)
Not every target field exists at session granularity in the `report` output:

- **Available directly:** agent/provider, cost, calls/apiCalls, token totals (input/output/cache), per-day timeline, activity categories, session ids (from `topSessions`).
- **Aggregate-only or derived:** per-session token split, per-session activity type, and `sessionDurationMinutes` are not present per-session in `report`. The collector derives session duration from session first/last timestamps where available, and otherwise sends `0` / `null` for fields CodeBurn does not expose at that grain.
- **Action for implementer:** before finalising the mapper, run `codeburn report --format json` and `codeburn export -f json` and confirm each mapped field against live output. Do not claim a field the tool does not emit.

### Event granularity decision
The collector uses `codeburn export -f json` as its source; an event = one row from `sessions[]` (real id, timestamp, cost, calls, turns, project). Per-session token/model/activity are enriched from the aggregate `models[]`/`activity[]` arrays. This decision and its trade-off are documented in DESIGN.md (Decision 8).

---

## 4. Component 1 — Collector

**Purpose:** extend CodeBurn to periodically collect AI agent data and transmit it to the ingest API. No existing CodeBurn source file is modified.

### Environment variables

| Variable | Required | Default | Description |
|---|---|---|---|
| `INGEST_ENDPOINT` | Yes | — | Azure Function base URL |
| `COLLECTOR_API_KEY` | Yes | — | Exchanged at `/auth/token` for a JWT bearer token |
| `COLLECTION_INTERVAL_MINUTES` | No | 60 | How often to collect |

### Acceptance criteria

**C1 — Successful scheduled collection**
```
Given the collector is running with interval = 60
When 60 minutes elapse
Then sessions are collected via CodeBurn for each provider
And mapped to AgentEvent format
And sent as a single batch POST to /events
And HTTP 201 is received
And success is logged with the event count
```

**C2 — Configurable interval**
```
Given COLLECTION_INTERVAL_MINUTES = 2
When the collector starts
Then collection runs every 2 minutes
And the default of 60 is used when the variable is unset
```

**C3 — Immediate collection on startup**
```
Given the collector starts for the first time
When the process initialises
Then one collection runs immediately
And the scheduled interval begins after that first run
```

**C4 — API unavailable does not crash collector**
```
Given the Azure Function is not running
When scheduled collection runs and the POST fails
Then the network error is caught and logged
And the collector process keeps running
And it retries on the next scheduled interval
```

**C5 — Missing required configuration fails fast**
```
Given INGEST_ENDPOINT is not set
When the collector starts
Then the process exits immediately
And the error names the missing variable
```

**C6 — Empty collection is handled**
```
Given no AI tool sessions exist on the workstation
When collection runs
Then no empty batch is sent
And a log line states that no events were collected
And the collector continues normally
```

**C7 — Manual trigger (required by assignment)**
```
Given a developer wants data without waiting for the schedule
When they run the documented manual-trigger command
  (e.g. `npm run collect:once`)
Then exactly one collection runs and POSTs immediately
And the scheduled loop is unaffected
```

---

## 5. Component 2 — Ingest API (Azure Function, C# .NET v4 isolated)

### Endpoint — `POST /auth/token`

Exchanges the shared API key for a short-lived JWT. The collector calls this first, caches the token, and reuses it until it expires.

**Request body**
```json
{ "apiKey": "<COLLECTOR_API_KEY>" }
```

**Response**
```json
{ "token": "<JWT>", "expiresIn": 86400 }
```

The token is an HMAC-SHA256 (HS256) JWT with claims `iss` = `observability-api`, `aud` = `collector`, `exp` = issue time + 24h, and `nbf` = issue time. A wrong or missing key returns `401` and no token is issued. The API validates issuer, audience, expiry, and signature on every `POST /events` call before any database write.

### Endpoint 1 — `POST /events`

**Request body**
```json
{
  "events": [
    {
      "agentName": "Cursor",
      "sessionId": "6d3aa141-07a0-405c-bd37-653bc7313154",
      "model": "Cursor (auto)",
      "inputTokens": 17220,
      "outputTokens": 23090,
      "cacheReadTokens": 0,
      "cacheWriteTokens": 0,
      "reasoningTokens": 0,
      "totalCostUsd": 0.398010,
      "activityType": "coding",
      "apiCalls": 52,
      "hasAgentSpawn": false,
      "sessionDurationMinutes": 0,
      "eventTimestamp": "2026-06-19T00:00:00Z",
      "collectedAt": "2026-06-23T10:39:33Z"
    }
  ],
  "collectorVersion": "1.0.0"
}
```

**C8 — Valid batch accepted**
```
Given a valid batch of 5 events with a valid bearer token
When POST /events is called
Then all 5 are saved in one transaction
And 201 is returned with { "stored": 5, "message": "..." }
```

**C9 — Authentication required**
```
Given a request with no valid Authorization: Bearer token
When POST /events is called
Then 401 is returned
And no database write is attempted
And the rejected attempt is logged
```

**C10 — Empty batch rejected**
```
Given an empty events array
When POST /events is called
Then 400 is returned with "Batch cannot be empty"
```

**C11 — Oversized batch rejected**
```
Given a batch of 1001 events
When POST /events is called
Then 400 is returned with "Batch cannot exceed 1000 events"
```

**C12 — Invalid event rejected atomically**
```
Given one event in the batch has a missing agentName
When POST /events is called
Then 400 is returned naming the missing field
And no events are saved (all-or-nothing)
```

**C13 — Malformed JSON rejected safely**
```
Given a malformed JSON body
When POST /events is called
Then 400 is returned
And no internal error details are exposed
```

**C14 — Database failure surfaced safely**
```
Given the database is unreachable
When POST /events is called
Then 503 is returned
And full details are logged internally only
And the caller sees "Service temporarily unavailable"
```

### Endpoint 2 — `GET /events/summary`

**Query:** `timeRange` ∈ {`1h`,`6h`,`24h`,`7d`}, default `24h`.

**Response**
```json
{
  "totalEvents": 142,
  "timeRange": "24h",
  "generatedAt": "2026-06-23T10:00:00Z",
  "totalCostUsd": 2.8431,
  "totalTokens": 1842930,
  "averageSessionDurationMinutes": 34,
  "byAgent": [
    { "agentName": "Cursor", "eventCount": 89, "totalCostUsd": 1.9231, "totalTokens": 1204830 }
  ],
  "byActivity": [ { "activityType": "coding", "count": 67 } ],
  "timeline": [ { "hour": "2026-06-23T09:00:00Z", "count": 12, "totalCost": 0.2341 } ],
  "recentEvents": []
}
```

**C15 — Valid range returns aggregates**
```
Given 142 events exist in the last 24 hours
When GET /events/summary?timeRange=24h is called
Then totalEvents = 142
And byAgent covers all active agents
And timeline has hourly buckets
And recentEvents has the last 20 events
And averageSessionDurationMinutes is returned
```

**C16 — Invalid range rejected**
```
Given timeRange=99h
When GET /events/summary is called
Then 400 is returned listing valid options
```

**C17 — Empty database returns zeros, not 404**
```
Given no events exist
When GET /events/summary is called
Then 200 is returned with zero counts and empty arrays
```

### Endpoint 3 — `GET /health`

**C18 — Healthy**
```
Given the database is connected
When GET /health is called
Then 200 with { status: "healthy", database: "connected" }
```

**C19 — Unhealthy**
```
Given the database container is stopped
When GET /health is called
Then 503 with { status: "unhealthy", database: "unreachable" }
```

---

## 6. Component 3 — Database (Azure SQL Edge)

```sql
CREATE TABLE AgentEvents (
  Id                     INT PRIMARY KEY IDENTITY(1,1),
  AgentName              NVARCHAR(100) NOT NULL,
  SessionId              NVARCHAR(200) NOT NULL,
  Model                  NVARCHAR(100),
  InputTokens            INT NOT NULL DEFAULT 0,
  OutputTokens           INT NOT NULL DEFAULT 0,
  CacheReadTokens        INT NOT NULL DEFAULT 0,
  CacheWriteTokens       INT NOT NULL DEFAULT 0,
  ReasoningTokens        INT NOT NULL DEFAULT 0,
  TotalCostUsd           DECIMAL(10,6) NOT NULL DEFAULT 0,
  ActivityType           NVARCHAR(50),
  ApiCalls               INT NOT NULL DEFAULT 1,
  HasAgentSpawn          BIT NOT NULL DEFAULT 0,
  SessionDurationMinutes INT NOT NULL DEFAULT 0,
  EventTimestamp         DATETIME2 NOT NULL,
  CollectedAt            DATETIME2 NOT NULL,
  CreatedAt              DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
  CONSTRAINT UQ_AgentEvents UNIQUE (SessionId, EventTimestamp, AgentName)
);

CREATE INDEX IX_AgentEvents_EventTimestamp
  ON AgentEvents(EventTimestamp DESC);
CREATE INDEX IX_AgentEvents_AgentName_Timestamp
  ON AgentEvents(AgentName, EventTimestamp DESC);
CREATE INDEX IX_AgentEvents_ActivityType
  ON AgentEvents(ActivityType, EventTimestamp DESC);
```

> The `UQ_AgentEvents` unique constraint makes re-sent batches idempotent: if the collector retries after a partial failure, duplicate sessions are rejected rather than double-counted.

**C20 — Time-range query is fast**
```
Given 10,000 events across 30 days
When querying the last 24 hours
Then the IX_AgentEvents_EventTimestamp index is used
And the query returns in under 500 ms
```

**C21 — Schema initialises on first run**
```
Given a fresh Docker environment
When docker compose up runs
Then init.sql creates the table and indexes
And seed.sql inserts sample data
```

---

## 7. Component 4 — Dashboard (React + TypeScript)

**C22 — Initial load with data**
```
Given events exist
When the dashboard opens
Then loading skeletons show briefly
And the total card, by-agent chart, timeline chart,
   and recent-events feed all render correctly
And the header shows a last-updated timestamp
```

**C23 — Time-range selection**
```
Given the dashboard shows 24h
When the user clicks 7d
Then all components update together
And the API is called with timeRange=7d
And no full page reload occurs
```

**C24 — API unavailable**
```
Given the Azure Function is down
When the dashboard loads or polls
Then an error banner appears
And the last known data stays visible if available
And the dashboard does not crash or blank out
```

**C25 — Empty state**
```
Given no events exist
When the dashboard loads
Then a "No data yet" state shows
And a hint explains how to trigger a manual collection
```

**C26 — Automatic polling**
```
Given the dashboard is open
When 60 seconds elapse
Then GET /events/summary is called automatically
And the last-updated timestamp refreshes
```

---

## 8. Traceability Matrix

> The role explicitly values "strong traceability between specifications, code, and tests." Each criterion maps to a test before it is built.

| Criterion | Component | Test (planned) |
|---|---|---|
| C1, C3, C6 | Collector | `scheduler.test.ts` |
| C2, C5, C7 | Collector | `config.test.ts` |
| C4 | Collector | `eventSender.test.ts` |
| C8, C10–C13 | API | `IngestEventsTests` |
| C9 | API | `AuthTests` |
| C14, C20 | API/DB | `EventRepositoryTests` |
| C15–C17 | API | `SummaryTests` |
| C18, C19 | API | `HealthTests` |
| C21 | DB | `docker compose up` smoke test |
| C22–C26 | Dashboard | `dashboard.test.tsx` |

---

## 9. Non-Functional Requirements

| Requirement | Target |
|---|---|
| `docker compose up` to ready | under 60 s |
| `POST /events` (100 events) | under 500 ms |
| `GET /events/summary` | under 200 ms |
| Dashboard initial load | under 3 s |
| Collector memory | under 100 MB |
| Collector with 0 sessions | no crash; log and continue |

---

## 10. Security Requirements

- `POST /events` requires a JWT bearer token (`Authorization: Bearer`), obtained by exchanging `COLLECTOR_API_KEY` at `POST /auth/token`.
- Secrets live in environment variables only — never in code or git history.
- The database is not exposed outside the Docker network.
- All SQL uses parameters — no string concatenation.
- Dashboard has no auth (local only); this is a stated, accepted limitation.

---

## 11. Out of Scope

- Real-time event streaming
- Multi-machine aggregation (single workstation only)
- Dashboard authentication
- Cloud deployment (local only, unless attempted as bonus)
- Backfill of sessions from before the collector was installed