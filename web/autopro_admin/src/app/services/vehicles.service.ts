import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';

import { API_BASE_URL } from '../core/api-config';
import { VehicleDetail, VehicleListItem } from '../models/vehicle.models';

@Injectable({ providedIn: 'root' })
export class VehiclesService {
  private readonly http = inject(HttpClient);

  list(options?: { q?: string; limit?: number }) {
    let params = new HttpParams();
    const q = options?.q?.trim();
    if (q) params = params.set('q', q);
    if (options?.limit != null) params = params.set('limit', String(options.limit));
    return this.http.get<VehicleListItem[]>(`${API_BASE_URL}/api/v1/admin/vehicles`, { params });
  }

  getById(id: string) {
    return this.http.get<VehicleDetail>(`${API_BASE_URL}/api/v1/admin/vehicles/${id}`);
  }
}
