export type JobStatus = 'OnGoing' | 'OnHold' | 'Cancelled' | 'Completed' | 'Closed';

export type PurchaseSource = 'Inventory' | 'ExternalVendor';

export interface JobListItem {
  id: string;
  jobNumber: string;
  status: JobStatus;
  jobType: string;
  jobTypeOtherDescription: string | null;
  jobTypeLabel: string;
  plateNumber: string;
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
  vehicleId: string;
  plateNumber: string;
  customerId: string;
  customerName: string;
  odometerReading: number | null;
  notes: string | null;
  createdAtUtc: string;
  completedAtUtc: string | null;
  closedAtUtc: string | null;
  purchasesTotal: number;
  purchases: JobPurchaseLine[];
  statusHistory: JobStatusHistoryEntry[];
}

export function lineTotal(line: JobPurchaseLine): number {
  return line.quantity * line.unitPrice;
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
