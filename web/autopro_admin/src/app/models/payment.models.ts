/** Must match API `PaymentMethodLabels` and status-history notes on close. */
export const PAYMENT_METHOD_OPTIONS = ['Cash', 'Card', 'Bank Transfer'] as const;

export type PaymentMethodOption = (typeof PAYMENT_METHOD_OPTIONS)[number];

export function isPaymentMethod(value: string): value is PaymentMethodOption {
  return (PAYMENT_METHOD_OPTIONS as readonly string[]).includes(value);
}
