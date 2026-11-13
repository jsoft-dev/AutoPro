import type { JobStatus } from './job.models';

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

export interface VehicleJobHistoryPurchase {
  description: string;
  quantity: number;
  inventoryPartNumber: string | null;
  vendorName: string | null;
}

export interface VehicleJobHistoryEntry {
  id: string;
  jobNumber: string;
  jobTypeLabel: string;
  status: JobStatus;
  odometerReading: number | null;
  notes: string | null;
  statusNotes: string[];
  createdAtUtc: string;
  completedAtUtc: string | null;
  purchases: VehicleJobHistoryPurchase[];
}

export function formatJobHistoryPurchase(p: VehicleJobHistoryPurchase): string {
  const qty = formatJobHistoryQty(p.quantity);
  let label = p.description;
  if (p.inventoryPartNumber) {
    label = `${p.inventoryPartNumber} — ${p.description}`;
  }
  if (p.vendorName) {
    label = `${label} (${p.vendorName})`;
  }
  return `${label} × ${qty}`;
}

export function formatJobHistoryQty(quantity: number): string {
  if (Number.isInteger(quantity)) return String(quantity);
  return quantity.toFixed(3).replace(/\.?0+$/, '');
}

export function vehicleJobInformationLines(entry: VehicleJobHistoryEntry): string[] {
  const lines: string[] = [];
  if (entry.notes?.trim()) lines.push(entry.notes.trim());
  for (const note of entry.statusNotes) {
    const trimmed = note.trim();
    if (trimmed) lines.push(trimmed);
  }
  return lines;
}

export function vehicleSummary(v: Pick<VehicleListItem, 'make' | 'model' | 'year'>): string {
  const parts: string[] = [];
  if (v.make) parts.push(v.make);
  if (v.model) parts.push(v.model);
  if (v.year != null) parts.push(String(v.year));
  return parts.length > 0 ? parts.join(' ') : '—';
}
