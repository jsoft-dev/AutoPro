import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';

import { API_BASE_URL } from '../core/api-config';
import { PagedResult } from '../models/pagination.models';
import {
  CustomerDetail,
  CustomerListItem,
  CustomerWritePayload,
} from '../models/customer.models';

@Injectable({ providedIn: 'root' })
export class CustomersService {
  private readonly http = inject(HttpClient);

  list(options?: { q?: string; page?: number; pageSize?: number }) {
    let params = new HttpParams();
    const q = options?.q?.trim();
    if (q) params = params.set('q', q);
    if (options?.page != null) params = params.set('page', String(options.page));
    if (options?.pageSize != null) params = params.set('pageSize', String(options.pageSize));
    return this.http.get<PagedResult<CustomerListItem>>(`${API_BASE_URL}/api/v1/admin/customers`, {
      params,
    });
  }

  getById(id: string) {
    return this.http.get<CustomerDetail>(`${API_BASE_URL}/api/v1/admin/customers/${id}`);
  }

  update(id: string, body: CustomerWritePayload) {
    return this.http.put<CustomerDetail>(`${API_BASE_URL}/api/v1/admin/customers/${id}`, body);
  }
}
