export interface VehicleListItem {
  id: string;
  platePrefix: string;
  plateSuffix: string;
  plateNumber: string;
  make: string | null;
  model: string | null;
  year: number | null;
  customerName: string;
  customerPhone: string;
  qrPayload: string;
  createdAtUtc: string;
}

export interface VehicleDetail extends VehicleListItem {
  vin: string | null;
  customerId: string;
}

export function vehicleSummary(v: Pick<VehicleListItem, 'make' | 'model' | 'year'>): string {
  const parts: string[] = [];
  if (v.make) parts.push(v.make);
  if (v.model) parts.push(v.model);
  if (v.year != null) parts.push(String(v.year));
  return parts.length > 0 ? parts.join(' ') : '—';
}
