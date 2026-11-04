import { useEffect, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { api } from '../../api/api';
import { ApiError } from '../../api/client';
import type { SiteStatus, SiteType, TowerStructureType } from '../../api/types';
import { ErrorBanner, PageHeader, Spinner } from '../../components/Feedback';
import { Field } from '../../components/Modal';
import { todayIso } from '../../lib/format';

interface FormState {
  siteCode: string;
  name: string;
  countryCode: string;
  city: string;
  address: string;
  latitude: string;
  longitude: string;
  siteType: SiteType;
  status: SiteStatus;
  acquiredOn: string;
  monthlySiteCost: string;
  notes: string;
  towerCode: string;
  structureType: TowerStructureType;
  heightMeters: string;
  tenantCapacity: string;
  antennaMountCapacity: string;
}

const emptyForm: FormState = {
  siteCode: '',
  name: '',
  countryCode: 'DE',
  city: '',
  address: '',
  latitude: '52.5200',
  longitude: '13.4050',
  siteType: 'Greenfield',
  status: 'Planned',
  acquiredOn: todayIso(),
  monthlySiteCost: '450',
  notes: '',
  towerCode: '',
  structureType: 'Lattice',
  heightMeters: '35',
  tenantCapacity: '3',
  antennaMountCapacity: '8'
};

/** Use case 1: register a site (optionally with its first tower) or update its details. */
export function SiteFormPage({ mode }: { mode: 'create' | 'edit' }) {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const isEdit = mode === 'edit';
  const [form, setForm] = useState<FormState>(emptyForm);
  const [error, setError] = useState<{ message: string; code?: string; fields?: Record<string, string[]> } | null>(null);

  const lookups = useQuery({ queryKey: ['lookups'], queryFn: api.lookups.get, staleTime: 5 * 60_000 });
  const existing = useQuery({
    queryKey: ['site', id],
    queryFn: () => api.sites.get(id!),
    enabled: isEdit && Boolean(id)
  });

  useEffect(() => {
    if (!existing.data) return;

    const site = existing.data;
    const firstTower = site.towers[0];

    setForm({
      siteCode: site.siteCode,
      name: site.name,
      countryCode: site.countryCode,
      city: site.city,
      address: site.address,
      latitude: String(site.latitude),
      longitude: String(site.longitude),
      siteType: site.siteType,
      status: site.status,
      acquiredOn: site.acquiredOn ?? '',
      monthlySiteCost: site.monthlySiteCost === null ? '' : String(site.monthlySiteCost),
      notes: site.notes ?? '',
      towerCode: firstTower?.towerCode ?? '',
      structureType: firstTower?.structureType ?? 'Lattice',
      heightMeters: String(firstTower?.heightMeters ?? 35),
      tenantCapacity: String(firstTower?.tenantCapacity ?? 3),
      antennaMountCapacity: String(firstTower?.antennaMountCapacity ?? 8)
    });
  }, [existing.data]);

  const save = useMutation({
    mutationFn: async () => {
      const shared = {
        name: form.name.trim(),
        countryCode: form.countryCode,
        city: form.city.trim(),
        address: form.address.trim(),
        latitude: Number(form.latitude),
        longitude: Number(form.longitude),
        siteType: form.siteType,
        acquiredOn: form.acquiredOn || null,
        monthlySiteCost: form.monthlySiteCost === '' ? null : Number(form.monthlySiteCost),
        notes: form.notes.trim() || null
      };

      if (isEdit && id) {
        return api.sites.update(id, shared);
      }

      return api.sites.create({
        ...shared,
        siteCode: form.siteCode.trim().toUpperCase(),
        status: form.status,
        tower: form.towerCode
          ? {
              towerCode: form.towerCode.trim().toUpperCase(),
              structureType: form.structureType,
              heightMeters: Number(form.heightMeters),
              tenantCapacity: Number(form.tenantCapacity),
              antennaMountCapacity: Number(form.antennaMountCapacity),
              commissionedOn: null
            }
          : null
      });
    },
    onSuccess: async (saved) => {
      await queryClient.invalidateQueries({ queryKey: ['sites'] });
      await queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      navigate(`/sites/${saved.id}`);
    },
    onError: (cause) =>
      setError(
        cause instanceof ApiError
          ? { message: cause.message, code: cause.code, fields: cause.errors }
          : { message: 'The site could not be saved.' }
      )
  });

  const update = <K extends keyof FormState>(key: K, value: FormState[K]) =>
    setForm((current) => ({ ...current, [key]: value }));

  const fieldError = (name: string) => error?.fields?.[name]?.[0];

  if (isEdit && existing.isLoading) {
    return <div className="py-20 text-center text-sm text-slate-500">Loading site…</div>;
  }

  return (
    <div className="space-y-5">
      <Link
        to={isEdit && id ? `/sites/${id}` : '/sites'}
        className="text-sm font-medium text-indigo-600 hover:underline"
      >
        ← Back
      </Link>

      <PageHeader
        title={isEdit ? 'Edit site' : 'Register a new site'}
        subtitle={
          isEdit
            ? 'Update the operational details of this site. The site code is immutable.'
            : 'Register the site and, optionally, its first tower in one step.'
        }
      />

      <form
        className="space-y-4"
        onSubmit={(event) => {
          event.preventDefault();
          setError(null);
          save.mutate();
        }}
      >
        {error && (
          <ErrorBanner
            message={error.fields ? 'Please fix the highlighted fields.' : error.message}
            code={error.code}
            onDismiss={() => setError(null)}
          />
        )}

        <section className="card p-5">
          <h2 className="text-sm font-semibold text-slate-800">Site details</h2>
          <div className="mt-4 grid gap-4 md:grid-cols-2">
            <Field
              label="Site code"
              htmlFor="siteCode"
              error={fieldError('SiteCode')}
              hint="Format: CC-CCC-NNNN, e.g. DE-BER-0142"
            >
              <input
                id="siteCode"
                className="input font-mono uppercase"
                value={form.siteCode}
                onChange={(event) => update('siteCode', event.target.value)}
                disabled={isEdit}
                required={!isEdit}
              />
            </Field>

            <Field label="Site name" htmlFor="name" error={fieldError('Name')}>
              <input
                id="name"
                className="input"
                value={form.name}
                onChange={(event) => update('name', event.target.value)}
                required
              />
            </Field>

            <Field label="Country" htmlFor="countryCode">
              <select
                id="countryCode"
                className="input"
                value={form.countryCode}
                onChange={(event) => update('countryCode', event.target.value)}
              >
                {(lookups.data?.countries ?? []).map((item) => (
                  <option key={item.value} value={item.value}>
                    {item.label}
                  </option>
                ))}
              </select>
            </Field>

            <Field label="City" htmlFor="city">
              <input
                id="city"
                className="input"
                value={form.city}
                onChange={(event) => update('city', event.target.value)}
                required
              />
            </Field>

            <div className="md:col-span-2">
              <Field label="Address" htmlFor="address">
                <input
                  id="address"
                  className="input"
                  value={form.address}
                  onChange={(event) => update('address', event.target.value)}
                  required
                />
              </Field>
            </div>

            <Field label="Latitude" htmlFor="latitude" error={fieldError('Latitude')}>
              <input
                id="latitude"
                className="input"
                type="number"
                step="0.0001"
                value={form.latitude}
                onChange={(event) => update('latitude', event.target.value)}
              />
            </Field>

            <Field label="Longitude" htmlFor="longitude" error={fieldError('Longitude')}>
              <input
                id="longitude"
                className="input"
                type="number"
                step="0.0001"
                value={form.longitude}
                onChange={(event) => update('longitude', event.target.value)}
              />
            </Field>

            <Field label="Site type" htmlFor="siteType">
              <select
                id="siteType"
                className="input"
                value={form.siteType}
                onChange={(event) => update('siteType', event.target.value as SiteType)}
              >
                {(lookups.data?.siteTypes ?? []).map((item) => (
                  <option key={item.value} value={item.value}>
                    {item.label}
                  </option>
                ))}
              </select>
            </Field>

            {!isEdit && (
              <Field label="Initial status" htmlFor="status" hint="Tenancies can only be created on Active sites.">
                <select
                  id="status"
                  className="input"
                  value={form.status}
                  onChange={(event) => update('status', event.target.value as SiteStatus)}
                >
                  {(lookups.data?.siteStatuses ?? []).map((item) => (
                    <option key={item.value} value={item.value}>
                      {item.label}
                    </option>
                  ))}
                </select>
              </Field>
            )}

            <Field label="Acquired on" htmlFor="acquiredOn">
              <input
                id="acquiredOn"
                className="input"
                type="date"
                value={form.acquiredOn}
                onChange={(event) => update('acquiredOn', event.target.value)}
              />
            </Field>

            <Field label="Ground lease cost / month" htmlFor="monthlySiteCost">
              <input
                id="monthlySiteCost"
                className="input"
                type="number"
                min={0}
                step="0.01"
                value={form.monthlySiteCost}
                onChange={(event) => update('monthlySiteCost', event.target.value)}
              />
            </Field>

            <div className="md:col-span-2">
              <Field label="Notes" htmlFor="notes">
                <textarea
                  id="notes"
                  className="input"
                  rows={2}
                  value={form.notes}
                  onChange={(event) => update('notes', event.target.value)}
                />
              </Field>
            </div>
          </div>
        </section>

        {!isEdit && (
          <section className="card p-5">
            <h2 className="text-sm font-semibold text-slate-800">First tower (optional)</h2>
            <p className="mt-1 text-xs text-slate-500">Leave the tower code empty to register the site on its own.</p>
            <div className="mt-4 grid gap-4 md:grid-cols-2">
              <Field label="Tower code" htmlFor="towerCode" error={fieldError('Tower.TowerCode')}>
                <input
                  id="towerCode"
                  className="input font-mono uppercase"
                  value={form.towerCode}
                  onChange={(event) => update('towerCode', event.target.value)}
                  placeholder={`${form.siteCode || 'DE-BER-0142'}-T1`}
                />
              </Field>

              <Field label="Structure type" htmlFor="structureType">
                <select
                  id="structureType"
                  className="input"
                  value={form.structureType}
                  onChange={(event) => update('structureType', event.target.value as TowerStructureType)}
                >
                  {(lookups.data?.towerStructureTypes ?? []).map((item) => (
                    <option key={item.value} value={item.value}>
                      {item.label}
                    </option>
                  ))}
                </select>
              </Field>

              <Field label="Height (m)" htmlFor="heightMeters">
                <input
                  id="heightMeters"
                  className="input"
                  type="number"
                  min={5}
                  max={250}
                  value={form.heightMeters}
                  onChange={(event) => update('heightMeters', event.target.value)}
                />
              </Field>

              <Field label="Tenant capacity" htmlFor="tenantCapacity" hint="Maximum concurrent tenants.">
                <input
                  id="tenantCapacity"
                  className="input"
                  type="number"
                  min={1}
                  max={50}
                  value={form.tenantCapacity}
                  onChange={(event) => update('tenantCapacity', event.target.value)}
                />
              </Field>

              <Field label="Antenna mount capacity" htmlFor="antennaMountCapacity">
                <input
                  id="antennaMountCapacity"
                  className="input"
                  type="number"
                  min={0}
                  max={200}
                  value={form.antennaMountCapacity}
                  onChange={(event) => update('antennaMountCapacity', event.target.value)}
                />
              </Field>
            </div>
          </section>
        )}

        <div className="flex items-center gap-2">
          <button type="submit" className="btn-primary" disabled={save.isPending}>
            {save.isPending && <Spinner className="border-white/40 border-t-white" />}
            {isEdit ? 'Save changes' : 'Register site'}
          </button>
          <Link to={isEdit && id ? `/sites/${id}` : '/sites'} className="btn-secondary">
            Cancel
          </Link>
        </div>
      </form>
    </div>
  );
}
