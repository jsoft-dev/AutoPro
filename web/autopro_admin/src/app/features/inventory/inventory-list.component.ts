import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

import { InventoryService } from '../../services/inventory.service';
import { SuppliersService } from '../../services/suppliers.service';
import { InventoryListItem, InventoryImportResult } from '../../models/inventory.models';
import { SupplierOption } from '../../models/supplier.models';
import { DEFAULT_PAGE_SIZE } from '../../models/pagination.models';
import { PaginationComponent } from '../../shared/pagination.component';

@Component({
  selector: 'app-inventory-list',
  standalone: true,
  imports: [RouterLink, DatePipe, CurrencyPipe, FormsModule, PaginationComponent],
  templateUrl: './inventory-list.component.html',
  styleUrl: './inventory-list.component.scss',
})
export class InventoryListComponent implements OnInit {
  private readonly inventoryApi = inject(InventoryService);
  private readonly suppliersApi = inject(SuppliersService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly items = signal<InventoryListItem[]>([]);
  readonly searchQuery = signal('');
  readonly showInactive = signal(false);
  readonly supplierFilterId = signal('');
  readonly supplierOptions = signal<SupplierOption[]>([]);
  readonly page = signal(1);
  readonly pageSize = signal(DEFAULT_PAGE_SIZE);
  readonly totalCount = signal(0);

  readonly importing = signal(false);
  readonly downloadingTemplate = signal(false);
  readonly exporting = signal(false);
  readonly importResult = signal<InventoryImportResult | null>(null);
  readonly importError = signal<string | null>(null);
  readonly createSuppliersOnImport = signal(true);

  readonly hasActiveFilters = () =>
    !!this.searchQuery().trim() || !!this.supplierFilterId() || this.showInactive();

  ngOnInit(): void {
    this.loadSupplierOptions();

    const supplierFromUrl = this.route.snapshot.queryParamMap.get('supplierId') ?? '';
    if (supplierFromUrl) {
      this.supplierFilterId.set(supplierFromUrl);
    }

    this.load();
  }

  private loadSupplierOptions(): void {
    this.suppliersApi.list({ includeInactive: true, pageSize: 100 }).subscribe({
      next: (result) => {
        this.supplierOptions.set(
          result.items.map((s) => ({ id: s.id, name: s.isActive ? s.name : `${s.name} (inactive)` })),
        );
      },
      error: () => this.supplierOptions.set([]),
    });
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    const q = this.searchQuery().trim() || undefined;
    const supplierId = this.supplierFilterId().trim() || undefined;
    this.inventoryApi
      .list({
        q,
        supplierId,
        includeInactive: this.showInactive(),
        page: this.page(),
        pageSize: this.pageSize(),
      })
      .subscribe({
        next: (result) => {
          this.items.set(result.items);
          this.page.set(result.page);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.error.set(err.error?.message ?? 'Could not load inventory.');
          this.loading.set(false);
        },
      });
  }

  onSearchSubmit(event: Event): void {
    event.preventDefault();
    this.page.set(1);
    this.load();
  }

  clearSearch(): void {
    this.searchQuery.set('');
    this.page.set(1);
    this.load();
  }

  onSupplierFilterChange(id: string): void {
    this.supplierFilterId.set(id);
    this.page.set(1);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { supplierId: id || null },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
    this.load();
  }

  clearSupplierFilter(): void {
    this.onSupplierFilterChange('');
  }

  onShowInactiveChange(checked: boolean): void {
    this.showInactive.set(checked);
    this.page.set(1);
    this.load();
  }

  clearAllFilters(): void {
    this.searchQuery.set('');
    this.showInactive.set(false);
    this.onSupplierFilterChange('');
  }

  goToPage(nextPage: number): void {
    this.page.set(nextPage);
    this.load();
  }

  exportInventory(): void {
    this.exporting.set(true);
    this.importError.set(null);
    const q = this.searchQuery().trim() || undefined;
    const supplierId = this.supplierFilterId().trim() || undefined;
    this.inventoryApi
      .exportExcel({
        q,
        supplierId,
        includeInactive: this.showInactive(),
      })
      .subscribe({
        next: (blob) => {
          const stamp = new Date().toISOString().slice(0, 16).replace('T', '-').replace(/:/g, '');
          const url = URL.createObjectURL(blob);
          const anchor = document.createElement('a');
          anchor.href = url;
          anchor.download = `inventory-export-${stamp}.xlsx`;
          anchor.click();
          URL.revokeObjectURL(url);
          this.exporting.set(false);
        },
        error: (err: HttpErrorResponse) => {
          void this.setBlobError(err, 'Could not export inventory.');
          this.exporting.set(false);
        },
      });
  }

  private async setBlobError(err: HttpErrorResponse, fallback: string): Promise<void> {
    let message = fallback;
    if (err.error instanceof Blob) {
      try {
        const text = await err.error.text();
        const body = JSON.parse(text) as { message?: string };
        if (body.message) message = body.message;
      } catch {
        /* keep fallback */
      }
    } else if (err.error?.message) {
      message = err.error.message;
    }
    this.importError.set(message);
  }

  downloadImportTemplate(): void {
    this.downloadingTemplate.set(true);
    this.importError.set(null);
    this.inventoryApi.downloadImportTemplate().subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = 'inventory-import-template.xlsx';
        anchor.click();
        URL.revokeObjectURL(url);
        this.downloadingTemplate.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.importError.set(err.error?.message ?? 'Could not download template.');
        this.downloadingTemplate.set(false);
      },
    });
  }

  onImportFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    this.importing.set(true);
    this.importError.set(null);
    this.importResult.set(null);

    this.inventoryApi.importExcel(file, this.createSuppliersOnImport()).subscribe({
      next: (result) => {
        this.importResult.set(result);
        this.importing.set(false);
        this.load();
        this.loadSupplierOptions();
      },
      error: (err: HttpErrorResponse) => {
        this.importError.set(err.error?.message ?? 'Import failed.');
        this.importing.set(false);
      },
    });
  }

  dismissImportSummary(): void {
    this.importResult.set(null);
    this.importError.set(null);
  }
}