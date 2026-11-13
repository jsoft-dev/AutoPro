import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DatePipe, CurrencyPipe, DecimalPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { switchMap, tap } from 'rxjs/operators';
import { of } from 'rxjs';

import { SalesService } from '../../services/sales.service';
import { JobsService } from '../../services/jobs.service';
import { InventorySearchItem } from '../../models/inventory.models';
import { BusinessSettingsService } from '../../services/business-settings.service';
import {
  OrganizationSettings,
  organizationDisplayName,
  organizationLogoSrc,
} from '../../models/business-settings.models';
import { JobDetail, lineTotal } from '../../models/job.models';
import { InventoryPartSearchFieldComponent } from '../../shared/inventory-part-search-field.component';
import { JobStatusChipComponent } from '../../shared/job-status-chip.component';
import { PaymentMethodSelectComponent } from '../../shared/payment-method-select.component';
import { FieldErrorComponent } from '../../shared/field-error.component';
import { FieldErrorsStore } from '../../shared/field-errors.store';
import { isPaymentMethod } from '../../models/payment.models';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-sale-detail',
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
  templateUrl: './sale-detail.component.html',
  styleUrl: './sale-detail.component.scss',
})
export class SaleDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly salesApi = inject(SalesService);
  private readonly jobsApi = inject(JobsService);
  private readonly settingsApi = inject(BusinessSettingsService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  readonly sale = signal<JobDetail | null>(null);
  readonly loading = signal(true);
  readonly pageError = signal<string | null>(null);
  readonly lineErrors = new FieldErrorsStore();
  readonly payErrors = new FieldErrorsStore();
  readonly paying = signal(false);
  readonly addingPurchase = signal(false);
  readonly selectedPart = signal<InventorySearchItem | null>(null);
  readonly showBill = signal(false);
  readonly organization = signal<OrganizationSettings | null>(null);

  readonly headerForm = this.fb.nonNullable.group({
    storeCustomerName: [''],
    notes: [''],
  });

  readonly payForm = this.fb.nonNullable.group({
    paymentMethod: [''],
  });

  readonly lineForm = this.fb.nonNullable.group({
    description: [''],
    quantity: [1],
    unitPrice: [0],
  });

  readonly lineTotal = lineTotal;

  ngOnInit(): void {
    this.settingsApi.getOrganization().subscribe({
      next: (settings) => this.organization.set(settings),
      error: () => this.organization.set(null),
    });

    this.payForm.controls.paymentMethod.valueChanges.subscribe(() => {
      this.payErrors.clear('paymentMethod');
    });

    this.route.paramMap
      .pipe(
        switchMap(() => {
          this.loading.set(true);
          this.pageError.set(null);

          if (this.router.url.endsWith('/sales/new')) {
            return this.salesApi.create({}).pipe(
              tap((sale) => {
                void this.router.navigate(['/sales', sale.id], { replaceUrl: true });
              }),
            );
          }

          const id = this.route.snapshot.paramMap.get('id');
          if (!id) return of(null);
          return this.salesApi.getById(id);
        }),
      )
      .subscribe({
        next: (detail) => {
          if (!detail) {
            this.pageError.set('Sale not found.');
            this.loading.set(false);
            return;
          }
          this.applySale(detail);
          this.loading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.pageError.set(err.error?.message ?? 'Could not load sale.');
          this.loading.set(false);
        },
      });
  }

  private applySale(detail: JobDetail): void {
    this.sale.set(detail);
    this.headerForm.patchValue(
      {
        storeCustomerName: detail.storeCustomerName ?? '',
        notes: detail.notes ?? '',
      },
      { emitEvent: false },
    );
  }

  saleTotal(j: JobDetail): number {
    if (j.invoice) return j.invoice.grandTotal;
    return j.purchasesTotal;
  }

  isEditable(): boolean {
    const j = this.sale();
    if (!j || j.invoice) return false;
    return j.status === 'OnGoing' || j.status === 'OnHold';
  }

  canPay(): boolean {
    const j = this.sale();
    if (!j) return false;
    if (j.status === 'Closed') return false;
    return (j.status === 'OnGoing' || j.status === 'OnHold') && j.purchases.length > 0;
  }

  canReprint(): boolean {
    const s = this.sale()?.status;
    return s === 'Closed' || s === 'Completed';
  }

  onInventoryPartChange(part: InventorySearchItem | null): void {
    this.selectedPart.set(part);
    this.lineErrors.clear('part');
    if (part) {
      this.lineForm.patchValue({ description: part.name, unitPrice: part.unitPrice });
    } else {
      this.lineForm.patchValue({ description: '', unitPrice: 0 });
    }
  }

  addLine(): void {
    const j = this.sale();
    if (!j || !this.isEditable()) return;

    this.lineErrors.clear('part', 'quantity', 'unitPrice', 'submit');

    if (!this.selectedPart()) {
      this.lineErrors.set('part', 'Choose a part from shop inventory.');
      return;
    }

    const raw = this.lineForm.getRawValue();
    const quantity = raw.quantity;
    const unitPrice = raw.unitPrice;
    const part = this.selectedPart()!;
    const description = raw.description.trim() || part.name;

    if (quantity <= 0) {
      this.lineErrors.set('quantity', 'Quantity must be greater than zero.');
      return;
    }
    if (unitPrice < 0) {
      this.lineErrors.set('unitPrice', 'Unit price cannot be negative.');
      return;
    }
    if (quantity > part.quantityOnHand) {
      this.lineErrors.set('quantity', `Only ${part.quantityOnHand} on hand for this part.`);
      return;
    }

    this.addingPurchase.set(true);

    this.persistHeader(j.id)
      .pipe(
        switchMap(() =>
          this.jobsApi.addPurchase(j.id, {
            source: 'Inventory',
            inventoryItemId: part.id,
            description,
            quantity,
            unitPrice,
            vendorName: null,
          }),
        ),
      )
      .subscribe({
        next: (updated) => {
          this.applySale(updated);
          this.addingPurchase.set(false);
          this.lineForm.reset({ description: '', quantity: 1, unitPrice: 0 });
          this.selectedPart.set(null);
          this.lineErrors.clear();
        },
        error: (err: HttpErrorResponse) => {
          this.lineErrors.set('submit', err.error?.message ?? 'Could not add line.');
          this.addingPurchase.set(false);
        },
      });
  }

  payAndPrint(): void {
    const j = this.sale();
    if (!j || !this.canPay()) return;

    this.payErrors.clear();
    const method = this.payForm.controls.paymentMethod.value.trim();
    if (!isPaymentMethod(method)) {
      this.payErrors.set('paymentMethod', 'Select a payment method.');
      return;
    }

    this.paying.set(true);

    this.persistHeader(j.id)
      .pipe(
        switchMap(() => this.salesApi.complete(j.id, 'Shop sale paid.')),
        switchMap((completed) => this.jobsApi.closeJob(completed.id, method)),
      )
      .subscribe({
        next: (closed) => {
          this.applySale(closed);
          this.paying.set(false);
          this.showBill.set(true);
          setTimeout(() => window.print(), 300);
        },
        error: (err: HttpErrorResponse) => {
          this.payErrors.set('paymentMethod', err.error?.message ?? 'Could not finish sale.');
          this.paying.set(false);
        },
      });
  }

  printBill(): void {
    this.showBill.set(true);
    setTimeout(() => window.print(), 150);
  }

  persistHeader(saleId: string) {
    const raw = this.headerForm.getRawValue();
    return this.salesApi.update(saleId, {
      storeCustomerName: raw.storeCustomerName.trim() || null,
      notes: raw.notes.trim() || null,
    });
  }

  lineFieldInvalid(field: 'quantity' | 'unitPrice'): boolean {
    return !!this.lineErrors.get(field);
  }

  officerName(): string {
    return this.auth.displayName();
  }

  billDateUtc(j: JobDetail): string {
    return j.closedAtUtc ?? j.invoice?.finalizedAtUtc ?? j.completedAtUtc ?? j.createdAtUtc;
  }

  paymentNote(j: JobDetail): string | null {
    const closed = [...j.statusHistory].reverse().find((h) => h.toStatus === 'Closed');
    const note = closed?.note?.trim();
    if (!note) return j.status === 'Closed' ? 'Payment received' : null;
    if (note === 'Payment received; job closed.') return 'Payment received';
    if (isPaymentMethod(note)) return note;
    return note;
  }

  isPaid(j: JobDetail): boolean {
    return j.status === 'Closed';
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
}
