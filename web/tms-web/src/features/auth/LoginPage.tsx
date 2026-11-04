import { useState } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { ApiError } from '../../api/client';
import { useAuth } from '../../auth/AuthContext';
import { ErrorBanner, Spinner } from '../../components/Feedback';

const DEMO_PASSWORD = 'TowerCo!2026';

const demoAccounts = [
  { email: 'admin@towerco.demo', role: 'Admin', hint: 'everything + user management' },
  { email: 'ops@towerco.demo', role: 'Operations', hint: 'register sites and towers' },
  { email: 'commercial@towerco.demo', role: 'Commercial', hint: 'operators and tenancies' },
  { email: 'manager@towerco.demo', role: 'Manager', hint: 'read-only portfolio + dashboard' }
];

export function LoginPage() {
  const { user, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const redirectTo = (location.state as { from?: string } | null)?.from ?? '/dashboard';

  const [email, setEmail] = useState(demoAccounts[0].email);
  const [password, setPassword] = useState(DEMO_PASSWORD);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  if (user) {
    return <Navigate to={redirectTo} replace />;
  }

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setSubmitting(true);
    setError(null);

    try {
      await login(email.trim(), password);
      navigate(redirectTo, { replace: true });
    } catch (cause) {
      setError(cause instanceof ApiError ? cause.message : 'Unable to sign in. Is the API running?');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="grid min-h-screen lg:grid-cols-2">
      <div className="hidden flex-col justify-between bg-slate-900 p-10 text-slate-100 lg:flex">
        <div className="flex items-center gap-2 text-lg font-semibold">
          <span aria-hidden="true">🗼</span> TowerCo
        </div>

        <div className="max-w-md space-y-4">
          <h1 className="text-3xl font-semibold leading-tight">Tower Management System</h1>
          <p className="text-sm text-slate-300">
            One place for the site portfolio, the towers we operate, the operators who lease them and the
            capacity we still have to sell.
          </p>
          <ul className="space-y-2 text-sm text-slate-400">
            <li>· Register sites and towers, keep the portfolio accurate</li>
            <li>· Assign tenants with live capacity checks</li>
            <li>· Search and filter the portfolio in seconds</li>
            <li>· Visualise coverage and utilisation on a map</li>
          </ul>
        </div>

        <p className="text-xs text-slate-500">
          Demo environment &middot; fictional operator and site data &middot; JWT secured API
        </p>
      </div>

      <div className="flex items-center justify-center px-5 py-10">
        <div className="w-full max-w-md">
          <div className="mb-6 lg:hidden">
            <p className="text-lg font-semibold">🗼 TowerCo</p>
            <p className="text-sm text-slate-500">Tower Management System</p>
          </div>

          <h2 className="text-xl font-semibold text-slate-900">Sign in</h2>
          <p className="mt-1 text-sm text-slate-500">Use a demo account below or type your own credentials.</p>

          <form onSubmit={submit} className="mt-6 space-y-4">
            {error && <ErrorBanner message={error} onDismiss={() => setError(null)} />}

            <div>
              <label className="label" htmlFor="email">
                E-mail
              </label>
              <input
                id="email"
                className="input"
                type="email"
                autoComplete="username"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                required
              />
            </div>

            <div>
              <label className="label" htmlFor="password">
                Password
              </label>
              <input
                id="password"
                className="input"
                type="password"
                autoComplete="current-password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                required
              />
            </div>

            <button type="submit" className="btn-primary w-full" disabled={submitting}>
              {submitting && <Spinner className="border-white/40 border-t-white" />}
              Sign in
            </button>
          </form>

          <div className="mt-8">
            <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">Demo accounts</p>
            <div className="mt-2 grid gap-2">
              {demoAccounts.map((account) => (
                <button
                  key={account.email}
                  type="button"
                  onClick={() => {
                    setEmail(account.email);
                    setPassword(DEMO_PASSWORD);
                  }}
                  className={`flex items-center justify-between gap-3 rounded-lg border px-3 py-2 text-left text-sm transition ${
                    email === account.email
                      ? 'border-indigo-300 bg-indigo-50'
                      : 'border-slate-200 bg-white hover:border-slate-300'
                  }`}
                >
                  <span>
                    <span className="font-medium text-slate-800">{account.role}</span>
                    <span className="block text-xs text-slate-500">{account.hint}</span>
                  </span>
                  <span className="font-mono text-xs text-slate-400">{account.email}</span>
                </button>
              ))}
            </div>
            <p className="mt-2 text-xs text-slate-400">Password for all demo users: {DEMO_PASSWORD}</p>
          </div>
        </div>
      </div>
    </div>
  );
}
