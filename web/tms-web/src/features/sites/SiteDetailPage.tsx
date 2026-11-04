import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import { api } from '../../api/api';
import { ApiError } from '../../api/client';
import type { MapSite, Tower } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { CapacityBar, Chip, StatusBadge } from '../../components/Badges';
import { CenteredSpinner, ErrorBanner, PageHeader, SuccessBanner } from '../../components/Feedback';
import { countryLabel, formatCurrency, formatDate, formatDateTime, formatPercent } from '../../lib/format';
import { PortfolioMap } from '../dashboard/PortfolioMap';
import { AddTenancyModal } from './AddTenancyModal';

export function SiteDetailPage() {
  const { id } = useParams<{ id: string }>();
  const { hasPermission } = useAuth();
  const queryClient = useQueryClient();

  const [tenancyTower, setTenancyTower] = useState<Tower | null>(null);
  const [feedback, setFeedback] = useState<{ kind: 'error' | 'success'; message: string; code?: string } | null>(
    null
  );

  const siteQuery = useQuery({ queryKey: ['site', id], queryFn: () => api.sites.get(id!), enabled: Boolean(id) });
  const lookups = useQuery({ queryKey: ['lookups'], queryFn: api.lookups.get, staleTime: 5 * 60_000 });

  const refresh = async () => {
    await queryClient.invalidateQueries({ queryKey: ['site', id] });
    await queryClient.invalidateQueries({ queryKey: ['sites'] });
    await queryClient.invalidateQueries({ queryKey: ['dashboard'] });
    await queryClient.invalidateQueries({ queryKey: ['tenancies'] });
  };

  const changeStatus = useMutation({
    mutationFn: (status: string) => api.sites.changeStatus(id!, status),
    onSuccess: async (updated) => {
      await refresh();
      setFeedback({ kind: 'success', message: `Site status changed to ${updated.status}.` });
    },
    onError: (cause) =>
      setFeedback({
        kind: 'error',
        message: cause instanceof ApiError ? cause.message : 'The status change failed.',
        code: cause instanceof ApiError ? cause.code : undefined
      })
  });

  const terminate = useMutation({
    mutationFn: (tenancyId: string) => api.tenancies.terminate(tenancyId),
    onSuccess: async () => {
      await refresh();
      setFeedback({ kind: 'success', message: 'Tenancy terminated - the tenant slot is free again.' });
    },
    onError: (cause) =>
      setFeedback({
        kind: 'error',
        message: cause instanceof ApiError ? cause.message : 'The tenancy could not be terminated.',
        code: cause instanceof ApiError ? cause.code : undefined
      })
  });

  if (siteQuery.isLoading) {
    return <CenteredSpinner label="Loading site details..." />;
  }

  if (siteQuery.isError || !siteQuery.data) {
    return <ErrorBanner message="This site does not exist (or you do not have access to it)." />;
  }

  const site = siteQuery.data;
  const nextStatuses = lookups.data?.siteStatusTransitions[site.status] ?? [];
  const mapPins: MapSite[] = [
    {
      id: site.id,
      siteCode: site.siteCode,
      name: site.name,
      countryCode: site.countryCode,
      city: site.city,
      status: site.status,
      latitude: site.latitude,
      longitude: site.longitude,
      towerCount: site.totals.towerCount,
      tenantCapacity: site.totals.tenantCapacity,
      activeTenants: site.totals.activeTenants,
      utilizationPercent: site.totals.utilizationPercent
    }
  ];

  return (
    <div className="space-y-5">
      <Link to="/sites" className="text-sm font-medium text-indigo-600 hover:underline">
        ← Back to sites
      </Link>

      <PageHeader
        title={site.name}
        subtitle={
          <span className="flex flex-wrap items-center gap-2">
            <span className="font-mono text-xs text-slate-500">{site.siteCode}</span>
            <StatusBadge status={site.status} />
            <Chip tone="slate">{site.siteType}</Chip>
            <span>
              {countryLabel(site.countryCode)} · {site.city}
            </span>
          </span>
        }
        actions={
          <>
            {hasPermission('Sites.Manage') && (
              <Link to={`/sites/${site.id}/edit`} className="btn-secondary">
                Edit site
              </Link>
            )}
            {hasPermission('Sites.Manage') &&
              nextStatuses
                .filter((status) => status !== site.status)
                .map((status) => (
                  <button
                    key={status}
                    type="button"
                    className={status === 'Decommissioned' ? 'btn-danger' : 'btn-secondary'}
                    disabled={changeStatus.isPending}
                    onClick={() => changeStatus.mutate(status)}
                  >
                    {status === 'Active' ? 'Activate site' : status === 'Decommissioned' ? 'Decommission' : status}
                  </button>
                ))}
          </>
        }
      />

      {feedback?.kind === 'error' && (
        <ErrorBanner message={feedback.message} code={feedback.code} onDismiss={() => setFeedback(null)} />
      )}
      {feedback?.kind === 'success' && <SuccessBanner message={feedback.message} onDismiss={() => setFeedback(null)} />}

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <Metric label="Monthly revenue" value={formatCurrency(site.totals.monthlyRevenue, site.totals.currency)} />
        <Metric label="Tenants" value={`${site.totals.activeTenants} of ${site.totals.tenantCapacity}`} />
        <Metric label="Free tenant slots" value={String(site.totals.availableSlots)} />
        <Metric label="Utilisation" value={formatPercent(site.totals.utilizationPercent)} />
      </div>

      <div className="grid gap-4 xl:grid-cols-3">
        <div className="space-y-4 xl:col-span-2">
          <h2 className="text-sm font-semibold text-slate-800">Towers and tenants</h2>

          {site.towers.map((tower) => (
            <TowerCard
              key={tower.id}
              tower={tower}
              canAssign={hasPermission('Tenancies.Manage') && site.status === 'Active' && tower.canAcceptTenant}
              canTerminate={hasPermission('Tenancies.Manage')}
              terminating={terminate.isPending}
              onAssign={() => setTenancyTower(tower)}
              onTerminate={(tenancyId) => terminate.mutate(tenancyId)}
            />
          ))}

          {site.towers.length === 0 && (
            <div className="card p-6 text-sm text-slate-500">This site has no towers yet.</div>
          )}
        </div>

        <div className="space-y-4">
          <div className="card p-4">
            <h2 className="text-sm font-semibold text-slate-800">Site facts</h2>
            <dl className="mt-3 space-y-2 text-sm">
              <Fact label="Site code" value={<span className="font-mono text-xs">{site.siteCode}</span>} />
              <Fact label="Address" value={`${site.address}, ${site.city}`} />
              <Fact label="Coordinates" value={`${site.latitude.toFixed(4)}, ${site.longitude.toFixed(4)}`} />
              <Fact label="Acquired" value={formatDate(site.acquiredOn)} />
              <Fact
                label="Ground lease cost"
                value={site.monthlySiteCost ? `${formatCurrency(site.monthlySiteCost)} / month` : '—'}
              />
              <Fact label="Towers" value={String(site.totals.towerCount)} />
              <Fact label="Last updated" value={formatDateTime(site.updatedAtUtc)} />
            </dl>
            {site.notes && <p className="mt-3 rounded-lg bg-slate-50 p-3 text-xs text-slate-600">{site.notes}</p>}
          </div>

          <div className="card overflow-hidden">
            <div className="border-b border-slate-200 px-4 py-3">
              <h2 className="text-sm font-semibold text-slate-800">Location</h2>
            </div>
            <PortfolioMap sites={mapPins} height={240} />
          </div>
        </div>
      </div>

      {tenancyTower && <AddTenancyModal tower={tenancyTower} onClose={() => setTenancyTower(null)} />}
    </div>
  );
}

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <div className="card p-4">
      <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">{label}</p>
      <p className="mt-1.5 text-xl font-semibold text-slate-900">{value}</p>
    </div>
  );
}

function Fact({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex items-start justify-between gap-3">
      <dt className="text-slate-500">{label}</dt>
      <dd className="text-right font-medium text-slate-800">{value}</dd>
    </div>
  );
}

function TowerCard({
  tower,
  canAssign,
  canTerminate,
  terminating,
  onAssign,
  onTerminate
}: {
  tower: Tower;
  canAssign: boolean;
  canTerminate: boolean;
  terminating: boolean;
  onAssign: () => void;
  onTerminate: (tenancyId: string) => void;
}) {
  return (
    <div className="card p-4">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="font-mono text-sm font-semibold text-slate-900">{tower.towerCode}</p>
          <p className="text-xs text-slate-500">
            {tower.structureType} · {tower.heightMeters} m · {tower.antennaMountCapacity} antenna mounts
            {tower.commissionedOn ? ` · commissioned ${formatDate(tower.commissionedOn)}` : ''}
          </p>
        </div>
        <div className="flex items-center gap-2">
          {tower.canAcceptTenant ? (
            <Chip tone="emerald">{tower.availableSlots} slot(s) free</Chip>
          ) : (
            <Chip tone="rose">Full</Chip>
          )}
          {canAssign && (
            <button type="button" className="btn-primary" onClick={onAssign}>
              + Assign tenant
            </button>
          )}
        </div>
      </div>

      <div className="mt-3">
        <CapacityBar
          percent={tower.utilizationPercent}
          label={`${tower.activeTenants}/${tower.tenantCapacity} tenant slots`}
        />
      </div>

      <div className="mt-3 overflow-x-auto">
        {tower.tenancies.length === 0 ? (
          <p className="rounded-lg border border-dashed border-slate-300 px-3 py-4 text-center text-xs text-slate-500">
            No tenancies yet - this tower is ready to be leased.
          </p>
        ) : (
          <table className="w-full">
            <thead>
              <tr>
                <th className="th">Operator</th>
                <th className="th">Space</th>
                <th className="th">Rent</th>
                <th className="th">Term</th>
                <th className="th">Status</th>
                <th className="th" />
              </tr>
            </thead>
            <tbody>
              {tower.tenancies.map((tenancy) => (
                <tr key={tenancy.id}>
                  <td className="td font-medium text-slate-800">{tenancy.operatorName}</td>
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
                    {canTerminate && tenancy.status === 'Active' && (
                      <button
                        type="button"
                        className="text-xs font-semibold text-rose-600 hover:underline disabled:opacity-50"
                        disabled={terminating}
                        onClick={() => onTerminate(tenancy.id)}
                      >
                        Terminate
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
