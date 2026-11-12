import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { switchMap } from 'rxjs/operators';
import { of } from 'rxjs';
import QRCode from 'qrcode';

import { VehiclesService } from '../../services/vehicles.service';
import { VehicleDetail, vehicleSummary } from '../../models/vehicle.models';

@Component({
  selector: 'app-vehicle-detail',
  standalone: true,
  imports: [RouterLink, DatePipe],
  templateUrl: './vehicle-detail.component.html',
  styleUrl: './vehicle-detail.component.scss',
})
export class VehicleDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly vehiclesApi = inject(VehiclesService);

  readonly vehicle = signal<VehicleDetail | null>(null);
  readonly qrDataUrl = signal<string | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly showPrint = signal(false);

  readonly vehicleSummary = vehicleSummary;

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        switchMap((params) => {
          const id = params.get('id');
          if (!id) return of(null);
          this.loading.set(true);
          this.error.set(null);
          this.qrDataUrl.set(null);
          return this.vehiclesApi.getById(id);
        }),
      )
      .subscribe({
        next: async (detail) => {
          if (!detail) {
            this.error.set('Vehicle not found.');
            this.loading.set(false);
            return;
          }
          this.vehicle.set(detail);
          try {
            const dataUrl = await QRCode.toDataURL(detail.qrPayload, {
              width: 280,
              margin: 2,
              errorCorrectionLevel: 'M',
            });
            this.qrDataUrl.set(dataUrl);
          } catch {
            this.error.set('Could not render QR code.');
          }
          this.loading.set(false);
        },
        error: (err: HttpErrorResponse) => {
          this.error.set(err.error?.message ?? 'Could not load vehicle.');
          this.loading.set(false);
        },
      });
  }

  printQr(): void {
    this.showPrint.set(true);
    setTimeout(() => window.print(), 100);
  }
}
