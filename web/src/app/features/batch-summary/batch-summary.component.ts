import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { Router } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';
import type { BatchSummary, Ticket } from '@core/api/api.types';
import { ActiveBatchService } from '@core/batches/active-batch.service';
import { PlantTimePipe } from '@core/clock/plant-time.pipe';
import { AppError, toAppError } from '@core/errors/app-error';
import { WeightPipe } from '@core/format/weight.pipe';
import { FunctionKeysService } from '@core/keyboard/function-keys.service';
import { WeighingApiService } from '@core/weighing/weighing-api.service';
import { BannerComponent } from '@shared/ui/banner/banner.component';
import { TicketPreviewComponent } from './ticket-preview.component';

/** How often the summary refreshes while the OrgaTEX write-back is pending. */
export const SUMMARY_REFRESH_MS = 2000;

/** Another press printed this copy first; the screen's print count is out of date. */
const ALREADY_PRINTED = 'Ticket.AlreadyPrinted';

/**
 * Screen 3 · Confirm & print (stories 009, 010): the server's summary, the ticket preview, Print or
 * Reprint (F1) and Next batch (F3). Opened from the review list it is read-only (REVIEW-READ-ONLY):
 * no print, no retry, and the only way on is back to the list.
 */
@Component({
  selector: 'wms-batch-summary',
  imports: [TranslocoPipe, WeightPipe, PlantTimePipe, BannerComponent, TicketPreviewComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './batch-summary.component.html',
  styleUrl: './batch-summary.component.scss',
})
export class BatchSummaryComponent {
  private readonly api = inject(WeighingApiService);
  private readonly router = inject(Router);
  private readonly activeBatch = inject(ActiveBatchService);

  /** Route parameter `:batchId`. */
  readonly batchId = input.required<string>();
  /** Route data: opened from the review list. */
  readonly readOnly = input(false);

  protected readonly summary = signal<BatchSummary | null>(null);
  protected readonly error = signal<AppError | null>(null);
  protected readonly retrying = signal(false);
  protected readonly ticket = signal<Ticket | null>(null);
  protected readonly ticketError = signal<AppError | null>(null);
  protected readonly printing = signal(false);
  protected readonly printError = signal<AppError | null>(null);

  /** The ticket's own state once loaded: it is newer than a summary refresh already on its way. */
  protected readonly ticketState = computed(() => this.ticket() ?? this.summary()?.ticket ?? null);
  protected readonly hasTicket = computed(() => {
    const state = this.summary()?.ticket.state;
    return state !== undefined && state !== 'notAvailable';
  });
  protected readonly canPrint = computed(
    () => !this.readOnly() && this.ticket() !== null && !this.printing(),
  );
  protected readonly isReprint = computed(() => (this.ticket()?.printCount ?? 0) > 0);

  private refresh: ReturnType<typeof setTimeout> | null = null;
  private destroyed = false;

  constructor() {
    const destroyRef = inject(DestroyRef);
    effect(() => {
      const id = this.batchId();
      untracked(() => {
        this.ticket.set(null);
        this.ticketError.set(null);
        void this.load(id);
      });
    });
    destroyRef.onDestroy(() => {
      this.destroyed = true;
      this.stopRefresh();
    });

    const keys = inject(FunctionKeysService);
    keys.register(
      'F1',
      { run: () => void this.print(), enabled: () => this.canPrint() },
      destroyRef,
    );
    keys.register(
      'F3',
      { run: () => void this.nextBatch(), enabled: () => !this.readOnly() },
      destroyRef,
    );
  }

  /**
   * Print (F1): the first print, or a reprint. The server decides whether the printer is up, so the
   * key doesn't depend on a status strip that may be out of date. The press carries the count on
   * screen; if another press got there first nothing prints and the screen catches up. A failure
   * leaves the summary and preview in place and the ticket as it was, so the press can be repeated.
   */
  protected async print(): Promise<void> {
    const ticket = this.ticket();
    if (ticket === null || !this.canPrint()) {
      return;
    }
    this.printing.set(true);
    this.printError.set(null);
    try {
      this.ticket.set(
        await firstValueFrom(this.api.printTicket(this.batchId(), ticket.printCount)),
      );
    } catch (e) {
      const error = toAppError(e);
      this.printError.set(error);
      if (error.code === ALREADY_PRINTED) {
        await this.loadTicket(this.batchId());
      }
    } finally {
      this.printing.set(false);
    }
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
      // The ticket is frozen, so one read is enough; a refresh of the summary doesn't repeat it.
      if (this.hasTicket() && this.ticket() === null && this.ticketError() === null) {
        void this.loadTicket(batchId);
      }
      // While the write-back is pending, follow it until OrgaTEX confirms it or it fails (story 004).
      // A response that arrives after the screen closed must not start a timer nothing would clear.
      if (summary.orgatex.state === 'pending' && !this.destroyed) {
        this.refresh = setTimeout(() => void this.load(batchId), SUMMARY_REFRESH_MS);
      }
    } catch (e) {
      this.error.set(toAppError(e));
    }
  }

  protected async loadTicket(batchId: string): Promise<void> {
    this.ticketError.set(null);
    try {
      this.ticket.set(await firstValueFrom(this.api.ticket(batchId)));
    } catch (e) {
      this.ticketError.set(toAppError(e));
    }
  }

  protected minutes(s: BatchSummary): number {
    return Math.floor((s.elapsedSeconds ?? 0) / 60);
  }

  protected seconds(s: BatchSummary): number {
    return (s.elapsedSeconds ?? 0) % 60;
  }

  /** Next batch (F3): the queue. An unprinted ticket doesn't hold the operator back (PD-02). */
  protected async nextBatch(): Promise<void> {
    this.activeBatch.close();
    await this.router.navigate(['/batches']);
  }

  protected async backToReview(): Promise<void> {
    await this.router.navigate(['/review']);
  }
}
