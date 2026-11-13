export type JobStatus = 'OnGoing' | 'OnHold' | 'Cancelled' | 'Completed' | 'Closed';

export type PurchaseSource = 'Inventory' | 'ExternalVendor';

export type InvoiceLineItemKind = 'ServiceCharge' | 'Purchase';

export type LineItemSource = 'InHouse' | 'OutSource';

export interface JobListItem {
  id: string;
  jobNumber: string;
  status: JobStatus;
  jobType: string;
  jobTypeOtherDescription: string | null;
  jobTypeLabel: string;
  plateNumber: string | null;
  customerName: string;
  odometerReading: number | null;
  createdAtUtc: string;
}

export interface JobPurchaseLine {
  id: string;
  source: PurchaseSource;
  inventoryItemId: string | null;
  inventoryPartNumber: string | null;
  description: string;
  quantity: number;
  unitPrice: number;
  vendorName: string | null;
  createdAtUtc: string;
}

export interface JobInvoiceLineItem {
  id: string;
  kind: InvoiceLineItemKind;
  source: LineItemSource | null;
  description: string;
  quantity: number;
  unitPrice: number;
  vendorName: string | null;
  inventoryPartNumber: string | null;
}

export interface JobInvoice {
  id: string;
  finalizedAtUtc: string;
  grandTotal: number;
  lineItems: JobInvoiceLineItem[];
}

export interface JobStatusHistoryEntry {
  fromStatus: JobStatus;
  toStatus: JobStatus;
  changedAtUtc: string;
  note: string | null;
}

export interface JobDetail {
  id: string;
  jobNumber: string;
  status: JobStatus;
  jobType: string;
  jobTypeOtherDescription: string | null;
  jobTypeLabel: string;
  vehicleId: string | null;
  plateNumber: string | null;
  customerId: string | null;
  customerName: string;
  storeCustomerName: string | null;
  odometerReading: number | null;
  notes: string | null;
  createdAtUtc: string;
  completedAtUtc: string | null;
  closedAtUtc: string | null;
  purchasesTotal: number;
  jobTypeServiceChargeLkr?: number;
  customServiceChargeLkr?: number | null;
  purchases: JobPurchaseLine[];
  invoice: JobInvoice | null;
  statusHistory: JobStatusHistoryEntry[];
  vehicleLastRecordedOdometerKm?: number | null;
}

export function validateOdometerAgainstLast(
  value: number | null | undefined,
  lastRecordedKm: number | null | undefined,
): string | null {
  if (value == null || lastRecordedKm == null) return null;
  if (value < lastRecordedKm) {
    return `Cannot be less than the last recorded reading (${lastRecordedKm.toLocaleString()} km).`;
  }
  return null;
}

/** Matches API seed defaults; used when detail predates `jobTypeServiceChargeLkr` on the wire. */
const JOB_TYPE_SERVICE_CHARGE_LKR: Record<string, number> = {
  Wash: 1500,
  FullService: 3500,
  Repair: 2000,
  Other: 0,
  StoreSale: 0,
};

export function isStoreSale(job: JobDetail | JobListItem): boolean {
  return job.jobType === 'StoreSale';
}

export function effectiveServiceChargeLkr(job: JobDetail): number {
  if (isStoreSale(job)) return 0;

  const fromApi = job.jobTypeServiceChargeLkr;
  if (typeof fromApi === 'number' && Number.isFinite(fromApi)) {
    return fromApi;
  }
  if (
    job.jobType === 'Other' &&
    typeof job.customServiceChargeLkr === 'number' &&
    Number.isFinite(job.customServiceChargeLkr)
  ) {
    return job.customServiceChargeLkr;
  }
  return JOB_TYPE_SERVICE_CHARGE_LKR[job.jobType] ?? 0;
}

export function estimatedChargeTotal(job: JobDetail): number {
  if (job.invoice) return job.invoice.grandTotal;
  return job.purchasesTotal + effectiveServiceChargeLkr(job);
}

export function lineTotal(line: JobPurchaseLine | JobInvoiceLineItem): number {
  return line.quantity * line.unitPrice;
}

export function invoiceKindLabel(kind: InvoiceLineItemKind): string {
  return kind === 'ServiceCharge' ? 'Service charge' : 'Purchase';
}

export function lineItemSourceLabel(source: LineItemSource | null): string {
  if (!source) return '—';
  return source === 'InHouse' ? 'In house' : 'Outsource';
}

export function statusLabel(status: JobStatus): string {
  const labels: Record<JobStatus, string> = {
    OnGoing: 'On going',
    OnHold: 'On hold',
    Cancelled: 'Cancelled',
    Completed: 'Completed',
    Closed: 'Closed',
  };
  return labels[status];
}
