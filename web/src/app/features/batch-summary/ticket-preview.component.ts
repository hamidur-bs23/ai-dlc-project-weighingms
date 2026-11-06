import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  effect,
  input,
  viewChild,
} from '@angular/core';
import { TranslocoPipe } from '@jsverse/transloco';
import JsBarcode from 'jsbarcode';
import type { Ticket } from '@core/api/api.types';

/** The server's name for the barcode type (PD-04), and JsBarcode's. */
const CODE_128 = 'Code128';
const JSBARCODE_CODE_128 = 'CODE128';

/**
 * The ticket as it prints (story 010): the server's frozen lines, shown as sent and never composed
 * here, then the Code 128 barcode with its value beneath it.
 */
@Component({
  selector: 'wms-ticket-preview',
  imports: [TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <figure class="ticket" data-testid="ticket-preview">
      <figcaption class="visually-hidden">{{ 'ticket.preview' | transloco }}</figcaption>
      <pre class="ticket__lines" data-testid="ticket-lines">{{ lines() }}</pre>
      @if (ticket().barcode.symbology === code128) {
        <svg
          #bars
          class="ticket__bars"
          role="img"
          [attr.aria-label]="'ticket.barcode' | transloco: { value: ticket().barcode.value }"
        ></svg>
      }
      <p class="ticket__value" data-testid="ticket-barcode-value">{{ ticket().barcode.value }}</p>
    </figure>
    <p class="ticket-state" data-testid="ticket-preview-state">
      {{ 'ticket.state.' + ticket().state | transloco: { count: ticket().printCount } }}
      @if (showReprintNote() && ticket().printCount > 0) {
        · {{ 'ticket.nextIsReprint' | transloco }}
      }
    </p>
  `,
  styles: `
    .ticket {
      width: fit-content;
      margin: 0;
      padding: 18px 16px;
      border-radius: 2px;
      background: var(--ticket-paper);
      box-shadow: 0 6px 18px rgb(0 0 0 / 35%);
      color: var(--ticket-ink);
      font-family: var(--mono);
      font-size: 11.5px;
      line-height: 1.65;
    }

    .ticket__lines {
      margin: 0;
      font: inherit;
    }

    .ticket__bars {
      display: block;
      margin: var(--space-2) auto var(--space-1);
    }

    .ticket__value {
      margin: 0;
      text-align: center;
    }

    .ticket-state {
      margin: var(--space-3) 0 0;
      color: var(--hmi-muted);
      font-family: var(--mono);
      font-size: 12px;
    }

    @media print {
      .ticket {
        position: absolute;
        top: 0;
        left: 0;
        width: 80mm;
        padding: 0;
        box-shadow: none;
      }
    }
  `,
})
export class TicketPreviewComponent {
  readonly ticket = input.required<Ticket>();
  /** Say that the next copy will be marked REPRINT; off when no one can print from this screen. */
  readonly showReprintNote = input(true);

  protected readonly code128 = CODE_128;
  protected readonly lines = computed(() => this.ticket().lines.join('\n'));
  private readonly bars = viewChild<ElementRef<SVGSVGElement>>('bars');

  constructor() {
    effect(() => {
      const svg = this.bars()?.nativeElement;
      if (svg) {
        JsBarcode(svg, this.ticket().barcode.value, {
          format: JSBARCODE_CODE_128,
          displayValue: false, // the value is printed beneath as text, so it stays selectable
          width: 2,
          height: 44,
          margin: 10, // the quiet zone a scanner needs on each side
          background: 'transparent',
        });
      }
    });
  }
}
