import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { getErrorMessage } from '../api/client';
import { Alert } from '../components/Common';
import { useAuth } from '../context/AuthContext';

const PASSWORD_RULE = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$/;

export default function Register() {
  const { register } = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState({ fullName: '', email: '', password: '', confirm: '' });
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  const set = (field) => (e) => setForm({ ...form, [field]: e.target.value });

  const validate = () => {
    if (form.fullName.trim().length < 2) return 'Please enter your full name.';
    if (!/^\S+@\S+\.\S+$/.test(form.email)) return 'Please enter a valid email address.';
    if (!PASSWORD_RULE.test(form.password))
      return 'Password must be at least 8 characters and include uppercase, lowercase and a number.';
    if (form.password !== form.confirm) return 'Passwords do not match.';
    return '';
  };

  const submit = async (e) => {
    e.preventDefault();
    const msg = validate();
    setError(msg);
    if (msg) return;

    setBusy(true);
    try {
      await register(form.fullName.trim(), form.email.trim(), form.password);
      navigate('/', { replace: true });
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
        <h1>Create account</h1>
        <p className="muted">New accounts start with the User role.</p>

        <Alert>{error}</Alert>

        <label htmlFor="fullName">Full name</label>
        <input id="fullName" autoComplete="name" value={form.fullName} onChange={set('fullName')} required />

        <label htmlFor="email">Email</label>
        <input id="email" type="email" autoComplete="email" value={form.email} onChange={set('email')} required />

        <label htmlFor="password">Password</label>
        <input id="password" type="password" autoComplete="new-password" value={form.password}
          onChange={set('password')} aria-describedby="pw-hint" required />
        <small id="pw-hint" className="muted">At least 8 characters, with upper & lower case letters and a number.</small>

        <label htmlFor="confirm">Confirm password</label>
        <input id="confirm" type="password" autoComplete="new-password" value={form.confirm} onChange={set('confirm')} required />

        <button type="submit" className="btn btn-primary btn-block" disabled={busy}>
          {busy ? 'Creating account…' : 'Create account'}
        </button>

        <p className="center">
          Already have an account? <Link to="/login">Sign in</Link>
        </p>
      </form>
    </div>
  );
}
