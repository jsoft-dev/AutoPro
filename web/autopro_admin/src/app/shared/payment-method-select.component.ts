import { Component, forwardRef, input } from '@angular/core';
import { FieldErrorComponent } from './field-error.component';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, ReactiveFormsModule } from '@angular/forms';

import { PAYMENT_METHOD_OPTIONS } from '../models/payment.models';

@Component({
  selector: 'app-payment-method-select',
  standalone: true,
  imports: [ReactiveFormsModule, FieldErrorComponent],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => PaymentMethodSelectComponent),
      multi: true,
    },
  ],
  template: `
    <label class="payment-method">
      <span class="payment-method__label">Payment method</span>
      <select
        class="payment-method__select"
        [class.field-invalid]="!!error()"
        [value]="value"
        (change)="onSelect($event)"
        [disabled]="disabled"
      >
        <option value="" disabled [selected]="!value">Select payment method</option>
        @for (method of options; track method) {
          <option [value]="method">{{ method }}</option>
        }
      </select>
      <app-field-error [message]="error()" />
    </label>
  `,
  styles: `
    .payment-method {
      display: flex;
      flex-direction: column;
      gap: 0.35rem;
      margin: 0;
      font-size: 0.9rem;
    }

    .payment-method__label {
      font-weight: 600;
      color: var(--ap-muted);
    }

    .payment-method__select {
      padding: 0.5rem 0.75rem;
      border: 1px solid var(--ap-border);
      border-radius: 8px;
      font-size: 0.95rem;
      font-family: inherit;
      background: white;
      cursor: pointer;
    }

    .payment-method__select:focus {
      outline: 2px solid rgba(13, 71, 161, 0.25);
      border-color: var(--ap-primary);
    }

    .payment-method__select.field-invalid {
      border-color: var(--ap-danger);
    }
  `,
})
export class PaymentMethodSelectComponent implements ControlValueAccessor {
  readonly options = PAYMENT_METHOD_OPTIONS;
  readonly error = input<string | null>(null);

  value = '';
  disabled = false;

  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  onSelect(event: Event): void {
    const el = event.target as HTMLSelectElement;
    this.value = el.value;
    this.onChange(this.value);
    this.onTouched();
  }

  writeValue(value: string | null): void {
    this.value = value ?? '';
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }
}
