import { HttpErrorResponse } from '@angular/common/http';

export function readApiError(err: HttpErrorResponse, fallback: string): string {
  if (err.status === 0) {
    return 'Could not reach the API. Check that it is running and you are signed in.';
  }

  const body = err.error;
  if (!body) {
    return `${fallback} (HTTP ${err.status})`;
  }
  if (typeof body === 'string') {
    return body.length > 0 ? body : fallback;
  }
  if (typeof body.message === 'string' && body.message.length > 0) {
    return body.message;
  }
  if (typeof body.detail === 'string' && body.detail.length > 0) {
    return body.detail;
  }
  if (typeof body.title === 'string') {
    const nested = body.errors as Record<string, string[] | undefined> | undefined;
    if (nested) {
      for (const messages of Object.values(nested)) {
        if (messages?.[0]) {
          return messages[0];
        }
      }
    }
    return body.title;
  }
  return `${fallback} (HTTP ${err.status})`;
}