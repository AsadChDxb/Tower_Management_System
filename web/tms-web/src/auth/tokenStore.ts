/**
 * Session storage for the JWT pair.
 *
 * Trade-off (documented in docs/architecture.md): tokens live in localStorage so a page
 * refresh keeps the user signed in. A production hardening step is to move the refresh token
 * into an HttpOnly/Secure/SameSite cookie issued by the API. We mitigate today by keeping the
 * access token short lived (15 min) and rotating + revoking refresh tokens server side.
 */
const ACCESS_TOKEN_KEY = 'tms.accessToken';
const REFRESH_TOKEN_KEY = 'tms.refreshToken';

export interface StoredSession {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAtUtc: string;
}

export const tokenStore = {
  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  },

  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  },

  save(session: StoredSession): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, session.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, session.refreshToken);
  },

  clear(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
  }
};
