import { Component, HostListener, input, output } from '@angular/core';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  templateUrl: './confirm-dialog.component.html',
  styleUrl: './confirm-dialog.component.scss',
})
export class ConfirmDialogComponent {
  readonly open = input(false);
  readonly title = input('Confirm');
  readonly confirmLabel = input('Confirm');
  readonly cancelLabel = input('Cancel');
  readonly confirmDanger = input(false);
  readonly confirmDisabled = input(false);

  readonly confirmed = output<void>();
  readonly cancelled = output<void>();

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.open()) this.cancelled.emit();
  }

  onBackdropClick(): void {
    this.cancelled.emit();
  }

  stopClick(event: Event): void {
    event.stopPropagation();
  }
}