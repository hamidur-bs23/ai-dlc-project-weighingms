import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { Router } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';
import type { BatchSummary } from '@core/api/api.types';
import { ActiveBatchService } from '@core/batches/active-batch.service';
import { PlantTimePipe } from '@core/clock/plant-time.pipe';
import { AppError, toAppError } from '@core/errors/app-error';
import { WeightPipe } from '@core/format/weight.pipe';
import { WeighingApiService } from '@core/weighing/weighing-api.service';
import { BannerComponent } from '@shared/ui/banner/banner.component';

/** How often the summary refreshes while the OrgaTEX write-back is pending. */
export const SUMMARY_REFRESH_MS = 2000;

/**
 * The interim, read-only batch summary (INTERIM-SUMMARY) shown after the last component: the server's summary (FR-18).
 * Bolt 013 extends this into the Confirm &amp; print screen.
 */
@Component({
  selector: 'wms-batch-summary',
  imports: [TranslocoPipe, WeightPipe, PlantTimePipe, BannerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="summary-page">
      @if (error(); as e) {
        <wms-banner [error]="e" [retryable]="true" (retry)="load(batchId())" />
      } @else if (summary(); as s) {
        <h1 class="summary-page__title">
          {{ 'summary.title' | transloco: { batchId: s.batchId } }}
        </h1>
        <dl class="summary">
          <div class="summary__row">
            <dt>{{ 'summary.weighed' | transloco }}</dt>
            <dd data-testid="summary-weighed">
              {{ 'summary.nOfN' | transloco: { done: s.done, total: s.total } }}
            </dd>
          </div>
          <div class="summary__row">
            <dt>{{ 'summary.withinTolerance' | transloco }}</dt>
            <dd [class.good]="s.allWithinTolerance" [class.bad]="!s.allWithinTolerance">
              @if (s.allWithinTolerance) {
                {{ 'summary.yes' | transloco }}
              } @else {
                {{ 'summary.noOverrides' | transloco: { count: s.overrideCount } }}
              }
            </dd>
          </div>
          <div class="summary__row">
            <dt>{{ 'summary.total' | transloco }}</dt>
            <dd class="mono">{{ s.totalDispensed | weight }}</dd>
          </div>
          <div class="summary__row">
            <dt>{{ 'summary.elapsed' | transloco }}</dt>
            <dd>
              {{ 'summary.duration' | transloco: { minutes: minutes(s), seconds: seconds(s) } }}
            </dd>
          </div>
          <div class="summary__row">
            <dt>{{ 'summary.operator' | transloco }}</dt>
            <dd>{{ s.operators.join(', ') }}</dd>
          </div>
          <div class="summary__row">
            <dt>{{ 'summary.completedAt' | transloco }}</dt>
            <dd class="mono">{{ s.completedAt | plantTime: true }}</dd>
          </div>
          <div class="summary__row">
            <dt>{{ 'summary.orgatex' | transloco }}</dt>
            <dd
              data-testid="orgatex-state"
              [class.good]="s.orgatex.state === 'sent'"
              [class.bad]="s.orgatex.state === 'syncFailed'"
            >
              @switch (s.orgatex.state) {
                @case ('sent') {
                  {{
                    'summary.state.sent'
                      | transloco: { time: (s.orgatex.writtenAt | plantTime: true) }
                  }}
                }
                @case ('pending') {
                  {{ 'summary.state.pending' | transloco }}
                }
                @case ('syncFailed') {
                  {{ 'summary.state.syncFailed' | transloco }}
                  <button
                    type="button"
                    class="key key--small summary__retry"
                    [disabled]="retrying()"
                    (click)="retry()"
                  >
                    {{ 'summary.retrySync' | transloco }}
                  </button>
                }
                @default {
                  {{ 'summary.state.notAvailable' | transloco }}
                }
              }
            </dd>
          </div>
          <div class="summary__row">
            <dt>{{ 'summary.ticket' | transloco }}</dt>
            <dd data-testid="ticket-state">
              {{
                'summary.ticketState.' + s.ticket.state | transloco: { count: s.ticket.printCount }
              }}
            </dd>
          </div>
        </dl>

        <table class="lines">
          <caption class="visually-hidden">
            {{
              'summary.lines' | transloco
            }}
          </caption>
          <thead>
            <tr>
              <th scope="col">{{ 'summary.columns.chemical' | transloco }}</th>
              <th scope="col" class="num">{{ 'summary.columns.required' | transloco }}</th>
              <th scope="col" class="num">{{ 'summary.columns.actual' | transloco }}</th>
              <th scope="col">{{ 'summary.columns.status' | transloco }}</th>
            </tr>
          </thead>
          <tbody>
            @for (l of s.lines; track l.sequence) {
              <tr>
                <td>{{ l.chemicalName }}</td>
                <td class="mono num">{{ l.required | weight }}</td>
                <td class="mono num">{{ l.actual | weight }}</td>
                <td>
                  {{ 'summary.status.' + l.status | transloco }}
                  @if (l.override) {
                    <span class="ovr">{{ 'weigh.rail.ovr' | transloco }}</span>
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>

        <div class="actions">
          <button type="button" class="key" (click)="back()">
            {{ 'summary.back' | transloco }}
          </button>
        </div>
      } @else {
        <p role="status">{{ 'summary.loading' | transloco }}</p>
      }
    </section>
  `,
  styles: `
    .summary-page {
      display: flex;
      flex-direction: column;
      gap: var(--space-4);
      max-width: 760px;
      padding: var(--space-6);
    }

    .summary-page__title {
      margin: 0;
      font-size: 24px;
      font-weight: 650;
    }

    .summary {
      display: flex;
      flex-direction: column;
      margin: 0;
      border: 1px solid var(--hmi-line);
      border-radius: 3px;
    }

    .summary__row {
      display: flex;
      justify-content: space-between;
      padding: 10px var(--space-4);
      border-bottom: 1px solid var(--hmi-line);

      dt {
        color: var(--hmi-muted);
      }

      dd {
        margin: 0;
        font-weight: 600;
      }
    }

    .good {
      color: var(--ok);
    }

    .bad {
      color: var(--warn);
    }

    .lines {
      width: 100%;
      border-collapse: collapse;
      font-size: 13.5px;

      th {
        padding: 0 var(--space-3) var(--space-2) 0;
        border-bottom: 1px solid var(--hmi-line);
        color: var(--hmi-dim);
        font-size: 10px;
        letter-spacing: 0.13em;
        text-align: left;
        text-transform: uppercase;
      }

      td {
        padding: var(--space-2) var(--space-3) var(--space-2) 0;
        border-bottom: 1px solid var(--hmi-line);
      }

      .num {
        text-align: right;
      }
    }

    .summary__retry {
      margin-left: var(--space-3);
    }

    .ovr {
      margin-left: var(--space-1);
      padding: 0 var(--space-1);
      border: 1px solid var(--over);
      border-radius: 2px;
      color: var(--over);
      font-family: var(--mono);
      font-size: 10px;
    }
  `,
})
export class BatchSummaryComponent {
  private readonly api = inject(WeighingApiService);
  private readonly router = inject(Router);
  private readonly activeBatch = inject(ActiveBatchService);

  /** Route parameter `:batchId`. */
  readonly batchId = input.required<string>();

  protected readonly summary = signal<BatchSummary | null>(null);
  protected readonly error = signal<AppError | null>(null);
  protected readonly retrying = signal(false);
  private refresh: ReturnType<typeof setTimeout> | null = null;
  private destroyed = false;

  constructor() {
    effect(() => {
      const id = this.batchId();
      untracked(() => void this.load(id));
    });
    inject(DestroyRef).onDestroy(() => {
      this.destroyed = true;
      this.stopRefresh();
    });
  }

  /** Manual retry of a sync-failed write-back (WRITEBACK-RETRY-ANYONE), then follow it until sent or failed. */
  protected async retry(): Promise<void> {
    this.retrying.set(true);
    try {
      await firstValueFrom(this.api.retryWriteBack(this.batchId()));
      await this.load(this.batchId());
    } catch (e) {
      this.error.set(toAppError(e));
    } finally {
      this.retrying.set(false);
    }
  }

  private stopRefresh(): void {
    if (this.refresh !== null) {
      clearTimeout(this.refresh);
      this.refresh = null;
    }
  }

  protected async load(batchId: string): Promise<void> {
    this.error.set(null);
    this.stopRefresh();
    try {
      const summary = await firstValueFrom(this.api.summary(batchId));
      this.summary.set(summary);
      // While the write-back is pending, follow it until OrgaTEX confirms it or it fails (story 004).
      // A response that arrives after the screen closed must not start a timer nothing would clear.
      if (summary.orgatex.state === 'pending' && !this.destroyed) {
        this.refresh = setTimeout(() => void this.load(batchId), SUMMARY_REFRESH_MS);
      }
    } catch (e) {
      this.error.set(toAppError(e));
    }
  }

  protected minutes(s: BatchSummary): number {
    return Math.floor((s.elapsedSeconds ?? 0) / 60);
  }

  protected seconds(s: BatchSummary): number {
    return (s.elapsedSeconds ?? 0) % 60;
  }

  protected async back(): Promise<void> {
    this.activeBatch.close();
    await this.router.navigate(['/batches']);
  }
}
