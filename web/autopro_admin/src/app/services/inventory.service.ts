import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';

import { API_BASE_URL } from '../core/api-config';
import { PagedResult } from '../models/pagination.models';
import {
  InventoryDetail,
  InventoryListItem,
  InventorySearchPage,
  InventoryWritePayload,
} from '../models/inventory.models';

@Injectable({ providedIn: 'root' })
export class InventoryService {
  private readonly http = inject(HttpClient);

  searchForJob(q?: string, limit = 20, offset = 0) {
    let params = new HttpParams()
      .set('limit', String(limit))
      .set('offset', String(offset));
    const term = q?.trim();
    if (term) params = params.set('q', term);
    return this.http.get<InventorySearchPage>(`${API_BASE_URL}/api/v1/admin/inventory/search`, {
      params,
    });
  }

  list(options?: { q?: string; includeInactive?: boolean; page?: number; pageSize?: number }) {
    let params = new HttpParams();
    const q = options?.q?.trim();
    if (q) params = params.set('q', q);
    if (options?.includeInactive) params = params.set('includeInactive', 'true');
    if (options?.page != null) params = params.set('page', String(options.page));
    if (options?.pageSize != null) params = params.set('pageSize', String(options.pageSize));
    return this.http.get<PagedResult<InventoryListItem>>(`${API_BASE_URL}/api/v1/admin/inventory`, {
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
