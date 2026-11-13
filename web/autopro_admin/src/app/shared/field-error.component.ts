import { Component, input } from '@angular/core';

@Component({
  selector: 'app-field-error',
  standalone: true,
  template: `@if (message()) {
    <p class="field-error" role="alert">{{ message() }}</p>
  }`,
  styles: `
    .field-error {
      margin: 0.35rem 0 0;
      font-size: 0.85rem;
      line-height: 1.35;
      color: var(--ap-danger);
    }
  `,
})
export class FieldErrorComponent {
  readonly message = input<string | null>(null);
}
