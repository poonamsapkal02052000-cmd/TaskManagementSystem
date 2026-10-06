import { useCallback, useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { notificationsApi } from '../api/services';
import { formatDateTime } from '../utils/format';

const POLL_MS = 30000;

export default function NotificationBell() {
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  const [count, setCount] = useState(0);
  const [items, setItems] = useState([]);
  const ref = useRef(null);

  const refreshCount = useCallback(() => {
    notificationsApi.unreadCount().then(setCount).catch(() => {});
  }, []);

  useEffect(() => {
    refreshCount();
    const id = setInterval(refreshCount, POLL_MS);
    return () => clearInterval(id);
  }, [refreshCount]);

  // Close when clicking outside.
  useEffect(() => {
    if (!open) return undefined;
    const onClick = (e) => !ref.current?.contains(e.target) && setOpen(false);
    const onKey = (e) => e.key === 'Escape' && setOpen(false);
    document.addEventListener('mousedown', onClick);
    document.addEventListener('keydown', onKey);
    return () => {
      document.removeEventListener('mousedown', onClick);
      document.removeEventListener('keydown', onKey);
    };
  }, [open]);

  const toggle = async () => {
    if (!open) {
      try {
        setItems(await notificationsApi.list());
      } catch {
        setItems([]);
      }
    }
    setOpen((o) => !o);
  };

  const openItem = async (n) => {
    if (!n.isRead) {
      await notificationsApi.markRead(n.id).catch(() => {});
      refreshCount();
    }
    setOpen(false);
    if (n.taskItemId) navigate(`/tasks/${n.taskItemId}`);
  };

  const markAll = async () => {
    await notificationsApi.markAllRead().catch(() => {});
    setItems((list) => list.map((n) => ({ ...n, isRead: true })));
    setCount(0);
  };

  return (
    <div className="bell" ref={ref}>
      <button
        type="button"
        className="icon-btn bell-btn"
        onClick={toggle}
        aria-haspopup="true"
        aria-expanded={open}
        aria-label={`Notifications${count ? `, ${count} unread` : ''}`}
      >
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
          <path d="M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9" />
          <path d="M13.73 21a2 2 0 0 1-3.46 0" />
        </svg>
        {count > 0 && <span className="bell-count">{count > 99 ? '99+' : count}</span>}
      </button>

      {open && (
        <div className="bell-panel" role="menu">
          <div className="bell-header">
            <strong>Notifications</strong>
            {items.some((n) => !n.isRead) && (
              <button type="button" className="link-btn" onClick={markAll}>
                Mark all read
              </button>
            )}
          </div>
          {items.length === 0 ? (
            <p className="empty small">You're all caught up.</p>
          ) : (
            <ul>
              {items.map((n) => (
                <li key={n.id}>
                  <button type="button" role="menuitem" className={`bell-item ${n.isRead ? '' : 'unread'}`} onClick={() => openItem(n)}>
                    <span className="bell-type">{n.type === 'TaskAssigned' ? 'Assigned' : 'Status update'}</span>
                    <span>{n.message}</span>
                    <time className="muted small">{formatDateTime(n.createdAt)}</time>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}
