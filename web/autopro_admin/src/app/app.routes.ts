import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { adminRoleGuard } from './core/admin-role.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: '',
    canActivate: [authGuard, adminRoleGuard],
    loadComponent: () =>
      import('./features/shell/shell.component').then((m) => m.ShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'jobs' },
      {
        path: 'jobs',
        loadComponent: () =>
          import('./features/jobs/job-list.component').then((m) => m.JobListComponent),
      },
      {
        path: 'jobs/:id',
        loadComponent: () =>
          import('./features/jobs/job-detail.component').then((m) => m.JobDetailComponent),
      },
      {
        path: 'customers',
        loadComponent: () =>
          import('./features/customers/customer-list.component').then(
            (m) => m.CustomerListComponent,
          ),
      },
      {
        path: 'customers/:id',
        loadComponent: () =>
          import('./features/customers/customer-detail.component').then(
            (m) => m.CustomerDetailComponent,
          ),
      },
      {
        path: 'vehicles',
        loadComponent: () =>
          import('./features/vehicles/vehicle-list.component').then((m) => m.VehicleListComponent),
      },
      {
        path: 'vehicles/:id',
        loadComponent: () =>
          import('./features/vehicles/vehicle-detail.component').then(
            (m) => m.VehicleDetailComponent,
          ),
      },
      {
        path: 'sales',
        loadComponent: () =>
          import('./features/sales/sale-list.component').then((m) => m.SaleListComponent),
      },
      {
        path: 'sales/new',
        loadComponent: () =>
          import('./features/sales/sale-detail.component').then((m) => m.SaleDetailComponent),
      },
      {
        path: 'sales/:id',
        loadComponent: () =>
          import('./features/sales/sale-detail.component').then((m) => m.SaleDetailComponent),
      },
      {
        path: 'inventory',
        loadComponent: () =>
          import('./features/inventory/inventory-list.component').then(
            (m) => m.InventoryListComponent,
          ),
      },
      {
        path: 'inventory/:id',
        loadComponent: () =>
          import('./features/inventory/inventory-form.component').then(
            (m) => m.InventoryFormComponent,
          ),
      },
      {
        path: 'settings/business',
        loadComponent: () =>
          import('./features/settings/business-settings.component').then(
            (m) => m.BusinessSettingsComponent,
          ),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
