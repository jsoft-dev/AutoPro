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
    ],
  },
  { path: '**', redirectTo: '' },
];
