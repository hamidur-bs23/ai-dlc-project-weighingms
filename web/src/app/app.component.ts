import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SessionService } from '@core/auth/session.service';
import { PlantClockService } from '@core/clock/plant-clock.service';
import { DeviceHealthService } from '@core/devices/device-health.service';
import { FunctionKeysService } from '@core/keyboard/function-keys.service';
import { StatusStripComponent } from '@features/shell/status-strip.component';
import { TopBarComponent } from '@features/shell/top-bar.component';

/** The station shell: top bar, the current screen, and the status strip (story 001). */
@Component({
  selector: 'wms-root',
  imports: [RouterOutlet, TopBarComponent, StatusStripComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '(document:keydown)': 'onKeydown($event)' },
  template: `
    <div class="screen">
      <wms-top-bar />
      <main class="screen__body">
        <router-outlet />
      </main>
      <wms-status-strip />
    </div>
  `,
  styles: `
    :host {
      display: block;
      min-height: 100vh;
      padding: var(--space-3);
      background: var(--hmi-bezel);
    }

    .screen {
      display: grid;
      grid-template-rows: auto 1fr auto;
      max-width: 1180px;
      min-height: calc(100vh - 2 * var(--space-3));
      margin: 0 auto;
      overflow: hidden;
      border-radius: 3px;
      background: var(--hmi-bg);
    }

    .screen__body {
      min-width: 0;
    }
  `,
})
export class App {
  private readonly keys = inject(FunctionKeysService);

  constructor() {
    const session = inject(SessionService);
    const health = inject(DeviceHealthService);
    const clock = inject(PlantClockService);

    // Device health and the plant clock need a session (the Api protects them).
    effect(() => {
      if (session.signedIn()) {
        health.start();
        void clock.start();
      } else {
        health.stop();
        clock.stop();
      }
    });
  }

  onKeydown(event: KeyboardEvent): void {
    this.keys.handle(event);
  }
}
