import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';

import { InventoryService } from '../../services/inventory.service';
import { InventoryWritePayload } from '../../models/inventory.models';

@Component({
  selector: 'app-inventory-form',
  standalone: true,
  imports: [RouterLink, FormsModule],
  templateUrl: './inventory-form.component.html',
  styleUrl: './inventory-form.component.scss',
})
export class InventoryFormComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly inventoryApi = inject(InventoryService);

  readonly createMode = signal(false);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  partNumber = '';
  name = '';
  notes = '';
  quantityOnHand = 0;
  unitPrice = 0;
  isActive = true;

  private partId: string | null = null;

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => this.loadPart(params.get('id')));
  }

  private loadPart(id: string | null): void {
    this.error.set(null);
    if (id === 'new' || !id) {
      this.createMode.set(true);
      this.partId = null;
      this.partNumber = '';
      this.name = '';
      this.notes = '';
      this.quantityOnHand = 0;
      this.unitPrice = 0;
      this.isActive = true;
      this.loading.set(false);
      return;
    }
    this.createMode.set(false);
    this.partId = id;
    this.loading.set(true);
    this.inventoryApi.getById(id).subscribe({
      next: (item) => {
        this.partNumber = item.partNumber;
        this.name = item.name;
        this.notes = item.notes ?? '';
        this.quantityOnHand = item.quantityOnHand;
        this.unitPrice = item.unitPrice;
        this.isActive = item.isActive;
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(err.error?.message ?? 'Part not found.');
        this.loading.set(false);
      },
    });
  }

  submit(event: Event): void {
    event.preventDefault();
    this.error.set(null);

    const payload: InventoryWritePayload = {
      partNumber: this.partNumber.trim(),
      name: this.name.trim(),
      notes: this.notes.trim() || null,
      quantityOnHand: this.quantityOnHand,
      unitPrice: this.unitPrice,
      isActive: this.isActive,
    };

    if (!payload.partNumber || !payload.name) {
      this.error.set('Part number and name are required.');
      return;
    }
    if (payload.quantityOnHand < 0 || payload.unitPrice < 0) {
      this.error.set('Stock and price cannot be negative.');
      return;
    }

    if (!this.createMode() && !this.partId) {
      this.error.set('Missing part id.');
      return;
    }

    this.saving.set(true);
    const req = this.createMode()
      ? this.inventoryApi.create(payload)
      : this.inventoryApi.update(this.partId!, payload);

    req.subscribe({
      next: () => {
        this.saving.set(false);
        void this.router.navigate(['/inventory']);
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(err.error?.message ?? 'Could not save part.');
        this.saving.set(false);
      },
    });
  }
}
