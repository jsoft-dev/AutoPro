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
    ],
  },
  { path: '**', redirectTo: '' },
];
