import { useCallback, useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { getErrorMessage } from '../api/client';
import { tasksApi } from '../api/services';
import { Alert, EmptyState, PriorityBadge, Spinner, StatusBadge } from '../components/Common';
import TaskFormModal from '../components/TaskFormModal';
import { useAuth } from '../context/AuthContext';
import { STATUSES, formatDate, formatDateTime } from '../utils/format';

export default function TaskDetail() {
  const { id } = useParams();
  const navigate = useNavigate();
  const { user, isAdmin, canManageTasks } = useAuth();
  const [task, setTask] = useState(null);
  const [comments, setComments] = useState([]);
  const [comment, setComment] = useState('');
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [editing, setEditing] = useState(false);
  const [posting, setPosting] = useState(false);

  const load = useCallback(async () => {
    try {
      const [t, c] = await Promise.all([tasksApi.get(id), tasksApi.comments(id)]);
      setTask(t);
      setComments(c);
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }, [id]);

  useEffect(() => {
    load();
  }, [load]);

  const changeStatus = async (status) => {
    setError('');
    try {
      setTask(await tasksApi.updateStatus(task.id, status));
      setNotice('Status updated.');
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  const postComment = async (e) => {
    e.preventDefault();
    if (!comment.trim()) return;
    setPosting(true);
    try {
      const c = await tasksApi.addComment(task.id, comment.trim());
      setComments([...comments, c]);
      setComment('');
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setPosting(false);
    }
  };

  const deleteComment = async (c) => {
    if (!window.confirm('Delete this comment?')) return;
    try {
      await tasksApi.deleteComment(task.id, c.id);
      setComments(comments.filter((x) => x.id !== c.id));
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  const deleteTask = async () => {
    if (!window.confirm(`Delete task "${task.title}"? This cannot be undone.`)) return;
    try {
      await tasksApi.remove(task.id);
      navigate('/tasks', { replace: true });
    } catch (err) {
      setError(getErrorMessage(err));
    }
  };

  if (!task) {
    return error ? (
      <>
        <Alert>{error}</Alert>
        <Link to="/tasks">← Back to tasks</Link>
      </>
    ) : (
      <Spinner />
    );
  }

  const canChangeStatus = canManageTasks || task.assignedToId === user.id;

  return (
    <>
      <Link to="/tasks" className="back-link">← Back to tasks</Link>

      <div className="page-header">
        <div>
          <h1>{task.title}</h1>
          <div className="badges">
            <StatusBadge status={task.status} />
            <PriorityBadge priority={task.priority} />
            {task.isOverdue && <span className="badge badge-danger">Overdue</span>}
          </div>
        </div>
        {canManageTasks && (
          <div className="actions">
            <button type="button" className="btn btn-ghost" onClick={() => setEditing(true)}>Edit</button>
            <button type="button" className="btn btn-danger" onClick={deleteTask}>Delete</button>
          </div>
        )}
      </div>

      <Alert type="success" onClose={() => setNotice('')}>{notice}</Alert>
      <Alert onClose={() => setError('')}>{error}</Alert>

      <div className="grid-2 detail-grid">
        <section className="card">
          <h2>Details</h2>
          <p className="pre-wrap">{task.description || <span className="muted">No description.</span>}</p>
          <dl className="meta">
            <dt>Assignee</dt><dd>{task.assignedToName ?? 'Unassigned'}</dd>
            <dt>Team</dt><dd>{task.teamName ?? '—'}</dd>
            <dt>Deadline</dt><dd className={task.isOverdue ? 'text-danger' : ''}>{formatDate(task.dueDate)}</dd>
            <dt>Created by</dt><dd>{task.createdByName}</dd>
            <dt>Created</dt><dd>{formatDateTime(task.createdAt)}</dd>
            <dt>Last updated</dt><dd>{formatDateTime(task.updatedAt)}</dd>
          </dl>

          {canChangeStatus && (
            <fieldset className="status-switch">
              <legend>Update status</legend>
              {STATUSES.map((s) => (
                <button key={s.value} type="button"
                  className={`btn btn-sm ${task.status === s.value ? 'btn-primary' : 'btn-ghost'}`}
                  aria-pressed={task.status === s.value}
                  onClick={() => task.status !== s.value && changeStatus(s.value)}>
                  {s.label}
                </button>
              ))}
            </fieldset>
          )}
        </section>

        <section className="card">
          <h2>Comments ({comments.length})</h2>
          {comments.length === 0 ? (
            <EmptyState>No comments yet. Start the conversation.</EmptyState>
          ) : (
            <ul className="comments">
              {comments.map((c) => (
                <li key={c.id} className="comment">
                  <span className="avatar small" aria-hidden="true">{c.authorName.charAt(0)}</span>
                  <div className="comment-body">
                    <div className="comment-head">
                      <strong>{c.authorName}</strong>
                      <time className="muted small">{formatDateTime(c.createdAt)}</time>
                      {(c.authorId === user.id || isAdmin) && (
                        <button type="button" className="link-btn small" onClick={() => deleteComment(c)}
                          aria-label={`Delete comment by ${c.authorName}`}>
                          Delete
                        </button>
                      )}
                    </div>
                    <p className="pre-wrap">{c.content}</p>
                  </div>
                </li>
              ))}
            </ul>
          )}

          <form onSubmit={postComment} className="comment-form">
            <label htmlFor="new-comment" className="sr-only">Add a comment</label>
            <textarea id="new-comment" rows={3} placeholder="Write a comment…" value={comment}
              maxLength={2000} onChange={(e) => setComment(e.target.value)} />
            <button type="submit" className="btn btn-primary" disabled={posting || !comment.trim()}>
              {posting ? 'Posting…' : 'Post comment'}
            </button>
          </form>
        </section>
      </div>

      {editing && (
        <TaskFormModal
          task={task}
          onClose={() => setEditing(false)}
          onSaved={(t) => {
            setTask(t);
            setEditing(false);
            setNotice('Task updated.');
          }}
        />
      )}
    </>
  );
}
