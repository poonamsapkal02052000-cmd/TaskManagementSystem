import { PRIORITIES, STATUSES } from '../utils/format';

/** Shared filter bar for status, priority and deadline range. */
export default function TaskFilters({ filters, onChange, showSearch = false, extra = null }) {
  const set = (field) => (e) => onChange({ ...filters, [field]: e.target.value });
  const hasFilters = Object.values(filters).some((v) => v !== '' && v !== undefined);

  return (
    <div className="filters" role="search" aria-label="Filter tasks">
      {showSearch && (
        <div className="field">
          <label htmlFor="f-search">Search</label>
          <input id="f-search" type="search" placeholder="Title or description" value={filters.search ?? ''} onChange={set('search')} />
        </div>
      )}
      <div className="field">
        <label htmlFor="f-status">Status</label>
        <select id="f-status" value={filters.status ?? ''} onChange={set('status')}>
          <option value="">All</option>
          {STATUSES.map((s) => <option key={s.value} value={s.value}>{s.label}</option>)}
        </select>
      </div>
      <div className="field">
        <label htmlFor="f-priority">Priority</label>
        <select id="f-priority" value={filters.priority ?? ''} onChange={set('priority')}>
          <option value="">All</option>
          {PRIORITIES.map((p) => <option key={p.value} value={p.value}>{p.label}</option>)}
        </select>
      </div>
      <div className="field">
        <label htmlFor="f-from">Due from</label>
        <input id="f-from" type="date" value={filters.dueFrom ?? ''} onChange={set('dueFrom')} />
      </div>
      <div className="field">
        <label htmlFor="f-to">Due to</label>
        <input id="f-to" type="date" value={filters.dueTo ?? ''} onChange={set('dueTo')} />
      </div>
      {extra}
      {hasFilters && (
        <button type="button" className="btn btn-ghost btn-sm filters-clear"
          onClick={() => onChange(Object.fromEntries(Object.keys(filters).map((k) => [k, ''])))}>
          Clear filters
        </button>
      )}
    </div>
  );
}
