import type { ReactNode } from 'react';

export function Spinner({ className = '' }: { className?: string }) {
  return (
    <span
      className={`inline-block h-4 w-4 animate-spin rounded-full border-2 border-slate-300 border-t-indigo-600 ${className}`}
      aria-hidden="true"
    />
  );
}

export function CenteredSpinner({ label }: { label?: string }) {
  return (
    <div className="flex min-h-[60vh] flex-col items-center justify-center gap-3 text-slate-500">
      <Spinner className="h-8 w-8 border-4" />
      {label && <p className="text-sm">{label}</p>}
    </div>
  );
}

export function ErrorBanner({
  message,
  onDismiss,
  code
}: {
  message: string;
  code?: string;
  onDismiss?: () => void;
}) {
  return (
    <div className="flex items-start gap-3 rounded-lg border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-800">
      <span className="mt-0.5" aria-hidden="true">
        ⚠
      </span>
      <div className="flex-1">
        <p className="font-medium">{message}</p>
        {code && <p className="mt-1 font-mono text-xs text-rose-600">{code}</p>}
      </div>
      {onDismiss && (
        <button type="button" onClick={onDismiss} className="text-rose-500 hover:text-rose-700" aria-label="Dismiss">
          ✕
        </button>
      )}
    </div>
  );
}

export function SuccessBanner({ message, onDismiss }: { message: string; onDismiss?: () => void }) {
  return (
    <div className="flex items-start gap-3 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800">
      <span className="mt-0.5" aria-hidden="true">
        ✓
      </span>
      <p className="flex-1 font-medium">{message}</p>
      {onDismiss && (
        <button type="button" onClick={onDismiss} className="text-emerald-600 hover:text-emerald-800" aria-label="Dismiss">
          ✕
        </button>
      )}
    </div>
  );
}

export function EmptyState({
  title,
  description,
  action
}: {
  title: string;
  description?: string;
  action?: ReactNode;
}) {
  return (
    <div className="flex flex-col items-center justify-center gap-2 rounded-xl border border-dashed border-slate-300 bg-white px-6 py-12 text-center">
      <p className="text-sm font-semibold text-slate-700">{title}</p>
      {description && <p className="max-w-md text-sm text-slate-500">{description}</p>}
      {action}
    </div>
  );
}

export function PageHeader({
  title,
  subtitle,
  actions
}: {
  title: string;
  subtitle?: ReactNode;
  actions?: ReactNode;
}) {
  return (
    <div className="mb-6 flex flex-wrap items-start justify-between gap-4">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight text-slate-900">{title}</h1>
        {subtitle && <p className="mt-1 text-sm text-slate-500">{subtitle}</p>}
      </div>
      {actions && <div className="flex flex-wrap items-center gap-2">{actions}</div>}
    </div>
  );
}
