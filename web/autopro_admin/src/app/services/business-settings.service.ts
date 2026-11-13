import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import { API_BASE_URL } from '../core/api-config';
import {
  JobTypePrice,
  JobTypePriceUpdate,
  OrganizationSettings,
  OrganizationSettingsUpdate,
} from '../models/business-settings.models';

@Injectable({ providedIn: 'root' })
export class BusinessSettingsService {
  private readonly http = inject(HttpClient);

  getJobTypePrices() {
    return this.http.get<JobTypePrice[]>(
      `${API_BASE_URL}/api/v1/admin/business-settings/job-type-prices`,
    );
  }

  saveJobTypePrices(prices: JobTypePriceUpdate[]) {
    return this.http.put<JobTypePrice[]>(
      `${API_BASE_URL}/api/v1/admin/business-settings/job-type-prices`,
      { prices },
    );
  }

  getOrganization() {
    return this.http.get<OrganizationSettings>(
      `${API_BASE_URL}/api/v1/admin/business-settings/organization`,
    );
  }

  saveOrganization(payload: OrganizationSettingsUpdate) {
    return this.http.put<OrganizationSettings>(
      `${API_BASE_URL}/api/v1/admin/business-settings/organization`,
      payload,
    );
  }

  uploadOrganizationLogo(file: File) {
    const body = new FormData();
    body.append('file', file, file.name);
    return this.http.post<OrganizationSettings>(
      `${API_BASE_URL}/api/v1/admin/business-settings/organization/logo`,
      body,
    );
  }

  deleteOrganizationLogo() {
    return this.http.delete<OrganizationSettings>(
      `${API_BASE_URL}/api/v1/admin/business-settings/organization/logo`,
    );
  }
}
