import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';

import { API_BASE_URL } from '../core/api-config';
import { PagedResult } from '../models/pagination.models';
import { JobDetail, JobListItem, JobStatus, PurchaseSource } from '../models/job.models';

export interface AddPurchasePayload {
  source: PurchaseSource;
  inventoryItemId: string | null;
  description: string;
  quantity: number;
  unitPrice: number;
  vendorName: string | null;
}

export interface JobListOptions {
  status?: JobStatus;
  inWorkshop?: boolean;
  from?: string;
  to?: string;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class JobsService {
  private readonly http = inject(HttpClient);

  listToday() {
    return this.http.get<JobListItem[]>(`${API_BASE_URL}/api/v1/jobs/today`);
  }

  list(options?: JobListOptions) {
    let params = new HttpParams();
    if (options?.status) params = params.set('status', options.status);
    if (options?.inWorkshop) params = params.set('inWorkshop', 'true');
    if (options?.from) params = params.set('from', options.from);
    if (options?.to) params = params.set('to', options.to);
    if (options?.page != null) params = params.set('page', String(options.page));
    if (options?.pageSize != null) params = params.set('pageSize', String(options.pageSize));
    return this.http.get<PagedResult<JobListItem>>(`${API_BASE_URL}/api/v1/jobs`, { params });
  }

  getById(id: string) {
    return this.http.get<JobDetail>(`${API_BASE_URL}/api/v1/jobs/${id}`);
  }

  closeJob(id: string, paymentMethod: string) {
    return this.http.post<JobDetail>(`${API_BASE_URL}/api/v1/admin/jobs/${id}/close`, {
      paymentNote: paymentMethod,
    });
  }

  resetWorkflow(id: string) {
    return this.http.post<JobDetail>(`${API_BASE_URL}/api/v1/admin/jobs/${id}/reset-workflow`, {});
  }

  addPurchase(id: string, payload: AddPurchasePayload) {
    return this.http.post<JobDetail>(`${API_BASE_URL}/api/v1/jobs/${id}/purchases`, payload);
  }
}
