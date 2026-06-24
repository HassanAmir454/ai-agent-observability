# Design Decisions

Why the system is built this way. SPEC.md says *what* it does; this says *why*.

---

## How I Used AI

I use AI to speed up the mechanical work and keep the judgement — design, verification, trade-offs — with me.

- **Spec before code.** I wrote SPEC.md and DESIGN.md first, using a model to find edge cases (empty collection, oversized batch, database down).
- **Check against real output.** My first data model assumed a wrong CodeBurn shape. I ran `codeburn report --format json`, saw the real shape, and rewrote the data section. I verify; I don't trust blindly.
- **Generate, then review.** Cursor's agent scaffolded the repetitive code; I reviewed every file for async usage, dependency injection, parameterised SQL, and error handling. The bugs I found came from that review.

---

## Decisions

**1 — Extend CodeBurn, don't replace it.** New code lives in `collector/`; no CodeBurn file is touched. Its parsing already works across many tools, so rewriting it would only add bugs. *Trade-off:* I depend on its output shape.

**2 — Read CodeBurn's CLI output.** I read `codeburn report --format json` instead of importing internal code. The CLI JSON is a stable public contract, so the brief's standalone-collector fallback is a one-file change. *Trade-off:* slightly slower than in-process — negligible at a 60-min interval.

**3 — Azure SQL Edge, not a document store.** Every query is relational and aggregate-heavy (events in the last N hours, grouped by agent/activity, recent 20). Indexed SQL does this directly. It's a real SQL Server engine, free, runs in Docker, and matches the Azure SQL I deploy to. *Rejected:* Cosmos (needs partition design), Azurite (no server-side aggregation).

**4 — Batch ingestion.** `POST /events` takes an array, since the collector produces all sessions at once — one round-trip, one transaction. The unique constraint makes resends safe, and the repository inserts events one by one so a duplicate never drops new events (this came from a real bug — see end).

**5 — Polling, not WebSockets.** The dashboard polls every 60s. Data only changes when the collector runs (every 60 min), so polling keeps the view fresh enough. WebSockets would add complexity for no gain.

**6 — JWT bearer-token auth on ingest.** The collector swaps a shared API key for a short-lived token, then sends batches with `Authorization: Bearer`. Any open endpoint that writes to a database is a risk. *Trade-off:* self-issued JWT is simpler but weaker than a managed provider; in production I'd use Entra ID. Full contract below.

**7 — Three Docker services.** `database`, `api`, `dashboard`, with the API and dashboard waiting on the database health check. Matches real deployment; one `docker compose up` starts everything in order.

**8 — One event = one session.** A session row from `codeburn export -f json` (real id, timestamp, cost, calls), enriched with model/activity from the aggregate arrays — the smallest unit with a real id. *Honest limit:* tokens/model/activity are aggregate-level, so per-session values are best-effort; I send `0`/`null` rather than invent data. The unique constraint on `(SessionId, EventTimestamp, AgentName)` stops duplicates double-counting.

**9 — CI per layer.** One GitHub Actions workflow, three path-filtered jobs (api, dashboard, collector), each running only when its files change. An automated build gate is what makes generated code trustworthy.

---

## Authentication

A self-issued JWT, in two steps.

**1. Get a token** — the collector sends its API key:
```
POST /api/auth/token   { "apiKey": "<COLLECTOR_API_KEY>" }
→ 200 { "token": "<JWT>", "expiresIn": 86400 }
```
A wrong/missing key returns `401`.

**2. Send events** with the token:
```
POST /api/events
Authorization: Bearer <JWT>
{ "events": [ ... ], "collectorVersion": "1.0.0" }
```

The token is an HS256 JWT with claims `iss=observability-api`, `aud=collector`, `exp=issue+24h`, `nbf=issue`. The API checks issuer, audience, expiry, and signature on every request, before any database write; failures return `401` and are logged. The collector caches the token and, on a `401`, fetches a fresh one and retries once.

Settings: `CollectorApiKey` (shared secret), `JwtSigningKey` (signing key), `INGEST_ENDPOINT` (API base URL). In production, the signing key would live in a secret store and the token would be replaced by Entra ID.

---

## API Endpoints

| Method | Route | Auth | Purpose |
|---|---|---|---|
| `POST` | `/api/auth/token` | API key in body | Swap the API key for a bearer token |
| `POST` | `/api/events` | Bearer token | Store a batch of up to 1000 events in one transaction |
| `GET` | `/api/events/summary?timeRange=1h\|6h\|24h\|7d` | None | Totals, per-agent/activity breakdowns, hourly timeline, recent events |
| `GET` | `/api/health` | None | API status and database connectivity |

Full contracts and acceptance criteria are in SPEC.md (Section 5).

---

## Known Limitations

- **Collector runs on the host, not in Docker** — CodeBurn reads local files (`~/.claude/`, `~/.cursor/`) a container can't see without fragile mounts. A deliberate choice; only this part runs outside Docker.
- **Aggregate-derived fields** — some values are per model/period, not per session (Decision 8).
- **Polling latency** — data up to ~60s old; fine since the source changes hourly.
- **Single machine** — no team aggregation yet.
- **No dashboard login** — the read-only dashboard is open; the ingest path that writes is protected.
- **No backfill** — only sessions from install onward.

**With more time:** a local delivery queue, a `DeviceId` field for team view, cost alerts, Terraform for the Azure deploy, and anomaly detection.

---

## Technology

Collector: TypeScript + Node 22 · API: C# Azure Functions v4 (.NET 8 isolated) · Data: EF Core · DB: Azure SQL Edge · Dashboard: React + TypeScript + Vite · Charts: Recharts · CI: GitHub Actions · Local: Docker Compose · Cloud (bonus): Azure SQL + Functions + Static Web Apps.

---

## Bugs Found During Review

- **Batch insert dropped new events** — the first repository saved the whole batch at once, so one duplicate rolled back the save and silently dropped new events. Fixed by inserting one by one and skipping only true duplicates.
- **Timestamps collapsed to midnight** — the mapper took `eventTimestamp` from CodeBurn's date (no time of day), so every event landed at midnight and the 1h/6h views were empty. Fixed by using the collection time.

Both were caught by reading the code and testing against the spec, not by the generator.