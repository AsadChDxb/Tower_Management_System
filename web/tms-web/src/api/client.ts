import type { AuthResponse } from './types';
import { tokenStore } from '../auth/tokenStore';

/**
 * Base URL of the Tower Management System API. The backend is hosted on Azure App Service
 * (https://weatherdemoapp-h0a4ggb7dzh4href.indiasouthcentral-01.azurewebsites.net), so the browser
 * calls it directly. Override it with VITE_API_BASE_URL (absolute URL, no trailing slash) to talk to
 * another deployment, or with an empty value to use same-origin requests (Vite dev proxy / wwwroot).
 */
export const API_BASE_URL = (
  import.meta.env.VITE_API_BASE_URL ??
  'https://weatherdemoapp-h0a4ggb7dzh4href.indiasouthcentral-01.azurewebsites.net'
).replace(/\/+$/, '');

const BASE_URL = `${API_BASE_URL}/api`;

/** RFC 7807 problem details surfaced by the API (plus validation `errors`). */
export class ApiError extends Error {
  readonly status: number;
  readonly code?: string;
  readonly errors?: Record<string, string[]>;

  constructor(status: number, message: string, code?: string, errors?: Record<string, string[]>) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.code = code;
    this.errors = errors;
  }

  /** First validation message, handy for form banners. */
  get firstValidationMessage(): string | undefined {
    if (!this.errors) return undefined;
    const [firstKey] = Object.keys(this.errors);
    return firstKey ? this.errors[firstKey]?.[0] : undefined;
  }
}

let unauthorizedHandler: (() => void) | null = null;
let refreshInFlight: Promise<boolean> | null = null;

export function setUnauthorizedHandler(handler: (() => void) | null): void {
  unauthorizedHandler = handler;
}

async function parseError(response: Response): Promise<ApiError> {
  const fallback = `Request failed with status ${response.status}`;

  try {
    const body = (await response.json()) as {
      title?: string;
      detail?: string;
      code?: string;
      errors?: Record<string, string[]>;
    };

    const message = body.detail ?? body.title ?? fallback;
    return new ApiError(response.status, message, body.code, body.errors);
  } catch {
    return new ApiError(response.status, fallback);
  }
}

async function refreshSession(): Promise<boolean> {
  const refreshToken = tokenStore.getRefreshToken();
  if (!refreshToken) return false;

  if (!refreshInFlight) {
    refreshInFlight = (async () => {
      try {
        const response = await fetch(`${BASE_URL}/auth/refresh`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ refreshToken })
        });

        if (!response.ok) {
          tokenStore.clear();
          return false;
        }

        const session = (await response.json()) as AuthResponse;
        tokenStore.save({
          accessToken: session.accessToken,
          refreshToken: session.refreshToken,
          accessTokenExpiresAtUtc: session.accessTokenExpiresAtUtc
        });

        return true;
      } catch {
        tokenStore.clear();
        return false;
      } finally {
        refreshInFlight = null;
      }
    })();
  }

  return refreshInFlight;
}

function buildHeaders(body: unknown, extra?: Record<string, string>): HeadersInit {
  const headers: Record<string, string> = { ...extra };
  const token = tokenStore.getAccessToken();

  if (body !== undefined) headers['Content-Type'] = 'application/json';
  if (token) headers.Authorization = `Bearer ${token}`;

  return headers;
}

/** Single entry point for HTTP calls: attaches the JWT, retries once after a silent refresh. */
export async function request<T>(
  method: string,
  path: string,
  body?: unknown,
  allowRetry = true
): Promise<T> {
  const response = await fetch(`${BASE_URL}${path}`, {
    method,
    headers: buildHeaders(body),
    body: body === undefined ? undefined : JSON.stringify(body)
  });

  if (response.status === 401 && allowRetry) {
    const refreshed = await refreshSession();

    if (refreshed) {
      return request<T>(method, path, body, false);
    }

    unauthorizedHandler?.();
  }

  if (!response.ok) {
    throw await parseError(response);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export const http = {
  get: <T>(path: string) => request<T>('GET', path),
  post: <T>(path: string, body?: unknown) => request<T>('POST', path, body),
  put: <T>(path: string, body?: unknown) => request<T>('PUT', path, body),
  patch: <T>(path: string, body?: unknown) => request<T>('PATCH', path, body),
  delete: <T>(path: string) => request<T>('DELETE', path)
};
