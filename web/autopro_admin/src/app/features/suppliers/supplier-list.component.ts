import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

import { SuppliersService } from '../../services/suppliers.service';
import { SupplierListItem } from '../../models/supplier.models';
import { DEFAULT_PAGE_SIZE } from '../../models/pagination.models';
import { PaginationComponent } from '../../shared/pagination.component';

@Component({
  selector: 'app-supplier-list',
  standalone: true,
  imports: [RouterLink, DatePipe, FormsModule, PaginationComponent],
  templateUrl: './supplier-list.component.html',
  styleUrl: './supplier-list.component.scss',
})
export class SupplierListComponent implements OnInit {
  private readonly suppliersApi = inject(SuppliersService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly items = signal<SupplierListItem[]>([]);
  readonly searchQuery = signal('');
  readonly showInactive = signal(false);
  readonly page = signal(1);
  readonly pageSize = signal(DEFAULT_PAGE_SIZE);
  readonly totalCount = signal(0);

  readonly hasActiveFilters = () => !!this.searchQuery().trim() || this.showInactive();

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    const q = this.searchQuery().trim() || undefined;
    this.suppliersApi
      .list({
        q,
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
          this.error.set(err.error?.message ?? 'Could not load suppliers.');
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

  onShowInactiveChange(checked: boolean): void {
    this.showInactive.set(checked);
    this.page.set(1);
    this.load();
  }

  clearAllFilters(): void {
    this.searchQuery.set('');
    this.showInactive.set(false);
    this.page.set(1);
    this.load();
  }

  goToPage(nextPage: number): void {
    this.page.set(nextPage);
    this.load();
  }
}