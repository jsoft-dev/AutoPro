import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

import { PurchaseOrdersService } from '../../services/purchase-orders.service';
import { SuppliersService } from '../../services/suppliers.service';
import { SupplierOption } from '../../models/supplier.models';
import { InventorySearchItem } from '../../models/inventory.models';
import { InventoryPartSearchFieldComponent } from '../../shared/inventory-part-search-field.component';
interface PoLineDraft {
  lineId?: string;
  inventoryItemId: string;
  partNumber: string;
  partName: string;
  quantityOrdered: number;
  quantityReceived: number;
  quantityRemaining: number;
  unitCost: number | null;
  notes: string;
}

@Component({
  selector: 'app-purchase-order-form',
  standalone: true,
  imports: [RouterLink, FormsModule, InventoryPartSearchFieldComponent],
  templateUrl: './purchase-order-form.component.html',
  styleUrl: './purchase-order-form.component.scss',
})
export class PurchaseOrderFormComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly poApi = inject(PurchaseOrdersService);
  private readonly suppliersApi = inject(SuppliersService);

  readonly createMode = signal(false);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly pageError = signal<string | null>(null);
  readonly poId = signal<string | null>(null);
  readonly status = signal<string>('Draft');
  readonly poNumber = signal('');
  readonly readOnly = signal(false);

  supplierOptions = signal<SupplierOption[]>([]);
  supplierId = '';
  notes = '';
  lines = signal<PoLineDraft[]>([]);
  partPick = signal<InventorySearchItem | null>(null);

  ngOnInit(): void {
    this.suppliersApi.options().subscribe({
      next: (opts) => this.supplierOptions.set(opts),
    });
    this.route.paramMap.subscribe((params) => this.load(params.get('id')));
  }

  private load(id: string | null): void {
    this.pageError.set(null);
    if (id === 'new' || !id) {
      this.createMode.set(true);
      this.poId.set(null);
      this.resetForm();
      this.loading.set(false);
      return;
    }
    this.createMode.set(false);
    this.poId.set(id);
    this.loading.set(true);
    this.poApi.getById(id).subscribe({
      next: (po) => {
        this.poNumber.set(po.poNumber);
        this.status.set(po.status);
        this.readOnly.set(po.status !== 'Draft');
        this.supplierId = po.supplierId;
        this.notes = po.notes ?? '';
        this.lines.set(
          po.lines.map((l) => ({
            lineId: l.id,
            inventoryItemId: l.inventoryItemId,
            partNumber: l.partNumber,
            partName: l.partName,
            quantityOrdered: l.quantityOrdered,
            quantityReceived: l.quantityReceived,
            quantityRemaining: l.quantityRemaining,
            unitCost: l.unitCost,
            notes: l.notes ?? '',
          })),
        );
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.pageError.set(err.error?.message ?? 'Purchase order not found.');
        this.loading.set(false);
      },
    });
  }

  private resetForm(): void {
    this.supplierId = '';
    this.notes = '';
    this.lines.set([]);
    this.status.set('Draft');
    this.readOnly.set(false);
  }

  addLineFromPicker(): void {
    const pick = this.partPick();
    if (!pick) return;
    if (this.lines().some((l) => l.inventoryItemId === pick.id)) {
      this.pageError.set('That part is already on this PO.');
      return;
    }
    this.lines.update((rows) => [
      ...rows,
      {
        inventoryItemId: pick.id,
        partNumber: pick.partNumber,
        partName: pick.name,
        quantityOrdered: 1,
        quantityReceived: 0,
        quantityRemaining: 0,
        unitCost: pick.unitPrice,
        notes: '',
      },
    ]);
    this.partPick.set(null);
    this.pageError.set(null);
  }

  removeLine(index: number): void {
    this.lines.update((rows) => rows.filter((_, i) => i !== index));
  }

  submit(event: Event): void {
    event.preventDefault();
    if (this.readOnly()) return;
    this.pageError.set(null);
    if (!this.supplierId) {
      this.pageError.set('Select a supplier.');
      return;
    }
    if (this.lines().length === 0) {
      this.pageError.set('Add at least one line.');
      return;
    }

    const payload = {
      supplierId: this.supplierId,
      notes: this.notes.trim() || null,
      lines: this.lines().map((l) => ({
        inventoryItemId: l.inventoryItemId,
        quantityOrdered: l.quantityOrdered,
        unitCost: l.unitCost,
        notes: l.notes.trim() || null,
      })),
    };

    this.saving.set(true);
    const id = this.poId();
    const req = this.createMode()
      ? this.poApi.create(payload)
      : this.poApi.update(id!, payload);

    req.subscribe({
      next: (po) => {
        this.saving.set(false);
        void this.router.navigate(['/inventory/purchase-orders', po.id]);
      },
      error: (err: HttpErrorResponse) => {
        this.pageError.set(err.error?.message ?? 'Could not save purchase order.');
        this.saving.set(false);
      },
    });
  }

  approve(): void {
    const id = this.poId();
    if (!id) return;
    this.saving.set(true);
    this.poApi.approve(id).subscribe({
      next: () => {
        this.saving.set(false);
        this.load(id);
      },
      error: (err: HttpErrorResponse) => {
        this.pageError.set(err.error?.message ?? 'Could not approve.');
        this.saving.set(false);
      },
    });
  }

  cancelPo(): void {
    const id = this.poId();
    if (!id) return;
    this.saving.set(true);
    this.poApi.cancel(id).subscribe({
      next: () => this.load(id),
      error: (err: HttpErrorResponse) => {
        this.pageError.set(err.error?.message ?? 'Could not cancel.');
        this.saving.set(false);
      },
    });
  }
}