import { loadConfig } from './config.js'
import { startScheduler, collectOnce } from './scheduler.js'

const isOnce = process.argv.includes('--once')
const config = loadConfig()

if (isOnce) {
  collectOnce(config)
    .then(() => process.exit(0))
    .catch((err) => {
      console.error('Collection failed:', err)
      process.exit(1)
    })
} else {
  startScheduler(config)
}
