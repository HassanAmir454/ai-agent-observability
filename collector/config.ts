export interface CollectorConfig {
  ingestEndpoint: string
  apiKey: string
  intervalMinutes: number
}

function requireEnv(name: string): string {
  const value = process.env[name]
  if (!value) {
    console.error(`Missing required environment variable: ${name}`)
    process.exit(1)
  }
  return value
}

export function loadConfig(): CollectorConfig {
  const ingestEndpoint = requireEnv('INGEST_ENDPOINT')
  const apiKey = requireEnv('COLLECTOR_API_KEY')

  const rawInterval = process.env['COLLECTION_INTERVAL_MINUTES']
  const intervalMinutes = rawInterval ? parseInt(rawInterval, 10) : 60

  if (isNaN(intervalMinutes) || intervalMinutes <= 0) {
    console.error(
      `COLLECTION_INTERVAL_MINUTES must be a positive integer, got: ${rawInterval}`,
    )
    process.exit(1)
  }

  return { ingestEndpoint, apiKey, intervalMinutes }
}
