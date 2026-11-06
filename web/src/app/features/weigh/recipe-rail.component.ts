import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import type { WeighingSession } from '@core/api/api.types';
import { WeightPipe } from '@core/format/weight.pipe';

/**
 * The recipe rail (story 004): every component with swatch, name, code · scale class, target and state
 * (✓ actual [OVR] / WEIGHING / PENDING), and "n of N components weighed". Pending rows can be touched.
 */
@Component({
  selector: 'wms-recipe-rail',
  imports: [TranslocoPipe, WeightPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <aside class="rail" [attr.aria-label]="'weigh.rail.label' | transloco">
      <div class="rail__head">
        <span class="rail__title">{{ 'weigh.rail.recipe' | transloco: { shade: shade() } }}</span>
        <span class="rail__batch mono">{{ session().batchId }}</span>
        <span class="rail__count">
          {{
            'weigh.rail.count'
              | transloco: { done: session().progress.done, total: session().progress.total }
          }}
        </span>
      </div>
      <ul class="rail__list">
        @for (c of session().components; track c.sequence) {
          <li>
            <button
              type="button"
              class="comp"
              [attr.data-state]="c.state"
              [disabled]="c.state === 'done' || disabled()"
              [attr.aria-current]="c.state === 'active' ? 'step' : null"
              (click)="selected.emit(c.sequence)"
            >
              <span
                class="comp__swatch"
                [style.background]="colourOf()(c.sequence)"
                aria-hidden="true"
              ></span>
              <span class="comp__main">
                <span class="comp__name">{{ c.name }}</span>
                <span class="comp__sub mono"
                  >{{ c.code }} · {{ 'weigh.scaleClass.' + c.scale.scaleClass | transloco }}</span
                >
              </span>
              <span class="comp__side">
                <span class="comp__qty mono">{{ c.target | weight }}</span>
                <span class="comp__state mono">
                  @switch (c.state) {
                    @case ('done') {
                      ✓ {{ c.record?.actual | weight }}
                      @if (c.record?.override) {
                        <span class="comp__ovr">{{ 'weigh.rail.ovr' | transloco }}</span>
                      }
                    }
                    @case ('active') {
                      {{ 'weigh.rail.weighing' | transloco }}
                    }
                    @default {
                      {{ 'weigh.rail.pending' | transloco }}
                    }
                  }
                </span>
              </span>
            </button>
          </li>
        }
      </ul>
    </aside>
  `,
  styles: `
    .rail {
      display: flex;
      flex-direction: column;
      border-right: 1px solid var(--hmi-line);
      background: var(--hmi-bg);
    }

    .rail__head {
      display: flex;
      flex-direction: column;
      gap: 3px;
      padding: 13px var(--space-4) 11px;
      border-bottom: 1px solid var(--hmi-line);
    }

    .rail__title {
      color: var(--hmi-dim);
      font-size: 10.5px;
      font-weight: 650;
      letter-spacing: 0.13em;
      text-transform: uppercase;
    }

    .rail__batch {
      font-size: 17px;
      font-weight: 600;
    }

    .rail__count {
      color: var(--hmi-muted);
      font-size: 12px;
    }

    .rail__list {
      margin: 0;
      padding: 0;
      list-style: none;
    }

    .comp {
      display: grid;
      grid-template-columns: 8px 1fr auto;
      gap: 11px;
      align-items: center;
      width: 100%;
      min-height: 56px;
      padding: 11px var(--space-4);
      border: 0;
      border-bottom: 1px solid var(--hmi-line);
      border-left: 3px solid transparent;
      background: transparent;
      color: inherit;
      font: inherit;
      text-align: left;
      cursor: pointer;

      &:hover:not(:disabled) {
        background: var(--hmi-panel);
      }

      &:focus-visible {
        outline: 2px solid var(--steel);
        outline-offset: -2px;
      }

      &:disabled {
        cursor: default;
      }

      &[data-state='active'] {
        border-left-color: var(--warn);
        background: var(--hmi-panel-2);
      }

      &[data-state='done'] {
        opacity: 0.62;
      }
    }

    .comp__swatch {
      width: 8px;
      height: 30px;
      border-radius: 1px;
    }

    .comp__main,
    .comp__side {
      display: flex;
      flex-direction: column;
    }

    .comp__side {
      align-items: flex-end;
    }

    .comp__name {
      font-size: 13.5px;
      line-height: 1.3;
    }

    .comp__sub {
      color: var(--hmi-dim);
      font-size: 10.5px;
      letter-spacing: 0.04em;
    }

    .comp__qty {
      font-size: 13.5px;
      font-variant-numeric: tabular-nums;
    }

    .comp__state {
      font-size: 9.5px;
      letter-spacing: 0.1em;
      color: var(--hmi-dim);
    }

    .comp[data-state='done'] .comp__state {
      color: var(--ok);
    }

    .comp[data-state='active'] .comp__state {
      color: var(--warn);
    }

    .comp__ovr {
      margin-left: var(--space-1);
      padding: 0 var(--space-1);
      border: 1px solid var(--over);
      border-radius: 2px;
      color: var(--over);
    }
  `,
})
export class RecipeRailComponent {
  readonly session = input.required<WeighingSession>();
  readonly shade = input('');
  readonly colourOf = input<(sequence: number) => string | null>(() => null);
  readonly disabled = input(false);
  readonly selected = output<number>();
}
