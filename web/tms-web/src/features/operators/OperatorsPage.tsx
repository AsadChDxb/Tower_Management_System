import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '../../api/api';
import { ApiError } from '../../api/client';
import type { Operator } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { CenteredSpinner, ErrorBanner, PageHeader } from '../../components/Feedback';
import { Field, Modal } from '../../components/Modal';
import { countryLabel, formatCurrency } from '../../lib/format';

export function OperatorsPage() {
  const { hasPermission } = useAuth();
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [editing, setEditing] = useState<Operator | 'new' | null>(null);

  const operators = useQuery({
    queryKey: ['operators', search],
    queryFn: () => api.operators.list(search || undefined)
  });

  const canManage = hasPermission('Operators.Manage');
  const totalRevenue = (operators.data ?? []).reduce((sum, operator) => sum + operator.monthlyRevenue, 0);

  return (
    <div className="space-y-5">
      <PageHeader
        title="Operators"
        subtitle={`${operators.data?.length ?? 0} tenant compan(ies) · ${formatCurrency(totalRevenue)} monthly revenue`}
        actions={
          canManage ? (
            <button type="button" className="btn-primary" onClick={() => setEditing('new')}>
              + Add operator
            </button>
          ) : undefined
        }
      />

      <form
        className="card flex flex-wrap items-end gap-3 p-4"
        onSubmit={(event) => {
          event.preventDefault();
          setSearch(searchInput.trim());
        }}
      >
        <div className="min-w-56 flex-1">
          <label className="label" htmlFor="operatorSearch">
            Search
          </label>
          <input
            id="operatorSearch"
            className="input"
            placeholder="Operator name or country code"
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
          />
        </div>
        <button type="submit" className="btn-primary">
          Search
        </button>
        <button
          type="button"
          className="btn-secondary"
          onClick={() => {
            setSearchInput('');
            setSearch('');
          }}
        >
          Reset
        </button>
      </form>

      {operators.isError && <ErrorBanner message="Operators could not be loaded." />}

      <div className="card overflow-hidden">
        {operators.isLoading ? (
          <CenteredSpinner label="Loading operators..." />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr>
                  <th className="th">Operator</th>
                  <th className="th">Home market</th>
                  <th className="th">Contact</th>
                  <th className="th">Active tenancies</th>
                  <th className="th">MRR</th>
                  <th className="th" />
                </tr>
              </thead>
              <tbody>
                {(operators.data ?? []).map((operator) => (
                  <tr key={operator.id}>
                    <td className="td font-medium text-slate-800">{operator.name}</td>
                    <td className="td">{countryLabel(operator.countryCode)}</td>
                    <td className="td text-xs text-slate-500">
                      {operator.contactEmail ?? '—'}
                      <span className="block">{operator.contactPhone ?? ''}</span>
                    </td>
                    <td className="td">{operator.activeTenancies}</td>
                    <td className="td">{formatCurrency(operator.monthlyRevenue)}</td>
                    <td className="td text-right">
                      {canManage && (
                        <button
                          type="button"
                          className="text-xs font-semibold text-indigo-600 hover:underline"
                          onClick={() => setEditing(operator)}
                        >
                          Edit
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {editing && (
        <OperatorFormModal operator={editing === 'new' ? null : editing} onClose={() => setEditing(null)} />
      )}
    </div>
  );
}

function OperatorFormModal({ operator, onClose }: { operator: Operator | null; onClose: () => void }) {
  const queryClient = useQueryClient();
  const lookups = useQuery({ queryKey: ['lookups'], queryFn: api.lookups.get, staleTime: 5 * 60_000 });

  const [name, setName] = useState(operator?.name ?? '');
  const [countryCode, setCountryCode] = useState(operator?.countryCode ?? 'DE');
  const [contactEmail, setContactEmail] = useState(operator?.contactEmail ?? '');
  const [contactPhone, setContactPhone] = useState(operator?.contactPhone ?? '');
  const [error, setError] = useState<{ message: string; code?: string } | null>(null);

  const save = useMutation({
    mutationFn: () => {
      const payload = {
        name,
        countryCode,
        contactEmail: contactEmail || null,
        contactPhone: contactPhone || null
      };

      return operator ? api.operators.update(operator.id, payload) : api.operators.create(payload);
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['operators'] });
      await queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      onClose();
    },
    onError: (cause) =>
      setError(
        cause instanceof ApiError
          ? { message: cause.message, code: cause.code }
          : { message: 'The operator could not be saved.' }
      )
  });

  return (
    <Modal
      title={operator ? `Edit ${operator.name}` : 'Add an operator'}
      onClose={onClose}
      footer={
        <>
          <button type="button" className="btn-secondary" onClick={onClose}>
            Cancel
          </button>
          <button type="submit" form="operator-form" className="btn-primary" disabled={save.isPending}>
            {operator ? 'Save changes' : 'Create operator'}
          </button>
        </>
      }
    >
      <form
        id="operator-form"
        className="grid gap-3 sm:grid-cols-2"
        onSubmit={(event) => {
          event.preventDefault();
          setError(null);
          save.mutate();
        }}
      >
        {error && (
          <div className="sm:col-span-2">
            <ErrorBanner message={error.message} code={error.code} onDismiss={() => setError(null)} />
          </div>
        )}

        <div className="sm:col-span-2">
          <Field label="Company name" htmlFor="operatorName">
            <input
              id="operatorName"
              className="input"
              value={name}
              onChange={(event) => setName(event.target.value)}
              required
            />
          </Field>
        </div>

        <Field label="Home market" htmlFor="operatorCountry">
          <select
            id="operatorCountry"
            className="input"
            value={countryCode}
            onChange={(event) => setCountryCode(event.target.value)}
          >
            {(lookups.data?.countries ?? []).map((item) => (
              <option key={item.value} value={item.value}>
                {item.label}
              </option>
            ))}
          </select>
        </Field>

        <Field label="Contact e-mail" htmlFor="operatorEmail">
          <input
            id="operatorEmail"
            className="input"
            type="email"
            value={contactEmail}
            onChange={(event) => setContactEmail(event.target.value)}
          />
        </Field>

        <div className="sm:col-span-2">
          <Field label="Contact phone" htmlFor="operatorPhone">
            <input
              id="operatorPhone"
              className="input"
              value={contactPhone}
              onChange={(event) => setContactPhone(event.target.value)}
            />
          </Field>
        </div>
      </form>
    </Modal>
  );
}
