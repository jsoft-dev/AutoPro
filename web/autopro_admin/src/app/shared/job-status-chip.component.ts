import { Component, input } from '@angular/core';
import { JobStatus, statusLabel } from '../models/job.models';

@Component({
  selector: 'app-job-status-chip',
  standalone: true,
  template: `<span class="chip" [attr.data-status]="status()">{{ label() }}</span>`,
  styles: [
    `
      .chip {
        display: inline-block;
        padding: 0.2rem 0.55rem;
        border-radius: 999px;
        font-size: 0.75rem;
        font-weight: 600;
        letter-spacing: 0.02em;
        text-transform: uppercase;
      }
      .chip[data-status='OnGoing'] {
        background: #e3f2fd;
        color: #0d47a1;
      }
      .chip[data-status='OnHold'] {
        background: #fff3e0;
        color: #e65100;
      }
      .chip[data-status='Completed'] {
        background: #e8f5e9;
        color: #1b5e20;
      }
      .chip[data-status='Closed'] {
        background: #eceff1;
        color: #37474f;
      }
      .chip[data-status='Cancelled'] {
        background: #ffebee;
        color: #b71c1c;
      }
    `,
  ],
})
export class JobStatusChipComponent {
  status = input.required<JobStatus>();

  label(): string {
    return statusLabel(this.status());
  }
}
