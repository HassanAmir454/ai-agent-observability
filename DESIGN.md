# Design Decisions

Companion to SPEC.md. Each decision says what I chose, why, and the trade-off.

---

## How I Used AI on This Assignment

M-Files said the thing that stands out is *how you applied AI and what impact it had*, so I'm honest about it.

I used AI as a partner, not an autopilot:

- **Spec first.** I wrote SPEC.md and DESIGN.md before coding, using an AI model to stress-test the acceptance criteria and find edge cases (empty collection, oversized batch, database down).
- **Grounded in real output.** An early draft assumed a CodeBurn data type. I ran `codeburn export -f json` on my own machine, saw the real shape (a real per-session list plus aggregate arrays), and rewrote the data section to match. This is the difference between AI-assisted and AI-dependent: I verified instead of trusting.
- **Cursor agent for building, me for reviewing.** I used Cursor's agent (Claude models) to generate boilerplate, then reviewed every file for async usage, dependency injection, parameterised SQL, and error handling.
- **Impact:** spec-first + AI scaffolding + review let me build a four-layer stack in the assignment window while keeping the spec, code, and behaviour aligned.

---

## Decision 1 — Extend CodeBurn, don't replace it

**Chose:** add new files in a `collector/` folder; touch no existing CodeBurn file.
**Why:** CodeBurn's parsing is already tested across many tools. Rewriting it would add bugs and waste time. The brief asks to *extend*.
**Trade-off:** I depend on CodeBurn's output shape; if they change it, my mapper breaks. Fine for this scope.

---

## Decision 2 — Use CodeBurn's CLI output, with a standalone fallback

**Chose:** read `codeburn export -f json` from the CLI rather than importing CodeBurn's internal code.
**Why:** the CLI JSON is a stable public contract. This keeps a clean seam, so the brief's "if CodeBurn blocks you, build a standalone collector" fallback becomes a one-file change.
**Trade-off:** calling the CLI is slightly slower than in-process calls — negligible at a 60-minute interval.

---

## Decision 3 — Azure SQL Edge over Cosmos DB / Azurite

**Chose:** Azure SQL Edge (relational).
**Why:** every query is relational and aggregate-heavy — events in the last N hours, grouped by agent, grouped by activity, recent 20. Indexed SQL does all of this directly.
**Alternatives:** Cosmos DB would need careful partition design for time-range queries; Azurite Table Storage has no server-side aggregation. Both rejected.

---

## Decision 4 — Batch ingestion, not one request per event

**Chose:** `POST /events` takes an array.
**Why:** the collector produces all sessions at once. A batch means one connection, one atomic transaction, one response to check.
**Trade-off:** a failed batch loses that run. The unique constraint plus retry makes resends safe.
**More time:** add a small local queue for guaranteed delivery.

---

## Decision 5 — Polling, not WebSockets

**Chose:** the dashboard polls every 60 seconds.
**Why:** data only changes when the collector runs (every 60 min). Polling is simple and shows fresh data within a minute. WebSockets would add a lot of complexity for no real gain here.

---

## Decision 6 — API-key auth on ingest

**Chose:** an `X-Api-Key` header on `POST /events` (the brief lists auth as optional).
**Why:** any open POST that writes to a database is a risk, even locally. ~30 minutes of work, and it shows security awareness.
**Trade-off:** API keys are weaker than Azure AD. For production I'd switch to Azure AD tokens.

---

## Decision 7 — Three Docker services

**Chose:** `database`, `api`, `dashboard`, each built separately with health-gated startup.
**Why:** matches how these would deploy in real life and makes local dev easier.
**Trade-off:** a slightly more complex compose file and a slower first start while the DB health check passes.

---

## Decision 8 — Event = one session

**Chose:** an event is one session row from `export -f json`, enriched with model/activity from the aggregate arrays.
**Why:** a session is the smallest unit with a real id, timestamp, cost, and call count — perfect for a real "recent events" feed and per-agent totals.
**Honest limit:** tokens, model, and activity are aggregate-level in CodeBurn, so per-session values are best-effort or `0`/`null`. I verified this against live output instead of claiming fields the tool doesn't emit.
**Real-data note:** some session ids repeat across projects and a few rows show `unknown` / `transcripts` ids with placeholder timestamps. The database de-duplicates on `(SessionId, EventTimestamp, AgentName)` so these don't double-count.

---

## Decision 9 — CI per layer

**Chose:** GitHub Actions workflows for api, dashboard, and collector, each filtered so only the changed layer runs.
**Why:** generated code is only trustworthy behind an automated build gate, and path filtering keeps runs fast.

---

## Decision 10 — Specification-driven, not test-first (no TDD)

**Chose:** spec first as the contract, then implement against it, then add a focused set of tests afterward to confirm behaviour. I do **not** write failing tests first.
**Why:** in the technical interview the team said they do not practice TDD, even though the job description mentions it. So I followed how they actually work: the spec is the single source of truth and drives the build; tests validate the result, they don't drive it. Traceability (spec → code → check) is kept without the test-first ceremony.
**Trade-off:** without test-first, I rely more on the spec and on reviewing AI-generated code carefully to catch issues early.

---

## What I'd Do With More Time

1. A local queue in the collector for guaranteed delivery on network failure.
2. A `DeviceId` field so several machines can report to one endpoint (team view).
3. Cost alerts when a daily threshold is passed.
4. Azure deployment with Terraform — one command to provision Function + SQL.
5. Anomaly detection for sessions far above a user's average usage.

---

## Known Limitations

1. **Windows first** — collector paths tested on Windows; macOS auto-detected by CodeBurn but not re-checked here.
2. **No backfill** — only sessions from install onward are captured.
3. **Polling lag** — data is up to ~60s old (accepted).
4. **Single machine** — no team aggregation yet.
5. **No dashboard login** — open on localhost; accepted for local use.
6. **Aggregate fields** — some per-session values are best-effort because CodeBurn reports them per model/period (Decision 8).

---

## Technology Choices

| Layer | Technology | Reason |
|---|---|---|
| Collector | TypeScript + Node 22 | Matches CodeBurn's stack |
| Scheduling | `setInterval` + env config | Simple, no extra deps |
| Ingest API | C# Azure Functions v4 (isolated) | Assignment requirement |
| Data access | EF Core or Dapper | Clean .NET, parameterised SQL |
| Database | Azure SQL Edge | Relational, indexed, runs in Docker |
| Dashboard | React 18 + TypeScript + Vite | Type-safe, fast dev server |
| Charts | Chart.js | Lightweight, no lock-in |
| CI | GitHub Actions | Native to GitHub, path-filtered |
| Local env | Docker Compose | One-command startup |