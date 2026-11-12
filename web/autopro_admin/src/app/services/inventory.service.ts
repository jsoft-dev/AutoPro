import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';

import { API_BASE_URL } from '../core/api-config';
import {
  InventoryDetail,
  InventoryListItem,
  InventoryWritePayload,
} from '../models/inventory.models';

@Injectable({ providedIn: 'root' })
export class InventoryService {
  private readonly http = inject(HttpClient);

  list(options?: { q?: string; includeInactive?: boolean; limit?: number }) {
    let params = new HttpParams();
    const q = options?.q?.trim();
    if (q) params = params.set('q', q);
    if (options?.includeInactive) params = params.set('includeInactive', 'true');
    if (options?.limit != null) params = params.set('limit', String(options.limit));
    return this.http.get<InventoryListItem[]>(`${API_BASE_URL}/api/v1/admin/inventory`, {
      params,
    });
  }

  getById(id: string) {
    return this.http.get<InventoryDetail>(`${API_BASE_URL}/api/v1/admin/inventory/${id}`);
  }

  create(payload: InventoryWritePayload) {
    return this.http.post<InventoryDetail>(`${API_BASE_URL}/api/v1/admin/inventory`, payload);
  }

  update(id: string, payload: InventoryWritePayload) {
    return this.http.put<InventoryDetail>(`${API_BASE_URL}/api/v1/admin/inventory/${id}`, payload);
  }
}
