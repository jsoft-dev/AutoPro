export type JobTypeKey = 'Wash' | 'FullService' | 'Repair' | 'Other';

export interface JobTypePrice {
  jobType: JobTypeKey;
  jobTypeLabel: string;
  serviceChargeLkr: number;
  updatedAtUtc: string;
}

export interface JobTypePriceUpdate {
  jobType: JobTypeKey;
  serviceChargeLkr: number;
}

export interface OrganizationSettings {
  organizationName: string;
  tagline: string | null;
  hasLogo: boolean;
  logoUrl: string | null;
  updatedAtUtc: string;
}

export interface OrganizationSettingsUpdate {
  organizationName: string;
  tagline: string | null;
}

export function organizationDisplayName(settings: OrganizationSettings | null): string {
  const name = settings?.organizationName?.trim();
  return name ? name : 'Your service station';
}

export function organizationLogoSrc(settings: OrganizationSettings | null): string | null {
  if (!settings?.hasLogo) return null;
  if (settings.logoUrl) return settings.logoUrl;
  return null;
}
