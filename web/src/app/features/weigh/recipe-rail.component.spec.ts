import { render, screen, within } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import type { WeighingSession } from '@core/api/api.types';
import { weighingSession } from '../../../testing/fixtures';
import { provideTestTransloco } from '../../../testing/test-providers';
import { RecipeRailComponent } from './recipe-rail.component';

function inProgress(): WeighingSession {
  const s = weighingSession(3, { progress: { done: 2, total: 5 } });
  s.components[0] = {
    ...s.components[0],
    state: 'done',
    record: {
      recordId: 'r1',
      actual: { value: 1241.5, unit: 'g' },
      class: 'withinRange',
      operator: 'OP-1001',
      confirmedAt: 'x',
      override: false,
    },
  };
  s.components[1] = {
    ...s.components[1],
    state: 'done',
    record: {
      recordId: 'r2',
      actual: { value: 389.1, unit: 'g' },
      class: 'overweight',
      operator: 'OP-1001',
      confirmedAt: 'x',
      override: true,
    },
  };
  return s;
}

describe('RecipeRailComponent', () => {
  const selected = vi.fn();

  beforeEach(async () => {
    selected.mockReset();
    await render(RecipeRailComponent, {
      providers: [provideTestTransloco()],
      inputs: { session: inProgress(), shade: 'Teal 4C', colourOf: () => '#e3b51d' },
      on: { selected },
    });
  });

  it('should show the recipe header and the counter', () => {
    expect(screen.getByText('Recipe · Teal 4C')).toBeTruthy();
    expect(screen.getByText('2 of 5 components weighed')).toBeTruthy();
  });

  it('should show every component with code, scale class, target and state', () => {
    const rows = screen.getAllByRole('button');
    expect(rows).toHaveLength(5);
    expect(within(rows[0]).getByText('✓ 1241.5 g', { exact: false })).toBeTruthy();
    expect(within(rows[1]).getByText('OVR')).toBeTruthy();
    expect(within(rows[2]).getByText('WEIGHING')).toBeTruthy();
    expect(within(rows[2]).getByText('DYE-4471 · precision')).toBeTruthy();
    expect(within(rows[2]).getByText('94.0 g')).toBeTruthy();
    expect(within(rows[3]).getByText('AUX-0101 · heavy-duty')).toBeTruthy();
    expect(within(rows[3]).getByText('24.000 kg')).toBeTruthy();
    expect(within(rows[4]).getByText('PENDING')).toBeTruthy();
    expect(rows[2].getAttribute('aria-current')).toBe('step');
  });

  it('should select a pending component when touched', async () => {
    await userEvent.setup().click(screen.getAllByRole('button')[4]);
    expect(selected).toHaveBeenCalledWith(5);
  });

  it('should not let done components be selected', async () => {
    const done = screen.getAllByRole('button')[0] as HTMLButtonElement;
    expect(done.disabled).toBe(true);
    await userEvent.setup().click(done);
    expect(selected).not.toHaveBeenCalled();
  });
});
