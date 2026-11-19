export interface InventoryListItem {
  id: string;
  partNumber: string;
  name: string;
  quantityOnHand: number;
  unitPrice: number;
  supplierId: string | null;
  supplierName: string | null;
  isActive: boolean;
  createdAtUtc: string;
}

export interface InventoryDetail {
  id: string;
  partNumber: string;
  name: string;
  notes: string | null;
  quantityOnHand: number;
  unitPrice: number;
  supplierId: string | null;
  supplierName: string | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface InventorySearchItem {
  id: string;
  partNumber: string;
  name: string;
  quantityOnHand: number;
  unitPrice: number;
}

export interface InventorySearchPage {
  items: InventorySearchItem[];
  hasMore: boolean;
}

export interface InventoryWritePayload {
  partNumber: string;
  name: string;
  notes?: string | null;
  quantityOnHand: number;
  unitPrice: number;
  supplierId?: string | null;
  isActive: boolean;
}

export interface InventoryImportResult {
  createdCount: number;
  updatedCount: number;
  skippedCount: number;
  errors: { rowNumber: number; message: string }[];
}
