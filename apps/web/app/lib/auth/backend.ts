// Shapes mirroring the FIXAR OS backend contracts (apps/api).
// Kept intentionally minimal — only the fields this app actually uses.

export interface BackendApiResponse<T> {
  success: boolean;
  data: T | null;
  message: string | null;
  errorCode: string | null;
}

export interface AuthResultDto {
  succeeded: boolean;
  errors: string[];
  userId: string;
  email: string;
  fullName: string;
  roles: string[];
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
}

export interface MeDto {
  userId: string;
  email: string;
  roles: string[];
}
