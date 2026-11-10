import { up } from './stack.mjs';

/** A clean stack for each run. E2E_REUSE_STACK=1 keeps one that is already running, for a faster local loop. */
export default function globalSetup(): void {
  if (process.env['E2E_REUSE_STACK'] !== '1') {
    up();
  }
}
