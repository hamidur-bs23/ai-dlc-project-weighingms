import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslocoPipe, TranslocoService } from '@jsverse/transloco';
import { AppError } from '@core/errors/app-error';

/**
 * Error or info banner (ux-guide `wms-banner`): an operator-readable, translated message and the
 * "Ref" (first 8 characters of the trace ID) for support. Raw technical text is never shown.
 * Pass `error` to show the message for its code, or `messageKey` for a specific message.
 */
@Component({
  selector: 'wms-banner',
  imports: [TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="banner"
      [class.banner--info]="kind() === 'info'"
      [attr.role]="kind() === 'error' ? 'alert' : 'status'"
    >
      <span class="banner__mark" aria-hidden="true">{{ kind() === 'error' ? '!' : 'i' }}</span>
      <span class="banner__text">
        {{ key() | transloco: params() }}
        @if (error()?.ref; as ref) {
          <span class="banner__ref">{{ 'banner.ref' | transloco: { ref } }}</span>
        }
      </span>
      @if (retryable()) {
        <button type="button" class="key key--small" (click)="retry.emit()">
          {{ 'banner.retry' | transloco }}
        </button>
      }
    </div>
  `,
  styles: `
    .banner {
      display: flex;
      gap: var(--space-3);
      align-items: center;
      padding: var(--space-3) var(--space-4);
      border: 1px solid var(--over);
      border-left-width: 4px;
      border-radius: 3px;
      background: var(--hmi-panel);
    }

    .banner--info {
      border-color: var(--steel);
    }

    .banner__mark {
      display: inline-grid;
      flex: none;
      place-items: center;
      width: 24px;
      height: 24px;
      border: 2px solid currentcolor;
      border-radius: 50%;
      color: var(--over);
      font-family: var(--mono);
      font-weight: 700;
    }

    .banner--info .banner__mark {
      color: var(--steel);
    }

    .banner__text {
      flex: 1 1 auto;
    }

    .banner__ref {
      margin-left: var(--space-2);
      color: var(--hmi-muted);
      font-family: var(--mono);
      font-size: 12px;
    }
  `,
})
export class BannerComponent {
  readonly error = input<AppError | null>(null);
  readonly messageKey = input<string | null>(null);
  readonly params = input<Record<string, unknown>>({});
  readonly kind = input<'error' | 'info'>('error');
  readonly retryable = input(false);
  readonly retry = output();

  private readonly translation = toSignal(inject(TranslocoService).selectTranslation(), {
    initialValue: {},
  });

  /** The message key; an error code without a translation falls back to the generic message. */
  protected readonly key = computed(() => {
    const explicit = this.messageKey();
    if (explicit) {
      return explicit;
    }
    const key = `errors.${this.error()?.code ?? AppError.unknownCode}`;
    return key in this.translation() ? key : `errors.${AppError.unknownCode}`;
  });
}
