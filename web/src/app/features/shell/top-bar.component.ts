import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { SessionService } from '@core/auth/session.service';
import { ActiveBatchService } from '@core/batches/active-batch.service';
import { PlantClockService } from '@core/clock/plant-clock.service';

/**
 * Top bar (story 001): WeighingMS · station, the open batch's BATCH · ORDER · MACHINE crumb, and the
 * operator chip. Touching the chip offers Sign out (decision D2, bolt 006).
 */
@Component({
  selector: 'wms-top-bar',
  imports: [TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { '(document:keydown.escape)': 'menuOpen.set(false)' },
  template: `
    <header class="topbar">
      <span class="brand">
        <span class="brand__name">{{ 'app.name' | transloco }}</span>
        @if (stationId(); as station) {
          <span class="brand__station">{{ station }}</span>
        }
      </span>

      @if (batch(); as b) {
        <span class="crumb" data-testid="batch-crumb">
          {{ 'topBar.batch' | transloco }} <b>{{ b.batchId }}</b> · {{ 'topBar.order' | transloco }}
          <b>{{ b.orderId }}</b> · {{ 'topBar.machine' | transloco }} <b>{{ b.machine }}</b>
        </span>
      }

      <span class="spacer"></span>

      @if (session(); as s) {
        <span class="operator">
          <button
            type="button"
            class="chip"
            [attr.aria-expanded]="menuOpen()"
            aria-controls="operator-menu"
            [attr.aria-label]="'topBar.operatorMenu' | transloco: { name: s.displayName }"
            (click)="menuOpen.set(!menuOpen())"
          >
            <span class="chip__dot" aria-hidden="true"></span>
            {{ 'topBar.operator' | transloco }}&nbsp;<b>{{ s.displayName }}</b>
            @if (s.shift) {
              · {{ 'topBar.shift' | transloco: { shift: s.shift } }}
            }
          </button>
          @if (menuOpen()) {
            <span class="menu" id="operator-menu">
              <button type="button" class="key key--small" (click)="signOut()">
                {{ 'topBar.signOut' | transloco }}
              </button>
            </span>
          }
        </span>
      } @else {
        <span class="chip chip--idle">{{ 'topBar.signedOut' | transloco }}</span>
      }
    </header>
  `,
  styles: `
    .topbar {
      display: flex;
      flex-wrap: wrap;
      gap: var(--space-4);
      align-items: center;
      padding: 11px 18px;
      border-bottom: 1px solid var(--hmi-line);
      background: var(--hmi-panel);
    }

    .brand {
      display: flex;
      gap: 9px;
      align-items: baseline;
    }

    .brand__name {
      font-size: 15px;
      font-weight: 700;
      letter-spacing: 0.01em;
    }

    .brand__station {
      color: var(--hmi-dim);
      font-family: var(--mono);
      font-size: 10.5px;
      letter-spacing: 0.1em;
      text-transform: uppercase;
    }

    .crumb {
      color: var(--hmi-muted);
      font-family: var(--mono);
      font-size: 12px;

      b {
        color: var(--hmi-ink);
        font-weight: 600;
      }
    }

    .spacer {
      flex: 1 1 auto;
    }

    .operator {
      position: relative;
    }

    .chip {
      display: inline-flex;
      gap: var(--space-2);
      align-items: center;
      min-height: 44px;
      padding: var(--space-1) 10px;
      border: 1px solid var(--hmi-line);
      border-radius: 3px;
      background: transparent;
      color: var(--hmi-muted);
      font-size: 12.5px;
      cursor: pointer;

      b {
        color: var(--hmi-ink);
      }

      &:focus-visible {
        outline: 2px solid var(--steel);
        outline-offset: 2px;
      }
    }

    .chip--idle {
      cursor: default;
    }

    .chip__dot {
      width: 7px;
      height: 7px;
      border-radius: 50%;
      background: var(--ok);
    }

    .menu {
      position: absolute;
      z-index: 10;
      top: calc(100% + var(--space-1));
      right: 0;
      padding: var(--space-2);
      border: 1px solid var(--hmi-line);
      border-radius: 3px;
      background: var(--hmi-panel-2);
    }
  `,
})
export class TopBarComponent {
  private readonly sessions = inject(SessionService);
  private readonly router = inject(Router);
  private readonly activeBatch = inject(ActiveBatchService);
  private readonly clock = inject(PlantClockService);

  protected readonly session = this.sessions.session;
  protected readonly batch = this.activeBatch.batch;
  protected readonly menuOpen = signal(false);
  protected readonly stationId = computed(
    () => this.session()?.stationId ?? this.clock.stationId(),
  );

  protected async signOut(): Promise<void> {
    this.menuOpen.set(false);
    try {
      await this.sessions.signOut();
    } catch {
      // The session is dropped locally either way; the Api cookie expires on its own.
    }
    this.activeBatch.close();
    await this.router.navigate(['/sign-in']);
  }
}
