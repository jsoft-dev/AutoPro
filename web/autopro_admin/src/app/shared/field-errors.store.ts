import { signal } from '@angular/core';

/** Per-field validation messages for a form section. */
export class FieldErrorsStore {
  private readonly _errors = signal<Record<string, string>>({});

  get(key: string): string | null {
    return this._errors()[key] ?? null;
  }

  set(key: string, message: string | null): void {
    this._errors.update((current) => {
      const next = { ...current };
      if (message) next[key] = message;
      else delete next[key];
      return next;
    });
  }

  clear(...keys: string[]): void {
    if (keys.length === 0) {
      this._errors.set({});
      return;
    }
    this._errors.update((current) => {
      const next = { ...current };
      for (const key of keys) delete next[key];
      return next;
    });
  }
}
