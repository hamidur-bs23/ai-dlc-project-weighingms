import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';
import type { CompletedBatchPage } from '@core/api/api.types';
import { PlantDateTimePipe } from '@core/clock/plant-time.pipe';
import { AppError, toAppError } from '@core/errors/app-error';
import { WeighingApiService } from '@core/weighing/weighing-api.service';
import { BannerComponent } from '@shared/ui/banner/banner.component';

/**
 * The completed-batch review list (story 012, REVIEW-LIST): newest first, 20 per page. A row opens
 * the batch's summary and ticket read-only (REVIEW-READ-ONLY). It reads WeighingMS's own records, so
 * it stays available while OrgaTEX is down.
 */
@Component({
  selector: 'wms-batch-review',
  imports: [TranslocoPipe, PlantDateTimePipe, BannerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './batch-review.component.html',
  styleUrl: './batch-review.component.scss',
})
export class BatchReviewComponent {
  private readonly api = inject(WeighingApiService);
  private readonly router = inject(Router);

  protected readonly page = signal(1);
  protected readonly list = signal<CompletedBatchPage | null>(null);
  protected readonly error = signal<AppError | null>(null);
  protected readonly loading = signal(false);

  protected readonly pages = computed(() => {
    const list = this.list();
    return list ? Math.max(1, Math.ceil(list.total / list.pageSize)) : 1;
  });

  constructor() {
    void this.load(1);
  }

  protected async load(page: number): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      this.list.set(await firstValueFrom(this.api.completedBatches(page)));
      this.page.set(page);
    } catch (e) {
      this.error.set(toAppError(e));
    } finally {
      this.loading.set(false);
    }
  }

  protected async open(batchId: string): Promise<void> {
    await this.router.navigate(['/review', batchId]);
  }

  protected async back(): Promise<void> {
    await this.router.navigate(['/batches']);
  }
}
