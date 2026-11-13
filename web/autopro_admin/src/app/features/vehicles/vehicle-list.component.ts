import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

import { VehiclesService } from '../../services/vehicles.service';
import { VehicleListItem, vehicleSummary } from '../../models/vehicle.models';
import { DEFAULT_PAGE_SIZE } from '../../models/pagination.models';
import { PaginationComponent } from '../../shared/pagination.component';

@Component({
  selector: 'app-vehicle-list',
  standalone: true,
  imports: [RouterLink, DatePipe, FormsModule, PaginationComponent],
  templateUrl: './vehicle-list.component.html',
  styleUrl: './vehicle-list.component.scss',
})
export class VehicleListComponent implements OnInit {
  private readonly vehiclesApi = inject(VehiclesService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly items = signal<VehicleListItem[]>([]);
  readonly searchQuery = signal('');
  readonly page = signal(1);
  readonly pageSize = signal(DEFAULT_PAGE_SIZE);
  readonly totalCount = signal(0);

  readonly vehicleSummary = vehicleSummary;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    const q = this.searchQuery().trim() || undefined;
    this.vehiclesApi
      .list({ q, page: this.page(), pageSize: this.pageSize() })
      .subscribe({
        next: (result) => {
          this.items.set(result.items);
          this.page.set(result.page);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.error.set(err.error?.message ?? 'Could not load vehicles.');
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

  goToPage(nextPage: number): void {
    this.page.set(nextPage);
    this.load();
  }
}
