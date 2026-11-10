import { Injectable, signal } from '@angular/core';
import type { Batch } from '@core/api/api.types';

/** What the top-bar crumb shows. The batch and the summary responses both carry it. */
export type BatchCrumb = Pick<Batch, 'batchId' | 'orderId' | 'machine'>;

/** The batch open at the station, shown in the top-bar crumb (BATCH · ORDER · MACHINE). */
@Injectable({ providedIn: 'root' })
export class ActiveBatchService {
  private readonly current = signal<BatchCrumb | null>(null);

  readonly batch = this.current.asReadonly();

  open(batch: BatchCrumb): void {
    this.current.set(batch);
  }

  close(): void {
    this.current.set(null);
  }
}
