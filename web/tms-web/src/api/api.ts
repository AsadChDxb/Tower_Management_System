import { http } from './client';
import type {
  AuthResponse,
  CreateUserInput,
  DashboardSummary,
  Lookups,
  Operator,
  OperatorInput,
  PagedResult,
  SiteDetail,
  SiteInput,
  SiteListItem,
  Tenancy,
  TenancyInput,
  Tower,
  TowerCapacity,
  UpdateSiteInput,
  UpdateUserInput,
  UserDto
} from './types';

export interface SiteListParams {
  search?: string;
  country?: string;
  status?: string;
  hasCapacity?: boolean;
  sort?: string;
  page?: number;
  pageSize?: number;
}

function toQueryString(params: object): string {
  const query = new URLSearchParams();

  Object.entries(params as Record<string, unknown>).forEach(([key, value]) => {
    if (value === undefined || value === null || value === '') return;
    query.set(key, String(value));
  });

  const serialized = query.toString();
  return serialized ? `?${serialized}` : '';
}

export const api = {
  auth: {
    login: (email: string, password: string) =>
      http.post<AuthResponse>('/auth/login', { email, password }),
    me: () => http.get<UserDto>('/auth/me'),
    logout: (refreshToken: string) => http.post<void>('/auth/logout', { refreshToken })
  },

  dashboard: {
    summary: () => http.get<DashboardSummary>('/dashboard/summary')
  },

  sites: {
    list: (params: SiteListParams) => http.get<PagedResult<SiteListItem>>(`/sites${toQueryString(params)}`),
    get: (id: string) => http.get<SiteDetail>(`/sites/${id}`),
    create: (input: SiteInput) => http.post<SiteDetail>('/sites', input),
    update: (id: string, input: UpdateSiteInput) => http.put<SiteDetail>(`/sites/${id}`, input),
    changeStatus: (id: string, status: string) => http.patch<SiteDetail>(`/sites/${id}/status`, { status }),
    remove: (id: string) => http.delete<void>(`/sites/${id}`),
    towers: (siteId: string) => http.get<Tower[]>(`/sites/${siteId}/towers`)
  },

  towers: {
    get: (id: string) => http.get<Tower>(`/towers/${id}`),
    capacity: (id: string) => http.get<TowerCapacity>(`/towers/${id}/capacity`)
  },

  operators: {
    list: (search?: string) => http.get<Operator[]>(`/operators${toQueryString({ search })}`),
    create: (input: OperatorInput) => http.post<Operator>('/operators', input),
    update: (id: string, input: OperatorInput) => http.put<Operator>(`/operators/${id}`, input)
  },

  tenancies: {
    list: (params: { status?: string; country?: string; search?: string; page?: number; pageSize?: number }) =>
      http.get<PagedResult<Tenancy>>(`/tenancies${toQueryString(params)}`),
    create: (input: TenancyInput) => http.post<Tenancy>('/tenancies', input),
    terminate: (id: string, endDate?: string | null) =>
      http.post<Tenancy>(`/tenancies/${id}/terminate`, { endDate: endDate ?? null })
  },

  users: {
    list: () => http.get<UserDto[]>('/users'),
    create: (input: CreateUserInput) => http.post<UserDto>('/users', input),
    update: (id: string, input: UpdateUserInput) => http.patch<UserDto>(`/users/${id}`, input),
    resetPassword: (id: string, newPassword: string) =>
      http.post<void>(`/users/${id}/reset-password`, { newPassword })
  },

  lookups: {
    get: () => http.get<Lookups>('/lookups')
  }
};
