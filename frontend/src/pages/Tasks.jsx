import { useCallback, useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { getErrorMessage } from '../api/client';
import { tasksApi } from '../api/services';
import { Alert, EmptyState, PriorityBadge, Spinner } from '../components/Common';
import TaskFilters from '../components/TaskFilters';
import TaskFormModal from '../components/TaskFormModal';
import { useAuth } from '../context/AuthContext';
import { STATUSES, formatDate } from '../utils/format';

const PAGE_SIZE = 10;
const EMPTY = { search: '', status: '', priority: '', dueFrom: '', dueTo: '' };

export default function Tasks() {
  const { user, canManageTasks } = useAuth();
  const [searchParams, setSearchParams] = useSearchParams();
  const [filters, setFilters] = useState({ ...EMPTY, status: searchParams.get('status') ?? '' });
  const [mineOnly, setMineOnly] = useState(false);
  const [sort, setSort] = useState({ sortBy: 'dueDate', desc: false });
  const [page, setPage] = useState(1);
  const [data, setData] = useState(null);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [showForm, setShowForm] = useState(searchParams.get('new') === '1' && canManageTasks);

  const load = useCallback(async () => {
    setError('');
    try {
      const result = await tasksApi.list({
        ...filters,
        ...sort,
        assignedToId: mineOnly ? user.id : '',
        page,
        pageSize: PAGE_SIZE
      });
      setData(result);
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }, [filters, sort, mineOnly, page, user.id]);

  // Debounce so typing in search does not fire a request per keystroke.
  useEffect(() => {
    const id = setTimeout(load, 250);
    return () => clearTimeout(id);
  }, [load]);

  const changeFilters = (f) => {
    setFilters(f);
    setPage(1);
  };

  const changeStatus = async (task, status) => {
    try {
      await tasksApi.updateStatus(task.id, status);
      setNotice(`"${task.title}" moved to ${STATUSES.find((s) => s.value === status).label}.`);
      load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  const closeForm = () => {
    setShowForm(false);
    if (searchParams.has('new')) setSearchParams({}, { replace: true });
  };

  const canChangeStatus = (task) => canManageTasks || task.assignedToId === user.id;
  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / PAGE_SIZE)) : 1;

  return (
    <>
      <div className="page-header">
        <div>
          <h1>Tasks</h1>
          <p className="muted">{user.role === 'User' ? 'Tasks assigned to you.' : 'Create, assign and track tasks.'}</p>
        </div>
        {canManageTasks && (
          <button type="button" className="btn btn-primary" onClick={() => setShowForm(true)}>+ New task</button>
        )}
      </div>

      <TaskFilters
        filters={filters}
        onChange={changeFilters}
        showSearch
        extra={
          <>
            <div className="field">
              <label htmlFor="f-sort">Sort by</label>
              <select id="f-sort" value={`${sort.sortBy}:${sort.desc}`}
                onChange={(e) => {
                  const [sortBy, desc] = e.target.value.split(':');
                  setSort({ sortBy, desc: desc === 'true' });
                }}>
                <option value="dueDate:false">Deadline (soonest)</option>
                <option value="dueDate:true">Deadline (latest)</option>
                <option value="priority:true">Priority (high → low)</option>
                <option value="priority:false">Priority (low → high)</option>
                <option value="status:false">Status</option>
                <option value="createdAt:true">Newest</option>
                <option value="title:false">Title A–Z</option>
              </select>
            </div>
            {canManageTasks && (
              <label className="checkbox">
                <input type="checkbox" checked={mineOnly} onChange={(e) => { setMineOnly(e.target.checked); setPage(1); }} />
                Assigned to me
              </label>
            )}
          </>
        }
      />

      <Alert type="success" onClose={() => setNotice('')}>{notice}</Alert>
      <Alert onClose={() => setError('')}>{error}</Alert>

      {!data ? (
        !error && <Spinner />
      ) : data.items.length === 0 ? (
        <div className="card"><EmptyState>No tasks found.</EmptyState></div>
      ) : (
        <>
          <div className="card table-card">
            <div className="table-wrap">
              <table className="task-table">
                <thead>
                  <tr>
                    <th scope="col">Task</th>
                    <th scope="col">Status</th>
                    <th scope="col">Priority</th>
                    <th scope="col">Deadline</th>
                    <th scope="col" className="hide-sm">Assignee</th>
                    <th scope="col" className="hide-sm">Team</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((task) => (
                    <tr key={task.id}>
                      <td data-label="Task">
                        <Link to={`/tasks/${task.id}`} className="strong">{task.title}</Link>
                        {task.commentCount > 0 && (
                          <span className="muted small" aria-label={`${task.commentCount} comments`}> 💬 {task.commentCount}</span>
                        )}
                      </td>
                      <td data-label="Status">
                        {canChangeStatus(task) ? (
                          <select className={`status-select status-${task.status}`} value={task.status}
                            aria-label={`Status of ${task.title}`}
                            onChange={(e) => changeStatus(task, e.target.value)}>
                            {STATUSES.map((s) => <option key={s.value} value={s.value}>{s.label}</option>)}
                          </select>
                        ) : (
                          STATUSES.find((s) => s.value === task.status).label
                        )}
                      </td>
                      <td data-label="Priority"><PriorityBadge priority={task.priority} /></td>
                      <td data-label="Deadline" className={task.isOverdue ? 'text-danger strong' : ''}>
                        {formatDate(task.dueDate)}{task.isOverdue && ' (overdue)'}
                      </td>
                      <td data-label="Assignee" className="hide-sm">{task.assignedToName ?? <span className="muted">Unassigned</span>}</td>
                      <td data-label="Team" className="hide-sm">{task.teamName ?? '—'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          <nav className="pagination" aria-label="Pagination">
            <button type="button" className="btn btn-ghost btn-sm" disabled={page <= 1} onClick={() => setPage(page - 1)}>← Previous</button>
            <span className="muted">Page {page} of {totalPages} · {data.totalCount} tasks</span>
            <button type="button" className="btn btn-ghost btn-sm" disabled={page >= totalPages} onClick={() => setPage(page + 1)}>Next →</button>
          </nav>
        </>
      )}

      {showForm && (
        <TaskFormModal
          onClose={closeForm}
          onSaved={(t) => {
            closeForm();
            setNotice(`Task "${t.title}" created.`);
            load();
          }}
        />
      )}
    </>
  );
}
