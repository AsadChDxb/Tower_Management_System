import { NavLink, Outlet } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { Chip } from './Badges';

export interface NavItem {
  to: string;
  label: string;
  icon: string;
  permission: string;
  description: string;
}

/**
 * The single source of truth for navigation. Every entry declares the permission it needs,
 * and the sidebar renders only the entries the signed-in user's permissions cover - so the
 * menu automatically follows the role (Operations, Commercial, Manager, Admin).
 */
export const navItems: NavItem[] = [
  {
    to: '/dashboard',
    label: 'Dashboard',
    icon: '📊',
    permission: 'Dashboard.View',
    description: 'Portfolio overview, map and utilisation'
  },
  {
    to: '/sites',
    label: 'Sites',
    icon: '🗼',
    permission: 'Portfolio.View',
    description: 'Search, filter and open tower sites'
  },
  {
    to: '/operators',
    label: 'Operators',
    icon: '📡',
    permission: 'Portfolio.View',
    description: 'Tenant companies leasing our towers'
  },
  {
    to: '/tenancies',
    label: 'Tenancies',
    icon: '📄',
    permission: 'Portfolio.View',
    description: 'Active and historical lease agreements'
  },
  {
    to: '/users',
    label: 'Users',
    icon: '👥',
    permission: 'Users.Manage',
    description: 'Accounts, roles and access'
  }
];

export function Layout() {
  const { user, logout, hasPermission } = useAuth();
  const visibleItems = navItems.filter((item) => hasPermission(item.permission));

  return (
    <div className="flex min-h-screen bg-slate-50">
      <aside className="hidden w-64 flex-col border-r border-slate-200 bg-white lg:flex">
        <div className="flex items-center gap-2 border-b border-slate-200 px-5 py-4">
          <span className="text-xl" aria-hidden="true">
            🗼
          </span>
          <div>
            <p className="text-sm font-semibold text-slate-900">TowerCo</p>
            <p className="text-xs text-slate-500">Tower Management System</p>
          </div>
        </div>

        <nav className="flex-1 space-y-1 px-3 py-4">
          {visibleItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                `flex items-start gap-3 rounded-lg px-3 py-2 text-sm transition ${
                  isActive
                    ? 'bg-indigo-50 font-semibold text-indigo-700'
                    : 'text-slate-700 hover:bg-slate-50'
                }`
              }
              title={item.description}
            >
              <span aria-hidden="true">{item.icon}</span>
              <span>{item.label}</span>
            </NavLink>
          ))}
        </nav>

        <div className="border-t border-slate-200 px-4 py-3 text-xs text-slate-500">
          Signed in as
          <p className="truncate font-medium text-slate-700">{user?.email}</p>
        </div>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-200 bg-white px-4 py-3 lg:px-8">
          <div className="flex items-center gap-3 lg:hidden">
            <span className="text-lg" aria-hidden="true">
              🗼
            </span>
            <span className="text-sm font-semibold">TowerCo</span>
          </div>

          <div className="flex flex-wrap items-center gap-2 lg:ml-auto">
            {user?.roles.map((role) => (
              <Chip key={role} tone={role === 'Admin' ? 'rose' : role === 'Manager' ? 'amber' : 'indigo'}>
                {role}
              </Chip>
            ))}
            <span className="text-sm font-medium text-slate-700">{user?.displayName}</span>
            <button type="button" onClick={() => void logout()} className="btn-secondary">
              Sign out
            </button>
          </div>
        </header>

        <nav className="flex gap-1 overflow-x-auto border-b border-slate-200 bg-white px-3 py-2 lg:hidden">
          {visibleItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                `whitespace-nowrap rounded-md px-3 py-1.5 text-sm ${
                  isActive ? 'bg-indigo-50 font-semibold text-indigo-700' : 'text-slate-600'
                }`
              }
            >
              {item.icon} {item.label}
            </NavLink>
          ))}
        </nav>

        <main className="flex-1 px-4 py-6 lg:px-8">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
