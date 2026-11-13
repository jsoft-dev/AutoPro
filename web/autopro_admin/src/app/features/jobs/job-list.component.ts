import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';

import { JobsService } from '../../services/jobs.service';
import { JobListItem, JobStatus, statusLabel } from '../../models/job.models';
import { DEFAULT_PAGE_SIZE } from '../../models/pagination.models';
import { JobStatusChipComponent } from '../../shared/job-status-chip.component';
import { PaginationComponent } from '../../shared/pagination.component';

type FilterKey = 'today' | 'ready' | 'active' | 'all';

@Component({
  selector: 'app-job-list',
  standalone: true,
  imports: [RouterLink, DatePipe, JobStatusChipComponent, PaginationComponent],
  templateUrl: './job-list.component.html',
  styleUrl: './job-list.component.scss',
})
export class JobListComponent implements OnInit {
  private readonly jobsApi = inject(JobsService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly items = signal<JobListItem[]>([]);
  readonly filter = signal<FilterKey>('today');
  readonly page = signal(1);
  readonly pageSize = signal(DEFAULT_PAGE_SIZE);
  readonly totalCount = signal(0);

  readonly filters: { key: FilterKey; label: string; description: string }[] = [
    { key: 'today', label: 'Today', description: 'Opened today — newest first' },
    { key: 'ready', label: 'Ready for payment', description: 'Completed — awaiting close' },
    { key: 'active', label: 'In workshop', description: 'On going or on hold' },
    { key: 'all', label: 'All', description: 'All jobs — newest first' },
  ];

  ngOnInit(): void {
    this.load();
  }

  setFilter(key: FilterKey): void {
    this.filter.set(key);
    this.page.set(1);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    const f = this.filter();

    const onError = (err: HttpErrorResponse) => {
      this.error.set(err.error?.message ?? 'Could not load jobs.');
      this.loading.set(false);
    };

    const page = this.page();
    const pageSize = this.pageSize();

    const options =
      f === 'today'
        ? { ...utcTodayBounds(), page, pageSize }
        : f === 'ready'
          ? { status: 'Completed' as JobStatus, page, pageSize }
          : f === 'active'
            ? { inWorkshop: true, page, pageSize }
            : { page, pageSize };

    this.jobsApi.list(options).subscribe({
      next: (result) => {
        this.items.set(result.items);
        this.page.set(result.page);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: onError,
    });
  }

  goToPage(nextPage: number): void {
    this.page.set(nextPage);
    this.load();
  }

  statusLabel = statusLabel;
}

/** Matches API `GET /jobs/today` (UTC calendar day). */
function utcTodayBounds(): { from: string; to: string } {
  const start = new Date();
  start.setUTCHours(0, 0, 0, 0);
  const end = new Date(start);
  end.setUTCDate(end.getUTCDate() + 1);
  return { from: start.toISOString(), to: end.toISOString() };
}
