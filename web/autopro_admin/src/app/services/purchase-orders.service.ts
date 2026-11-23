import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';

import { API_BASE_URL } from '../core/api-config';
import { PagedResult } from '../models/pagination.models';
import {
  PurchaseOrderDetail,
  PurchaseOrderListItem,
  PurchaseOrderOption,
  PurchaseOrderWritePayload,
} from '../models/purchase-order.models';

@Injectable({ providedIn: 'root' })
export class PurchaseOrdersService {
  private readonly http = inject(HttpClient);

  options(supplierId?: string) {
    let params = new HttpParams();
    if (supplierId) params = params.set('supplierId', supplierId);
    return this.http.get<PurchaseOrderOption[]>(`${API_BASE_URL}/api/v1/admin/purchase-orders/options`, {
      params,
    });
  }

  list(options?: {
    q?: string;
    status?: string;
    supplierId?: string;
    page?: number;
    pageSize?: number;
  }) {
    let params = new HttpParams();
    const q = options?.q?.trim();
    if (q) params = params.set('q', q);
    if (options?.status) params = params.set('status', options.status);
    if (options?.supplierId) params = params.set('supplierId', options.supplierId);
    if (options?.page != null) params = params.set('page', String(options.page));
    if (options?.pageSize != null) params = params.set('pageSize', String(options.pageSize));
    return this.http.get<PagedResult<PurchaseOrderListItem>>(
      `${API_BASE_URL}/api/v1/admin/purchase-orders`,
      { params },
    );
  }

  getById(id: string) {
    return this.http.get<PurchaseOrderDetail>(`${API_BASE_URL}/api/v1/admin/purchase-orders/${id}`);
  }

  create(payload: PurchaseOrderWritePayload) {
    return this.http.post<PurchaseOrderDetail>(`${API_BASE_URL}/api/v1/admin/purchase-orders`, payload);
  }

  update(id: string, payload: PurchaseOrderWritePayload) {
    return this.http.put<PurchaseOrderDetail>(`${API_BASE_URL}/api/v1/admin/purchase-orders/${id}`, payload);
  }

  approve(id: string) {
    return this.http.post<PurchaseOrderDetail>(`${API_BASE_URL}/api/v1/admin/purchase-orders/${id}/approve`, {});
  }

  cancel(id: string) {
    return this.http.post<PurchaseOrderDetail>(`${API_BASE_URL}/api/v1/admin/purchase-orders/${id}/cancel`, {});
  }
}