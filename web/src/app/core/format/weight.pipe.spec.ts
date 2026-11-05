import { WeightPipe } from './weight.pipe';

describe('WeightPipe', () => {
  const pipe = new WeightPipe();

  it.each([
    [{ value: 94, unit: 'g' }, '94.0 g'],
    [{ value: 1240, unit: 'g' }, '1240.0 g'],
    [{ value: 24, unit: 'kg' }, '24.000 kg'],
    [{ value: 9.6, unit: 'kg' }, '9.600 kg'],
  ])('should show %o as %s', (quantity, expected) => {
    expect(pipe.transform(quantity)).toBe(expected);
  });

  it('should use the decimals given for the active scale', () => {
    expect(pipe.transform({ value: 94.25, unit: 'g' }, 2)).toBe('94.25 g');
  });

  it('should show a dash when there is no quantity', () => {
    expect(pipe.transform(null)).toBe('—');
  });
});
