import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

import { GoodsReceiptsService } from '../../services/goods-receipts.service';
import { GoodsReceiptListItem } from '../../models/goods-receipt.models';
import { DEFAULT_PAGE_SIZE } from '../../models/pagination.models';
import { PaginationComponent } from '../../shared/pagination.component';

@Component({
  selector: 'app-goods-receipt-list',
  standalone: true,
  imports: [RouterLink, DatePipe, FormsModule, PaginationComponent],
  templateUrl: './goods-receipt-list.component.html',
  styleUrl: './goods-receipt-list.component.scss',
})
export class GoodsReceiptListComponent implements OnInit {
  private readonly api = inject(GoodsReceiptsService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly items = signal<GoodsReceiptListItem[]>([]);
  readonly searchQuery = signal('');
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
    this.api
      .list({ q, page: this.page(), pageSize: this.pageSize() })
      .subscribe({
        next: (result) => {
          this.items.set(result.items);
          this.page.set(result.page);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.error.set(err.error?.message ?? 'Could not load goods receipts.');
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
}