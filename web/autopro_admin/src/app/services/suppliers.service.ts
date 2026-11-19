import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';

import { API_BASE_URL } from '../core/api-config';
import { PagedResult } from '../models/pagination.models';
import {
  SupplierDetail,
  SupplierListItem,
  SupplierOption,
  SupplierWritePayload,
} from '../models/supplier.models';

@Injectable({ providedIn: 'root' })
export class SuppliersService {
  private readonly http = inject(HttpClient);

  options() {
    return this.http.get<SupplierOption[]>(`${API_BASE_URL}/api/v1/admin/suppliers/options`);
  }

  list(options?: { q?: string; includeInactive?: boolean; page?: number; pageSize?: number }) {
    let params = new HttpParams();
    const q = options?.q?.trim();
    if (q) params = params.set('q', q);
    if (options?.includeInactive) params = params.set('includeInactive', 'true');
    if (options?.page != null) params = params.set('page', String(options.page));
    if (options?.pageSize != null) params = params.set('pageSize', String(options.pageSize));
    return this.http.get<PagedResult<SupplierListItem>>(`${API_BASE_URL}/api/v1/admin/suppliers`, {
      params,
    });
  }

  getById(id: string) {
    return this.http.get<SupplierDetail>(`${API_BASE_URL}/api/v1/admin/suppliers/${id}`);
  }

  create(payload: SupplierWritePayload) {
    return this.http.post<SupplierDetail>(`${API_BASE_URL}/api/v1/admin/suppliers`, payload);
  }

  update(id: string, payload: SupplierWritePayload) {
    return this.http.put<SupplierDetail>(`${API_BASE_URL}/api/v1/admin/suppliers/${id}`, payload);
  }
}