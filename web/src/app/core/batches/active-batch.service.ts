import { Injectable, signal } from '@angular/core';
import type { Batch } from '@core/api/api.types';

/** The batch open at the station, shown in the top-bar crumb (BATCH · ORDER · MACHINE). */
@Injectable({ providedIn: 'root' })
export class ActiveBatchService {
  private readonly current = signal<Batch | null>(null);

  readonly batch = this.current.asReadonly();

  open(batch: Batch): void {
    this.current.set(batch);
  }

  close(): void {
    this.current.set(null);
  }
}
