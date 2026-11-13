import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';

import { API_BASE_URL } from '../core/api-config';
import { PagedResult } from '../models/pagination.models';
import { JobDetail, JobListItem, JobStatus } from '../models/job.models';

export interface CreateStoreSalePayload {
  storeCustomerName?: string | null;
  notes?: string | null;
}

export interface SaleListOptions {
  status?: JobStatus;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class SalesService {
  private readonly http = inject(HttpClient);

  list(options?: SaleListOptions) {
    let params = new HttpParams();
    if (options?.status) params = params.set('status', options.status);
    if (options?.page != null) params = params.set('page', String(options.page));
    if (options?.pageSize != null) params = params.set('pageSize', String(options.pageSize));
    return this.http.get<PagedResult<JobListItem>>(`${API_BASE_URL}/api/v1/admin/sales`, { params });
  }

  create(payload: CreateStoreSalePayload = {}) {
    return this.http.post<JobDetail>(`${API_BASE_URL}/api/v1/admin/sales`, {
      storeCustomerName: payload.storeCustomerName ?? null,
      notes: payload.notes ?? null,
    });
  }

  getById(id: string) {
    return this.http.get<JobDetail>(`${API_BASE_URL}/api/v1/admin/sales/${id}`);
  }

  update(id: string, payload: CreateStoreSalePayload) {
    return this.http.patch<JobDetail>(`${API_BASE_URL}/api/v1/admin/sales/${id}`, {
      storeCustomerName: payload.storeCustomerName ?? null,
      notes: payload.notes ?? null,
    });
  }

  complete(id: string, note?: string) {
    return this.http.post<JobDetail>(`${API_BASE_URL}/api/v1/admin/sales/${id}/complete`, {
      note: note ?? null,
    });
  }
}
