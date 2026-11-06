import { LiveAnnouncer } from '@angular/cdk/a11y';
import { Dialog } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
  linkedSignal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';
import type { WeightClass } from '@core/api/api.types';
import { ActiveBatchService } from '@core/batches/active-batch.service';
import { BatchesApiService } from '@core/batches/batches-api.service';
import { WeightPipe } from '@core/format/weight.pipe';
import { FunctionKeysService } from '@core/keyboard/function-keys.service';
import { ScannerService } from '@core/scanner/scanner.service';
import { BannerComponent } from '@shared/ui/banner/banner.component';
import { OverrideDialogComponent } from './override-dialog.component';
import { RecipeRailComponent } from './recipe-rail.component';
import { WeighBarComponent, WeighZone } from './weigh-bar.component';
import { WeighingSessionStore } from './weighing-session.store';

/** The share of the target from which an underweight shows the amber "near target" hint (OQ-4, display only). */
export const NEAR_TARGET_SHARE = 0.9;

/**
 * Screen 2 · Weigh (stories 004–008). Renders the server's session and live reading and sends the operator's
 * commands; it never judges a weight itself. F1 confirm, F2 tare / re-tare, F4 quit.
 */
@Component({
  selector: 'wms-weigh',
  imports: [TranslocoPipe, WeightPipe, BannerComponent, RecipeRailComponent, WeighBarComponent],
  providers: [WeighingSessionStore],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './weigh.component.html',
  styleUrl: './weigh.component.scss',
})
export class WeighComponent {
  protected readonly store = inject(WeighingSessionStore);
  private readonly router = inject(Router);
  private readonly dialog = inject(Dialog);
  private readonly activeBatch = inject(ActiveBatchService);
  private readonly batches = inject(BatchesApiService);
  private readonly announcer = inject(LiveAnnouncer);
  private readonly transloco = inject(TranslocoService);

  /** Route parameter `:batchId`. */
  readonly batchId = input.required<string>();

  protected readonly active = this.store.active;
  protected readonly reading = this.store.activeReading;
  protected readonly attempt = computed(() => this.active()?.attempt ?? null);
  private readonly activeSequence = computed(() => this.active()?.sequence ?? null);

  /**
   * The lot and container as being typed (saved on Enter or leaving the field). They reset synchronously when the
   * active component or its saved value changes, and otherwise keep what the operator types, so a server response
   * can never wipe a value that is being entered.
   */
  protected readonly lotDraft = linkedSignal({
    source: () => `${String(this.activeSequence())}|${this.attempt()?.lot ?? ''}`,
    computation: () => this.attempt()?.lot ?? '',
  });
  protected readonly containerDraft = linkedSignal({
    source: () => `${String(this.activeSequence())}|${this.attempt()?.container ?? ''}`,
    computation: () => this.attempt()?.container ?? this.active()?.suggestedContainer ?? '',
  });
  /** Quit was pressed once and asks for confirmation (something was captured); reset per component. */
  protected readonly quitArmed = linkedSignal({
    source: this.activeSequence,
    computation: () => false,
  });
  protected readonly scaleName = computed(() => {
    const scale = this.active()?.scale;
    return scale ? (this.store.scaleNames()[scale.scaleId] ?? scale.scaleId) : '';
  });

  protected readonly weightClass = computed(
    () => (this.reading()?.class ?? null) as WeightClass | null,
  );

  /** Near target: underweight but at least 90% of the target (amber hint only; not a class). */
  protected readonly nearTarget = computed(() => {
    const r = this.reading();
    return (
      r?.class === 'underweight' &&
      r.net !== null &&
      r.band !== null &&
      r.net.value >= r.band.target.value * NEAR_TARGET_SHARE
    );
  });

  protected readonly zone = computed<WeighZone>(() => {
    switch (this.weightClass()) {
      case 'underweight':
        return this.nearTarget() ? 'near' : 'under';
      case 'withinRange':
      case 'targetAchieved':
        return 'ok';
      case 'overweight':
        return 'over';
      default:
        return 'none';
    }
  });

  /** The plain-language reasons Confirm is disabled (from the server's `missing`), tolerance shown by the status. */
  protected readonly missingHints = computed(() =>
    (this.reading()?.confirmation.missing ?? []).filter((m) => m !== 'inTolerance'),
  );

  protected readonly hasCaptured = computed(() => {
    const a = this.attempt();
    return a !== null && (a.lot !== null || a.container !== null || a.tare !== null);
  });

  constructor() {
    const destroyRef = inject(DestroyRef);

    effect(() => {
      const id = this.batchId();
      untracked(() => void this.openBatch(id));
    });

    // The last record completes the batch (AUTO-COMPLETE) → the interim summary (INTERIM-SUMMARY).
    effect(() => {
      if (this.store.completed()) {
        untracked(() => void this.router.navigate(['/batches', this.batchId(), 'summary']));
      }
    });

    // Announce class changes politely, not every reading (ux-guide).
    let lastClass: string | null = null;
    effect(() => {
      const c = this.weightClass();
      if (c !== null && c !== lastClass) {
        lastClass = c;
        untracked(
          () =>
            void this.announcer.announce(this.transloco.translate(`weigh.class.${c}`), 'polite'),
        );
      }
    });

    const keys = inject(FunctionKeysService);
    keys.register(
      'F1',
      { run: () => void this.confirm(), enabled: () => this.store.canConfirm() },
      destroyRef,
    );
    keys.register(
      'F2',
      {
        run: () => void this.store.tare(),
        enabled: () => this.active() !== null && !this.store.isBusy(),
      },
      destroyRef,
    );
    keys.register(
      'F4',
      {
        run: () => void this.quit(),
        enabled: () => this.active() !== null && !this.store.isBusy(),
      },
      destroyRef,
    );

    // A scanned lot fills the lot (PD-03); a scan never takes over a field the operator is typing in.
    inject(ScannerService)
      .scans()
      .pipe(takeUntilDestroyed(destroyRef))
      .subscribe((code) => {
        const typing = document.activeElement instanceof HTMLInputElement;
        if (this.active() !== null && !typing) {
          void this.store.captureLot(code);
        }
      });
  }

  private async openBatch(batchId: string): Promise<void> {
    if (this.activeBatch.batch()?.batchId !== batchId) {
      try {
        this.activeBatch.open(await firstValueFrom(this.batches.get(batchId)));
      } catch {
        // The session call below reports the problem (for example Batch.NotFound).
      }
    }
    await this.store.open(batchId);
  }

  protected async saveLot(): Promise<void> {
    const lot = this.lotDraft().trim();
    if (lot.length > 0 && lot !== this.attempt()?.lot) {
      await this.store.captureLot(lot);
    }
  }

  protected async saveContainer(): Promise<void> {
    const container = this.containerDraft().trim();
    if (container.length > 0 && container !== this.attempt()?.container) {
      await this.store.captureContainer(container);
    }
  }

  protected async confirm(): Promise<void> {
    if (this.store.canConfirm()) {
      await this.store.confirm();
    }
  }

  /** Quit (F4): asks first when lot, container or tare were captured (ux-guide: destructive actions). */
  protected async quit(): Promise<void> {
    if (this.hasCaptured() && !this.quitArmed()) {
      this.quitArmed.set(true);
      return;
    }
    this.quitArmed.set(false);
    await this.store.quit();
  }

  protected async backToQueue(): Promise<void> {
    await this.store.leave();
    this.activeBatch.close();
    await this.router.navigate(['/batches']);
  }

  protected openOverride(): void {
    if (this.store.overrideOffered()) {
      this.dialog.open(OverrideDialogComponent, {
        data: { store: this.store },
        ariaModal: true,
        autoFocus: 'first-tabbable',
      });
    }
  }

  protected inputValue(event: Event): string {
    return event.target instanceof HTMLInputElement ? event.target.value : '';
  }
}
