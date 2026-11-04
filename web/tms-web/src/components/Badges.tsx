import type { ReactNode } from 'react';
import { formatPercent, utilizationTone } from '../lib/format';

const statusStyles: Record<string, string> = {
  Active: 'bg-emerald-50 text-emerald-700 ring-emerald-200',
  Planned: 'bg-sky-50 text-sky-700 ring-sky-200',
  Decommissioned: 'bg-slate-100 text-slate-600 ring-slate-200',
  Pending: 'bg-amber-50 text-amber-700 ring-amber-200',
  Terminated: 'bg-slate-100 text-slate-500 ring-slate-200'
};

export function StatusBadge({ status }: { status: string }) {
  const style = statusStyles[status] ?? 'bg-slate-100 text-slate-600 ring-slate-200';

  return (
    <span
      className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset ${style}`}
    >
      {status}
    </span>
  );
}

const toneBarStyles: Record<string, string> = {
  idle: 'bg-slate-300',
  ok: 'bg-emerald-500',
  warn: 'bg-amber-500',
  full: 'bg-rose-500'
};

const toneTextStyles: Record<string, string> = {
  idle: 'text-slate-500',
  ok: 'text-emerald-700',
  warn: 'text-amber-700',
  full: 'text-rose-700'
};

export function CapacityBar({
  percent,
  label,
  right
}: {
  percent: number;
  label?: string;
  right?: ReactNode;
}) {
  const clamped = Math.min(100, Math.max(0, percent));
  const tone = utilizationTone(percent);

  return (
    <div className="w-full">
      <div className="flex items-center justify-between gap-2 text-xs">
        <span className="text-slate-500">{label ?? 'Utilization'}</span>
        <span className={`font-semibold ${toneTextStyles[tone]}`}>{right ?? formatPercent(percent)}</span>
      </div>
      <div className="mt-1 h-2 w-full overflow-hidden rounded-full bg-slate-100">
        <div className={`h-full rounded-full ${toneBarStyles[tone]}`} style={{ width: `${clamped}%` }} />
      </div>
    </div>
  );
}

const chipTones: Record<string, string> = {
  slate: 'bg-slate-100 text-slate-700',
  indigo: 'bg-indigo-50 text-indigo-700',
  emerald: 'bg-emerald-50 text-emerald-700',
  amber: 'bg-amber-50 text-amber-700',
  rose: 'bg-rose-50 text-rose-700'
};

export function Chip({ children, tone = 'slate' }: { children: ReactNode; tone?: keyof typeof chipTones }) {
  return (
    <span className={`inline-flex items-center rounded-md px-2 py-0.5 text-xs font-medium ${chipTones[tone]}`}>
      {children}
    </span>
  );
}
