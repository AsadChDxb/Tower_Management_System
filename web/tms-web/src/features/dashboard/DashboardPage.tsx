import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis
} from 'recharts';
import { api } from '../../api/api';
import { CapacityBar, StatusBadge } from '../../components/Badges';
import { CenteredSpinner, ErrorBanner, PageHeader } from '../../components/Feedback';
import { countryName, formatCurrency, formatNumber, formatPercent } from '../../lib/format';
import { MapLegend, PortfolioMap } from './PortfolioMap';

const bucketColors = ['#cbd5e1', '#10b981', '#f59e0b', '#e11d48'];

export function DashboardPage() {
  const { data, isLoading, error } = useQuery({
    queryKey: ['dashboard'],
    queryFn: api.dashboard.summary
  });

  if (isLoading) {
    return <CenteredSpinner label="Building the portfolio overview..." />;
  }

  if (error || !data) {
    return <ErrorBanner message="The dashboard could not be loaded. Is the API running?" />;
  }

  const { kpis } = data;
  const revenueByCountry = data.byCountry.map((country) => ({
    ...country,
    name: countryName(country.countryCode)
  }));

  return (
    <div className="space-y-6">
      <PageHeader
        title="Portfolio overview"
        subtitle={`${kpis.siteCount} sites · ${kpis.towerCount} towers · refreshed ${new Date(
          data.generatedAtUtc
        ).toLocaleTimeString('en-GB')}`}
        actions={
          <Link to="/sites" className="btn-secondary">
            Browse sites
          </Link>
        }
      />

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <KpiCard
          label="Monthly recurring revenue"
          value={formatCurrency(kpis.monthlyRevenue, data.currency)}
          hint={`${formatCurrency(kpis.averageRevenuePerTenant, data.currency)} average per tenant`}
        />
        <KpiCard
          label="Active tenants"
          value={formatNumber(kpis.activeTenants)}
          hint={`${kpis.availableSlots} free tenant slots`}
        />
        <KpiCard
          label="Portfolio utilisation"
          value={formatPercent(kpis.utilizationPercent)}
          hint={`${kpis.fullyUtilizedTowerCount} tower(s) fully utilized`}
        />
        <KpiCard
          label="Sites"
          value={formatNumber(kpis.siteCount)}
          hint={`${kpis.activeSiteCount} active · ${kpis.plannedSiteCount} planned · ${kpis.decommissionedSiteCount} decommissioned`}
        />
      </div>

      <div className="grid gap-4 xl:grid-cols-3">
        <div className="card overflow-hidden xl:col-span-2">
          <div className="flex flex-wrap items-center justify-between gap-2 border-b border-slate-200 px-4 py-3">
            <h2 className="text-sm font-semibold text-slate-800">Where our sites are</h2>
            <MapLegend />
          </div>
          <PortfolioMap sites={data.mapSites} />
        </div>

        <div className="space-y-4">
          <div className="card p-4">
            <h2 className="text-sm font-semibold text-slate-800">Site status</h2>
            <ul className="mt-3 space-y-2">
              {data.byStatus.map((status) => (
                <li key={status.status} className="flex items-center justify-between text-sm">
                  <StatusBadge status={status.status} />
                  <span className="font-semibold text-slate-700">{status.siteCount}</span>
                </li>
              ))}
            </ul>
          </div>

          <div className="card p-4">
            <h2 className="text-sm font-semibold text-slate-800">Tower utilisation</h2>
            <div className="mt-3 h-40">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={data.utilizationBuckets} margin={{ top: 4, right: 8, bottom: 0, left: -20 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" vertical={false} />
                  <XAxis dataKey="label" tick={{ fontSize: 10 }} interval={0} />
                  <YAxis tick={{ fontSize: 10 }} allowDecimals={false} />
                  <Tooltip formatter={(value) => [`${Number(value ?? 0)} tower(s)`, 'Towers']} />
                  <Bar dataKey="towerCount" radius={[4, 4, 0, 0]}>
                    {data.utilizationBuckets.map((bucket, index) => (
                      <Cell key={bucket.label} fill={bucketColors[index % bucketColors.length]} />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            </div>
          </div>
        </div>
      </div>
      <div className="grid gap-4 xl:grid-cols-2">
        <div className="card p-4">
          <h2 className="text-sm font-semibold text-slate-800">Monthly revenue by country</h2>
          <div className="mt-3 h-64">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={revenueByCountry} margin={{ top: 4, right: 8, bottom: 0, left: 8 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#e2e8f0" vertical={false} />
                <XAxis dataKey="name" tick={{ fontSize: 11 }} />
                <YAxis tick={{ fontSize: 11 }} />
                <Tooltip formatter={(value) => [formatCurrency(Number(value ?? 0), data.currency), 'MRR']} />
                <Bar dataKey="monthlyRevenue" fill="#4f46e5" radius={[4, 4, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>

        <div className="card p-4">
          <h2 className="text-sm font-semibold text-slate-800">Top tenants by revenue</h2>
          <table className="mt-3 w-full">
            <thead>
              <tr>
                <th className="th">Operator</th>
                <th className="th">Tenancies</th>
                <th className="th">MRR</th>
                <th className="th">Share</th>
              </tr>
            </thead>
            <tbody>
              {data.topOperatorsByRevenue.map((operator) => (
                <tr key={operator.operatorId}>
                  <td className="td">
                    <span className="font-medium text-slate-800">{operator.name}</span>
                    <span className="ml-1 text-xs text-slate-400">{operator.countryCode}</span>
                  </td>
                  <td className="td">{operator.activeTenancies}</td>
                  <td className="td">{formatCurrency(operator.monthlyRevenue, data.currency)}</td>
                  <td className="td">
                    <div className="flex items-center gap-2">
                      <div className="h-1.5 w-16 overflow-hidden rounded-full bg-slate-100">
                        <div
                          className="h-full rounded-full bg-indigo-500"
                          style={{ width: `${operator.revenueSharePercent}%` }}
                        />
                      </div>
                      <span className="text-xs text-slate-500">{operator.revenueSharePercent}%</span>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <div className="card">
        <div className="flex flex-wrap items-center justify-between gap-2 border-b border-slate-200 px-4 py-3">
          <h2 className="text-sm font-semibold text-slate-800">Sales opportunities - towers with free capacity</h2>
          <span className="text-xs text-slate-500">Ordered by free slots</span>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr>
                <th className="th">Tower</th>
                <th className="th">Site</th>
                <th className="th">Market</th>
                <th className="th">Free slots</th>
                <th className="th w-40">Utilisation</th>
                <th className="th" />
              </tr>
            </thead>
            <tbody>
              {data.underUtilizedTowers.map((tower) => (
                <tr key={tower.towerId}>
                  <td className="td font-mono text-xs">{tower.towerCode}</td>
                  <td className="td">
                    <span className="font-medium text-slate-800">{tower.siteName}</span>
                    <span className="ml-1 text-xs text-slate-400">{tower.siteCode}</span>
                  </td>
                  <td className="td">
                    {countryName(tower.countryCode)} · {tower.city}
                  </td>
                  <td className="td font-semibold text-emerald-700">
                    {tower.availableSlots} of {tower.tenantCapacity}
                  </td>
                  <td className="td">
                    <CapacityBar percent={tower.utilizationPercent} label="Utilisation" />
                  </td>
                  <td className="td text-right">
                    <Link
                      to={`/sites/${tower.siteId}`}
                      className="text-xs font-semibold text-indigo-600 hover:underline"
                    >
                      Add tenant →
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}

function KpiCard({ label, value, hint }: { label: string; value: string; hint: string }) {
  return (
    <div className="card p-4">
      <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">{label}</p>
      <p className="mt-2 text-2xl font-semibold text-slate-900">{value}</p>
      <p className="mt-1 text-xs text-slate-500">{hint}</p>
    </div>
  );
}
