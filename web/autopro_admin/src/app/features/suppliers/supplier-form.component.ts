import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

import { SuppliersService } from '../../services/suppliers.service';
import { SupplierWritePayload } from '../../models/supplier.models';
import { FieldErrorComponent } from '../../shared/field-error.component';
import { FieldErrorsStore } from '../../shared/field-errors.store';

@Component({
  selector: 'app-supplier-form',
  standalone: true,
  imports: [RouterLink, FormsModule, FieldErrorComponent],
  templateUrl: './supplier-form.component.html',
  styleUrl: './supplier-form.component.scss',
})
export class SupplierFormComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly suppliersApi = inject(SuppliersService);

  readonly createMode = signal(false);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly pageError = signal<string | null>(null);
  readonly fieldErrors = new FieldErrorsStore();

  name = '';
  contactPerson = '';
  phone = '';
  email = '';
  address = '';
  notes = '';
  isActive = true;

  readonly supplierId = signal<string | null>(null);

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => this.loadSupplier(params.get('id')));
  }

  private loadSupplier(id: string | null): void {
    this.pageError.set(null);
    if (id === 'new' || !id) {
      this.createMode.set(true);
      this.supplierId.set(null);
      this.resetForm();
      this.loading.set(false);
      return;
    }
    this.createMode.set(false);
    this.supplierId.set(id);
    this.loading.set(true);
    this.suppliersApi.getById(id).subscribe({
      next: (item) => {
        this.name = item.name;
        this.contactPerson = item.contactPerson ?? '';
        this.phone = item.phone ?? '';
        this.email = item.email ?? '';
        this.address = item.address ?? '';
        this.notes = item.notes ?? '';
        this.isActive = item.isActive;
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.pageError.set(err.error?.message ?? 'Supplier not found.');
        this.loading.set(false);
      },
    });
  }

  private resetForm(): void {
    this.name = '';
    this.contactPerson = '';
    this.phone = '';
    this.email = '';
    this.address = '';
    this.notes = '';
    this.isActive = true;
  }

  submit(event: Event): void {
    event.preventDefault();
    this.fieldErrors.clear();

    const payload: SupplierWritePayload = {
      name: this.name.trim(),
      contactPerson: this.contactPerson.trim() || null,
      phone: this.phone.trim() || null,
      email: this.email.trim() || null,
      address: this.address.trim() || null,
      notes: this.notes.trim() || null,
      isActive: this.isActive,
    };

    let invalid = false;
    if (!payload.name) {
      this.fieldErrors.set('name', 'Name is required.');
      invalid = true;
    }
    const id = this.supplierId();
    if (!this.createMode() && !id) {
      this.fieldErrors.set('submit', 'Missing supplier id.');
      invalid = true;
    }
    if (invalid) return;

    this.saving.set(true);
    const req = this.createMode()
      ? this.suppliersApi.create(payload)
      : this.suppliersApi.update(id!, payload);

    req.subscribe({
      next: () => {
        this.saving.set(false);
        void this.router.navigate(['/suppliers']);
      },
      error: (err: HttpErrorResponse) => {
        this.fieldErrors.set('submit', err.error?.message ?? 'Could not save supplier.');
        this.saving.set(false);
      },
    });
  }

}