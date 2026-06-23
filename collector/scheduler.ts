import { exec } from 'node:child_process'
import { promisify } from 'node:util'
import type { CodeBurnReport } from './types.js'
import type { CollectorConfig } from './config.js'
import { reportMap } from './mapper.js'
import { sendEvents } from './eventSender.js'

const execAsync = promisify(exec)

interface Provider {
  name: string
  cliProvider: string
  command: string
}

// Single extensible list — adding "Claude Code" later is one line.
const PROVIDERS: Provider[] = [
  { name: 'Cursor', cliProvider: 'cursor', command: 'codeburn report --provider cursor --format json' },
]

export async function collectOnce(config: CollectorConfig): Promise<void> {
  for (const provider of PROVIDERS) {
    try {
      const { stdout } = await execAsync(provider.command)
      const report = JSON.parse(stdout) as CodeBurnReport
      const events = reportMap(report, provider.name)

      if (events.length === 0) {
        console.log(`[${provider.name}] nothing collected`)
        continue
      }

      await sendEvents(events, config)
    } catch (err) {
      console.error(`[${provider.name}] collection failed:`, err)
    }
  }
}

export function startScheduler(config: CollectorConfig): void {
  const intervalMs = config.intervalMinutes * 60 * 1000

  const runSafe = (): void => {
    void collectOnce(config)
  }

  console.log(
    `Collector starting — running immediately, then every ${config.intervalMinutes} minute(s).`,
  )
  runSafe()
  setInterval(runSafe, intervalMs)
}
