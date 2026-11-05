import { AppError, toAppError } from './app-error';

describe('AppError', () => {
  it('should take the ref from the trace-id part of a W3C trace context', () => {
    const error = new AppError(
      'Batch.NotFound',
      404,
      '00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01',
    );
    expect(error.ref).toBe('4bf92f35');
  });

  it('should take the first 8 characters of any other trace id', () => {
    expect(new AppError('Unknown', 500, '0HN7ABCDEF:00000001').ref).toBe('0HN7ABCD');
  });

  it('should have no ref without a trace id', () => {
    expect(new AppError(AppError.networkCode, 0, null).ref).toBeNull();
  });

  it('should turn anything that is not an AppError into an unknown error', () => {
    const error = toAppError(new Error('boom'));
    expect(error.code).toBe(AppError.unknownCode);
    expect(error.ref).toBeNull();
  });

  it('should pass an AppError through unchanged', () => {
    const original = new AppError('OrgaTex.Unavailable', 503, null);
    expect(toAppError(original)).toBe(original);
  });
});
