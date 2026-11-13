import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';

import { API_BASE_URL } from '../core/api-config';
import { PagedResult } from '../models/pagination.models';
import {
  VehicleDetail,
  VehicleJobHistoryEntry,
  VehicleListItem,
} from '../models/vehicle.models';

@Injectable({ providedIn: 'root' })
export class VehiclesService {
  private readonly http = inject(HttpClient);

  list(options?: { q?: string; page?: number; pageSize?: number }) {
    let params = new HttpParams();
    const q = options?.q?.trim();
    if (q) params = params.set('q', q);
    if (options?.page != null) params = params.set('page', String(options.page));
    if (options?.pageSize != null) params = params.set('pageSize', String(options.pageSize));
    return this.http.get<PagedResult<VehicleListItem>>(`${API_BASE_URL}/api/v1/admin/vehicles`, {
      params,
    });
  }

  getById(id: string) {
    return this.http.get<VehicleDetail>(`${API_BASE_URL}/api/v1/admin/vehicles/${id}`);
  }

  getJobHistory(vehicleId: string) {
    return this.http.get<VehicleJobHistoryEntry[]>(
      `${API_BASE_URL}/api/v1/admin/vehicles/${vehicleId}/job-history`,
    );
  }
}
