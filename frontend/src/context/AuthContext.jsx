import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { session, setUnauthorizedHandler } from '../api/client';
import { authApi } from '../api/services';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const navigate = useNavigate();
  const [user, setUser] = useState(() => (session.token && !session.isExpired() ? session.user : null));
  const [authMessage, setAuthMessage] = useState('');

  const logout = useCallback(
    (message = '') => {
      session.clear();
      setUser(null);
      setAuthMessage(message);
      navigate('/login', { replace: true });
    },
    [navigate]
  );

  // Any 401 from the API logs the user out.
  useEffect(() => {
    setUnauthorizedHandler((msg) => logout(msg));
  }, [logout]);

  // Log out automatically when the token expires.
  useEffect(() => {
    if (!user || !session.expiresAt) return undefined;
    const ms = session.expiresAt.getTime() - Date.now();
    if (ms <= 0) {
      logout('Your session has expired. Please log in again.');
      return undefined;
    }
    const timer = setTimeout(() => logout('Your session has expired. Please log in again.'), ms);
    return () => clearTimeout(timer);
  }, [user, logout]);

  // Refresh the profile on load (role / team may have changed).
  useEffect(() => {
    if (!session.token || session.isExpired()) return;
    authApi
      .me()
      .then((profile) => {
        session.updateUser(profile);
        setUser(profile);
      })
      .catch(() => {});
  }, []);

  const handleAuth = (data) => {
    session.save(data);
    setUser(data.user);
    setAuthMessage('');
  };

  const value = useMemo(
    () => ({
      user,
      authMessage,
      isAdmin: user?.role === 'Admin',
      isManager: user?.role === 'Manager',
      canManageTasks: user?.role === 'Admin' || user?.role === 'Manager',
      login: async (email, password) => handleAuth(await authApi.login(email, password)),
      register: async (fullName, email, password) => handleAuth(await authApi.register(fullName, email, password)),
      logout
    }),
    [user, authMessage, logout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export const useAuth = () => useContext(AuthContext);
