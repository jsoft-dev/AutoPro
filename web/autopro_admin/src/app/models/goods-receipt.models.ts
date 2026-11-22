export type GoodsReceiptStatus = 'Draft' | 'Posted' | 'Cancelled';

export interface GoodsReceiptListItem {
  id: string;
  grnNumber: string;
  supplierId: string;
  supplierName: string;
  purchaseOrderId: string | null;
  purchaseOrderNumber: string | null;
  status: GoodsReceiptStatus;
  receivedAtUtc: string;
  lineCount: number;
  createdAtUtc: string;
  postedAtUtc: string | null;
}

export interface GoodsReceiptLine {
  id: string;
  inventoryItemId: string;
  partNumber: string;
  partName: string;
  purchaseOrderLineId: string | null;
  quantityReceived: number;
  unitCost: number | null;
  notes: string | null;
}

export interface GoodsReceiptDetail {
  id: string;
  grnNumber: string;
  supplierId: string;
  supplierName: string;
  purchaseOrderId: string | null;
  purchaseOrderNumber: string | null;
  status: GoodsReceiptStatus;
  receivedAtUtc: string;
  supplierReference: string | null;
  notes: string | null;
  createdAtUtc: string;
  updatedAtUtc: string | null;
  postedAtUtc: string | null;
  lines: GoodsReceiptLine[];
}

export interface GoodsReceiptLineWrite {
  inventoryItemId: string;
  purchaseOrderLineId?: string | null;
  quantityReceived: number;
  unitCost?: number | null;
  notes?: string | null;
}

export interface GoodsReceiptWritePayload {
  supplierId: string;
  purchaseOrderId?: string | null;
  receivedAtUtc: string;
  supplierReference?: string | null;
  notes?: string | null;
  lines: GoodsReceiptLineWrite[];
}