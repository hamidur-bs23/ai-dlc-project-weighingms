import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslocoPipe } from '@jsverse/transloco';
import { firstValueFrom } from 'rxjs';
import { AppError, toAppError } from '@core/errors/app-error';
import { WeighingApiService } from '@core/weighing/weighing-api.service';
import { BannerComponent } from '@shared/ui/banner/banner.component';
import type { WeighingSessionStoreType } from './weighing-session.store';

/** The longest reason the server accepts. */
export const OVERRIDE_REASON_MAX = 500;

export interface OverrideDialogData {
  store: WeighingSessionStoreType;
}

/**
 * Supervisor override (story 008, OVR-GRANT-FLOW): another supervisor's badge and PIN give a single-use grant
 * (`POST /api/session/step-up`), then the override records the true class with OVR. Nothing is recorded on any error.
 */
@Component({
  selector: 'wms-override-dialog',
  imports: [ReactiveFormsModule, TranslocoPipe, BannerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <form class="dialog" [formGroup]="form" (ngSubmit)="submit()" novalidate>
      <h2 class="dialog__title" id="override-title">{{ 'override.title' | transloco }}</h2>
      <p class="dialog__hint">{{ 'override.hint' | transloco }}</p>

      <label class="field">
        <span class="field__label">{{ 'override.badge' | transloco }}</span>
        <input
          class="field__input mono"
          formControlName="badgeId"
          autocomplete="off"
          maxlength="32"
        />
      </label>
      <label class="field">
        <span class="field__label">{{ 'override.pin' | transloco }}</span>
        <input
          class="field__input mono"
          type="password"
          inputmode="numeric"
          formControlName="pin"
          autocomplete="off"
          maxlength="12"
        />
      </label>
      <label class="field">
        <span class="field__label">{{ 'override.reason' | transloco }}</span>
        <textarea
          class="field__input field__area"
          formControlName="reason"
          rows="3"
          [attr.maxlength]="reasonMax"
        ></textarea>
      </label>

      @if (missing()) {
        <p class="field__error" role="alert">{{ 'override.required' | transloco }}</p>
      }
      @if (error(); as e) {
        <wms-banner [error]="e" />
      }

      <div class="dialog__actions">
        <button type="submit" class="key key--ok" [disabled]="busy()">
          {{ 'override.submit' | transloco }}
        </button>
        <button type="button" class="key key--ghost" [disabled]="busy()" (click)="ref.close()">
          {{ 'override.cancel' | transloco }}
        </button>
      </div>
    </form>
  `,
  styles: `
    .dialog {
      display: flex;
      flex-direction: column;
      gap: var(--space-3);
      width: min(480px, 92vw);
      padding: var(--space-6);
      border: 1px solid var(--hmi-line);
      border-radius: 3px;
      background: var(--hmi-panel);
      color: var(--hmi-ink);
    }

    .dialog__title {
      margin: 0;
      font-size: 19px;
    }

    .dialog__hint {
      margin: 0;
      color: var(--hmi-muted);
      font-size: 13px;
    }

    .field {
      display: flex;
      flex-direction: column;
      gap: var(--space-1);
    }

    .field__label {
      color: var(--hmi-muted);
      font-size: 12px;
    }

    .field__input {
      min-height: 44px;
      padding: var(--space-2) var(--space-3);
      border: 1px solid var(--hmi-line);
      border-radius: 3px;
      background: var(--hmi-bg);
      color: var(--hmi-ink);
      font-size: 16px;

      &:focus-visible {
        outline: 2px solid var(--steel);
        outline-offset: 2px;
      }
    }

    .field__area {
      resize: vertical;
    }

    .field__error {
      margin: 0;
      color: var(--warn);
      font-size: 13px;
    }

    .dialog__actions {
      display: flex;
      gap: var(--space-3);
    }
  `,
})
export class OverrideDialogComponent {
  protected readonly ref = inject<DialogRef>(DialogRef);
  private readonly data = inject<OverrideDialogData>(DIALOG_DATA);
  private readonly api = inject(WeighingApiService);

  protected readonly reasonMax = OVERRIDE_REASON_MAX;
  protected readonly form = new FormGroup({
    badgeId: new FormControl('', { nonNullable: true }),
    pin: new FormControl('', { nonNullable: true }),
    reason: new FormControl('', { nonNullable: true }),
  });
  protected readonly busy = signal(false);
  protected readonly missing = signal(false);
  protected readonly error = signal<AppError | null>(null);

  protected async submit(): Promise<void> {
    const { badgeId, pin, reason } = this.form.getRawValue();
    const ready = badgeId.trim().length > 0 && pin.length > 0 && reason.trim().length > 0;
    this.missing.set(!ready);
    if (!ready || this.busy()) {
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    try {
      const grant = await firstValueFrom(this.api.stepUp(badgeId.trim(), pin));
      const failed = await this.data.store.override(grant.grantId, reason.trim());
      if (failed) {
        this.error.set(failed);
        return;
      }
      this.ref.close(true);
    } catch (e) {
      this.error.set(toAppError(e));
      this.form.controls.pin.setValue('');
    } finally {
      this.busy.set(false);
    }
  }
}
