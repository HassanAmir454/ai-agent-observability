import type { AgentEvent, CodeBurnReport, ReportModel, ReportActivity } from './types.js'

// Maps codeburn's title-case activity labels back to the canonical
// lowercase keys used by the API / database (e.g. "Feature Dev" -> "feature").
const LABEL_TO_CATEGORY: Record<string, string> = {
  Coding: 'coding',
  Debugging: 'debugging',
  'Feature Dev': 'feature',
  Refactoring: 'refactoring',
  Testing: 'testing',
  Exploration: 'exploration',
  Planning: 'planning',
  Delegation: 'delegation',
  'Git Ops': 'git',
  'Build/Deploy': 'build/deploy',
  Conversation: 'conversation',
  Brainstorming: 'brainstorming',
  General: 'general',
}

function dominantModel(models: ReportModel[]): string {
  if (models.length === 0) return 'unknown'
  return models.reduce((best, m) => (m.calls > best.calls ? m : best)).name
}

function dominantActivity(activities: ReportActivity[]): string {
  if (activities.length === 0) return 'unknown'
  const top = activities.reduce((best, a) => (a.turns > best.turns ? a : best))
  return LABEL_TO_CATEGORY[top.category] ?? top.category.toLowerCase()
}


export function reportMap(reportJson: CodeBurnReport, provider: string): AgentEvent[] {
  const sessions = reportJson.topSessions
  const sessionCount = sessions.length
  if (sessionCount === 0) return []

  const { models, activities } = reportJson

  const model = dominantModel(models)
  const activityType = dominantActivity(activities)

  const totalInputTokens = models.reduce((sum, m) => sum + m.inputTokens, 0)
  const totalOutputTokens = models.reduce((sum, m) => sum + m.outputTokens, 0)
  const totalCacheReadTokens = models.reduce((sum, m) => sum + m.cacheReadTokens, 0)
  const totalCacheWriteTokens = models.reduce((sum, m) => sum + m.cacheWriteTokens, 0)

  const totalCost = sessions.reduce((sum, s) => sum + s.cost, 0)
  const collectedAt = new Date().toISOString()

  return sessions.map((entry): AgentEvent => {
    // Report exposes tokens only at the aggregate (models[]) level, so spread
    // them across sessions by cost share — even split when total cost is 0.
    const share = totalCost > 0 ? entry.cost / totalCost : 1 / sessionCount

    return {
      agentName: provider,
      sessionId: entry.sessionId,
      model,
      inputTokens: Math.round(totalInputTokens * share),
      outputTokens: Math.round(totalOutputTokens * share),
      cacheReadTokens: Math.round(totalCacheReadTokens * share),
      cacheWriteTokens: Math.round(totalCacheWriteTokens * share),
      reasoningTokens: 0,
      totalCostUsd: entry.cost,
      activityType,
      apiCalls: entry.calls,
      hasAgentSpawn: false,
      sessionDurationMinutes: 0,
      eventTimestamp: collectedAt,
      collectedAt,
    }
  })
}
