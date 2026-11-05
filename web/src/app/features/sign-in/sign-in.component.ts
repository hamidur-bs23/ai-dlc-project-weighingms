import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslocoPipe } from '@jsverse/transloco';
import { SessionService } from '@core/auth/session.service';
import { AppError, toAppError } from '@core/errors/app-error';
import { FunctionKeysService } from '@core/keyboard/function-keys.service';
import { BannerComponent } from '@shared/ui/banner/banner.component';

/** Badge sign-in (story 002, ADR-009): a known badge opens the batch queue. OK is also F1. */
@Component({
  selector: 'wms-sign-in',
  imports: [ReactiveFormsModule, TranslocoPipe, BannerComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="sign-in">
      <h1 class="sign-in__title">{{ 'signIn.title' | transloco }}</h1>
      <p class="sign-in__hint">{{ 'signIn.hint' | transloco }}</p>

      <form class="sign-in__form" [formGroup]="form" (ngSubmit)="submit()" novalidate>
        <label class="field">
          <span class="field__label">{{ 'signIn.badge' | transloco }}</span>
          <input
            class="field__input mono"
            formControlName="badgeId"
            autocomplete="off"
            autocapitalize="characters"
            spellcheck="false"
            maxlength="32"
            [attr.aria-invalid]="showRequired()"
            [attr.aria-describedby]="showRequired() ? 'badge-required' : null"
          />
        </label>
        @if (showRequired()) {
          <p class="field__error" id="badge-required">{{ 'signIn.badgeRequired' | transloco }}</p>
        }

        <label class="field">
          <span class="field__label">{{ 'signIn.shift' | transloco }}</span>
          <input class="field__input" formControlName="shift" autocomplete="off" maxlength="16" />
        </label>

        @if (error(); as e) {
          <wms-banner [error]="e" />
        }

        <button type="submit" class="key key--ok" [disabled]="busy()">
          {{ (busy() ? 'signIn.busy' : 'signIn.submit') | transloco }}
          <span class="key__fn">F1</span>
        </button>
      </form>
    </section>
  `,
  styles: `
    .sign-in {
      display: flex;
      flex-direction: column;
      gap: var(--space-2);
      max-width: 460px;
      margin: 0 auto;
      padding: 48px var(--space-6);
    }

    .sign-in__title {
      margin: 0;
      font-size: 22px;
      font-weight: 650;
    }

    .sign-in__hint {
      margin: 0 0 var(--space-4);
      color: var(--hmi-muted);
    }

    .sign-in__form {
      display: flex;
      flex-direction: column;
      gap: var(--space-4);
    }

    .field {
      display: flex;
      flex-direction: column;
      gap: var(--space-1);
    }

    .field__label {
      color: var(--hmi-muted);
      font-size: 13px;
    }

    .field__input {
      min-height: 52px;
      padding: 0 var(--space-3);
      border: 1px solid var(--hmi-line);
      border-radius: 3px;
      background: var(--hmi-panel);
      color: var(--hmi-ink);
      font-size: 20px;

      &:focus-visible {
        outline: 2px solid var(--steel);
        outline-offset: 2px;
      }

      &[aria-invalid='true'] {
        border-color: var(--warn);
      }
    }

    .field__error {
      margin: calc(-1 * var(--space-2)) 0 0;
      color: var(--warn);
      font-size: 13px;
    }
  `,
})
export class SignInComponent {
  private readonly sessions = inject(SessionService);
  private readonly router = inject(Router);

  protected readonly form = new FormGroup({
    badgeId: new FormControl('', { nonNullable: true }), // checked on submit (trimmed)
    shift: new FormControl('', { nonNullable: true }),
  });
  protected readonly busy = signal(false);
  protected readonly error = signal<AppError | null>(null);
  protected readonly showRequired = signal(false);

  constructor() {
    inject(FunctionKeysService).register(
      'F1',
      { run: () => void this.submit(), enabled: () => !this.busy() },
      inject(DestroyRef),
    );
  }

  protected async submit(): Promise<void> {
    if (this.busy()) {
      return;
    }
    const { badgeId, shift } = this.form.getRawValue();
    const badge = badgeId.trim();
    this.showRequired.set(badge.length === 0);
    if (badge.length === 0) {
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    try {
      await this.sessions.signIn({ badgeId: badge, shift: shift.trim() || null });
      await this.router.navigate(['/batches']);
    } catch (e) {
      this.error.set(toAppError(e));
    } finally {
      this.busy.set(false);
    }
  }
}
