import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';

import { API_BASE_URL } from './api-config';
import { AuthSession, LoginResponse, UserRole } from '../models/auth.models';

const STORAGE_KEY = 'autopro.ao.session';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly sessionSignal = signal<AuthSession | null>(this.loadStored());

  readonly session = this.sessionSignal.asReadonly();
  readonly isLoggedIn = computed(() => !!this.sessionSignal()?.accessToken);
  readonly displayName = computed(() => this.sessionSignal()?.displayName ?? '');
  readonly role = computed(() => this.sessionSignal()?.role);

  login(userName: string, password: string) {
    return this.http
      .post<LoginResponse>(`${API_BASE_URL}/api/v1/auth/login`, { userName, password })
      .pipe(
        tap((res) => {
          const session: AuthSession = {
            accessToken: res.accessToken,
            expiresAtUtc: res.expiresAtUtc,
            userId: res.userId,
            displayName: res.displayName,
            role: res.role,
          };
          this.persist(session);
          this.sessionSignal.set(session);
        }),
      );
  }

  logout(): void {
    localStorage.removeItem(STORAGE_KEY);
    this.sessionSignal.set(null);
    void this.router.navigateByUrl('/login');
  }

  getToken(): string | null {
    const s = this.sessionSignal();
    if (!s) return null;
    if (new Date(s.expiresAtUtc) <= new Date()) {
      this.logout();
      return null;
    }
    return s.accessToken;
  }

  hasRole(role: UserRole): boolean {
    return this.sessionSignal()?.role === role;
  }

  private persist(session: AuthSession): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
  }

  private loadStored(): AuthSession | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return null;
      const parsed = JSON.parse(raw) as AuthSession;
      if (!parsed.accessToken || new Date(parsed.expiresAtUtc) <= new Date()) {
        localStorage.removeItem(STORAGE_KEY);
        return null;
      }
      return parsed;
    } catch {
      return null;
    }
  }
}
