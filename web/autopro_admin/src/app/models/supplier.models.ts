export interface SupplierListItem {
  id: string;
  name: string;
  phone: string | null;
  email: string | null;
  inventoryItemCount: number;
  isActive: boolean;
  createdAtUtc: string;
}

export interface SupplierDetail {
  id: string;
  name: string;
  contactPerson: string | null;
  phone: string | null;
  email: string | null;
  address: string | null;
  notes: string | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface SupplierOption {
  id: string;
  name: string;
}

export interface SupplierWritePayload {
  name: string;
  contactPerson?: string | null;
  phone?: string | null;
  email?: string | null;
  address?: string | null;
  notes?: string | null;
  isActive: boolean;
}