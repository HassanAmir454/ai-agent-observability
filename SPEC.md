# AI Agent Observability — Specification

**Status:** Written before any code
**Version:** 1.1
**Last updated:** 2026-06-23
**Author:** Hassan Amir
**Assignment:** M-Files — AI Native Engineer (Agentic Development)

---

## 1. What This System Does

Developers use AI coding tools (Claude Code, Cursor, GitHub Copilot) every day, but have little visibility into how they are used — how many tokens, which agent, how much cost, what kind of work.

This system makes that visible. A **collector** reads AI usage data on a schedule, an **API** validates and stores it, and a **dashboard** shows it live. Everything runs locally with one `docker compose up`.

---

## 2. Architecture

```
Developer machine
│
├── AI tools (run normally) → write session files to disk
│
└── Collector (CodeBurn extension)
      Runs on a schedule (default 60 min)
      Reads data from CodeBurn, maps to AgentEvent
      POSTs a batch to the API (with X-Api-Key)
            │
            ▼
      Azure Function (C# .NET v4 isolated)
        POST /events          → validate + store batch
        GET  /events/summary  → aggregated data for dashboard
        GET  /health          → database check
            │
            ▼
      Azure SQL Edge (Docker)  →  AgentEvents table, indexed
            │
            ▼
      React + TypeScript dashboard  →  polls /events/summary
```

---

## 3. Data Source (based on real CodeBurn output)

This was written after running the tool, not from guesses. The collector uses CodeBurn's existing JSON output:

```
codeburn export -f json     # primary source (has a real sessions[] list)
codeburn report --format json
```

`export -f json` gives a real per-session list. Each session has: `Project`, `Session ID`, `Started At` (real timestamp), `Cost`, `API Calls`, `Turns`. It also gives aggregated arrays: `models[]` (with tokens), `activity[]`, `daily[]`, `projects[]`, `tools[]`.

**What is real per session vs. aggregate (honest note):**
- Per session: id, start time, cost, API calls, turns, project.
- Aggregate only: tokens, model, and activity type are reported per *model/period*, not per session.
- So each event is a session row, enriched (best effort) with the model and activity breakdown from the aggregate arrays. The mapper fills `0`/`null` for any field CodeBurn does not expose at session level.

**Event = one session** (real id, timestamp, cost, calls). This keeps the data honest and gives the dashboard a real "recent events" feed. Trade-off is in DESIGN.md (Decision 8).

---

## 4. Collector

**Purpose:** extend CodeBurn to collect on a schedule and send to the API. No existing CodeBurn file is changed.

### Config (environment variables)

| Variable | Required | Default | Description |
|---|---|---|---|
| `INGEST_ENDPOINT` | Yes | — | API base URL |
| `COLLECTOR_API_KEY` | Yes | — | Sent as `X-Api-Key` |
| `COLLECTION_INTERVAL_MINUTES` | No | 60 | How often to collect |

### Acceptance criteria

**C1 — Scheduled collection works**
```
Given the collector runs with interval = 60
When 60 minutes pass
Then sessions are collected, mapped, and POSTed as one batch
And a 201 response is logged with the event count
```

**C2 — Interval is configurable**
```
Given COLLECTION_INTERVAL_MINUTES = 2
When the collector starts
Then it collects every 2 minutes (default 60 if unset)
```

**C3 — Collects once on startup**
```
Given the collector starts
Then one collection runs immediately, then the schedule begins
```

**C4 — API down does not crash the collector**
```
Given the API is not running
When a POST fails
Then the error is logged, the process keeps running, and it retries next interval
```

**C5 — Missing config fails fast**
```
Given INGEST_ENDPOINT is not set
When the collector starts
Then it exits immediately and names the missing variable
```

**C6 — Empty collection handled**
```
Given no AI sessions exist
When collection runs
Then no empty batch is sent, and a "nothing collected" line is logged
```

**C7 — Manual trigger (assignment requires this)**
```
Given a developer does not want to wait for the schedule
When they run the documented command (e.g. `npm run collect:once`)
Then one collection runs and POSTs immediately
```

---

## 5. Ingest API (Azure Function, C# .NET v4 isolated)

### `POST /events`

**Body**
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
      "eventTimestamp": "2026-06-19T18:08:56Z",
      "collectedAt": "2026-06-23T10:39:33Z"
    }
  ],
  "collectorVersion": "1.0.0"
}
```

**C8 — Valid batch accepted**
```
Given a valid batch of 5 events with the correct API key
When POST /events is called
Then all 5 are saved in one transaction
And 201 is returned with { "stored": 5 }
```

**C9 — Auth required**
```
Given no X-Api-Key header
When POST /events is called
Then 401 is returned, nothing is written, the attempt is logged
```

**C10 — Empty batch rejected** → `400 "Batch cannot be empty"`

**C11 — Oversized batch rejected** → `400 "Batch cannot exceed 1000 events"`

**C12 — Bad event rejected (all-or-nothing)**
```
Given one event missing agentName
Then 400 names the missing field and nothing is saved
```

**C13 — Malformed JSON rejected safely** → `400`, no internal details leaked

**C14 — Database failure handled safely**
```
Given the database is down
Then 503 is returned, details logged internally only,
  caller sees "Service temporarily unavailable"
```

### `GET /events/summary`

**Query:** `timeRange` = `1h` | `6h` | `24h` | `7d` (default `24h`).

**Response**
```json
{
  "totalEvents": 142,
  "timeRange": "24h",
  "generatedAt": "2026-06-23T10:00:00Z",
  "totalCostUsd": 2.8431,
  "totalTokens": 1842930,
  "averageSessionDurationMinutes": 34,
  "byAgent": [ { "agentName": "Cursor", "eventCount": 89, "totalCostUsd": 1.92, "totalTokens": 1204830 } ],
  "byActivity": [ { "activityType": "coding", "count": 67 } ],
  "timeline": [ { "hour": "2026-06-23T09:00:00Z", "count": 12, "totalCost": 0.23 } ],
  "recentEvents": []
}
```

**C15 — Valid range returns aggregates**
```
Given 142 events in the last 24h
Then totalEvents = 142, byAgent covers all agents,
  timeline has hourly buckets, recentEvents has the last 20
```

**C16 — Invalid range rejected** → `400` listing valid options

**C17 — Empty database returns zeros (not 404)** → `200` with zero counts and empty arrays

### `GET /health`

**C18 — Healthy** → `200 { status: "healthy", database: "connected" }`
**C19 — Unhealthy** → `503 { status: "unhealthy", database: "unreachable" }`

---

## 6. Database (Azure SQL Edge)

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

CREATE INDEX IX_AgentEvents_EventTimestamp      ON AgentEvents(EventTimestamp DESC);
CREATE INDEX IX_AgentEvents_AgentName_Timestamp ON AgentEvents(AgentName, EventTimestamp DESC);
CREATE INDEX IX_AgentEvents_ActivityType        ON AgentEvents(ActivityType, EventTimestamp DESC);
```

The unique constraint makes resent batches safe: a retry rejects duplicate sessions instead of double-counting them.

**C20 — Time-range query is fast** → uses the timestamp index, returns quickly even with thousands of rows.

**C21 — Schema sets up on first run** → `init.sql` creates the table + indexes, `seed.sql` adds sample data.

---

## 7. Dashboard (React + TypeScript)

**C22 — Loads with data** → total card, by-agent chart, timeline chart, and recent-events feed all render; header shows last-updated time.

**C23 — Time-range buttons** → clicking `7d` updates every component and calls the API with `timeRange=7d`, no page reload.

**C24 — API down** → an error banner shows, last known data stays visible, nothing crashes.

**C25 — Empty state** → a "No data yet" message with a hint to run a manual collection.

**C26 — Auto refresh** → polls `/events/summary` every 60s and updates the last-updated time.

---

## 8. Traceability (spec → code → check)

Each acceptance criterion is traceable to the part that implements it and how it is checked. Tests are written **after** implementation to confirm the spec — not test-first. (The team confirmed in interview they do not use TDD; see DESIGN.md Decision 10.)

| Criteria | Component | How it's checked |
|---|---|---|
| C1, C3, C6, C7 | Collector | Run with a 2-min interval and watch logs |
| C2, C5 | Collector | Start with/without env vars |
| C4 | Collector | Stop the API, confirm it keeps running |
| C8–C14 | API | Call endpoints via REST client / Swagger |
| C15–C17 | API | Query summary with valid/invalid ranges |
| C18, C19 | API | Hit /health with DB up and down |
| C20, C21 | Database | `docker compose up` smoke check |
| C22–C26 | Dashboard | Open in browser, toggle ranges, stop API |

A small set of automated tests covers the highest-value paths (batch validation, auth, summary aggregation).

---

## 9. Non-Functional Targets

| Target | Goal |
|---|---|
| `docker compose up` to ready | under 60 s |
| `POST /events` (100 events) | under 500 ms |
| `GET /events/summary` | under 200 ms |
| Dashboard first load | under 3 s |
| Collector with 0 sessions | no crash; log and continue |

---

## 10. Security

- `POST /events` needs the `X-Api-Key` header.
- Secrets only in environment variables — never in code or git.
- Database not exposed outside the Docker network.
- All SQL uses parameters — no string building.
- Dashboard has no login (local only) — accepted limitation.

---

## 11. Out of Scope

- Real-time streaming
- Multi-machine aggregation
- Dashboard login
- Cloud deployment (local only, unless done as bonus)
- Backfill of sessions from before install