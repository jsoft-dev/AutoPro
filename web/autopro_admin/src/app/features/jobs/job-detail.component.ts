import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe, CurrencyPipe, DecimalPipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { switchMap } from 'rxjs/operators';
import { of } from 'rxjs';

import { JobsService } from '../../services/jobs.service';
import { JobDetail, lineTotal, statusLabel } from '../../models/job.models';
import { JobStatusChipComponent } from '../../shared/job-status-chip.component';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-job-detail',
  standalone: true,
  imports: [
    RouterLink,
    DatePipe,
    CurrencyPipe,
    DecimalPipe,
    ReactiveFormsModule,
    JobStatusChipComponent,
  ],
  templateUrl: './job-detail.component.html',
  styleUrl: './job-detail.component.scss',
})
export class JobDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly jobsApi = inject(JobsService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  readonly job = signal<JobDetail | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly closing = signal(false);
  readonly showBill = signal(false);

  readonly closeForm = this.fb.nonNullable.group({
    paymentNote: [''],
  });

  readonly lineTotal = lineTotal;
  readonly statusLabel = statusLabel;

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        switchMap((params) => {
          const id = params.get('id');
          if (!id) return of(null);
          this.loading.set(true);
          this.error.set(null);
          return this.jobsApi.getById(id);
        }),
      )
      .subscribe({
        next: (detail) => {
          if (!detail) {
            this.error.set('Job not found.');
            this.loading.set(false);
            return;
          }
          this.job.set(detail);
          this.loading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.error.set(err.error?.message ?? 'Could not load job.');
          this.loading.set(false);
        },
      });
  }

  canClose(): boolean {
    return this.job()?.status === 'Completed';
  }

  canReprint(): boolean {
    const s = this.job()?.status;
    return s === 'Closed' || s === 'Completed';
  }

  receivePaymentAndClose(): void {
    const j = this.job();
    if (!j || !this.canClose()) return;
    this.closing.set(true);
    this.error.set(null);
    const note = this.closeForm.controls.paymentNote.value.trim() || undefined;
    this.jobsApi.closeJob(j.id, note).subscribe({
      next: (updated) => {
        this.job.set(updated);
        this.closing.set(false);
        this.showBill.set(true);
        setTimeout(() => window.print(), 300);
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(err.error?.message ?? 'Could not close job.');
        this.closing.set(false);
      },
    });
  }

  printBill(): void {
    this.showBill.set(true);
    setTimeout(() => window.print(), 100);
  }

  officerName(): string {
    return this.auth.displayName();
  }
}
