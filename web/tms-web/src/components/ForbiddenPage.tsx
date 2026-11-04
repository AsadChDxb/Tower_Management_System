import { Link } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';

export function ForbiddenPage({ permission }: { permission?: string }) {
  const { user } = useAuth();

  return (
    <div className="flex min-h-[60vh] flex-col items-center justify-center gap-3 text-center">
      <p className="text-5xl" aria-hidden="true">
        🔒
      </p>
      <h1 className="text-2xl font-semibold text-slate-900">You do not have access to this page</h1>
      <p className="max-w-md text-sm text-slate-500">
        {permission ? (
          <>
            This area needs the <code className="rounded bg-slate-100 px-1.5 py-0.5 font-mono text-xs">{permission}</code>{' '}
            permission. Your roles: <strong>{user?.roles.join(', ') || 'none'}</strong>.
          </>
        ) : (
          'Ask an administrator if you need access.'
        )}
      </p>
      <Link to="/dashboard" className="btn-primary mt-2">
        Back to dashboard
      </Link>
    </div>
  );
}

export function NotFoundPage() {
  return (
    <div className="flex min-h-[60vh] flex-col items-center justify-center gap-3 text-center">
      <p className="text-5xl" aria-hidden="true">
        🧭
      </p>
      <h1 className="text-2xl font-semibold text-slate-900">Page not found</h1>
      <Link to="/dashboard" className="btn-primary mt-2">
        Back to dashboard
      </Link>
    </div>
  );
}
