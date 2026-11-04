import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode
} from 'react';
import { api } from '../api/api';
import { setUnauthorizedHandler } from '../api/client';
import type { UserDto } from '../api/types';
import { tokenStore } from './tokenStore';

interface AuthContextValue {
  user: UserDto | null;
  initializing: boolean;
  login: (email: string, password: string) => Promise<UserDto>;
  logout: () => Promise<void>;
  hasPermission: (permission: string) => boolean;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserDto | null>(null);
  const [initializing, setInitializing] = useState(true);

  // Restore the session on first load (the access token may have expired -> silent refresh).
  useEffect(() => {
    let cancelled = false;

    const bootstrap = async () => {
      if (!tokenStore.getAccessToken() && !tokenStore.getRefreshToken()) {
        setInitializing(false);
        return;
      }

      try {
        const currentUser = await api.auth.me();

        if (!cancelled) {
          setUser(currentUser);
        }
      } catch {
        tokenStore.clear();
      } finally {
        if (!cancelled) {
          setInitializing(false);
        }
      }
    };

    void bootstrap();

    return () => {
      cancelled = true;
    };
  }, []);

  // The API client calls this when a refresh is not possible any more.
  useEffect(() => {
    setUnauthorizedHandler(() => {
      tokenStore.clear();
      setUser(null);
    });

    return () => setUnauthorizedHandler(null);
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const auth = await api.auth.login(email, password);

    tokenStore.save({
      accessToken: auth.accessToken,
      refreshToken: auth.refreshToken,
      accessTokenExpiresAtUtc: auth.accessTokenExpiresAtUtc
    });
    setUser(auth.user);

    return auth.user;
  }, []);

  const logout = useCallback(async () => {
    const refreshToken = tokenStore.getRefreshToken();

    try {
      if (refreshToken) {
        await api.auth.logout(refreshToken);
      }
    } catch {
      // Revocation is best effort - the local session is cleared regardless.
    }

    tokenStore.clear();
    setUser(null);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      initializing,
      login,
      logout,
      hasPermission: (permission: string) => user?.permissions.includes(permission) ?? false
    }),
    [user, initializing, login, logout]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error('useAuth must be used inside <AuthProvider>');
  }

  return context;
}
