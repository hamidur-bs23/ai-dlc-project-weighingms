import { DestroyRef } from '@angular/core';
import { FunctionKeysService } from './function-keys.service';

class FakeDestroyRef implements DestroyRef {
  private readonly callbacks: (() => void)[] = [];
  readonly destroyed = false;
  onDestroy(callback: () => void): () => void {
    this.callbacks.push(callback);
    return () => undefined;
  }
  destroy(): void {
    this.callbacks.forEach((c) => {
      c();
    });
  }
}

const press = (key: string, init: KeyboardEventInit = {}) =>
  new KeyboardEvent('keydown', { key, cancelable: true, ...init });

describe('FunctionKeysService', () => {
  let keys: FunctionKeysService;

  beforeEach(() => {
    keys = new FunctionKeysService();
  });

  it('should run the action the current screen registered for the key', () => {
    const run = vi.fn();
    keys.register('F1', { run }, new FakeDestroyRef());

    const event = press('F1');
    expect(keys.handle(event)).toBe(true);
    expect(run).toHaveBeenCalledOnce();
    expect(event.defaultPrevented).toBe(true);
  });

  it('should ignore keys no screen uses, and let the browser have them', () => {
    const event = press('F2');
    expect(keys.handle(event)).toBe(false);
    expect(event.defaultPrevented).toBe(false);
  });

  it('should not run a disabled action', () => {
    const run = vi.fn();
    keys.register('F1', { run, enabled: () => false }, new FakeDestroyRef());
    expect(keys.handle(press('F1'))).toBe(false);
    expect(run).not.toHaveBeenCalled();
  });

  it('should ignore modified keys and other keys', () => {
    const run = vi.fn();
    keys.register('F1', { run }, new FakeDestroyRef());
    expect(keys.handle(press('F1', { ctrlKey: true }))).toBe(false);
    expect(keys.handle(press('Enter'))).toBe(false);
    expect(run).not.toHaveBeenCalled();
  });

  it('should drop the action when its screen is destroyed', () => {
    const screen = new FakeDestroyRef();
    keys.register('F4', { run: vi.fn() }, screen);
    screen.destroy();
    expect(keys.registered().has('F4')).toBe(false);
  });

  it('should keep the newer screen action when an older screen is destroyed', () => {
    const older = new FakeDestroyRef();
    const newer = { run: vi.fn() };
    keys.register('F1', { run: vi.fn() }, older);
    keys.register('F1', newer, new FakeDestroyRef());
    older.destroy();
    keys.handle(press('F1'));
    expect(newer.run).toHaveBeenCalledOnce();
  });
});
