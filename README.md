# AI Agent Observability

A local-first observability stack for AI coding-agent usage. It collects telemetry from AI coding tools (Cursor, Claude Code, GitHub Copilot) via [CodeBurn](https://github.com/getagentseal/codeburn), stores it in a relational database, and visualizes cost, tokens, and activity on a live dashboard. Built as a fork that **extends** CodeBurn rather than replacing it — no existing CodeBurn files are modified.

## Architecture

```
Developer machine
│
├── AI tools (run normally) → write session files to disk
│
└── Collector (CodeBurn extension)
      Runs on a schedule (default 60 min)
      Reads data from CodeBurn, maps to AgentEvent
      POSTs a batch to the API (Bearer JWT)
            │
            ▼
      Azure Function (C# .NET 8 isolated)
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

Four layers:

- **Collector** — host-side TypeScript process that wraps the CodeBurn CLI, maps its output to events, and posts them to the API.
- **Ingest API** — Azure Functions (C# .NET 8 isolated worker) that validates, authenticates, and stores event batches and serves aggregates.
- **Storage** — Azure SQL Edge running in Docker, with an indexed `AgentEvents` table.
- **Dashboard** — React + TypeScript (Vite), polling the summary endpoint every 60 seconds.

## Prerequisites

- **Docker Desktop**
- **Node.js 22+**
- **.NET 8 SDK + ASP.NET Core 8 Runtime** — only needed to run the API outside Docker; the Docker image covers it otherwise.
- **Azure Functions Core Tools v4** — only needed to run the API locally outside Docker.
- **CodeBurn installed globally** (`npm install -g codeburn`) on the host, since the collector wraps it.

## Quick Start

**a. Clone the repo**

```bash
git clone <repo-url>
cd ai-agent-observability
```

**b. Start the stack**

```bash
docker compose -f infrastructure/docker-compose.yml up -d --build
```

This starts three services:

- `database` — Azure SQL Edge, seeded with sample data
- `api` — the ingest API on port **7071**
- `dashboard` — the web dashboard on port **3000**

**c. Open the dashboard**

Visit [http://localhost:3000](http://localhost:3000). It renders immediately from the seeded sample data.

**d. Run the collector on the host** (not in Docker) to ingest your real usage:

In a terminal at the repo root:

```bash
npm install
```

Set the required environment variables:

```bash
# PowerShell
$env:INGEST_ENDPOINT   = "http://localhost:7071"
$env:COLLECTOR_API_KEY = "local-dev-key-change-me"

# bash/zsh
export INGEST_ENDPOINT=http://localhost:7071
export COLLECTOR_API_KEY=local-dev-key-change-me
```

Then run on a schedule (every 60 minutes):

```bash
npm run collect
```

Or for a one-time manual collection:

```bash
npm run collect:once
```

## Cloud Deployment

This project is also deployed to Azure as a bonus, demonstrating the same architecture running in the cloud:

- **Database**: Azure SQL Database (serverless, free tier) at `ai-obs-sql-v2.database.windows.net`
- **API**: Azure Functions (Consumption plan, free tier) at [https://ai-obs-api-2026v2.azurewebsites.net](https://ai-obs-api-2026v2.azurewebsites.net)
- **Dashboard**: Azure Static Web Apps (free tier) at [https://ashy-stone-03ea7b210.7.azurestaticapps.net](https://ashy-stone-03ea7b210.7.azurestaticapps.net)

The dashboard's API base URL is configurable via the `VITE_API_BASE_URL` environment variable, allowing the same codebase to run locally (relative paths, via the Vite/nginx proxy) or against the deployed Azure Function (absolute URL), with no code changes between environments. For local Docker builds, `VITE_API_BASE_URL` is left unset so the dashboard defaults to relative paths. For Azure deployment, it is set explicitly at build time (e.g. `$env:VITE_API_BASE_URL="https://ai-obs-api-2026v2.azurewebsites.net"` before running `npm run build`) rather than via a committed `.env.production` file, since Vite always loads `.env.production` during any production build regardless of target — keeping it unset by default avoids leaking the Azure URL into local Docker builds., allowing the same codebase to run locally (relative paths, via the Vite/nginx proxy) or against the deployed Azure Function (absolute URL), with no code changes between environments.

The collector can target either environment by setting `INGEST_ENDPOINT` to either `http://localhost:7071` (local Docker) or the live Azure Function URL above.

## Why the collector runs on the host, not in Docker

CodeBurn reads local AI-tool session files (`~/.cursor/`, `~/.claude/`, etc.) on the developer's machine. A container has its own isolated filesystem and cannot see these directories without fragile, OS-specific host-path volume mounts. The collector is therefore intentionally run on the host and points at the dockerized API. This is a documented design decision, not a limitation of the pipeline.

## Authentication

The ingest endpoint is protected by a self-issued JWT. The collector first POSTs its API key to `/api/auth/token` to obtain a short-lived (24-hour) bearer token, caches it, and then sends events with an `Authorization: Bearer <token>` header. A single shared secret (`CollectorApiKey`) serves as the credential to obtain tokens; the signing key (`JwtSigningKey`) must be replaced with a real secret in production.

## API Endpoints

- **`POST /api/auth/token`** — exchange an API key for a short-lived JWT bearer token.
- **`POST /api/events`** — ingest a validated batch of events in one transaction (requires bearer token).
- **`GET /api/events/summary?timeRange=1h|6h|24h|7d`** — aggregated totals, per-agent and per-activity breakdowns, an hourly timeline, and recent events.
- **`GET /api/health`** — reports API and database connectivity.

## Database

Storage uses **Azure SQL Edge** running in Docker. It is a real SQL Server engine (relational, indexed, free for local development) that fits the workload, which is entirely aggregate-heavy: events in the last N hours, grouped by agent, grouped by activity, plus a recent feed. Indexed SQL handles all of these directly, and the `AgentEvents` table de-duplicates on `(SessionId, EventTimestamp, AgentName)` so re-sent batches never double-count.

## Known Limitations

- **Aggregate-derived metrics** — tokens, model, and activity type are reported by CodeBurn per model/period, not per session, so per-session values are best-effort estimates distributed across sessions by cost share.
- **Event timestamps use collection time** — CodeBurn's session date has no time-of-day component, so `eventTimestamp` is set to the moment the collector ran. This keeps the dashboard's recency-based ranges (1h/6h) meaningful.
- **Collector runs host-side** — it cannot run in Docker because CodeBurn needs direct access to local AI-tool session directories.
- **Single machine** — no multi-machine/team aggregation yet.
- **No dashboard login** — open on localhost; accepted for local-only use.
- **Polling lag** — dashboard data can be up to ~60 seconds old.

## Documentation

- [SPEC.md](SPEC.md) — full specification and acceptance criteria.
- [DESIGN.md](DESIGN.md) — design decisions, trade-offs, and rationale.
