import { useState } from 'react';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { api } from '../../api/api';
import { useAuth } from '../../auth/AuthContext';
import { CapacityBar, StatusBadge } from '../../components/Badges';
import { CenteredSpinner, EmptyState, ErrorBanner, PageHeader } from '../../components/Feedback';
import { Pagination } from '../../components/Modal';
import type { SiteListItem } from '../../api/types';
import { countryLabel, formatCurrency, formatNumber, formatPercent } from '../../lib/format';

const PAGE_SIZE = 10;

export function SitesPage() {
  const { hasPermission } = useAuth();

  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [country, setCountry] = useState('');
  const [status, setStatus] = useState('');
  const [capacityFilter, setCapacityFilter] = useState('');
  const [sort, setSort] = useState('code');
  const [page, setPage] = useState(1);

  const lookups = useQuery({ queryKey: ['lookups'], queryFn: api.lookups.get, staleTime: 5 * 60_000 });

  const sites = useQuery({
    queryKey: ['sites', { search, country, status, capacityFilter, sort, page }],
    queryFn: () =>
      api.sites.list({
        search: search || undefined,
        country: country || undefined,
        status: status || undefined,
        hasCapacity: capacityFilter === '' ? undefined : capacityFilter === 'true',
        sort,
        page,
        pageSize: PAGE_SIZE
      }),
    placeholderData: keepPreviousData
  });

  const applyFilters = (event: React.FormEvent) => {
    event.preventDefault();
    setSearch(searchInput.trim());
    setPage(1);
  };

  const resetFilters = () => {
    setSearchInput('');
    setSearch('');
    setCountry('');
    setStatus('');
    setCapacityFilter('');
    setSort('code');
    setPage(1);
  };

  return (
    <div className="space-y-5">
      <PageHeader
        title="Sites"
        subtitle="Search and filter the tower portfolio, then open a site for the full picture"
        actions={
          hasPermission('Sites.Manage') ? (
            <Link to="/sites/new" className="btn-primary">
              + Register site
            </Link>
          ) : undefined
        }
      />

      <form onSubmit={applyFilters} className="card grid gap-3 p-4 md:grid-cols-2 xl:grid-cols-6">
        <div className="xl:col-span-2">
          <label className="label" htmlFor="search">
            Search
          </label>
          <input
            id="search"
            className="input"
            placeholder="Site code, name or city"
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
          />
        </div>

        <div>
          <label className="label" htmlFor="country">
            Country
          </label>
          <select
            id="country"
            className="input"
            value={country}
            onChange={(event) => {
              setCountry(event.target.value);
              setPage(1);
            }}
          >
            <option value="">All countries</option>
            {(lookups.data?.countries ?? []).map((item) => (
              <option key={item.value} value={item.value}>
                {countryLabel(item.value)}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="label" htmlFor="status">
            Status
          </label>
          <select
            id="status"
            className="input"
            value={status}
            onChange={(event) => {
              setStatus(event.target.value);
              setPage(1);
            }}
          >
            <option value="">Any status</option>
            {(lookups.data?.siteStatuses ?? []).map((item) => (
              <option key={item.value} value={item.value}>
                {item.label}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="label" htmlFor="capacity">
            Capacity
          </label>
          <select
            id="capacity"
            className="input"
            value={capacityFilter}
            onChange={(event) => {
              setCapacityFilter(event.target.value);
              setPage(1);
            }}
          >
            <option value="">Any capacity</option>
            <option value="true">Has free slots</option>
            <option value="false">Fully utilized</option>
          </select>
        </div>
        <div>
          <label className="label" htmlFor="sort">
            Sort by
          </label>
          <select id="sort" className="input" value={sort} onChange={(event) => setSort(event.target.value)}>
            <option value="code">Site code</option>
            <option value="name">Name</option>
            <option value="country">Country</option>
            <option value="utilization">Utilisation (high → low)</option>
            <option value="revenue">Revenue (high → low)</option>
          </select>
        </div>

        <div className="flex items-end gap-2 md:col-span-2 xl:col-span-6">
          <button type="submit" className="btn-primary">
            Apply filters
          </button>
          <button type="button" className="btn-secondary" onClick={resetFilters}>
            Reset
          </button>
        </div>
      </form>

      {sites.isError && <ErrorBanner message="Sites could not be loaded." />}

      <div className="card overflow-hidden">
        {sites.isLoading ? (
          <CenteredSpinner label="Loading sites..." />
        ) : sites.data && sites.data.items.length > 0 ? (
          <>
            <SiteTable items={sites.data.items} />
            <Pagination
              page={sites.data.page}
              totalPages={sites.data.totalPages}
              totalCount={sites.data.totalCount}
              onPageChange={setPage}
            />
          </>
        ) : (
          <EmptyState
            title="No sites match these filters"
            description="Try clearing the country or status filter, or search for a different site code."
          />
        )}
      </div>
    </div>
  );
}

function SiteTable({ items }: { items: SiteListItem[] }) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full">
        <thead>
          <tr>
            <th className="th">Site</th>
            <th className="th">Market</th>
            <th className="th">Status</th>
            <th className="th">Towers</th>
            <th className="th w-44">Tenant utilisation</th>
            <th className="th">Free slots</th>
            <th className="th">MRR</th>
          </tr>
        </thead>
        <tbody>
          {items.map((site) => (
            <tr key={site.id} className="transition hover:bg-slate-50">
              <td className="td">
                <Link to={`/sites/${site.id}`} className="font-medium text-indigo-700 hover:underline">
                  {site.name}
                </Link>
                <span className="ml-2 font-mono text-xs text-slate-400">{site.siteCode}</span>
                <span className="block text-xs text-slate-500">
                  {site.siteType} · {formatNumber(site.towerCount)} tower(s)
                </span>
              </td>
              <td className="td">
                {countryLabel(site.countryCode)}
                <span className="block text-xs text-slate-500">{site.city}</span>
              </td>
              <td className="td">
                <StatusBadge status={site.status} />
              </td>
              <td className="td">{formatNumber(site.towerCount)}</td>
              <td className="td">
                <CapacityBar
                  percent={site.utilizationPercent}
                  label={`${site.activeTenants}/${site.tenantCapacity || 0} slots`}
                  right={formatPercent(site.utilizationPercent)}
                />
              </td>
              <td className="td">
                <span
                  className={site.availableSlots > 0 ? 'font-semibold text-emerald-700' : 'font-semibold text-rose-700'}
                >
                  {site.availableSlots}
                </span>
              </td>
              <td className="td">{formatCurrency(site.monthlyRevenue, site.currency)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
