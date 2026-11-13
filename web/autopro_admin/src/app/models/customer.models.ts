export interface CustomerListItem {
  id: string;
  fullName: string;
  phone: string;
  email: string | null;
  vehicleCount: number;
  createdAtUtc: string;
}

export interface CustomerVehicleSummary {
  id: string;
  plateNumber: string;
  make: string | null;
  model: string | null;
  year: number | null;
  createdAtUtc: string;
}

export interface CustomerDetail {
  id: string;
  fullName: string;
  phone: string;
  email: string | null;
  createdAtUtc: string;
  vehicles: CustomerVehicleSummary[];
}

export interface CustomerWritePayload {
  fullName: string;
  phone: string;
  email: string | null;
}

export function customerVehicleSummary(
  v: Pick<CustomerVehicleSummary, 'make' | 'model' | 'year'>,
): string {
  const parts: string[] = [];
  if (v.make) parts.push(v.make);
  if (v.model) parts.push(v.model);
  if (v.year != null) parts.push(String(v.year));
  return parts.length > 0 ? parts.join(' ') : '—';
}
