export type UserRole =
  | 'CustomerRepresentative'
  | 'AdministrativeOfficer'
  | 'ChiefTechnicalOfficer'
  | 'Supervisor';

export interface LoginResponse {
  accessToken: string;
  expiresAtUtc: string;
  userId: string;
  displayName: string;
  role: UserRole;
}

export interface AuthSession {
  accessToken: string;
  expiresAtUtc: string;
  userId: string;
  displayName: string;
  role: UserRole;
}
