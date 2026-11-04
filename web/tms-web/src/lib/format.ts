const COUNTRY_NAMES: Record<string, string> = {
  DE: 'Germany',
  NL: 'Netherlands',
  FR: 'France',
  ES: 'Spain',
  PL: 'Poland',
  GB: 'United Kingdom',
  BE: 'Belgium',
  AT: 'Austria'
};

export const countryName = (code: string): string => COUNTRY_NAMES[code] ?? code;

export const countryLabel = (code: string): string => `${countryName(code)} (${code})`;

export const formatNumber = (value: number): string => new Intl.NumberFormat('en-GB').format(value);

export const formatCurrency = (value: number, currency = 'EUR'): string =>
  new Intl.NumberFormat('en-GB', {
    style: 'currency',
    currency,
    maximumFractionDigits: 0
  }).format(value);

export const formatPercent = (value: number): string =>
  `${value.toLocaleString('en-GB', { maximumFractionDigits: 1 })}%`;

/** DateOnly values are formatted from their parts so no timezone can shift the day. */
export const formatDate = (value: string | null | undefined): string => {
  if (!value) return '—';

  const dateOnly = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);

  if (dateOnly) {
    const [, year, month, day] = dateOnly;
    return new Date(Number(year), Number(month) - 1, Number(day)).toLocaleDateString('en-GB', {
      year: 'numeric',
      month: 'short',
      day: '2-digit'
    });
  }

  return new Date(value).toLocaleDateString('en-GB', {
    year: 'numeric',
    month: 'short',
    day: '2-digit'
  });
};

export const formatDateTime = (value: string | null | undefined): string =>
  value
    ? new Date(value).toLocaleString('en-GB', {
        year: 'numeric',
        month: 'short',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit'
      })
    : '—';

export type UtilizationTone = 'idle' | 'ok' | 'warn' | 'full';

export const utilizationTone = (percent: number): UtilizationTone => {
  if (percent >= 100) return 'full';
  if (percent >= 75) return 'warn';
  if (percent > 0) return 'ok';
  return 'idle';
};

export const todayIso = (): string => new Date().toISOString().slice(0, 10);
