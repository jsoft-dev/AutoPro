import { Component, inject, signal, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

import { VehiclesService } from '../../services/vehicles.service';
import { VehicleListItem, vehicleSummary } from '../../models/vehicle.models';

@Component({
  selector: 'app-vehicle-list',
  standalone: true,
  imports: [RouterLink, DatePipe, FormsModule],
  templateUrl: './vehicle-list.component.html',
  styleUrl: './vehicle-list.component.scss',
})
export class VehicleListComponent implements OnInit {
  private readonly vehiclesApi = inject(VehiclesService);

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly items = signal<VehicleListItem[]>([]);
  readonly searchQuery = signal('');

  readonly vehicleSummary = vehicleSummary;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    const q = this.searchQuery().trim() || undefined;
    this.vehiclesApi.list({ q }).subscribe({
      next: (list) => {
        this.items.set(list);
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
    this.load();
  }

  clearSearch(): void {
    this.searchQuery.set('');
    this.load();
  }
}
