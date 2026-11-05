import { DestroyRef, Injectable, signal } from '@angular/core';

export type FunctionKey = 'F1' | 'F2' | 'F3' | 'F4';

/** An action a screen offers on a function key; the key's label is shown on the screen's button. */
export interface FunctionKeyAction {
  run: () => void;
  /** Whether the action can run now (a disabled button's key does nothing). */
  enabled?: () => boolean;
}

/**
 * F1–F4 trigger the labelled action of the current screen (FR-22). Screens register their actions
 * for as long as they are shown; the shell forwards key presses here.
 */
@Injectable({ providedIn: 'root' })
export class FunctionKeysService {
  private readonly actions = signal<ReadonlyMap<FunctionKey, FunctionKeyAction>>(new Map());

  readonly registered = this.actions.asReadonly();

  /** Registers an action until the screen is destroyed. */
  register(key: FunctionKey, action: FunctionKeyAction, destroyRef: DestroyRef): void {
    this.actions.update((current) => new Map(current).set(key, action));
    destroyRef.onDestroy(() => {
      this.actions.update((current) => {
        if (current.get(key) !== action) {
          return current;
        }
        const next = new Map(current);
        next.delete(key);
        return next;
      });
    });
  }

  /** Handles a key press; returns true when a registered, enabled action ran. */
  handle(event: KeyboardEvent): boolean {
    if (!isFunctionKey(event.key) || event.altKey || event.ctrlKey || event.metaKey) {
      return false;
    }
    const action = this.actions().get(event.key);
    if (!action) {
      return false;
    }
    event.preventDefault(); // F-keys never reach the browser (F1 help, F3 find …) when a screen uses them
    if (action.enabled && !action.enabled()) {
      return false;
    }
    action.run();
    return true;
  }
}

function isFunctionKey(key: string): key is FunctionKey {
  return key === 'F1' || key === 'F2' || key === 'F3' || key === 'F4';
}
