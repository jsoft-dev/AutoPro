import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe, CurrencyPipe, DecimalPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { switchMap } from 'rxjs/operators';
import { of } from 'rxjs';

import { JobsService } from '../../services/jobs.service';
import { InventorySearchItem } from '../../models/inventory.models';
import { BusinessSettingsService } from '../../services/business-settings.service';
import {
  OrganizationSettings,
  organizationDisplayName,
  organizationLogoSrc,
} from '../../models/business-settings.models';
import {
  JobDetail,
  PurchaseSource,
  effectiveServiceChargeLkr,
  estimatedChargeTotal,
  invoiceKindLabel,
  lineItemSourceLabel,
  lineTotal,
  statusLabel,
} from '../../models/job.models';
import { InventoryPartSearchFieldComponent } from '../../shared/inventory-part-search-field.component';
import { PaymentMethodSelectComponent } from '../../shared/payment-method-select.component';
import { FieldErrorComponent } from '../../shared/field-error.component';
import { FieldErrorsStore } from '../../shared/field-errors.store';
import { isPaymentMethod } from '../../models/payment.models';
import { JobStatusChipComponent } from '../../shared/job-status-chip.component';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-job-detail',
  standalone: true,
  imports: [
    RouterLink,
    DatePipe,
    CurrencyPipe,
    DecimalPipe,
    ReactiveFormsModule,
    JobStatusChipComponent,
    InventoryPartSearchFieldComponent,
    PaymentMethodSelectComponent,
    FieldErrorComponent,
  ],
  templateUrl: './job-detail.component.html',
  styleUrl: './job-detail.component.scss',
})
export class JobDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly jobsApi = inject(JobsService);
  private readonly settingsApi = inject(BusinessSettingsService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  readonly job = signal<JobDetail | null>(null);
  readonly loading = signal(true);
  readonly pageError = signal<string | null>(null);
  readonly purchaseErrors = new FieldErrorsStore();
  readonly paymentErrors = new FieldErrorsStore();
  readonly resetError = signal<string | null>(null);
  readonly closing = signal(false);
  readonly resetting = signal(false);
  readonly addingPurchase = signal(false);
  readonly selectedPart = signal<InventorySearchItem | null>(null);
  readonly showBill = signal(false);
  readonly organization = signal<OrganizationSettings | null>(null);

  readonly closeForm = this.fb.nonNullable.group({
    paymentMethod: [''],
  });

  readonly purchaseForm = this.fb.nonNullable.group({
    source: ['Inventory' as PurchaseSource],
    description: [''],
    quantity: [1],
    unitPrice: [0],
    vendorName: [''],
  });

  readonly lineTotal = lineTotal;
  readonly statusLabel = statusLabel;
  readonly invoiceKindLabel = invoiceKindLabel;
  readonly lineItemSourceLabel = lineItemSourceLabel;
  readonly effectiveServiceChargeLkr = effectiveServiceChargeLkr;
  readonly estimatedChargeTotal = estimatedChargeTotal;

  chargeTotal(j: JobDetail): number {
    return estimatedChargeTotal(j);
  }

  ngOnInit(): void {
    this.closeForm.controls.paymentMethod.valueChanges.subscribe(() => {
      this.paymentErrors.clear('paymentMethod');
    });
    this.loadJobAndSettings();
  }

  private loadJobAndSettings(): void {
    this.settingsApi.getOrganization().subscribe({
      next: (settings) => this.organization.set(settings),
      error: () => this.organization.set(null),
    });

    this.route.paramMap
      .pipe(
        switchMap((params) => {
          const id = params.get('id');
          if (!id) return of(null);
          this.loading.set(true);
          this.pageError.set(null);
          return this.jobsApi.getById(id);
        }),
      )
      .subscribe({
        next: (detail) => {
          if (!detail) {
            this.pageError.set('Job not found.');
            this.loading.set(false);
            return;
          }
          this.job.set(detail);
          this.loading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.pageError.set(err.error?.message ?? 'Could not load job.');
          this.loading.set(false);
        },
      });
  }

  canAddPurchase(): boolean {
    const j = this.job();
    if (!j || j.invoice) return false;
    return j.status === 'OnGoing' || j.status === 'OnHold';
  }

  onInventoryPartChange(part: InventorySearchItem | null): void {
    this.selectedPart.set(part);
    this.purchaseErrors.clear('part');
    if (part) {
      this.purchaseForm.patchValue({ description: part.name, unitPrice: part.unitPrice });
    } else {
      this.purchaseForm.patchValue({ description: '', unitPrice: 0 });
    }
  }

  onPurchaseSourceChange(source: PurchaseSource): void {
    this.purchaseForm.controls.source.setValue(source);
    this.selectedPart.set(null);
    this.purchaseErrors.clear();
    if (source === 'ExternalVendor') {
      this.purchaseForm.patchValue({ description: '', unitPrice: 0 });
    } else {
      this.purchaseForm.patchValue({ description: '', unitPrice: 0, vendorName: '' });
    }
  }

  addPurchaseLine(): void {
    const j = this.job();
    if (!j || !this.canAddPurchase()) return;

    const raw = this.purchaseForm.getRawValue();
    const source = raw.source;
    const description = raw.description.trim();
    const quantity = raw.quantity;
    const unitPrice = raw.unitPrice;
    const vendorName = raw.vendorName.trim();

    this.purchaseErrors.clear('part', 'description', 'quantity', 'unitPrice', 'vendor', 'submit');

    if (source === 'Inventory' && !this.selectedPart()) {
      this.purchaseErrors.set('part', 'Choose a part from shop inventory.');
      return;
    }
    if (source === 'Inventory' && this.selectedPart() && quantity > this.selectedPart()!.quantityOnHand) {
      this.purchaseErrors.set('quantity', `Only ${this.selectedPart()!.quantityOnHand} on hand for this part.`);
      return;
    }
    if (!description) {
      this.purchaseErrors.set('description', 'Enter a description for this line.');
      return;
    }
    if (quantity <= 0) {
      this.purchaseErrors.set('quantity', 'Quantity must be greater than zero.');
      return;
    }
    if (unitPrice < 0) {
      this.purchaseErrors.set('unitPrice', 'Unit price cannot be negative.');
      return;
    }
    if (source === 'ExternalVendor' && !vendorName) {
      this.purchaseErrors.set('vendor', 'Vendor name is required for external purchases.');
      return;
    }

    this.addingPurchase.set(true);
    this.jobsApi
      .addPurchase(j.id, {
        source,
        inventoryItemId: source === 'Inventory' ? this.selectedPart()!.id : null,
        description,
        quantity,
        unitPrice,
        vendorName: source === 'ExternalVendor' ? vendorName : null,
      })
      .subscribe({
        next: (updated) => {
          this.job.set(updated);
          this.addingPurchase.set(false);
          this.purchaseForm.reset({
            source: 'Inventory',
            description: '',
            quantity: 1,
            unitPrice: 0,
            vendorName: '',
          });
          this.selectedPart.set(null);
          this.purchaseErrors.clear();
        },
        error: (err: HttpErrorResponse) => {
          this.purchaseErrors.set('submit', err.error?.message ?? 'Could not add purchase line.');
          this.addingPurchase.set(false);
        },
      });
  }

  purchaseFieldInvalid(field: 'description' | 'quantity' | 'unitPrice' | 'vendor'): boolean {
    return !!this.purchaseErrors.get(field);
  }

  canClose(): boolean {
    return this.job()?.status === 'Completed';
  }

  canResetWorkflow(): boolean {
    const j = this.job();
    if (!j || j.status === 'Cancelled') return false;
    return j.status !== 'OnGoing' || j.purchases.length > 0 || j.statusHistory.length > 1;
  }

  resetWorkflow(): void {
    const j = this.job();
    if (!j || !this.canResetWorkflow()) return;
    const ok = window.confirm(
      'Reset this job? All purchase lines will be removed (shop stock restored), status set to On going, and completion/payment cleared so you can run the full flow again.',
    );
    if (!ok) return;

    this.resetting.set(true);
    this.resetError.set(null);
    this.jobsApi.resetWorkflow(j.id).subscribe({
      next: (updated) => {
        this.job.set(updated);
        this.showBill.set(false);
        this.resetting.set(false);
        this.closeForm.reset();
      },
      error: (err: HttpErrorResponse) => {
        this.resetError.set(err.error?.message ?? 'Could not reset job.');
        this.resetting.set(false);
      },
    });
  }

  canReprint(): boolean {
    const s = this.job()?.status;
    return s === 'Closed' || s === 'Completed';
  }

  receivePaymentAndClose(): void {
    const j = this.job();
    if (!j || !this.canClose()) return;
    this.paymentErrors.clear();
    const method = this.closeForm.controls.paymentMethod.value.trim();
    if (!isPaymentMethod(method)) {
      this.paymentErrors.set('paymentMethod', 'Select a payment method.');
      return;
    }

    this.closing.set(true);
    this.jobsApi.closeJob(j.id, method).subscribe({
      next: (updated) => {
        this.job.set(updated);
        this.closing.set(false);
        this.showBill.set(true);
        setTimeout(() => window.print(), 300);
      },
      error: (err: HttpErrorResponse) => {
        this.paymentErrors.set('paymentMethod', err.error?.message ?? 'Could not close job.');
        this.closing.set(false);
      },
    });
  }

  printBill(): void {
    this.showBill.set(true);
    setTimeout(() => window.print(), 150);
  }

  officerName(): string {
    return this.auth.displayName();
  }

  billDateUtc(j: JobDetail): string {
    return j.closedAtUtc ?? j.invoice?.finalizedAtUtc ?? j.completedAtUtc ?? j.createdAtUtc;
  }

  paymentNote(j: JobDetail): string | null {
    const closed = [...j.statusHistory]
      .reverse()
      .find((h) => h.toStatus === 'Closed');
    const note = closed?.note?.trim();
    if (!note) return j.status === 'Closed' ? 'Payment received' : null;
    if (note === 'Payment received; job closed.') return 'Payment received';
    if (isPaymentMethod(note)) return note;
    return note;
  }

  isPaid(j: JobDetail): boolean {
    return j.status === 'Closed';
  }

  isEstimateBill(j: JobDetail): boolean {
    return !j.invoice && j.status !== 'Closed';
  }

  organizationName(): string {
    return organizationDisplayName(this.organization());
  }

  organizationTagline(): string | null {
    const tagline = this.organization()?.tagline?.trim();
    return tagline ? tagline : null;
  }

  organizationLogoUrl(): string | null {
    return organizationLogoSrc(this.organization());
  }

  organizationNeedsSetup(): boolean {
    return !this.organization()?.organizationName?.trim();
  }
}
