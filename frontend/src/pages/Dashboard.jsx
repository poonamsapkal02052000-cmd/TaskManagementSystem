import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { getErrorMessage } from '../api/client';
import { dashboardApi } from '../api/services';
import { Alert, EmptyState, PriorityBadge, Spinner, StatusBadge } from '../components/Common';
import TaskFilters from '../components/TaskFilters';
import { useAuth } from '../context/AuthContext';
import { formatDate } from '../utils/format';

const EMPTY = { status: '', priority: '', dueFrom: '', dueTo: '' };

function StatusBar({ counts }) {
  const total = counts.total || 1;
  return (
    <div className="stack-bar" role="img"
      aria-label={`To Do ${counts.toDo}, In Progress ${counts.inProgress}, Done ${counts.done}`}>
      <span className="seg seg-ToDo" style={{ width: `${(counts.toDo / total) * 100}%` }} />
      <span className="seg seg-InProgress" style={{ width: `${(counts.inProgress / total) * 100}%` }} />
      <span className="seg seg-Done" style={{ width: `${(counts.done / total) * 100}%` }} />
    </div>
  );
}

export default function Dashboard() {
  const { user, canManageTasks } = useAuth();
  const [filters, setFilters] = useState(EMPTY);
  const [data, setData] = useState(null);
  const [error, setError] = useState('');

  useEffect(() => {
    let active = true;
    setError('');
    dashboardApi
      .get(filters)
      .then((d) => active && setData(d))
      .catch((err) => active && setError(getErrorMessage(err)));
    return () => {
      active = false;
    };
  }, [filters]);

  const t = data?.totals;
  const pct = t?.total ? Math.round((t.done / t.total) * 100) : 0;

  return (
    <>
      <div className="page-header">
        <div>
          <h1>Welcome, {user.fullName.split(' ')[0]}</h1>
          <p className="muted">
            {user.role === 'User' ? 'Overview of tasks assigned to you.' : 'Overview of your team’s tasks.'}
          </p>
        </div>
        {canManageTasks && <Link to="/tasks?new=1" className="btn btn-primary">+ New task</Link>}
      </div>

      <TaskFilters filters={filters} onChange={setFilters} />
      <Alert onClose={() => setError('')}>{error}</Alert>

      {!data ? (
        !error && <Spinner />
      ) : (
        <>
          <section className="stats" aria-label="Task totals">
            <div className="stat"><span className="stat-label">Total</span><span className="stat-value">{t.total}</span></div>
            <div className="stat stat-ToDo"><span className="stat-label">To Do</span><span className="stat-value">{t.toDo}</span></div>
            <div className="stat stat-InProgress"><span className="stat-label">In Progress</span><span className="stat-value">{t.inProgress}</span></div>
            <div className="stat stat-Done"><span className="stat-label">Done</span><span className="stat-value">{t.done}</span></div>
            <div className="stat stat-overdue"><span className="stat-label">Overdue</span><span className="stat-value">{data.overdue}</span></div>
            <div className="stat"><span className="stat-label">Due in 7 days</span><span className="stat-value">{data.dueThisWeek}</span></div>
          </section>

          <div className="grid-2">
            <section className="card">
              <h2>Progress</h2>
              <p className="big-number">{pct}% <span className="muted small">completed</span></p>
              <StatusBar counts={t} />
              <ul className="legend">
                <li><span className="dot seg-ToDo" /> To Do</li>
                <li><span className="dot seg-InProgress" /> In Progress</li>
                <li><span className="dot seg-Done" /> Done</li>
              </ul>
              <h3>By priority</h3>
              <div className="priority-row">
                {Object.entries(data.byPriority).map(([p, n]) => (
                  <div key={p} className="priority-cell"><PriorityBadge priority={p} /> <strong>{n}</strong></div>
                ))}
              </div>
            </section>

            <section className="card">
              <h2>Upcoming deadlines</h2>
              {data.upcomingDeadlines.length === 0 ? (
                <EmptyState>No open tasks with deadlines.</EmptyState>
              ) : (
                <ul className="list">
                  {data.upcomingDeadlines.map((task) => (
                    <li key={task.id} className="list-item">
                      <div>
                        <Link to={`/tasks/${task.id}`} className="strong">{task.title}</Link>
                        <div className="muted small">{task.assignedToName ?? 'Unassigned'}</div>
                      </div>
                      <div className="right">
                        <StatusBadge status={task.status} />
                        <div className={`small ${task.isOverdue ? 'text-danger' : 'muted'}`}>
                          {task.isOverdue ? 'Overdue · ' : ''}{formatDate(task.dueDate)}
                        </div>
                      </div>
                    </li>
                  ))}
                </ul>
              )}
            </section>
          </div>

          <section className="card">
            <h2>Task status per user</h2>
            {data.perUser.length === 0 ? (
              <EmptyState>No assigned tasks match the filters.</EmptyState>
            ) : (
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th scope="col">User</th>
                      <th scope="col">To Do</th>
                      <th scope="col">In Progress</th>
                      <th scope="col">Done</th>
                      <th scope="col">Overdue</th>
                      <th scope="col" className="hide-sm">Progress</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.perUser.map((u) => (
                      <tr key={u.userId}>
                        <th scope="row">{u.fullName}</th>
                        <td>{u.counts.toDo}</td>
                        <td>{u.counts.inProgress}</td>
                        <td>{u.counts.done}</td>
                        <td className={u.overdue ? 'text-danger strong' : ''}>{u.overdue}</td>
                        <td className="hide-sm" style={{ minWidth: 140 }}><StatusBar counts={u.counts} /></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </>
      )}
    </>
  );
}
