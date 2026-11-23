export type PurchaseOrderStatus =
  | 'Draft'
  | 'Approved'
  | 'PartiallyReceived'
  | 'Closed'
  | 'Cancelled';

export interface PurchaseOrderListItem {
  id: string;
  poNumber: string;
  supplierId: string;
  supplierName: string;
  status: PurchaseOrderStatus;
  lineCount: number;
  createdAtUtc: string;
  approvedAtUtc: string | null;
}

export interface PurchaseOrderLine {
  id: string;
  inventoryItemId: string;
  partNumber: string;
  partName: string;
  quantityOrdered: number;
  quantityReceived: number;
  quantityRemaining: number;
  unitCost: number | null;
  notes: string | null;
}

export interface PurchaseOrderDetail {
  id: string;
  poNumber: string;
  supplierId: string;
  supplierName: string;
  status: PurchaseOrderStatus;
  notes: string | null;
  createdAtUtc: string;
  updatedAtUtc: string | null;
  approvedAtUtc: string | null;
  lines: PurchaseOrderLine[];
}

export interface PurchaseOrderLineWrite {
  inventoryItemId: string;
  quantityOrdered: number;
  unitCost?: number | null;
  notes?: string | null;
}

export interface PurchaseOrderWritePayload {
  supplierId: string;
  notes?: string | null;
  lines: PurchaseOrderLineWrite[];
}

export interface PurchaseOrderOption {
  id: string;
  poNumber: string;
  supplierId: string;
  supplierName: string;
  status: PurchaseOrderStatus;
}