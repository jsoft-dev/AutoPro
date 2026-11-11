import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService } from './auth.service';

export const adminRoleGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.hasRole('AdministrativeOfficer')) return true;
  auth.logout();
  return router.createUrlTree(['/login'], {
    queryParams: { reason: 'role' },
  });
};
