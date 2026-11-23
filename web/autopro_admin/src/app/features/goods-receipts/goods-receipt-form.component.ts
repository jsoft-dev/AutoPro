import { Component, HostListener, inject, signal, OnInit } from '@angular/core';
import { Observable } from 'rxjs';

import { GoodsReceiptDetail } from '../../models/goods-receipt.models';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

import { readApiError } from '../../core/read-api-error';
import { GoodsReceiptsService } from '../../services/goods-receipts.service';
import { PurchaseOrdersService } from '../../services/purchase-orders.service';
import { SuppliersService } from '../../services/suppliers.service';
import { SupplierOption } from '../../models/supplier.models';
import { PurchaseOrderDetail, PurchaseOrderOption } from '../../models/purchase-order.models';
import { InventorySearchItem } from '../../models/inventory.models';
import { ConfirmDialogComponent } from '../../shared/confirm-dialog.component';
import { InventoryPartSearchFieldComponent } from '../../shared/inventory-part-search-field.component';

interface GrnLineDraft {
  inventoryItemId: string;
  partNumber: string;
  partName: string;
  purchaseOrderLineId: string | null;
  quantityReceived: number;
  unitCost: number | null;
  notes: string;
}

@Component({
  selector: 'app-goods-receipt-form',
  standalone: true,
  imports: [RouterLink, FormsModule, InventoryPartSearchFieldComponent, ConfirmDialogComponent],
  templateUrl: './goods-receipt-form.component.html',
  styleUrl: './goods-receipt-form.component.scss',
})
export class GoodsReceiptFormComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly grnApi = inject(GoodsReceiptsService);
  private readonly poApi = inject(PurchaseOrdersService);
  private readonly suppliersApi = inject(SuppliersService);

  readonly createMode = signal(false);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly pageError = signal<string | null>(null);
  readonly grnId = signal<string | null>(null);
  readonly grnNumber = signal('');
  readonly status = signal<string>('Draft');
  readonly readOnly = signal(false);

  supplierOptions = signal<SupplierOption[]>([]);
  poOptions = signal<PurchaseOrderOption[]>([]);
  linkedPo = signal<PurchaseOrderDetail | null>(null);

  supplierId = '';
  purchaseOrderId: string | null = null;
  receivedAtLocal = '';
  supplierReference = '';
  notes = '';
  updatePriceOnPost = false;

  lines = signal<GrnLineDraft[]>([]);
  partPick = signal<InventorySearchItem | null>(null);
  readonly saveMenuOpen = signal(false);
  readonly postConfirmOpen = signal(false);
  readonly cancelConfirmOpen = signal(false);
  private pendingPostPayload: ReturnType<GoodsReceiptFormComponent['buildPayload']> | null = null;

  @HostListener('document:click')
  closeSaveMenu(): void {
    this.saveMenuOpen.set(false);
  }

  ngOnInit(): void {
    this.suppliersApi.options().subscribe({
      next: (opts) => this.supplierOptions.set(opts),
    });
    this.route.paramMap.subscribe((params) => {
      const queryPo = this.route.snapshot.queryParamMap.get('purchaseOrderId');
      this.load(params.get('id'), queryPo);
    });
  }

  private load(id: string | null, initialPoId: string | null): void {
    this.pageError.set(null);
    if (id === 'new' || !id) {
      this.createMode.set(true);
      this.grnId.set(null);
      this.resetForm();
      this.receivedAtLocal = this.formatLocalInput(new Date());
      if (initialPoId) {
        this.purchaseOrderId = initialPoId;
        void this.onPurchaseOrderSelected();
      }
      this.loading.set(false);
      return;
    }
    this.createMode.set(false);
    this.grnId.set(id);
    this.loading.set(true);
    this.grnApi.getById(id).subscribe({
      next: (grn) => {
        this.grnNumber.set(grn.grnNumber);
        this.status.set(grn.status);
        this.readOnly.set(grn.status !== 'Draft');
        this.supplierId = grn.supplierId;
        this.purchaseOrderId = grn.purchaseOrderId;
        this.receivedAtLocal = this.formatLocalInput(new Date(grn.receivedAtUtc));
        this.supplierReference = grn.supplierReference ?? '';
        this.notes = grn.notes ?? '';
        this.lines.set(
          grn.lines.map((l) => ({
            inventoryItemId: l.inventoryItemId,
            partNumber: l.partNumber,
            partName: l.partName,
            purchaseOrderLineId: l.purchaseOrderLineId,
            quantityReceived: l.quantityReceived,
            unitCost: l.unitCost,
            notes: l.notes ?? '',
          })),
        );
        if (grn.purchaseOrderId) void this.onPurchaseOrderSelected();
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.pageError.set(err.error?.message ?? 'Goods receipt not found.');
        this.loading.set(false);
      },
    });
  }

  private resetForm(): void {
    this.supplierId = '';
    this.purchaseOrderId = null;
    this.supplierReference = '';
    this.notes = '';
    this.lines.set([]);
    this.status.set('Draft');
    this.readOnly.set(false);
    this.linkedPo.set(null);
  }

  onSupplierChange(): void {
    this.purchaseOrderId = null;
    this.linkedPo.set(null);
    this.refreshPoOptions();
  }

  refreshPoOptions(): void {
    if (!this.supplierId) {
      this.poOptions.set([]);
      return;
    }
    this.poApi.options(this.supplierId).subscribe({
      next: (opts) => this.poOptions.set(opts),
    });
  }

  async onPurchaseOrderSelected(): Promise<void> {
    if (!this.purchaseOrderId) {
      this.linkedPo.set(null);
      return;
    }
    this.poApi.getById(this.purchaseOrderId).subscribe({
      next: (po) => {
        this.linkedPo.set(po);
        this.supplierId = po.supplierId;
        this.refreshPoOptions();
      },
    });
  }

  addLineFromPicker(): void {
    const pick = this.partPick();
    if (!pick) return;
    this.lines.update((rows) => [
      ...rows,
      {
        inventoryItemId: pick.id,
        partNumber: pick.partNumber,
        partName: pick.name,
        purchaseOrderLineId: null,
        quantityReceived: 1,
        unitCost: pick.unitPrice,
        notes: '',
      },
    ]);
    this.partPick.set(null);
  }

  addLineFromPo(poLineId: string): void {
    const po = this.linkedPo();
    if (!po) return;
    const line = po.lines.find((l) => l.id === poLineId);
    if (!line || line.quantityRemaining <= 0) return;
    if (this.lines().some((l) => l.purchaseOrderLineId === poLineId)) return;
    this.lines.update((rows) => [
      ...rows,
      {
        inventoryItemId: line.inventoryItemId,
        partNumber: line.partNumber,
        partName: line.partName,
        purchaseOrderLineId: line.id,
        quantityReceived: line.quantityRemaining,
        unitCost: line.unitCost,
        notes: '',
      },
    ]);
  }

  removeLine(index: number): void {
    this.lines.update((rows) => rows.filter((_, i) => i !== index));
  }

  private buildPayload() {
    const received = new Date(this.receivedAtLocal);
    if (Number.isNaN(received.getTime())) {
      throw new Error('Enter a valid received date and time.');
    }

    const poLinked = !!this.purchaseOrderId?.trim();

    return {
      supplierId: this.supplierId,
      purchaseOrderId: poLinked ? this.purchaseOrderId : null,
      receivedAtUtc: received.toISOString(),
      supplierReference: this.supplierReference.trim() || null,
      notes: this.notes.trim() || null,
      lines: this.lines().map((l) => ({
        inventoryItemId: l.inventoryItemId,
        purchaseOrderLineId: poLinked ? l.purchaseOrderLineId : null,
        quantityReceived: Number(l.quantityReceived),
        unitCost:
          l.unitCost === null || l.unitCost === undefined || (l.unitCost as unknown) === ''
            ? null
            : Number(l.unitCost),
        notes: (l.notes ?? '').trim() || null,
      })),
    };
  }

  toggleSaveMenu(event: Event): void {
    event.stopPropagation();
    if (this.saving()) return;
    this.saveMenuOpen.update((open) => !open);
  }

  saveAsDraft(event?: Event): void {
    event?.preventDefault();
    if (this.readOnly() || this.saving()) return;
    this.saveMenuOpen.set(false);

    const validated = this.validateForm();
    if (!validated.ok) {
      this.pageError.set(validated.error);
      return;
    }

    this.saving.set(true);
    this.persistDraft(validated.payload).subscribe({
      next: () => {
        this.saving.set(false);
        void this.router.navigate(['/inventory/goods-receipts']);
      },
      error: (err: HttpErrorResponse) => {
        this.pageError.set(readApiError(err, 'Could not save GRN.'));
        this.saving.set(false);
      },
    });
  }

  saveAndPost(): void {
    if (this.readOnly() || this.saving()) return;
    this.saveMenuOpen.set(false);

    const validated = this.validateForm({ forPost: true });
    if (!validated.ok) {
      this.pageError.set(validated.error);
      return;
    }

    this.pendingPostPayload = validated.payload;
    this.postConfirmOpen.set(true);
  }

  onPostConfirmDismiss(): void {
    this.postConfirmOpen.set(false);
    this.pendingPostPayload = null;
  }

  onPostConfirm(): void {
    const payload = this.pendingPostPayload;
    this.postConfirmOpen.set(false);
    this.pendingPostPayload = null;
    if (!payload) return;

    this.saving.set(true);
    this.persistDraft(payload).subscribe({
      next: (grn) => {
        this.grnApi.post(grn.id, this.updatePriceOnPost).subscribe({
          next: () => {
            this.saving.set(false);
            void this.router.navigate(['/inventory/goods-receipts']);
          },
          error: (err: HttpErrorResponse) => {
            this.pageError.set(readApiError(err, 'GRN was saved as draft but could not be posted.'));
            this.saving.set(false);
            this.grnId.set(grn.id);
            this.createMode.set(false);
            void this.router.navigate(['/inventory/goods-receipts', grn.id]);
          },
        });
      },
      error: (err: HttpErrorResponse) => {
        this.pageError.set(readApiError(err, 'Could not save GRN.'));
        this.saving.set(false);
      },
    });
  }

  cancelGrn(): void {
    const id = this.grnId();
    if (!id || this.saving()) return;
    this.cancelConfirmOpen.set(true);
  }

  onCancelConfirm(): void {
    const id = this.grnId();
    this.cancelConfirmOpen.set(false);
    if (!id) return;

    this.saving.set(true);
    this.grnApi.cancel(id).subscribe({
      next: () => {
        this.saving.set(false);
        void this.router.navigate(['/inventory/goods-receipts']);
      },
      error: (err: HttpErrorResponse) => {
        this.pageError.set(readApiError(err, 'Could not cancel GRN.'));
        this.saving.set(false);
      },
    });
  }

  private validateForm(options?: { forPost: boolean }):
    | { ok: true; payload: ReturnType<GoodsReceiptFormComponent['buildPayload']> }
    | { ok: false; error: string } {
    this.pageError.set(null);

    if (!this.supplierId?.trim()) {
      return { ok: false, error: 'Select a supplier.' };
    }
    if (this.lines().length === 0) {
      return { ok: false, error: 'Add at least one line.' };
    }
    if (this.purchaseOrderId && this.lines().some((l) => !l.purchaseOrderLineId)) {
      return { ok: false, error: 'Each line must come from the PO when a purchase order is linked.' };
    }

    if (options?.forPost) {
      for (const line of this.lines()) {
        const qty = Number(line.quantityReceived);
        if (!Number.isFinite(qty) || qty <= 0) {
          return {
            ok: false,
            error: 'Each line needs a quantity greater than zero before posting.',
          };
        }
      }
    }

    try {
      return { ok: true, payload: this.buildPayload() };
    } catch (e) {
      return { ok: false, error: e instanceof Error ? e.message : 'Invalid form data.' };
    }
  }

  private persistDraft(payload: ReturnType<GoodsReceiptFormComponent['buildPayload']>): Observable<GoodsReceiptDetail> {
    const id = this.grnId();
    return this.createMode() ? this.grnApi.create(payload) : this.grnApi.update(id!, payload);
  }

  postConfirmSupplierName(): string {
    return this.supplierOptions().find((s) => s.id === this.supplierId)?.name ?? '—';
  }

  postConfirmLineCount(): number {
    return this.lines().length;
  }

  postConfirmTotalQty(): number {
    return this.lines().reduce((sum, l) => sum + Number(l.quantityReceived), 0);
  }

  private formatLocalInput(date: Date): string {
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }
}