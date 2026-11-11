import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';

import { API_BASE_URL } from '../core/api-config';
import { JobDetail, JobListItem, JobStatus } from '../models/job.models';

@Injectable({ providedIn: 'root' })
export class JobsService {
  private readonly http = inject(HttpClient);

  listToday() {
    return this.http.get<JobListItem[]>(`${API_BASE_URL}/api/v1/jobs/today`);
  }

  list(options?: { status?: JobStatus; from?: string; to?: string }) {
    let params = new HttpParams();
    if (options?.status) params = params.set('status', options.status);
    if (options?.from) params = params.set('from', options.from);
    if (options?.to) params = params.set('to', options.to);
    return this.http.get<JobListItem[]>(`${API_BASE_URL}/api/v1/jobs`, { params });
  }

  getById(id: string) {
    return this.http.get<JobDetail>(`${API_BASE_URL}/api/v1/jobs/${id}`);
  }

  closeJob(id: string, paymentNote?: string) {
    return this.http.post<JobDetail>(`${API_BASE_URL}/api/v1/admin/jobs/${id}/close`, {
      paymentNote: paymentNote ?? null,
    });
  }
}
