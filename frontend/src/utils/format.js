export const STATUSES = [
  { value: 'ToDo', label: 'To Do' },
  { value: 'InProgress', label: 'In Progress' },
  { value: 'Done', label: 'Done' }
];

export const PRIORITIES = [
  { value: 'Low', label: 'Low' },
  { value: 'Medium', label: 'Medium' },
  { value: 'High', label: 'High' }
];

export const ROLES = ['Admin', 'Manager', 'User'];

export const statusLabel = (s) => STATUSES.find((x) => x.value === s)?.label ?? s;

export function formatDate(value) {
  if (!value) return '—';
  return new Date(value).toLocaleDateString(undefined, { year: 'numeric', month: 'short', day: 'numeric' });
}

export function formatDateTime(value) {
  if (!value) return '';
  // API returns UTC timestamps without a "Z" suffix.
  const iso = /Z|[+-]\d\d:\d\d$/.test(value) ? value : `${value}Z`;
  return new Date(iso).toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' });
}

/** yyyy-MM-dd for <input type="date"> */
export const toDateInput = (value) => (value ? value.substring(0, 10) : '');
