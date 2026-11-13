import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';

import { SalesService } from '../../services/sales.service';
import { JobListItem, JobStatus, statusLabel } from '../../models/job.models';
import { DEFAULT_PAGE_SIZE } from '../../models/pagination.models';
import { JobStatusChipComponent } from '../../shared/job-status-chip.component';
import { PaginationComponent } from '../../shared/pagination.component';

type FilterKey = 'open' | 'ready' | 'all';

@Component({
  selector: 'app-sale-list',
  standalone: true,
  imports: [RouterLink, DatePipe, JobStatusChipComponent, PaginationComponent],
  templateUrl: './sale-list.component.html',
  styleUrl: './sale-list.component.scss',
})
export class SaleListComponent implements OnInit {
  private readonly salesApi = inject(SalesService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly items = signal<JobListItem[]>([]);
  readonly filter = signal<FilterKey>('open');
  readonly page = signal(1);
  readonly pageSize = signal(DEFAULT_PAGE_SIZE);
  readonly totalCount = signal(0);

  readonly filters: { key: FilterKey; label: string; description: string }[] = [
    { key: 'open', label: 'Open', description: 'Adding items — invoice not finalized' },
    { key: 'ready', label: 'Ready for payment', description: 'Completed — awaiting payment' },
    { key: 'all', label: 'All', description: 'Every counter sale' },
  ];

  ngOnInit(): void {
    this.load();
  }

  setFilter(key: FilterKey): void {
    this.filter.set(key);
    this.page.set(1);
    this.load();
  }

  goToPage(p: number): void {
    this.page.set(p);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    const f = this.filter();
    const page = this.page();
    const pageSize = this.pageSize();

    const options =
      f === 'open'
        ? { status: 'OnGoing' as JobStatus, page, pageSize }
        : f === 'ready'
          ? { status: 'Completed' as JobStatus, page, pageSize }
          : { page, pageSize };

    this.salesApi.list(options).subscribe({
      next: (result) => {
        this.items.set(result.items);
        this.page.set(result.page);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(err.error?.message ?? 'Could not load sales.');
        this.loading.set(false);
      },
    });
  }

  statusLabel = statusLabel;
}
