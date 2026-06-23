export interface AgentEvent {
  agentName: string
  sessionId: string
  model: string | null
  inputTokens: number
  outputTokens: number
  cacheReadTokens: number
  cacheWriteTokens: number
  reasoningTokens: number
  totalCostUsd: number
  activityType: string | null
  apiCalls: number
  hasAgentSpawn: boolean
  sessionDurationMinutes: number
  eventTimestamp: string
  collectedAt: string
}

// Shape of `codeburn report --format json` output (clean JSON on stdout).
// Field names mirror the real CLI output documented in SPEC §3.

export interface ReportOverviewTokens {
  input: number
  output: number
  cacheRead: number
  cacheWrite: number
}

export interface ReportOverview {
  cost: number
  savings: number
  calls: number
  sessions: number
  cacheHitPercent: number
  tokens: ReportOverviewTokens
}

export interface ReportDaily {
  date: string
  cost: number
  calls: number
  turns: number
}

export interface ReportProject {
  name: string
  path: string
  cost: number
  calls: number
  sessions: number
}

export interface ReportModel {
  name: string
  calls: number
  inputTokens: number
  outputTokens: number
  cacheReadTokens: number
  cacheWriteTokens: number
  cost: number
}

export interface ReportActivity {
  category: string
  cost: number
  turns: number
}

export interface ReportTool {
  name: string
  calls: number
}

export interface ReportSession {
  project: string
  sessionId: string
  date: string | null
  cost: number
  calls: number
}

export interface CodeBurnReport {
  overview: ReportOverview
  daily: ReportDaily[]
  projects: ReportProject[]
  models: ReportModel[]
  activities: ReportActivity[]
  tools: ReportTool[]
  topSessions: ReportSession[]
}
