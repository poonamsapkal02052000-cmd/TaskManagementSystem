import { useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { getErrorMessage } from '../api/client';
import { Alert } from '../components/Common';
import { useAuth } from '../context/AuthContext';

export default function Login() {
  const { login, authMessage } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [form, setForm] = useState({ email: '', password: '' });
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const submit = async (e) => {
    e.preventDefault();
    setError('');
    if (!form.email || !form.password) {
      setError('Email and password are required.');
      return;
    }
    setBusy(true);
    try {
      await login(form.email, form.password);
      navigate(location.state?.from?.pathname ?? '/', { replace: true });
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="auth-page">
      <form className="auth-card" onSubmit={submit} noValidate>
        <div className="brand brand-lg">
          <span className="brand-mark" aria-hidden="true">✓</span> TaskManager
        </div>
        <h1>Sign in</h1>
        <p className="muted">Manage your team's work in one place.</p>

        {authMessage && <Alert type="info">{authMessage}</Alert>}
        <Alert>{error}</Alert>

        <label htmlFor="email">Email</label>
        <input id="email" type="email" autoComplete="email" value={form.email}
          onChange={(e) => setForm({ ...form, email: e.target.value })} required />

        <label htmlFor="password">Password</label>
        <input id="password" type="password" autoComplete="current-password" value={form.password}
          onChange={(e) => setForm({ ...form, password: e.target.value })} required />

        <button type="submit" className="btn btn-primary btn-block" disabled={busy}>
          {busy ? 'Signing in…' : 'Sign in'}
        </button>

        <p className="center">
          No account? <Link to="/register">Create one</Link>
        </p>
      </form>
    </div>
  );
}
