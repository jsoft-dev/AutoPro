import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';

import { API_BASE_URL } from '../core/api-config';
import { PagedResult } from '../models/pagination.models';
import {
  InventoryDetail,
  InventoryImportResult,
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

  list(options?: {
    q?: string;
    supplierId?: string;
    includeInactive?: boolean;
    page?: number;
    pageSize?: number;
  }) {
    let params = new HttpParams();
    const q = options?.q?.trim();
    if (q) params = params.set('q', q);
    if (options?.supplierId) params = params.set('supplierId', options.supplierId);
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

  downloadImportTemplate() {
    return this.http.get(`${API_BASE_URL}/api/v1/admin/inventory/import/template`, {
      responseType: 'blob',
    });
  }

  exportExcel(options?: { q?: string; supplierId?: string; includeInactive?: boolean }) {
    let params = new HttpParams();
    const q = options?.q?.trim();
    if (q) params = params.set('q', q);
    if (options?.supplierId) params = params.set('supplierId', options.supplierId);
    if (options?.includeInactive) params = params.set('includeInactive', 'true');
    return this.http.get(`${API_BASE_URL}/api/v1/admin/inventory/export`, {
      params,
      responseType: 'blob',
    });
  }

  importExcel(file: File, createMissingSuppliers = true) {
    const body = new FormData();
    body.append('file', file);
    let params = new HttpParams();
    if (!createMissingSuppliers) params = params.set('createMissingSuppliers', 'false');
    return this.http.post<InventoryImportResult>(`${API_BASE_URL}/api/v1/admin/inventory/import`, body, {
      params,
    });
  }
}
