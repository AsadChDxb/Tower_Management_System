import { Navigate, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from './AuthContext';
import { CenteredSpinner } from '../components/Feedback';
import { ForbiddenPage } from '../components/ForbiddenPage';

/** Blocks anonymous visitors and remembers where they wanted to go. */
export function RequireAuth() {
  const { user, initializing } = useAuth();
  const location = useLocation();

  if (initializing) {
    return <CenteredSpinner label="Restoring your session..." />;
  }

  if (!user) {
    return <Navigate to="/login" replace state={{ from: `${location.pathname}${location.search}` }} />;
  }

  return <Outlet />;
}

/** Route-level permission guard - mirrors the API policy of the same name. */
export function RequirePermission({ permission }: { permission: string }) {
  const { hasPermission } = useAuth();

  return hasPermission(permission) ? <Outlet /> : <ForbiddenPage permission={permission} />;
}
