import { down } from './stack.mjs';

/** Leaves nothing running, unless the stack was being reused or E2E_KEEP_STACK=1 asks to inspect it. */
export default function globalTeardown(): void {
  if (process.env['E2E_REUSE_STACK'] !== '1' && process.env['E2E_KEEP_STACK'] !== '1') {
    down();
  }
}
