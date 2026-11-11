import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe, CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';

import { JobsService } from '../../services/jobs.service';
import { JobListItem, JobStatus, statusLabel } from '../../models/job.models';
import { JobStatusChipComponent } from '../../shared/job-status-chip.component';

type FilterKey = 'today' | 'ready' | 'active' | 'all';

@Component({
  selector: 'app-job-list',
  standalone: true,
  imports: [RouterLink, DatePipe, JobStatusChipComponent],
  templateUrl: './job-list.component.html',
  styleUrl: './job-list.component.scss',
})
export class JobListComponent implements OnInit {
  private readonly jobsApi = inject(JobsService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly items = signal<JobListItem[]>([]);
  readonly filter = signal<FilterKey>('today');

  readonly filters: { key: FilterKey; label: string; description: string }[] = [
    { key: 'today', label: 'Today', description: 'Jobs opened today' },
    { key: 'ready', label: 'Ready for payment', description: 'Completed — awaiting close' },
    { key: 'active', label: 'In workshop', description: 'On going or on hold' },
    { key: 'all', label: 'Recent', description: 'Latest jobs (up to 100)' },
  ];

  ngOnInit(): void {
    this.load();
  }

  setFilter(key: FilterKey): void {
    this.filter.set(key);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    const f = this.filter();

    const handle = (list: JobListItem[]) => {
      let result = list;
      if (f === 'active') {
        result = list.filter((j) => j.status === 'OnGoing' || j.status === 'OnHold');
      }
      this.items.set(result);
      this.loading.set(false);
    };

    const onError = (err: HttpErrorResponse) => {
      this.error.set(err.error?.message ?? 'Could not load jobs.');
      this.loading.set(false);
    };

    if (f === 'today') {
      this.jobsApi.listToday().subscribe({ next: handle, error: onError });
      return;
    }
    if (f === 'ready') {
      this.jobsApi.list({ status: 'Completed' }).subscribe({ next: handle, error: onError });
      return;
    }
    this.jobsApi.list().subscribe({ next: handle, error: onError });
  }

  statusLabel = statusLabel;
}
