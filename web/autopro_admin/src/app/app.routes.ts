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
          import('./features/inventory/inventory-shell.component').then(
            (m) => m.InventoryShellComponent,
          ),
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'items' },
          {
            path: 'items',
            loadComponent: () =>
              import('./features/inventory/inventory-list.component').then(
                (m) => m.InventoryListComponent,
              ),
          },
          {
            path: 'items/:id',
            loadComponent: () =>
              import('./features/inventory/inventory-form.component').then(
                (m) => m.InventoryFormComponent,
              ),
          },
          {
            path: 'purchase-orders',
            loadComponent: () =>
              import('./features/purchase-orders/purchase-order-list.component').then(
                (m) => m.PurchaseOrderListComponent,
              ),
          },
          {
            path: 'purchase-orders/:id',
            loadComponent: () =>
              import('./features/purchase-orders/purchase-order-form.component').then(
                (m) => m.PurchaseOrderFormComponent,
              ),
          },
          {
            path: 'goods-receipts',
            loadComponent: () =>
              import('./features/goods-receipts/goods-receipt-list.component').then(
                (m) => m.GoodsReceiptListComponent,
              ),
          },
          {
            path: 'goods-receipts/:id',
            loadComponent: () =>
              import('./features/goods-receipts/goods-receipt-form.component').then(
                (m) => m.GoodsReceiptFormComponent,
              ),
          },
        ],
      },
      {
        path: 'inventory/:id',
        redirectTo: (route) => {
          const id = route.params['id'];
          return `inventory/items/${id}`;
        },
      },
      {
        path: 'goods-receipts',
        redirectTo: 'inventory/goods-receipts',
        pathMatch: 'full',
      },
      {
        path: 'goods-receipts/:id',
        redirectTo: (route) => {
          const id = route.params['id'];
          return `inventory/goods-receipts/${id}`;
        },
      },
      {
        path: 'purchase-orders',
        redirectTo: 'inventory/purchase-orders',
        pathMatch: 'full',
      },
      {
        path: 'purchase-orders/:id',
        redirectTo: (route) => {
          const id = route.params['id'];
          return `inventory/purchase-orders/${id}`;
        },
      },
      {
        path: 'suppliers',
        loadComponent: () =>
          import('./features/suppliers/supplier-list.component').then(
            (m) => m.SupplierListComponent,
          ),
      },
      {
        path: 'suppliers/:id',
        loadComponent: () =>
          import('./features/suppliers/supplier-form.component').then(
            (m) => m.SupplierFormComponent,
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