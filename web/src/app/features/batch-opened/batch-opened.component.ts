import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { Router } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';
import type { Batch, Recipe } from '@core/api/api.types';
import { ActiveBatchService } from '@core/batches/active-batch.service';
import { BatchesApiService } from '@core/batches/batches-api.service';
import { PlantTimePipe } from '@core/clock/plant-time.pipe';
import { AppError, toAppError } from '@core/errors/app-error';
import { WeightPipe } from '@core/format/weight.pipe';
import { BannerComponent } from '@shared/ui/banner/banner.component';

type OpenedState =
  | { kind: 'loading' }
  | { kind: 'ready'; batch: Batch; recipe: Recipe }
  | { kind: 'error'; error: AppError };

/**
 * The opened batch (decision D1, bolt 006): the batch header and its recipe snapshot in weighing
 * sequence, from `GET /api/batches/{id}/recipe`. The Weigh screen (bolt 010) replaces this view on
 * the same route. Opened from the queue, the header is already known; on a reload it is fetched.
 */
@Component({
  selector: 'wms-batch-opened',
  imports: [TranslocoPipe, PlantTimePipe, WeightPipe, BannerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './batch-opened.component.html',
  styleUrl: './batch-opened.component.scss',
})
export class BatchOpenedComponent {
  private readonly api = inject(BatchesApiService);
  private readonly activeBatch = inject(ActiveBatchService);
  private readonly router = inject(Router);

  /** Route parameter `:batchId`. */
  readonly batchId = input.required<string>();

  protected readonly state = signal<OpenedState>({ kind: 'loading' });

  constructor() {
    effect(() => {
      const id = this.batchId();
      untracked(() => void this.load(id));
    });
  }

  protected async load(batchId: string = this.batchId()): Promise<void> {
    this.state.set({ kind: 'loading' });
    try {
      const known = this.activeBatch.batch();
      const batch =
        known?.batchId === batchId ? known : await firstValueFrom(this.api.get(batchId));
      this.activeBatch.open(batch);
      const recipe = await firstValueFrom(this.api.recipe(batchId));
      this.state.set({ kind: 'ready', batch, recipe });
    } catch (e) {
      this.state.set({ kind: 'error', error: toAppError(e) });
    }
  }

  protected async backToQueue(): Promise<void> {
    this.activeBatch.close();
    await this.router.navigate(['/batches']);
  }
}
