// The E2E stack: the Compose full profile as its own project, with its own volume and ports, so that
// resetting it can never touch the development database (bolt 014). Usage: node playwright/stack.mjs up|down
import { spawnSync } from 'node:child_process';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');

export const API_PORT = '5280';
export const SIMULATORS_PORT = '5290';

const env = {
  ...process.env,
  API_PORT,
  SIMULATORS_PORT,
  POSTGRES_PORT: '5434',
  POSTGRES_DB: 'weighingms_e2e',
  POSTGRES_USER: 'weighingms_e2e',
  // A throwaway database that exists only for the length of a test run.
  POSTGRES_PASSWORD: 'e2e-only',
};

function compose(...args) {
  const result = spawnSync(
    'docker',
    ['compose', '--project-name', 'weighingms-e2e', '--project-directory', root, '--env-file', '/dev/null',
      '--file', 'docker-compose.yml', '--file', 'playwright/compose.e2e.yml', '--profile', 'full', ...args],
    { cwd: root, env, stdio: 'inherit' },
  );
  if (result.status !== 0) {
    throw new Error(`docker compose ${args.join(' ')} failed with exit code ${String(result.status)}`);
  }
}

/** Removes any earlier run, then builds and starts a clean stack with an empty database. */
export function up() {
  down();
  compose('up', '--build', '--detach', '--wait', '--wait-timeout', '180');
}

/** Stops the stack and deletes its database. */
export function down() {
  compose('down', '--volumes', '--remove-orphans');
}

if (process.argv[1] === fileURLToPath(import.meta.url)) {
  const command = process.argv[2];
  if (command === 'up') {
    up();
  } else if (command === 'down') {
    down();
  } else {
    throw new Error('Usage: node playwright/stack.mjs up|down');
  }
}
