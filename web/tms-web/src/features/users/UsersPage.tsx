import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from '../../api/api';
import { ApiError } from '../../api/client';
import type { UserDto } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { Chip, StatusBadge } from '../../components/Badges';
import { CenteredSpinner, ErrorBanner, PageHeader, SuccessBanner } from '../../components/Feedback';
import { Field, Modal } from '../../components/Modal';
import { formatDateTime } from '../../lib/format';

export function UsersPage() {
  const { user: currentUser } = useAuth();
  const [creating, setCreating] = useState(false);
  const [managing, setManaging] = useState<UserDto | null>(null);
  const [feedback, setFeedback] = useState<{ kind: 'error' | 'success'; message: string; code?: string } | null>(
    null
  );

  const users = useQuery({ queryKey: ['users'], queryFn: api.users.list });
  const lookups = useQuery({ queryKey: ['lookups'], queryFn: api.lookups.get, staleTime: 5 * 60_000 });

  return (
    <div className="space-y-5">
      <PageHeader
        title="Users"
        subtitle="Accounts, roles and access. Menus and API permissions both derive from the assigned roles."
        actions={
          <button type="button" className="btn-primary" onClick={() => setCreating(true)}>
            + New user
          </button>
        }
      />

      {feedback?.kind === 'error' && (
        <ErrorBanner message={feedback.message} code={feedback.code} onDismiss={() => setFeedback(null)} />
      )}
      {feedback?.kind === 'success' && <SuccessBanner message={feedback.message} onDismiss={() => setFeedback(null)} />}

      {users.isError && <ErrorBanner message="Users could not be loaded." />}

      <div className="card overflow-hidden">
        {users.isLoading ? (
          <CenteredSpinner label="Loading users..." />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead>
                <tr>
                  <th className="th">Name</th>
                  <th className="th">E-mail</th>
                  <th className="th">Roles</th>
                  <th className="th">State</th>
                  <th className="th">Created</th>
                  <th className="th" />
                </tr>
              </thead>
              <tbody>
                {(users.data ?? []).map((user) => (
                  <tr key={user.id}>
                    <td className="td font-medium text-slate-800">
                      {user.displayName}
                      {user.id === currentUser?.id && <span className="ml-2 text-xs text-slate-400">(you)</span>}
                    </td>
                    <td className="td text-xs text-slate-600">{user.email}</td>
                    <td className="td">
                      <span className="flex flex-wrap gap-1">
                        {user.roles.map((role) => (
                          <Chip key={role} tone={role === 'Admin' ? 'rose' : role === 'Manager' ? 'amber' : 'indigo'}>
                            {role}
                          </Chip>
                        ))}
                      </span>
                    </td>
                    <td className="td">
                      <StatusBadge status={user.isActive ? 'Active' : 'Decommissioned'} />
                    </td>
                    <td className="td text-xs text-slate-500">{formatDateTime(user.createdAtUtc)}</td>
                    <td className="td text-right">
                      <button
                        type="button"
                        className="text-xs font-semibold text-indigo-600 hover:underline"
                        onClick={() => setManaging(user)}
                      >
                        Manage
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {creating && (
        <CreateUserModal
          roles={(lookups.data?.roles ?? []).map((role) => role.value)}
          onClose={() => setCreating(false)}
          onCreated={(message) => setFeedback({ kind: 'success', message })}
        />
      )}

      {managing && (
        <ManageUserModal
          user={managing}
          roles={(lookups.data?.roles ?? []).map((role) => role.value)}
          isSelf={managing.id === currentUser?.id}
          onClose={() => setManaging(null)}
          onSaved={(message) => setFeedback({ kind: 'success', message })}
        />
      )}
    </div>
  );
}

function RolePicker({
  options,
  selected,
  onToggle,
  disabledRoles = []
}: {
  options: string[];
  selected: string[];
  onToggle: (role: string) => void;
  disabledRoles?: string[];
}) {
  return (
    <div className="grid grid-cols-2 gap-2">
      {options.map((role) => (
        <label
          key={role}
          className={`flex items-center gap-2 rounded-lg border px-3 py-2 text-sm ${
            selected.includes(role) ? 'border-indigo-300 bg-indigo-50' : 'border-slate-200 bg-white'
          } ${disabledRoles.includes(role) ? 'opacity-60' : ''}`}
        >
          <input
            type="checkbox"
            checked={selected.includes(role)}
            disabled={disabledRoles.includes(role)}
            onChange={() => onToggle(role)}
          />
          {role}
        </label>
      ))}
    </div>
  );
}

function CreateUserModal({
  roles,
  onClose,
  onCreated
}: {
  roles: string[];
  onClose: () => void;
  onCreated: (message: string) => void;
}) {
  const queryClient = useQueryClient();

  const [email, setEmail] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [password, setPassword] = useState('TowerCo!2026');
  const [selectedRoles, setSelectedRoles] = useState<string[]>(['Manager']);
  const [error, setError] = useState<{ message: string; code?: string } | null>(null);

  const create = useMutation({
    mutationFn: () =>
      api.users.create({ email: email.trim(), displayName: displayName.trim(), password, roles: selectedRoles }),
    onSuccess: async (created) => {
      await queryClient.invalidateQueries({ queryKey: ['users'] });
      onCreated(`User ${created.email} created with roles ${created.roles.join(', ')}.`);
      onClose();
    },
    onError: (cause) =>
      setError(
        cause instanceof ApiError
          ? { message: cause.message, code: cause.code }
          : { message: 'The user could not be created.' }
      )
  });

  return (
    <Modal
      title="Create a user"
      description="Pick the roles that match the person's job - the menu and API permissions follow automatically."
      onClose={onClose}
      footer={
        <>
          <button type="button" className="btn-secondary" onClick={onClose}>
            Cancel
          </button>
          <button type="submit" form="create-user-form" className="btn-primary" disabled={create.isPending}>
            Create user
          </button>
        </>
      }
    >
      <form
        id="create-user-form"
        className="space-y-3"
        onSubmit={(event) => {
          event.preventDefault();
          setError(null);
          create.mutate();
        }}
      >
        {error && <ErrorBanner message={error.message} code={error.code} onDismiss={() => setError(null)} />}

        <Field label="E-mail" htmlFor="newEmail">
          <input
            id="newEmail"
            className="input"
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            required
          />
        </Field>

        <Field label="Display name" htmlFor="newDisplayName">
          <input
            id="newDisplayName"
            className="input"
            value={displayName}
            onChange={(event) => setDisplayName(event.target.value)}
            required
          />
        </Field>

        <Field label="Initial password" htmlFor="newPassword" hint="Minimum 8 characters, with upper, lower and a digit.">
          <input
            id="newPassword"
            className="input"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            required
          />
        </Field>

        <Field label="Roles">
          <RolePicker
            options={roles}
            selected={selectedRoles}
            onToggle={(role) =>
              setSelectedRoles((current) =>
                current.includes(role) ? current.filter((item) => item !== role) : [...current, role]
              )
            }
          />
        </Field>
      </form>
    </Modal>
  );
}

function ManageUserModal({
  user,
  roles,
  isSelf,
  onClose,
  onSaved
}: {
  user: UserDto;
  roles: string[];
  isSelf: boolean;
  onClose: () => void;
  onSaved: (message: string) => void;
}) {
  const queryClient = useQueryClient();

  const [selectedRoles, setSelectedRoles] = useState<string[]>(user.roles);
  const [isActive, setIsActive] = useState(user.isActive);
  const [newPassword, setNewPassword] = useState('');
  const [error, setError] = useState<{ message: string; code?: string } | null>(null);

  const save = useMutation({
    mutationFn: () => api.users.update(user.id, { roles: selectedRoles, isActive }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['users'] });
      onSaved(`User ${user.email} updated.`);
      onClose();
    },
    onError: (cause) =>
      setError(
        cause instanceof ApiError
          ? { message: cause.message, code: cause.code }
          : { message: 'The user could not be updated.' }
      )
  });

  const resetPassword = useMutation({
    mutationFn: () => api.users.resetPassword(user.id, newPassword),
    onSuccess: () => {
      onSaved(`Password reset for ${user.email}.`);
      onClose();
    },
    onError: (cause) =>
      setError(
        cause instanceof ApiError
          ? { message: cause.message, code: cause.code }
          : { message: 'The password could not be reset.' }
      )
  });

  return (
    <Modal
      title={`Manage ${user.displayName}`}
      description={user.email}
      onClose={onClose}
      footer={
        <>
          <button type="button" className="btn-secondary" onClick={onClose}>
            Close
          </button>
          <button type="button" className="btn-primary" disabled={save.isPending} onClick={() => save.mutate()}>
            Save changes
          </button>
        </>
      }
    >
      <div className="space-y-4">
        {error && <ErrorBanner message={error.message} code={error.code} onDismiss={() => setError(null)} />}

        <Field label="Roles" hint={isSelf ? 'You cannot remove your own Admin role.' : undefined}>
          <RolePicker
            options={roles}
            selected={selectedRoles}
            disabledRoles={isSelf ? ['Admin'] : []}
            onToggle={(role) =>
              setSelectedRoles((current) =>
                current.includes(role) ? current.filter((item) => item !== role) : [...current, role]
              )
            }
          />
        </Field>

        <label className="flex items-center gap-2 text-sm text-slate-700">
          <input
            type="checkbox"
            checked={isActive}
            disabled={isSelf}
            onChange={(event) => setIsActive(event.target.checked)}
          />
          Account is active (deactivated users cannot sign in)
        </label>

        <div className="rounded-lg border border-slate-200 p-3">
          <p className="text-sm font-medium text-slate-700">Reset password</p>
          <div className="mt-2 flex gap-2">
            <input
              className="input"
              placeholder="New password"
              value={newPassword}
              onChange={(event) => setNewPassword(event.target.value)}
            />
            <button
              type="button"
              className="btn-secondary whitespace-nowrap"
              disabled={newPassword.length < 8 || resetPassword.isPending}
              onClick={() => {
                setError(null);
                resetPassword.mutate();
              }}
            >
              Reset
            </button>
          </div>
        </div>
      </div>
    </Modal>
  );
}
