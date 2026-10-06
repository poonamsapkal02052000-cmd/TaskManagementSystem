import { useEffect, useState } from 'react';
import { getErrorMessage } from '../api/client';
import { tasksApi, teamsApi, usersApi } from '../api/services';
import { useAuth } from '../context/AuthContext';
import { PRIORITIES, toDateInput } from '../utils/format';
import { Alert, Modal } from './Common';

/**
 * Create or edit a task.
 * Admins can assign to any Manager/User; Managers to themselves or their team members.
 */
export default function TaskFormModal({ task, onClose, onSaved }) {
  const { user, isAdmin } = useAuth();
  const editing = Boolean(task);
  const [form, setForm] = useState({
    title: task?.title ?? '',
    description: task?.description ?? '',
    priority: task?.priority ?? 'Medium',
    dueDate: toDateInput(task?.dueDate),
    teamId: task?.teamId ?? '',
    assignedToId: task?.assignedToId ?? ''
  });
  const [teams, setTeams] = useState([]);
  const [assignees, setAssignees] = useState([]);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    (async () => {
      try {
        const teamList = await teamsApi.list();
        setTeams(teamList);
        if (isAdmin) {
          const users = await usersApi.list();
          setAssignees(users.filter((u) => u.role !== 'Admin'));
        } else {
          const members = teamList.flatMap((t) => t.members);
          const unique = [{ ...user, teamName: 'Me' }, ...members.filter((m) => m.id !== user.id)];
          setAssignees(unique);
        }
      } catch (err) {
        setError(getErrorMessage(err));
      }
    })();
  }, [isAdmin, user]);

  const set = (field) => (e) => setForm({ ...form, [field]: e.target.value });

  const submit = async (e) => {
    e.preventDefault();
    if (form.title.trim().length < 3) {
      setError('Title must be at least 3 characters.');
      return;
    }
    setBusy(true);
    setError('');
    const payload = {
      title: form.title.trim(),
      description: form.description.trim() || null,
      priority: form.priority,
      dueDate: form.dueDate || null,
      teamId: form.teamId ? Number(form.teamId) : null
    };
    try {
      let saved;
      if (editing) {
        saved = await tasksApi.update(task.id, payload);
        const newAssignee = form.assignedToId ? Number(form.assignedToId) : null;
        if (newAssignee !== (task.assignedToId ?? null)) saved = await tasksApi.assign(task.id, newAssignee);
      } else {
        saved = await tasksApi.create({ ...payload, assignedToId: form.assignedToId ? Number(form.assignedToId) : null });
      }
      onSaved(saved);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <Modal title={editing ? 'Edit task' : 'New task'} onClose={onClose}>
      <form onSubmit={submit} className="form" noValidate>
        <Alert>{error}</Alert>

        <label htmlFor="t-title">Title *</label>
        <input id="t-title" value={form.title} onChange={set('title')} maxLength={200} required />

        <label htmlFor="t-desc">Description</label>
        <textarea id="t-desc" rows={4} value={form.description} onChange={set('description')} maxLength={4000} />

        <div className="form-row">
          <div>
            <label htmlFor="t-priority">Priority</label>
            <select id="t-priority" value={form.priority} onChange={set('priority')}>
              {PRIORITIES.map((p) => <option key={p.value} value={p.value}>{p.label}</option>)}
            </select>
          </div>
          <div>
            <label htmlFor="t-due">Deadline</label>
            <input id="t-due" type="date" value={form.dueDate} onChange={set('dueDate')} />
          </div>
        </div>

        <div className="form-row">
          <div>
            <label htmlFor="t-assignee">Assign to</label>
            <select id="t-assignee" value={form.assignedToId} onChange={set('assignedToId')}>
              <option value="">Unassigned</option>
              {assignees.map((u) => (
                <option key={u.id} value={u.id}>
                  {u.fullName} {u.role === 'Manager' ? '(Manager)' : ''} {u.teamName ? `· ${u.teamName}` : ''}
                </option>
              ))}
            </select>
          </div>
          <div>
            <label htmlFor="t-team">Team</label>
            <select id="t-team" value={form.teamId} onChange={set('teamId')}>
              <option value="">{editing ? 'No team' : 'Auto (assignee’s team)'}</option>
              {teams.map((t) => <option key={t.id} value={t.id}>{t.name}</option>)}
            </select>
          </div>
        </div>

        <div className="modal-actions">
          <button type="button" className="btn btn-ghost" onClick={onClose}>Cancel</button>
          <button type="submit" className="btn btn-primary" disabled={busy}>
            {busy ? 'Saving…' : editing ? 'Save changes' : 'Create task'}
          </button>
        </div>
      </form>
    </Modal>
  );
}
