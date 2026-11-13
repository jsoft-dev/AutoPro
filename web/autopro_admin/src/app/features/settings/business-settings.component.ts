import { Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin } from 'rxjs';

import { BusinessSettingsService } from '../../services/business-settings.service';
import {
  JobTypePrice,
  OrganizationSettings,
  organizationLogoSrc,
} from '../../models/business-settings.models';

@Component({
  selector: 'app-business-settings',
  standalone: true,
  imports: [DatePipe, FormsModule],
  templateUrl: './business-settings.component.html',
  styleUrl: './business-settings.component.scss',
})
export class BusinessSettingsComponent implements OnInit {
  private readonly settingsApi = inject(BusinessSettingsService);

  readonly loading = signal(true);
  readonly savingPrices = signal(false);
  readonly savingOrganization = signal(false);
  readonly uploadingLogo = signal(false);
  readonly error = signal<string | null>(null);
  readonly pricesSaved = signal(false);
  readonly organizationSaved = signal(false);

  readonly prices = signal<JobTypePrice[]>([]);
  readonly drafts = signal<Record<string, number>>({});

  readonly organization = signal<OrganizationSettings | null>(null);
  readonly orgNameDraft = signal('');
  readonly orgTaglineDraft = signal('');

  ngOnInit(): void {
    this.load();
  }

  organizationLogoUrl(): string | null {
    return organizationLogoSrc(this.organization());
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.pricesSaved.set(false);
    this.organizationSaved.set(false);

    forkJoin({
      prices: this.settingsApi.getJobTypePrices(),
      organization: this.settingsApi.getOrganization(),
    }).subscribe({
      next: ({ prices, organization }) => {
        this.prices.set(prices);
        const draft: Record<string, number> = {};
        for (const row of prices) {
          draft[row.jobType] = row.serviceChargeLkr;
        }
        this.drafts.set(draft);

        this.organization.set(organization);
        this.orgNameDraft.set(organization.organizationName ?? '');
        this.orgTaglineDraft.set(organization.tagline ?? '');

        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(err.error?.message ?? 'Could not load business settings.');
        this.loading.set(false);
      },
    });
  }

  draftValue(jobType: string): number {
    return this.drafts()[jobType] ?? 0;
  }

  setDraft(jobType: string, value: string): void {
    const parsed = Number(value);
    this.drafts.update((d) => ({ ...d, [jobType]: Number.isFinite(parsed) ? parsed : 0 }));
    this.pricesSaved.set(false);
  }

  setOrgName(value: string): void {
    this.orgNameDraft.set(value);
    this.organizationSaved.set(false);
  }

  setOrgTagline(value: string): void {
    this.orgTaglineDraft.set(value);
    this.organizationSaved.set(false);
  }

  savePrices(): void {
    const payload = this.prices().map((row) => ({
      jobType: row.jobType,
      serviceChargeLkr: this.draftValue(row.jobType),
    }));

    this.savingPrices.set(true);
    this.error.set(null);
    this.settingsApi.saveJobTypePrices(payload).subscribe({
      next: (rows) => {
        this.prices.set(rows);
        const draft: Record<string, number> = {};
        for (const row of rows) {
          draft[row.jobType] = row.serviceChargeLkr;
        }
        this.drafts.set(draft);
        this.savingPrices.set(false);
        this.pricesSaved.set(true);
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(err.error?.message ?? 'Could not save prices.');
        this.savingPrices.set(false);
      },
    });
  }

  saveOrganization(): void {
    const organizationName = this.orgNameDraft().trim();
    if (!organizationName) {
      this.error.set('Organization name is required.');
      return;
    }

    const tagline = this.orgTaglineDraft().trim();

    this.savingOrganization.set(true);
    this.error.set(null);
    this.settingsApi
      .saveOrganization({
        organizationName,
        tagline: tagline.length > 0 ? tagline : null,
      })
      .subscribe({
        next: (settings) => {
          this.organization.set(settings);
          this.orgNameDraft.set(settings.organizationName);
          this.orgTaglineDraft.set(settings.tagline ?? '');
          this.savingOrganization.set(false);
          this.organizationSaved.set(true);
        },
        error: (err: HttpErrorResponse) => {
          this.error.set(err.error?.message ?? 'Could not save organization settings.');
          this.savingOrganization.set(false);
        },
      });
  }

  onLogoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      this.error.set('Logo must be an image (PNG, JPEG, or WebP).');
      return;
    }

    this.uploadingLogo.set(true);
    this.error.set(null);
    this.settingsApi.uploadOrganizationLogo(file).subscribe({
      next: (settings) => {
        this.organization.set(settings);
        this.uploadingLogo.set(false);
        this.organizationSaved.set(true);
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(err.error?.message ?? 'Could not upload logo.');
        this.uploadingLogo.set(false);
      },
    });
  }

  removeLogo(): void {
    const ok = window.confirm('Remove the organization logo from printed invoices?');
    if (!ok) return;

    this.uploadingLogo.set(true);
    this.error.set(null);
    this.settingsApi.deleteOrganizationLogo().subscribe({
      next: (settings) => {
        this.organization.set(settings);
        this.uploadingLogo.set(false);
        this.organizationSaved.set(true);
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(err.error?.message ?? 'Could not remove logo.');
        this.uploadingLogo.set(false);
      },
    });
  }
}
