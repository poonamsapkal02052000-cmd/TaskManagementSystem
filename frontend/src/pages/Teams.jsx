import { useCallback, useEffect, useState } from 'react';
import { getErrorMessage } from '../api/client';
import { teamsApi, usersApi } from '../api/services';
import { Alert, EmptyState, Modal, Spinner } from '../components/Common';
import { useAuth } from '../context/AuthContext';

function TeamFormModal({ team, managers, onClose, onSaved }) {
  const [form, setForm] = useState({
    name: team?.name ?? '',
    description: team?.description ?? '',
    managerId: team?.managerId ?? ''
  });
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const submit = async (e) => {
    e.preventDefault();
    if (form.name.trim().length < 2) return setError('Team name must be at least 2 characters.');
    setBusy(true);
    const payload = { name: form.name.trim(), description: form.description.trim() || null, managerId: form.managerId ? Number(form.managerId) : null };
    try {
      onSaved(team ? await teamsApi.update(team.id, payload) : await teamsApi.create(payload));
    } catch (err) {
      setError(getErrorMessage(err));
      setBusy(false);
    }
  };

  return (
    <Modal title={team ? 'Edit team' : 'New team'} onClose={onClose}>
      <form className="form" onSubmit={submit} noValidate>
        <Alert>{error}</Alert>
        <label htmlFor="tm-name">Name *</label>
        <input id="tm-name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} maxLength={100} />
        <label htmlFor="tm-desc">Description</label>
        <textarea id="tm-desc" rows={3} value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} maxLength={500} />
        <label htmlFor="tm-manager">Manager</label>
        <select id="tm-manager" value={form.managerId} onChange={(e) => setForm({ ...form, managerId: e.target.value })}>
          <option value="">No manager</option>
          {managers.map((m) => <option key={m.id} value={m.id}>{m.fullName}</option>)}
        </select>
        <div className="modal-actions">
          <button type="button" className="btn btn-ghost" onClick={onClose}>Cancel</button>
          <button type="submit" className="btn btn-primary" disabled={busy}>{busy ? 'Saving…' : 'Save'}</button>
        </div>
      </form>
    </Modal>
  );
}

function AddMember({ team, candidates, onAdded, onError }) {
  const [userId, setUserId] = useState('');
  const add = async (e) => {
    e.preventDefault();
    if (!userId) return;
    try {
      onAdded(await teamsApi.addMember(team.id, Number(userId)));
      setUserId('');
    } catch (err) {
      onError(getErrorMessage(err));
    }
  };

  return (
    <form className="inline-form" onSubmit={add}>
      <label htmlFor={`add-${team.id}`} className="sr-only">Add member to {team.name}</label>
      <select id={`add-${team.id}`} value={userId} onChange={(e) => setUserId(e.target.value)}>
        <option value="">{candidates.length ? 'Select a user to add…' : 'No available users'}</option>
        {candidates.map((u) => (
          <option key={u.id} value={u.id}>{u.fullName} ({u.role}){u.teamName ? ` · in ${u.teamName}` : ''}</option>
        ))}
      </select>
      <button type="submit" className="btn btn-primary btn-sm" disabled={!userId}>Add</button>
    </form>
  );
}

export default function Teams() {
  const { user, isAdmin, isManager } = useAuth();
  const [teams, setTeams] = useState(null);
  const [users, setUsers] = useState([]);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [modal, setModal] = useState(null); // { team } | {}

  const load = useCallback(async () => {
    try {
      setTeams(await teamsApi.list());
      if (isAdmin || isManager) setUsers(await usersApi.list());
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }, [isAdmin, isManager]);

  useEffect(() => {
    load();
  }, [load]);

  const replaceTeam = (updated) => {
    setTeams((list) => list.map((t) => (t.id === updated.id ? updated : t)));
    load();
  };

  const removeMember = async (team, member) => {
    if (!window.confirm(`Remove ${member.fullName} from ${team.name}?`)) return;
    try {
      replaceTeam(await teamsApi.removeMember(team.id, member.id));
      setNotice(`${member.fullName} removed from ${team.name}.`);
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  const deleteTeam = async (team) => {
    if (!window.confirm(`Delete team "${team.name}"? Members and tasks will be kept but unlinked.`)) return;
    try {
      await teamsApi.remove(team.id);
      setNotice(`Team "${team.name}" deleted.`);
      load();
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  // Admins can add any non-admin user (moves them between teams); Managers can add unassigned Users.
  const candidatesFor = (team) =>
    users.filter((u) =>
      u.teamId !== team.id && u.role !== 'Admin' &&
      (isAdmin || (u.role === 'User' && !u.teamId)));

  const canManage = (team) => isAdmin || (isManager && team.managerId === user.id);

  return (
    <>
      <div className="page-header">
        <div>
          <h1>Teams</h1>
          <p className="muted">
            {isAdmin ? 'Create teams, assign managers and members.' : isManager ? 'Manage the members of your teams.' : 'Your team.'}
          </p>
        </div>
        {isAdmin && <button type="button" className="btn btn-primary" onClick={() => setModal({})}>+ New team</button>}
      </div>

      <Alert type="success" onClose={() => setNotice('')}>{notice}</Alert>
      <Alert onClose={() => setError('')}>{error}</Alert>

      {!teams ? (
        !error && <Spinner />
      ) : teams.length === 0 ? (
        <div className="card"><EmptyState>{isAdmin ? 'No teams yet.' : 'You are not part of any team yet.'}</EmptyState></div>
      ) : (
        <div className="team-grid">
          {teams.map((team) => (
            <section key={team.id} className="card team-card" aria-labelledby={`team-${team.id}`}>
              <div className="team-head">
                <div>
                  <h2 id={`team-${team.id}`}>{team.name}</h2>
                  <p className="muted small">{team.description || 'No description'}</p>
                </div>
                {isAdmin && (
                  <div className="actions">
                    <button type="button" className="btn btn-ghost btn-sm" onClick={() => setModal({ team })}>Edit</button>
                    <button type="button" className="btn btn-danger btn-sm" onClick={() => deleteTeam(team)}>Delete</button>
                  </div>
                )}
              </div>
              <p className="small"><strong>Manager:</strong> {team.managerName ?? <span className="muted">None</span>}</p>

              <h3 className="small-heading">Members ({team.members.length})</h3>
              {team.members.length === 0 ? (
                <EmptyState>No members.</EmptyState>
              ) : (
                <ul className="member-list">
                  {team.members.map((m) => (
                    <li key={m.id}>
                      <span className="avatar small" aria-hidden="true">{m.fullName.charAt(0)}</span>
                      <span className="member-name">{m.fullName}<span className="muted small"> · {m.email}</span></span>
                      {canManage(team) && (
                        <button type="button" className="link-btn small" onClick={() => removeMember(team, m)}
                          aria-label={`Remove ${m.fullName}`}>
                          Remove
                        </button>
                      )}
                    </li>
                  ))}
                </ul>
              )}

              {canManage(team) && (
                <AddMember team={team} candidates={candidatesFor(team)} onError={setError}
                  onAdded={(t) => { replaceTeam(t); setNotice(`Member added to ${t.name}.`); }} />
              )}
            </section>
          ))}
        </div>
      )}

      {modal && (
        <TeamFormModal
          team={modal.team}
          managers={users.filter((u) => u.role === 'Manager')}
          onClose={() => setModal(null)}
          onSaved={(t) => {
            setModal(null);
            setNotice(`Team "${t.name}" saved.`);
            load();
          }}
        />
      )}
    </>
  );
}
