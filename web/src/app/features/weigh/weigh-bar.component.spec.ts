import { render, screen } from '@testing-library/angular';
import { provideTestTransloco } from '../../../testing/test-providers';
import { WeighBarComponent } from './weigh-bar.component';

const g = (value: number) => ({ value, unit: 'g' });
const band = { target: g(94), plusMinus: g(0.5), lower: g(93.5), upper: g(94.5) };

describe('WeighBarComponent (BAR-RANGE)', () => {
  const renderBar = (net: number | null, zone = 'under' as const) =>
    render(WeighBarComponent, {
      providers: [provideTestTransloco()],
      inputs: { band, net: net === null ? null : g(net), zone },
    });

  it('should show the caption with the range numbers only', async () => {
    await renderBar(55.3);

    expect(screen.getByText('TOLERANCE ±0.5 g (93.5 – 94.5)')).toBeTruthy();
    expect(screen.getByText('118.1 g')).toBeTruthy(); // 1.25 × 94.5 g
  });

  it.each([
    [0, 0],
    [59.1, 50],
    [94.5, 80],
    [118.1, 100],
    [500, 100], // above the range: pinned
  ])('should fill %s g to %s %% of 0 – 118.1 g', async (net, percent) => {
    await renderBar(net);

    expect(Number(screen.getByRole('progressbar').getAttribute('aria-valuenow'))).toBeCloseTo(
      percent,
      0,
    );
  });

  it('should place the band and the target line at about 80 % of the bar', async () => {
    const { container } = await renderBar(0);
    const bandEl = container.querySelector<HTMLElement>('.bar__band');
    const target = container.querySelector<HTMLElement>('.bar__target');

    expect(parseFloat(bandEl?.style.left ?? '0')).toBeCloseTo(79.2, 0);
    expect(parseFloat(target?.style.left ?? '0')).toBeCloseTo(79.6, 0);
  });

  it('should expose the zone for colour (and the text carries the class elsewhere)', async () => {
    await render(WeighBarComponent, {
      providers: [provideTestTransloco()],
      inputs: { band, net: g(96.4), zone: 'over' },
    });

    expect(screen.getByRole('progressbar').getAttribute('data-zone')).toBe('over');
  });
});
