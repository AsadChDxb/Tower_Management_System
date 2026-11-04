export type SiteStatus = 'Planned' | 'Active' | 'Decommissioned';
export type SiteType = 'Greenfield' | 'Rooftop' | 'Indoor';
export type TowerStructureType = 'Lattice' | 'Monopole' | 'Guyed' | 'RooftopMast';
export type SpaceType = 'Antenna' | 'Equipment' | 'Compound' | 'Full';
export type TenancyStatus = 'Pending' | 'Active' | 'Terminated';

export interface UserDto {
  id: string;
  email: string;
  displayName: string;
  isActive: boolean;
  roles: string[];
  permissions: string[];
  createdAtUtc: string;
}

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  user: UserDto;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface SiteListItem {
  id: string;
  siteCode: string;
  name: string;
  countryCode: string;
  city: string;
  status: SiteStatus;
  siteType: SiteType;
  latitude: number;
  longitude: number;
  towerCount: number;
  tenantCapacity: number;
  activeTenants: number;
  availableSlots: number;
  utilizationPercent: number;
  monthlyRevenue: number;
  currency: string;
  updatedAtUtc: string;
}

export interface SitePortfolioTotals {
  towerCount: number;
  tenantCapacity: number;
  activeTenants: number;
  availableSlots: number;
  utilizationPercent: number;
  monthlyRevenue: number;
  currency: string;
}

export interface Tenancy {
  id: string;
  towerId: string;
  towerCode: string;
  siteId: string;
  siteCode: string;
  siteName: string;
  operatorId: string;
  operatorName: string;
  spaceType: SpaceType;
  antennaCount: number;
  monthlyRent: number;
  currency: string;
  startDate: string;
  endDate: string | null;
  status: TenancyStatus;
  notes: string | null;
  createdAtUtc: string;
}

export interface Tower {
  id: string;
  towerCode: string;
  structureType: TowerStructureType;
  heightMeters: number;
  tenantCapacity: number;
  antennaMountCapacity: number;
  commissionedOn: string | null;
  activeTenants: number;
  availableSlots: number;
  utilizationPercent: number;
  canAcceptTenant: boolean;
  tenancies: Tenancy[];
}

export interface SiteDetail {
  id: string;
  siteCode: string;
  name: string;
  countryCode: string;
  city: string;
  address: string;
  latitude: number;
  longitude: number;
  siteType: SiteType;
  status: SiteStatus;
  acquiredOn: string | null;
  monthlySiteCost: number | null;
  notes: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  totals: SitePortfolioTotals;
  towers: Tower[];
}

export interface TowerCapacity {
  towerId: string;
  towerCode: string;
  siteCode: string;
  tenantCapacity: number;
  activeTenants: number;
  availableSlots: number;
  utilizationPercent: number;
  canAcceptTenant: boolean;
  summary: string;
}

export interface Operator {
  id: string;
  name: string;
  countryCode: string;
  contactEmail: string | null;
  contactPhone: string | null;
  activeTenancies: number;
  monthlyRevenue: number;
  createdAtUtc: string;
}

export interface DashboardKpi {
  siteCount: number;
  activeSiteCount: number;
  plannedSiteCount: number;
  decommissionedSiteCount: number;
  towerCount: number;
  tenantCapacity: number;
  activeTenants: number;
  availableSlots: number;
  fullyUtilizedTowerCount: number;
  utilizationPercent: number;
  monthlyRevenue: number;
  averageRevenuePerTenant: number;
}

export interface CountrySummary {
  countryCode: string;
  siteCount: number;
  towerCount: number;
  tenantCapacity: number;
  activeTenants: number;
  availableSlots: number;
  utilizationPercent: number;
  monthlyRevenue: number;
}

export interface StatusBreakdown {
  status: string;
  siteCount: number;
}

export interface UtilizationBucket {
  label: string;
  towerCount: number;
}

export interface OperatorRevenue {
  operatorId: string;
  name: string;
  countryCode: string;
  activeTenancies: number;
  monthlyRevenue: number;
  revenueSharePercent: number;
}

export interface UnderUtilizedTower {
  towerId: string;
  towerCode: string;
  siteId: string;
  siteCode: string;
  siteName: string;
  countryCode: string;
  city: string;
  tenantCapacity: number;
  activeTenants: number;
  availableSlots: number;
  utilizationPercent: number;
}

export interface MapSite {
  id: string;
  siteCode: string;
  name: string;
  countryCode: string;
  city: string;
  status: string;
  latitude: number;
  longitude: number;
  towerCount: number;
  tenantCapacity: number;
  activeTenants: number;
  utilizationPercent: number;
}

export interface DashboardSummary {
  generatedAtUtc: string;
  currency: string;
  kpis: DashboardKpi;
  byCountry: CountrySummary[];
  byStatus: StatusBreakdown[];
  utilizationBuckets: UtilizationBucket[];
  topOperatorsByRevenue: OperatorRevenue[];
  underUtilizedTowers: UnderUtilizedTower[];
  mapSites: MapSite[];
}

export interface LookupItem {
  value: string;
  label: string;
}

export interface Lookups {
  countries: LookupItem[];
  siteTypes: LookupItem[];
  siteStatuses: LookupItem[];
  towerStructureTypes: LookupItem[];
  spaceTypes: LookupItem[];
  tenancyStatuses: LookupItem[];
  currencies: LookupItem[];
  roles: LookupItem[];
  permissions: LookupItem[];
  siteStatusTransitions: Record<string, string[]>;
}

export interface OperatorInput {
  name: string;
  countryCode: string;
  contactEmail?: string | null;
  contactPhone?: string | null;
}

export interface TowerInput {
  towerCode: string;
  structureType: TowerStructureType;
  heightMeters: number;
  tenantCapacity: number;
  antennaMountCapacity: number;
  commissionedOn?: string | null;
}

export interface SiteInput {
  siteCode: string;
  name: string;
  countryCode: string;
  city: string;
  address: string;
  latitude: number;
  longitude: number;
  siteType: SiteType;
  status: SiteStatus;
  acquiredOn?: string | null;
  monthlySiteCost?: number | null;
  notes?: string | null;
  tower?: TowerInput | null;
}

export interface UpdateSiteInput {
  name: string;
  countryCode: string;
  city: string;
  address: string;
  latitude: number;
  longitude: number;
  siteType: SiteType;
  acquiredOn?: string | null;
  monthlySiteCost?: number | null;
  notes?: string | null;
}

export interface TenancyInput {
  towerId: string;
  operatorId: string;
  spaceType: SpaceType;
  antennaCount: number;
  monthlyRent: number;
  currency: string;
  startDate: string;
  endDate?: string | null;
  notes?: string | null;
}

export interface CreateUserInput {
  email: string;
  displayName: string;
  password: string;
  roles: string[];
}

export interface UpdateUserInput {
  displayName?: string;
  isActive?: boolean;
  roles?: string[];
}
