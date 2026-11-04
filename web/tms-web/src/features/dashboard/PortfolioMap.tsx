import { CircleMarker, MapContainer, Popup, TileLayer } from 'react-leaflet';
import { Link } from 'react-router-dom';
import type { MapSite } from '../../api/types';
import { countryName, formatPercent } from '../../lib/format';

function colorFor(site: MapSite): string {
  if (site.status !== 'Active') return '#94a3b8';
  if (site.utilizationPercent >= 100) return '#e11d48';
  if (site.utilizationPercent >= 75) return '#f59e0b';
  if (site.utilizationPercent > 0) return '#10b981';
  return '#cbd5e1';
}

/** Leaflet map with one pin per site, coloured by utilisation (use case 4). */
export function PortfolioMap({ sites, height = 440 }: { sites: MapSite[]; height?: number }) {
  return (
    <div style={{ height }} className="overflow-hidden rounded-xl">
      <MapContainer center={[50.4, 5.2]} zoom={5} scrollWheelZoom={false} style={{ height: '100%', width: '100%' }}>
        <TileLayer
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
          url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
        />

        {sites.map((site) => (
          <CircleMarker
            key={site.id}
            center={[site.latitude, site.longitude]}
            radius={site.status === 'Active' ? 8 : 6}
            pathOptions={{
              color: colorFor(site),
              fillColor: colorFor(site),
              fillOpacity: 0.75,
              weight: 2
            }}
          >
            <Popup>
              <div className="space-y-1 text-sm">
                <p className="font-semibold">
                  {site.name} <span className="font-mono text-xs text-slate-500">{site.siteCode}</span>
                </p>
                <p className="text-xs text-slate-600">
                  {countryName(site.countryCode)} · {site.city}
                </p>
                <p className="text-xs text-slate-600">
                  {site.towerCount} tower(s) · {site.activeTenants}/{site.tenantCapacity} slots ·{' '}
                  {formatPercent(site.utilizationPercent)} utilized
                </p>
                <p className="text-xs">
                  <span className="font-medium">{site.status}</span>
                </p>
                <Link className="text-xs font-semibold text-indigo-600 hover:underline" to={`/sites/${site.id}`}>
                  Open site →
                </Link>
              </div>
            </Popup>
          </CircleMarker>
        ))}
      </MapContainer>
    </div>
  );
}

export function MapLegend() {
  const entries = [
    { color: 'bg-slate-300', label: 'Idle' },
    { color: 'bg-emerald-500', label: '1-74% utilized' },
    { color: 'bg-amber-500', label: '75-99% utilized' },
    { color: 'bg-rose-500', label: 'Full' },
    { color: 'bg-slate-400', label: 'Not active' }
  ];

  return (
    <div className="flex flex-wrap items-center gap-3 text-xs text-slate-500">
      {entries.map((entry) => (
        <span key={entry.label} className="inline-flex items-center gap-1.5">
          <span className={`h-2.5 w-2.5 rounded-full ${entry.color}`} />
          {entry.label}
        </span>
      ))}
    </div>
  );
}
