import { Component, computed, input, output } from '@angular/core';

import { pageRangeLabel, totalPages } from '../models/pagination.models';

@Component({
  selector: 'app-pagination',
  standalone: true,
  templateUrl: './pagination.component.html',
  styleUrl: './pagination.component.scss',
})
export class PaginationComponent {
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly totalCount = input.required<number>();
  readonly disabled = input(false);

  readonly pageChange = output<number>();

  readonly rangeLabel = computed(() =>
    pageRangeLabel(this.page(), this.pageSize(), this.totalCount()),
  );

  readonly totalPages = computed(() => totalPages({ pageSize: this.pageSize(), totalCount: this.totalCount() }));

  readonly canPrev = computed(() => this.page() > 1);
  readonly canNext = computed(() => this.page() < this.totalPages());

  prev(): void {
    if (this.canPrev() && !this.disabled()) {
      this.pageChange.emit(this.page() - 1);
    }
  }

  next(): void {
    if (this.canNext() && !this.disabled()) {
      this.pageChange.emit(this.page() + 1);
    }
  }
}
