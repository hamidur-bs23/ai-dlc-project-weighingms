import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import type { Quantity } from '@core/api/api.types';
import { WeightPipe } from '@core/format/weight.pipe';

export type WeighZone = 'none' | 'under' | 'near' | 'ok' | 'over';

interface Band {
  target: Quantity;
  plusMinus: Quantity;
  lower: Quantity;
  upper: Quantity;
}

/** How far the bar reaches beyond the upper tolerance bound (decision BAR-RANGE). */
export const BAR_HEADROOM = 1.25;

/**
 * The progress bar (story 005): fill, tolerance band and target line over 0 → 1.25 × the upper bound (BAR-RANGE),
 * with the caption "TOLERANCE ±0.5 g (93.5 – 94.5)". Readings above the range pin the fill at 100%.
 */
@Component({
  selector: 'wms-weigh-bar',
  imports: [TranslocoPipe, WeightPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="bar-block">
      <div
        class="bar"
        role="progressbar"
        [attr.data-zone]="zone()"
        [attr.aria-label]="'weigh.bar.label' | transloco"
        aria-valuemin="0"
        aria-valuemax="100"
        [attr.aria-valuenow]="fillPercent()"
      >
        <div class="bar__fill" [style.width.%]="fillPercent()"></div>
        @if (band(); as b) {
          <div
            class="bar__band"
            [style.left.%]="lowerPercent()"
            [style.width.%]="bandWidth()"
          ></div>
          <div class="bar__target" [style.left.%]="targetPercent()"></div>
        }
      </div>
      <div class="bar__caption mono">
        <span>0</span>
        @if (band(); as b) {
          <span>
            {{
              'weigh.bar.tolerance'
                | transloco
                  : {
                      plusMinus: (b.plusMinus | weight),
                      lower: (b.lower | weight: decimals() : false),
                      upper: (b.upper | weight: decimals() : false),
                    }
            }}
          </span>
          <span>{{ max() | weight }}</span>
        }
      </div>
    </div>
  `,
  styles: `
    .bar-block {
      display: flex;
      flex-direction: column;
      gap: var(--space-2);
    }

    .bar {
      position: relative;
      height: 62px;
      overflow: hidden;
      border: 1px solid var(--hmi-line);
      border-radius: 3px;
      background: var(--hmi-panel);
    }

    .bar__fill {
      position: absolute;
      inset: 0 auto 0 0;
      background: var(--steel);
      transition: width 0.09s linear;
    }

    .bar[data-zone='near'] .bar__fill {
      background: var(--warn);
    }

    .bar[data-zone='ok'] .bar__fill {
      background: var(--ok);
    }

    .bar[data-zone='over'] .bar__fill {
      background: var(--over);
    }

    .bar__band {
      position: absolute;
      top: 0;
      bottom: 0;
      border-right: 1px solid var(--ok);
      border-left: 1px solid var(--ok);
      background: color-mix(in srgb, var(--ok) 16%, transparent);
    }

    .bar__target {
      position: absolute;
      top: 0;
      bottom: 0;
      width: 2px;
      background: var(--hmi-ink);
      opacity: 0.85;
    }

    .bar__caption {
      display: flex;
      justify-content: space-between;
      color: var(--hmi-muted);
      font-size: 11px;
    }

    @media (prefers-reduced-motion: reduce) {
      .bar__fill {
        transition: none;
      }
    }
  `,
})
export class WeighBarComponent {
  readonly band = input<Band | null>(null);
  readonly net = input<Quantity | null>(null);
  readonly zone = input<WeighZone>('none');

  /** The bar's full-scale value, in the band's unit. */
  protected readonly max = computed<Quantity | null>(() => {
    const b = this.band();
    return b
      ? { value: Math.round(b.upper.value * BAR_HEADROOM * 1000) / 1000, unit: b.upper.unit }
      : null;
  });

  protected readonly decimals = computed(() => (this.band()?.target.unit === 'kg' ? 3 : 1));

  private percent(value: number): number {
    const max = this.max()?.value ?? 0;
    return max > 0 ? Math.min(100, Math.max(0, (value / max) * 100)) : 0;
  }

  protected readonly fillPercent = computed(
    () => Math.round(this.percent(this.net()?.value ?? 0) * 10) / 10,
  );
  protected readonly lowerPercent = computed(() => this.percent(this.band()?.lower.value ?? 0));
  protected readonly targetPercent = computed(() => this.percent(this.band()?.target.value ?? 0));
  protected readonly bandWidth = computed(() =>
    Math.max(0.6, this.percent(this.band()?.upper.value ?? 0) - this.lowerPercent()),
  );
}
