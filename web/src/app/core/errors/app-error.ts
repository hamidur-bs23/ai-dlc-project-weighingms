/**
 * An Api failure as the UI sees it: the stable error code (`Area.Reason`, from ProblemDetails
 * `errorCode`), the HTTP status and the trace ID shown to the operator as "Ref" (ux-guide, FR-24).
 * The raw ProblemDetails `detail` is never shown.
 */
export class AppError extends Error {
  constructor(
    readonly code: string,
    readonly status: number,
    readonly traceId: string | null,
  ) {
    super(code);
    this.name = 'AppError';
  }

  /**
   * The first 8 characters of the trace ID, shown as "Ref …". The Api sends the W3C trace context
   * (`00-<trace-id>-<span-id>-01`), so the ref is taken from the trace-id part, which is what the
   * logs and traces are searched by.
   */
  get ref(): string | null {
    if (!this.traceId) {
      return null;
    }
    const traceparent = /^[\da-f]{2}-([\da-f]{32})-[\da-f]{16}-[\da-f]{2}$/i.exec(this.traceId);
    return (traceparent?.[1] ?? this.traceId).slice(0, 8);
  }

  /** The WeighingMS.Api could not be reached at all (network down, Api stopped). */
  static readonly networkCode = 'Network.Unavailable';

  /** A response without a recognisable error code. */
  static readonly unknownCode = 'Unknown';
}

/** Narrows anything caught from an Api call to an AppError. */
export function toAppError(error: unknown): AppError {
  return error instanceof AppError ? error : new AppError(AppError.unknownCode, 0, null);
}
