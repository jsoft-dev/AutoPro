import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';

import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss',
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    userName: ['ao.demo', Validators.required],
    password: ['ChangeMe123!', Validators.required],
  });

  constructor() {
    if (this.route.snapshot.queryParamMap.get('reason') === 'role') {
      this.error.set('Only administrative officer accounts can sign in here.');
    }
    if (this.auth.isLoggedIn() && this.auth.hasRole('AdministrativeOfficer')) {
      void this.router.navigateByUrl('/');
    }
  }

  submit(): void {
    if (this.form.invalid) return;
    this.submitting.set(true);
    this.error.set(null);
    const { userName, password } = this.form.getRawValue();
    this.auth.login(userName, password).subscribe({
      next: () => {
        if (!this.auth.hasRole('AdministrativeOfficer')) {
          this.auth.logout();
          this.error.set('Only administrative officer accounts can sign in here.');
          this.submitting.set(false);
          return;
        }
        void this.router.navigateByUrl('/');
      },
      error: (err: HttpErrorResponse) => {
        const msg =
          typeof err.error?.message === 'string'
            ? err.error.message
            : 'Could not sign in. Check your username and password.';
        this.error.set(msg);
        this.submitting.set(false);
      },
      complete: () => this.submitting.set(false),
    });
  }
}
