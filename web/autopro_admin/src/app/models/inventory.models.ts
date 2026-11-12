export interface InventoryListItem {
  id: string;
  partNumber: string;
  name: string;
  quantityOnHand: number;
  unitPrice: number;
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
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface InventoryWritePayload {
  partNumber: string;
  name: string;
  notes?: string | null;
  quantityOnHand: number;
  unitPrice: number;
  isActive: boolean;
}
