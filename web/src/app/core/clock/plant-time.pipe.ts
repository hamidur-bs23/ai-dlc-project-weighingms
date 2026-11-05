import { Pipe, PipeTransform, inject } from '@angular/core';
import { PlantClockService } from './plant-clock.service';

/**
 * Shows an instant in plant time (the station's time zone from `GET /api/station`), not the
 * browser's. Impure because the zone arrives after start-up; formatting is cheap.
 */
@Pipe({ name: 'plantTime', pure: false })
export class PlantTimePipe implements PipeTransform {
  private readonly clock = inject(PlantClockService);

  transform(instant: Date | string | null | undefined, withSeconds = false): string {
    return instant ? this.clock.format(instant, withSeconds) : '—';
  }
}
