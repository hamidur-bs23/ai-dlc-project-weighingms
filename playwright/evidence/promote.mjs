// Copies the finished captures from staging into evidence/, refusing to overwrite anything there.
import { createHash } from 'node:crypto';
import { copyFileSync, existsSync, readFileSync, statSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '../..');
const staging = resolve(root, 'playwright/test-results-evidence/staging');
const files = [
  'STEP-15-plan.png', 'STEP-20-weigh-screen.png', 'STEP-21-scale-switch.png', 'STEP-22-tolerance-ui.png',
  'STEP-23-five-component-demo.mp4', 'STEP-24-overshoot-ui.png', 'STEP-25-confirmation.png',
  'STEP-26-request.png', 'STEP-26-response.png', 'STEP-26-data.png', 'STEP-27-ticket-test.png', 'STEP-35-final-demo.mp4',
];
const missing = files.filter((f) => !existsSync(resolve(staging, f)));
const clash = files.filter((f) => existsSync(resolve(root, 'evidence', f)));
if (missing.length || clash.length) {
  console.error({ missing, alreadyInEvidence: clash });
  process.exit(1);
}
for (const f of files) {
  const to = resolve(root, 'evidence', f);
  copyFileSync(resolve(staging, f), to);
  const sha = createHash('sha256').update(readFileSync(to)).digest('hex').slice(0, 16);
  console.log(`${f.padEnd(36)} ${String(statSync(to).size).padStart(9)} B  sha256:${sha}`);
}
