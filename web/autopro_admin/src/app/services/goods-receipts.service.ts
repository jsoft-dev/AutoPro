import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';

import { API_BASE_URL } from '../core/api-config';
import { PagedResult } from '../models/pagination.models';
import {
  GoodsReceiptDetail,
  GoodsReceiptListItem,
  GoodsReceiptWritePayload,
} from '../models/goods-receipt.models';

@Injectable({ providedIn: 'root' })
export class GoodsReceiptsService {
  private readonly http = inject(HttpClient);

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
    return this.http.get<PagedResult<GoodsReceiptListItem>>(
      `${API_BASE_URL}/api/v1/admin/goods-receipts`,
      { params },
    );
  }

  getById(id: string) {
    return this.http.get<GoodsReceiptDetail>(`${API_BASE_URL}/api/v1/admin/goods-receipts/${id}`);
  }

  create(payload: GoodsReceiptWritePayload) {
    return this.http.post<GoodsReceiptDetail>(`${API_BASE_URL}/api/v1/admin/goods-receipts`, payload);
  }

  update(id: string, payload: GoodsReceiptWritePayload) {
    return this.http.put<GoodsReceiptDetail>(`${API_BASE_URL}/api/v1/admin/goods-receipts/${id}`, payload);
  }

  post(id: string, updateSellingPriceFromUnitCost = false) {
    return this.http.post<GoodsReceiptDetail>(
      `${API_BASE_URL}/api/v1/admin/goods-receipts/${id}/post`,
      { updateSellingPriceFromUnitCost },
    );
  }

  cancel(id: string) {
    return this.http.post<GoodsReceiptDetail>(`${API_BASE_URL}/api/v1/admin/goods-receipts/${id}/cancel`, {});
  }
}