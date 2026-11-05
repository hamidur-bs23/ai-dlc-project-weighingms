import { Pipe, PipeTransform } from '@angular/core';
import type { Quantity } from '@core/api/api.types';

/** Default display decimals per unit: g to 0.1 g, kg to 1 g (the training scales' resolutions). */
const DEFAULT_DECIMALS: Readonly<Partial<Record<string, number>>> = { g: 1, kg: 3 };

/**
 * Formats a quantity with its unit, never mixing g and kg in one value (ux-guide). Pass the active
 * scale's decimals when known; otherwise the unit's default applies.
 */
@Pipe({ name: 'weight' })
export class WeightPipe implements PipeTransform {
  transform(quantity: Quantity | null | undefined, decimals?: number): string {
    if (!quantity) {
      return '—';
    }
    const places = decimals ?? DEFAULT_DECIMALS[quantity.unit] ?? 3;
    return `${quantity.value.toFixed(places)} ${quantity.unit}`;
  }
}
