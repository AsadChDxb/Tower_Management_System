import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '../../api/api';
import { ApiError } from '../../api/client';
import type { SpaceType, Tower } from '../../api/types';
import { ErrorBanner, Spinner } from '../../components/Feedback';
import { Field, Modal } from '../../components/Modal';
import { formatPercent, todayIso } from '../../lib/format';

/** Use case 2: assign an operator to a tower. The capacity verdict is shown up front and the
 *  API's 409 response is surfaced verbatim if another user grabbed the last slot. */
export function AddTenancyModal({ tower, onClose }: { tower: Tower; onClose: () => void }) {
  const queryClient = useQueryClient();

  const operators = useQuery({ queryKey: ['operators'], queryFn: () => api.operators.list() });
  const lookups = useQuery({ queryKey: ['lookups'], queryFn: api.lookups.get, staleTime: 5 * 60_000 });

  const [operatorId, setOperatorId] = useState('');
  const [spaceType, setSpaceType] = useState<SpaceType>('Antenna');
  const [antennaCount, setAntennaCount] = useState(1);
  const [monthlyRent, setMonthlyRent] = useState(1500);
  const [currency, setCurrency] = useState('EUR');
  const [startDate, setStartDate] = useState(todayIso());
  const [endDate, setEndDate] = useState('');
  const [notes, setNotes] = useState('');
  const [error, setError] = useState<{ message: string; code?: string } | null>(null);

  const assign = useMutation({
    mutationFn: () =>
      api.tenancies.create({
        towerId: tower.id,
        operatorId,
        spaceType,
        antennaCount,
        monthlyRent,
        currency,
        startDate,
        endDate: endDate || null,
        notes: notes || null
      }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['site'] });
      await queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      await queryClient.invalidateQueries({ queryKey: ['tenancies'] });
      await queryClient.invalidateQueries({ queryKey: ['operators'] });
      onClose();
    },
    onError: (cause) =>
      setError(
        cause instanceof ApiError
          ? { message: cause.message, code: cause.code }
          : { message: 'The tenant could not be assigned.' }
      )
  });

  const alreadyLeasing = new Set(
    tower.tenancies.filter((tenancy) => tenancy.status === 'Active').map((tenancy) => tenancy.operatorId)
  );

  return (
    <Modal
      title={`Assign a tenant to ${tower.towerCode}`}
      description="Only active sites can be leased, and a tower can never host more active tenants than its capacity."
      onClose={onClose}
      footer={
        <>
          <button type="button" className="btn-secondary" onClick={onClose}>
            Cancel
          </button>
          <button
            type="submit"
            form="assign-tenancy-form"
            className="btn-primary"
            disabled={assign.isPending || !operatorId}
          >
            {assign.isPending && <Spinner className="border-white/40 border-t-white" />}
            Assign tenant
          </button>
        </>
      }
    >
      <div className="mb-4 rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 text-xs text-slate-600">
        Capacity: <strong>{tower.activeTenants}</strong> of <strong>{tower.tenantCapacity}</strong> slots used ·{' '}
        <strong>{formatPercent(tower.utilizationPercent)}</strong> utilized ·{' '}
        {tower.canAcceptTenant ? (
          <span className="font-semibold text-emerald-700">{tower.availableSlots} slot(s) free</span>
        ) : (
          <span className="font-semibold text-rose-700">tower is full</span>
        )}
      </div>

      <form
        id="assign-tenancy-form"
        className="grid gap-3 sm:grid-cols-2"
        onSubmit={(event) => {
          event.preventDefault();
          setError(null);
          assign.mutate();
        }}
      >
        {error && (
          <div className="sm:col-span-2">
            <ErrorBanner message={error.message} code={error.code} onDismiss={() => setError(null)} />
          </div>
        )}

        <div className="sm:col-span-2">
          <Field label="Operator" htmlFor="operator" hint="Operators already leasing this tower are marked.">
            <select
              id="operator"
              className="input"
              value={operatorId}
              onChange={(event) => setOperatorId(event.target.value)}
              required
            >
              <option value="">Select an operator…</option>
              {(operators.data ?? []).map((operator) => (
                <option key={operator.id} value={operator.id}>
                  {operator.name} ({operator.countryCode}){alreadyLeasing.has(operator.id) ? ' - already leasing' : ''}
                </option>
              ))}
            </select>
          </Field>
        </div>

        <Field label="Space type" htmlFor="spaceType">
          <select
            id="spaceType"
            className="input"
            value={spaceType}
            onChange={(event) => setSpaceType(event.target.value as SpaceType)}
          >
            {(lookups.data?.spaceTypes ?? []).map((item) => (
              <option key={item.value} value={item.value}>
                {item.label}
              </option>
            ))}
          </select>
        </Field>

        <Field label="Antennas" htmlFor="antennaCount">
          <input
            id="antennaCount"
            className="input"
            type="number"
            min={0}
            max={100}
            value={antennaCount}
            onChange={(event) => setAntennaCount(Number(event.target.value))}
          />
        </Field>

        <Field label="Monthly rent" htmlFor="monthlyRent">
          <input
            id="monthlyRent"
            className="input"
            type="number"
            min={0}
            step="0.01"
            value={monthlyRent}
            onChange={(event) => setMonthlyRent(Number(event.target.value))}
            required
          />
        </Field>

        <Field label="Currency" htmlFor="currency">
          <select
            id="currency"
            className="input"
            value={currency}
            onChange={(event) => setCurrency(event.target.value)}
          >
            {(lookups.data?.currencies ?? []).map((item) => (
              <option key={item.value} value={item.value}>
                {item.label}
              </option>
            ))}
          </select>
        </Field>

        <Field label="Start date" htmlFor="startDate">
          <input
            id="startDate"
            className="input"
            type="date"
            value={startDate}
            onChange={(event) => setStartDate(event.target.value)}
            required
          />
        </Field>

        <Field label="End date" htmlFor="endDate" hint="Leave empty for an open-ended lease.">
          <input
            id="endDate"
            className="input"
            type="date"
            value={endDate}
            onChange={(event) => setEndDate(event.target.value)}
          />
        </Field>

        <div className="sm:col-span-2">
          <Field label="Notes" htmlFor="notes">
            <textarea
              id="notes"
              className="input"
              rows={2}
              value={notes}
              onChange={(event) => setNotes(event.target.value)}
              placeholder="e.g. 5G rollout, phase 2"
            />
          </Field>
        </div>
      </form>
    </Modal>
  );
}
