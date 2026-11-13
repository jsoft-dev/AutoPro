import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { switchMap } from 'rxjs/operators';
import { of } from 'rxjs';

import { CustomersService } from '../../services/customers.service';
import { CustomerDetail, CustomerWritePayload, customerVehicleSummary } from '../../models/customer.models';
import { FieldErrorComponent } from '../../shared/field-error.component';
import { FieldErrorsStore } from '../../shared/field-errors.store';

@Component({
  selector: 'app-customer-detail',
  standalone: true,
  imports: [RouterLink, DatePipe, FormsModule, FieldErrorComponent],
  templateUrl: './customer-detail.component.html',
  styleUrl: './customer-detail.component.scss',
})
export class CustomerDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly customersApi = inject(CustomersService);

  readonly customer = signal<CustomerDetail | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly pageError = signal<string | null>(null);
  readonly fieldErrors = new FieldErrorsStore();
  readonly saved = signal(false);

  readonly customerVehicleSummary = customerVehicleSummary;

  fullName = '';
  phone = '';
  email = '';

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        switchMap((params) => {
          const id = params.get('id');
          if (!id) return of(null);
          this.loading.set(true);
          this.pageError.set(null);
          this.saved.set(false);
          return this.customersApi.getById(id);
        }),
      )
      .subscribe({
        next: (detail) => {
          if (!detail) {
            this.pageError.set('Customer not found.');
            this.loading.set(false);
            return;
          }
          this.applyDetail(detail);
          this.loading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.pageError.set(err.error?.message ?? 'Could not load customer.');
          this.loading.set(false);
        },
      });
  }

  submit(event: Event): void {
    event.preventDefault();
    const current = this.customer();
    if (!current) return;

    this.fieldErrors.clear();
    this.saved.set(false);

    const payload: CustomerWritePayload = {
      fullName: this.fullName.trim(),
      phone: this.phone.trim(),
      email: this.email.trim() || null,
    };

    let invalid = false;
    if (!payload.fullName) {
      this.fieldErrors.set('fullName', 'Name is required.');
      invalid = true;
    }
    if (!payload.phone) {
      this.fieldErrors.set('phone', 'Phone is required.');
      invalid = true;
    }
    if (invalid) return;

    this.saving.set(true);
    this.customersApi.update(current.id, payload).subscribe({
      next: (detail) => {
        this.applyDetail(detail);
        this.saving.set(false);
        this.saved.set(true);
      },
      error: (err: HttpErrorResponse) => {
        this.fieldErrors.set('submit', err.error?.message ?? 'Could not save customer.');
        this.saving.set(false);
      },
    });
  }

  private applyDetail(detail: CustomerDetail): void {
    this.customer.set(detail);
    this.fullName = detail.fullName;
    this.phone = detail.phone;
    this.email = detail.email ?? '';
  }
}
