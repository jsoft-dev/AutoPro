import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

import { InventoryService } from '../../services/inventory.service';
import { InventoryListItem } from '../../models/inventory.models';
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

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly items = signal<InventoryListItem[]>([]);
  readonly searchQuery = signal('');
  readonly showInactive = signal(false);
  readonly page = signal(1);
  readonly pageSize = signal(DEFAULT_PAGE_SIZE);
  readonly totalCount = signal(0);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    const q = this.searchQuery().trim() || undefined;
    this.inventoryApi
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

  toggleInactive(): void {
    this.showInactive.update((v) => !v);
    this.page.set(1);
    this.load();
  }

  goToPage(nextPage: number): void {
    this.page.set(nextPage);
    this.load();
  }
}
