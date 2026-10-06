import { useCallback, useEffect, useState } from 'react';
import { getErrorMessage } from '../api/client';
import { usersApi } from '../api/services';
import { Alert, Modal, Spinner } from '../components/Common';
import { useAuth } from '../context/AuthContext';
import { ROLES } from '../utils/format';

function CreateUserModal({ onClose, onSaved }) {
  const [form, setForm] = useState({ fullName: '', email: '', password: '', role: 'User' });
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const set = (f) => (e) => setForm({ ...form, [f]: e.target.value });

  const submit = async (e) => {
    e.preventDefault();
    setBusy(true);
    try {
      onSaved(await usersApi.create(form));
    } catch (err) {
      setError(getErrorMessage(err));
      setBusy(false);
    }
  };

  return (
    <Modal title="New user" onClose={onClose}>
      <form className="form" onSubmit={submit} noValidate>
        <Alert>{error}</Alert>
        <label htmlFor="u-name">Full name</label>
        <input id="u-name" value={form.fullName} onChange={set('fullName')} />
        <label htmlFor="u-email">Email</label>
        <input id="u-email" type="email" value={form.email} onChange={set('email')} />
        <label htmlFor="u-pass">Temporary password</label>
        <input id="u-pass" type="password" autoComplete="new-password" value={form.password} onChange={set('password')} />
        <label htmlFor="u-role">Role</label>
        <select id="u-role" value={form.role} onChange={set('role')}>
          {ROLES.map((r) => <option key={r}>{r}</option>)}
        </select>
        <div className="modal-actions">
          <button type="button" className="btn btn-ghost" onClick={onClose}>Cancel</button>
          <button type="submit" className="btn btn-primary" disabled={busy}>{busy ? 'Creating…' : 'Create user'}</button>
        </div>
      </form>
    </Modal>
  );
}

export default function Users() {
  const { user: me } = useAuth();
  const [users, setUsers] = useState(null);
  const [roleFilter, setRoleFilter] = useState('');
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [creating, setCreating] = useState(false);

  const load = useCallback(async () => {
    try {
      setUsers(await usersApi.list({ role: roleFilter }));
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }, [roleFilter]);

  useEffect(() => {
    load();
  }, [load]);

  const changeRole = async (u, role) => {
    try {
      const updated = await usersApi.updateRole(u.id, role);
      setUsers((list) => list.map((x) => (x.id === u.id ? updated : x)));
      setNotice(`${u.fullName} is now a ${role}.`);
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  const remove = async (u) => {
    if (!window.confirm(`Delete ${u.fullName}? This cannot be undone.`)) return;
    try {
      await usersApi.remove(u.id);
      setNotice(`${u.fullName} deleted.`);
      load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  return (
    <>
      <div className="page-header">
        <div>
          <h1>Users</h1>
          <p className="muted">Manage accounts and roles.</p>
        </div>
        <button type="button" className="btn btn-primary" onClick={() => setCreating(true)}>+ New user</button>
      </div>

      <div className="filters">
        <div className="field">
          <label htmlFor="role-filter">Role</label>
          <select id="role-filter" value={roleFilter} onChange={(e) => setRoleFilter(e.target.value)}>
            <option value="">All roles</option>
            {ROLES.map((r) => <option key={r}>{r}</option>)}
          </select>
        </div>
      </div>

      <Alert type="success" onClose={() => setNotice('')}>{notice}</Alert>
      <Alert onClose={() => setError('')}>{error}</Alert>

      {!users ? (
        !error && <Spinner />
      ) : (
        <div className="card table-card">
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th scope="col">Name</th>
                  <th scope="col" className="hide-sm">Email</th>
                  <th scope="col">Role</th>
                  <th scope="col" className="hide-sm">Team</th>
                  <th scope="col"><span className="sr-only">Actions</span></th>
                </tr>
              </thead>
              <tbody>
                {users.map((u) => (
                  <tr key={u.id}>
                    <th scope="row">{u.fullName}</th>
                    <td className="hide-sm">{u.email}</td>
                    <td>
                      {u.id === me.id ? (
                        <span className={`role-tag role-${u.role}`}>{u.role} (you)</span>
                      ) : (
                        <select value={u.role} aria-label={`Role of ${u.fullName}`} onChange={(e) => changeRole(u, e.target.value)}>
                          {ROLES.map((r) => <option key={r}>{r}</option>)}
                        </select>
                      )}
                    </td>
                    <td className="hide-sm">{u.teamName ?? '—'}</td>
                    <td className="right">
                      {u.id !== me.id && (
                        <button type="button" className="link-btn small text-danger" onClick={() => remove(u)}>Delete</button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {creating && (
        <CreateUserModal
          onClose={() => setCreating(false)}
          onSaved={(u) => {
            setCreating(false);
            setNotice(`User ${u.fullName} created.`);
            load();
          }}
        />
      )}
    </>
  );
}
