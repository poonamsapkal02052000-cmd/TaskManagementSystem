import axios from 'axios';

const TOKEN_KEY = 'tm_token';
const USER_KEY = 'tm_user';
const EXPIRES_KEY = 'tm_expires';

export const session = {
  get token() {
    return localStorage.getItem(TOKEN_KEY);
  },
  get user() {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? JSON.parse(raw) : null;
  },
  get expiresAt() {
    const raw = localStorage.getItem(EXPIRES_KEY);
    return raw ? new Date(raw) : null;
  },
  isExpired() {
    const exp = this.expiresAt;
    return !exp || exp.getTime() <= Date.now();
  },
  save({ token, expiresAt, user }) {
    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem(EXPIRES_KEY, expiresAt);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
  },
  updateUser(user) {
    localStorage.setItem(USER_KEY, JSON.stringify(user));
  },
  clear() {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    localStorage.removeItem(EXPIRES_KEY);
  }
};

const api = axios.create({
  baseURL: `${import.meta.env.VITE_API_URL ?? ''}/api`,
  headers: { 'Content-Type': 'application/json' },
  timeout: 15000
});

// Attach the JWT to every request.
api.interceptors.request.use((config) => {
  const token = session.token;
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// Called when the session expires or the token is rejected (set by AuthContext).
let onUnauthorized = () => {};
export const setUnauthorizedHandler = (fn) => {
  onUnauthorized = fn;
};

api.interceptors.response.use(
  (res) => res,
  (error) => {
    const status = error.response?.status;
    const isAuthCall = ['/auth/login', '/auth/register'].includes(error.config?.url);
    if (status === 401 && !isAuthCall) {
      const expired = error.response?.headers?.['token-expired'] === 'true';
      onUnauthorized(expired ? 'Your session has expired. Please log in again.' : 'Please log in to continue.');
    }
    return Promise.reject(error);
  }
);

/** Converts an Axios error (ProblemDetails / ValidationProblemDetails) into a readable message. */
export function getErrorMessage(error) {
  if (!error?.response) return error?.code === 'ECONNABORTED' ? 'The request timed out.' : 'Cannot reach the server. Please check your connection.';
  const data = error.response.data;
  if (data?.errors) {
    return Object.values(data.errors).flat().join(' ');
  }
  return data?.detail || data?.title || `Request failed (${error.response.status}).`;
}

export default api;
