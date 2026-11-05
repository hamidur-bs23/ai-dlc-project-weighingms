import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import { PlantClockService } from '@core/clock/plant-clock.service';
import { DeviceHealthService } from '@core/devices/device-health.service';

/**
 * Status strip (story 001): both scales, the scanner, the printer and OrgaTEX with a dot *and* a
 * READY / OFFLINE label (never colour alone), the language and the plant-time clock.
 * The OrgaTEX unsynced count joins with write-back (bolt 011).
 */
@Component({
  selector: 'wms-status-strip',
  imports: [TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <footer class="strip">
      <ul class="strip__devices" [attr.aria-label]="'statusStrip.label' | transloco">
        @for (d of devices(); track d.device) {
          <li class="stat" [attr.data-state]="d.state" [attr.data-device]="d.device">
            <span [class]="'stat__dot stat__dot--' + d.state" aria-hidden="true"></span>
            @if (d.kind === 'scale') {
              {{ 'statusStrip.scale' | transloco }} {{ d.name ?? d.device }}
            } @else {
              {{ 'statusStrip.' + d.kind | transloco }}
            }
            · {{ 'statusStrip.state.' + d.state | transloco }}
          </li>
        }
      </ul>
      <span class="spacer"></span>
      <span class="stat">{{ 'statusStrip.language' | transloco }}</span>
      @if (time(); as t) {
        <time class="stat" [attr.aria-label]="'statusStrip.clock' | transloco">{{ t }}</time>
      }
    </footer>
  `,
  styles: `
    .strip {
      display: flex;
      flex-wrap: wrap;
      gap: 22px;
      align-items: center;
      min-height: 40px;
      padding: 10px 18px;
      border-top: 1px solid var(--hmi-line);
      background: var(--hmi-panel);
      color: var(--hmi-muted);
      font-family: var(--mono);
      font-size: 11px;
      letter-spacing: 0.04em;
    }

    .strip__devices {
      display: flex;
      flex-wrap: wrap;
      gap: 22px;
      margin: 0;
      padding: 0;
      list-style: none;
    }

    .stat {
      display: flex;
      gap: 7px;
      align-items: center;
      text-transform: uppercase;
    }

    .stat[data-state='offline'] {
      color: var(--over);
    }

    .stat__dot {
      width: 7px;
      height: 7px;
      border-radius: 50%;
      background: var(--hmi-dim);
    }

    .stat__dot--ready {
      background: var(--ok);
    }

    .stat__dot--offline {
      border-radius: 1px; // a square, not a dot: offline differs by shape too
      background: var(--over);
    }

    .spacer {
      flex: 1 1 auto;
    }
  `,
})
export class StatusStripComponent {
  protected readonly devices = inject(DeviceHealthService).devices;
  protected readonly time = inject(PlantClockService).time;
}
