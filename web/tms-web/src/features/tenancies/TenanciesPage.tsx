import { useState } from 'react';
import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { api } from '../../api/api';
import { ApiError } from '../../api/client';
import type { Tenancy } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { StatusBadge } from '../../components/Badges';
import { CenteredSpinner, ErrorBanner, PageHeader, SuccessBanner } from '../../components/Feedback';
import { Pagination } from '../../components/Modal';
import { countryLabel, formatCurrency, formatDate } from '../../lib/format';

export function TenanciesPage() {
  const { hasPermission } = useAuth();
  const queryClient = useQueryClient();

  const [status, setStatus] = useState('Active');
  const [country, setCountry] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [feedback, setFeedback] = useState<{ kind: 'error' | 'success'; message: string; code?: string } | null>(
    null
  );

  const lookups = useQuery({ queryKey: ['lookups'], queryFn: api.lookups.get, staleTime: 5 * 60_000 });

  const tenancies = useQuery({
    queryKey: ['tenancies', { status, country, search, page }],
    queryFn: () =>
      api.tenancies.list({
        status: status || undefined,
        country: country || undefined,
        search: search || undefined,
        page,
        pageSize: 15
      }),
    placeholderData: keepPreviousData
  });

  const terminate = useMutation({
    mutationFn: (tenancyId: string) => api.tenancies.terminate(tenancyId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['tenancies'] });
      await queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      await queryClient.invalidateQueries({ queryKey: ['site'] });
      setFeedback({ kind: 'success', message: 'Tenancy terminated - the tenant slot is free again.' });
    },
    onError: (cause) =>
      setFeedback({
        kind: 'error',
        message: cause instanceof ApiError ? cause.message : 'The tenancy could not be terminated.',
        code: cause instanceof ApiError ? cause.code : undefined
      })
  });

  return (
    <div className="space-y-5">
      <PageHeader title="Tenancies" subtitle="Every lease agreement across the portfolio, active and historical" />

      {feedback?.kind === 'error' && (
        <ErrorBanner message={feedback.message} code={feedback.code} onDismiss={() => setFeedback(null)} />
      )}
      {feedback?.kind === 'success' && <SuccessBanner message={feedback.message} onDismiss={() => setFeedback(null)} />}

      <form
        className="card grid gap-3 p-4 md:grid-cols-4"
        onSubmit={(event) => {
          event.preventDefault();
          setSearch(searchInput.trim());
          setPage(1);
        }}
      >
        <div>
          <label className="label" htmlFor="tenancySearch">
            Search
          </label>
          <input
            id="tenancySearch"
            className="input"
            placeholder="Operator, tower or site"
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
          />
        </div>

        <div>
          <label className="label" htmlFor="tenancyStatus">
            Status
          </label>
          <select
            id="tenancyStatus"
            className="input"
            value={status}
            onChange={(event) => {
              setStatus(event.target.value);
              setPage(1);
            }}
          >
            <option value="">Any status</option>
            {(lookups.data?.tenancyStatuses ?? []).map((item) => (
              <option key={item.value} value={item.value}>
                {item.label}
              </option>
            ))}
          </select>
        </div>

        <div>
          <label className="label" htmlFor="tenancyCountry">
            Country
          </label>
          <select
            id="tenancyCountry"
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

        <div className="flex items-end gap-2">
          <button type="submit" className="btn-primary">
            Apply
          </button>
          <button
            type="button"
            className="btn-secondary"
            onClick={() => {
              setSearchInput('');
              setSearch('');
              setStatus('Active');
              setCountry('');
              setPage(1);
            }}
          >
            Reset
          </button>
        </div>
      </form>

      {tenancies.isError && <ErrorBanner message="Tenancies could not be loaded." />}

      <div className="card overflow-hidden">
        {tenancies.isLoading ? (
          <CenteredSpinner label="Loading tenancies..." />
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead>
                  <tr>
                    <th className="th">Operator</th>
                    <th className="th">Tower</th>
                    <th className="th">Site</th>
                    <th className="th">Space</th>
                    <th className="th">Rent</th>
                    <th className="th">Term</th>
                    <th className="th">Status</th>
                    <th className="th" />
                  </tr>
                </thead>
                <tbody>
                  {(tenancies.data?.items ?? []).map((tenancy: Tenancy) => (
                    <tr key={tenancy.id}>
                      <td className="td font-medium text-slate-800">{tenancy.operatorName}</td>
                      <td className="td font-mono text-xs">{tenancy.towerCode}</td>
                      <td className="td">
                        <Link to={`/sites/${tenancy.siteId}`} className="text-indigo-600 hover:underline">
                          {tenancy.siteName}
                        </Link>
                        <span className="block text-xs text-slate-500">{tenancy.siteCode}</span>
                      </td>
                      <td className="td text-xs">
                        {tenancy.spaceType} · {tenancy.antennaCount} antenna(s)
                      </td>
                      <td className="td">{formatCurrency(tenancy.monthlyRent, tenancy.currency)}</td>
                      <td className="td text-xs">
                        {formatDate(tenancy.startDate)} → {tenancy.endDate ? formatDate(tenancy.endDate) : 'open'}
                      </td>
                      <td className="td">
                        <StatusBadge status={tenancy.status} />
                      </td>
                      <td className="td text-right">
                        {hasPermission('Tenancies.Manage') && tenancy.status === 'Active' && (
                          <button
                            type="button"
                            className="text-xs font-semibold text-rose-600 hover:underline disabled:opacity-50"
                            disabled={terminate.isPending}
                            onClick={() => terminate.mutate(tenancy.id)}
                          >
                            Terminate
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {tenancies.data && (
              <Pagination
                page={tenancies.data.page}
                totalPages={tenancies.data.totalPages}
                totalCount={tenancies.data.totalCount}
                onPageChange={setPage}
              />
            )}
          </>
        )}
      </div>
    </div>
  );
}
