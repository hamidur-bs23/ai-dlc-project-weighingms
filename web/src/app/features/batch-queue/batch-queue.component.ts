import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';
import type { Batch } from '@core/api/api.types';
import { ActiveBatchService } from '@core/batches/active-batch.service';
import { BatchesApiService } from '@core/batches/batches-api.service';
import { PlantTimePipe } from '@core/clock/plant-time.pipe';
import { AppError, toAppError } from '@core/errors/app-error';
import { ScannerService } from '@core/scanner/scanner.service';
import { BannerComponent } from '@shared/ui/banner/banner.component';

type QueueState =
  | { kind: 'loading' }
  | { kind: 'ready'; batches: readonly Batch[]; receivedAt: Date }
  | { kind: 'error'; error: AppError };

/**
 * Screen 1 · Select batch (story 003, workbook STEP 19). The queue comes only from
 * `GET /api/batches`; a touched row or a scanned batch ID is checked with `GET /api/batches/{id}`
 * before the batch opens. An unknown ID shows "not found" and the screen stays.
 */
@Component({
  selector: 'wms-batch-queue',
  imports: [TranslocoPipe, PlantTimePipe, BannerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './batch-queue.component.html',
  styleUrl: './batch-queue.component.scss',
})
export class BatchQueueComponent {
  private readonly api = inject(BatchesApiService);
  private readonly router = inject(Router);
  private readonly activeBatch = inject(ActiveBatchService);

  protected readonly state = signal<QueueState>({ kind: 'loading' });
  /** The batch being opened (rows are disabled meanwhile). */
  protected readonly opening = signal<string | null>(null);
  /** A failed open: not found (with the ID) or another Api error. */
  protected readonly openError = signal<{ batchId: string; error: AppError } | null>(null);

  constructor() {
    this.activeBatch.close();
    void this.load();

    inject(ScannerService)
      .scans()
      .pipe(takeUntilDestroyed(inject(DestroyRef)))
      .subscribe((code) => void this.open(code));
  }

  protected async load(): Promise<void> {
    this.state.set({ kind: 'loading' });
    try {
      const list = await firstValueFrom(this.api.list());
      this.state.set({ kind: 'ready', batches: list.items, receivedAt: new Date() });
    } catch (e) {
      this.state.set({ kind: 'error', error: toAppError(e) });
    }
  }

  /** Opens a batch by touch or scan: confirm it exists, then go to its Weigh screen. */
  protected async open(batchId: string): Promise<void> {
    if (this.opening() !== null) {
      return;
    }
    this.opening.set(batchId);
    this.openError.set(null);
    try {
      const batch = await firstValueFrom(this.api.get(batchId));
      this.activeBatch.open(batch);
      await this.router.navigate(['/batches', batch.batchId, 'weigh']);
    } catch (e) {
      this.openError.set({ batchId, error: toAppError(e) });
    } finally {
      this.opening.set(null);
    }
  }

  protected isNotFound(error: AppError): boolean {
    return error.code === 'Batch.NotFound';
  }
}
