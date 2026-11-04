import { Navigate, Route, Routes } from 'react-router-dom';
import { RequireAuth, RequirePermission } from './auth/guards';
import { Layout } from './components/Layout';
import { NotFoundPage } from './components/ForbiddenPage';
import { LoginPage } from './features/auth/LoginPage';
import { DashboardPage } from './features/dashboard/DashboardPage';
import { SitesPage } from './features/sites/SitesPage';
import { SiteDetailPage } from './features/sites/SiteDetailPage';
import { SiteFormPage } from './features/sites/SiteFormPage';
import { OperatorsPage } from './features/operators/OperatorsPage';
import { TenanciesPage } from './features/tenancies/TenanciesPage';
import { UsersPage } from './features/users/UsersPage';

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />

      <Route element={<RequireAuth />}>
        <Route element={<Layout />}>
          <Route index element={<Navigate to="/dashboard" replace />} />

          <Route element={<RequirePermission permission="Dashboard.View" />}>
            <Route path="/dashboard" element={<DashboardPage />} />
          </Route>

          <Route element={<RequirePermission permission="Portfolio.View" />}>
            <Route path="/sites" element={<SitesPage />} />
            <Route path="/sites/:id" element={<SiteDetailPage />} />
            <Route path="/operators" element={<OperatorsPage />} />
            <Route path="/tenancies" element={<TenanciesPage />} />
          </Route>

          <Route element={<RequirePermission permission="Sites.Manage" />}>
            <Route path="/sites/new" element={<SiteFormPage mode="create" />} />
            <Route path="/sites/:id/edit" element={<SiteFormPage mode="edit" />} />
          </Route>

          <Route element={<RequirePermission permission="Users.Manage" />}>
            <Route path="/users" element={<UsersPage />} />
          </Route>

          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Route>
    </Routes>
  );
}
