import { signal } from '@angular/core';
import { render, screen, within } from '@testing-library/angular';
import { PlantClockService } from '@core/clock/plant-clock.service';
import { DeviceHealthService, DeviceStat } from '@core/devices/device-health.service';
import { provideTestTransloco } from '../../../testing/test-providers';
import { StatusStripComponent } from './status-strip.component';

describe('StatusStripComponent', () => {
  const devices = signal<DeviceStat[]>([]);
  const unsynced = signal(0);

  beforeEach(async () => {
    unsynced.set(0);
    devices.set([
      { device: 'scale-3kg', kind: 'scale', name: '3 kg precision', state: 'ready' },
      { device: 'scale-32kg', kind: 'scale', name: '32 kg floor', state: 'offline' },
      { device: 'scanner', kind: 'scanner', name: null, state: 'ready' },
      { device: 'printer', kind: 'printer', name: null, state: 'ready' },
      { device: 'orgatex', kind: 'orgatex', name: null, state: 'unknown' },
    ]);
    await render(StatusStripComponent, {
      providers: [
        provideTestTransloco(),
        { provide: DeviceHealthService, useValue: { devices, unsynced } },
        { provide: PlantClockService, useValue: { time: signal('07:47:03') } },
      ],
    });
  });

  it('should list both scales, the scanner, the printer and OrgaTEX with their state in text', () => {
    const items = within(screen.getByRole('list', { name: 'Device status' }))
      .getAllByRole('listitem')
      .map((li) => li.textContent.replace(/\s+/g, ' ').trim());
    expect(items).toEqual([
      'SCALE 3 kg precision · READY',
      'SCALE 32 kg floor · OFFLINE',
      'SCANNER · READY',
      'PRINTER · READY',
      'ORGATEX · NO DATA',
    ]);
  });

  it('should show the language and the plant-time clock', () => {
    expect(screen.getByText('EN')).toBeTruthy();
    expect(screen.getByLabelText('Plant time').textContent).toBe('07:47:03');
  });

  it('should follow device changes', async () => {
    devices.update((d) =>
      d.map((x) => (x.device === 'scale-32kg' ? { ...x, state: 'ready' as const } : x)),
    );
    expect(await screen.findByText(/SCALE 32 kg floor · READY/)).toBeTruthy();
  });

  it('should hide the unsynced count when every write-back is sent', () => {
    expect(screen.queryByTestId('unsynced')).toBeNull();
  });

  it('should show the unsynced count on OrgaTEX only (STEP 30)', async () => {
    devices.update((d) =>
      d.map((x) => (x.device === 'orgatex' ? { ...x, state: 'offline' as const } : x)),
    );
    unsynced.set(2);

    const count = await screen.findByTestId('unsynced');
    expect(count.closest('li')?.textContent.replace(/\s+/g, ' ').trim()).toBe(
      'ORGATEX · OFFLINE · 2 UNSYNCED',
    );
    expect(screen.getAllByTestId('unsynced')).toHaveLength(1);
  });
});
