export interface AgentSummary {
  agentName: string
  eventCount: number
  totalCostUsd: number
  totalTokens: number
}

export interface ActivitySummary {
  activityType: string | null
  count: number
}

export interface TimelineBucket {
  hour: string
  count: number
  totalCost: number
}

export interface RecentEvent {
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

export interface SummaryData {
  totalEvents: number
  timeRange: string
  generatedAt: string
  totalCostUsd: number
  totalTokens: number
  averageSessionDurationMinutes: number
  byAgent: AgentSummary[]
  byActivity: ActivitySummary[]
  timeline: TimelineBucket[]
  recentEvents: RecentEvent[]
}
