import { createContext, useContext, useMemo, useState } from "react";
import { api, clearSession, getStoredUser, getToken, storeSession } from "./api.js";

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [token, setToken] = useState(getToken());
  const [user, setUser] = useState(getStoredUser());

  const value = useMemo(() => ({
    token,
    user,
    isAuthenticated: Boolean(token),
    async register(payload) {
      const auth = await api("/api/auth/register", {
        method: "POST",
        body: JSON.stringify(payload)
      });
      storeSession(auth);
      setToken(auth.accessToken);
      setUser(auth.user);
      return auth;
    },
    async login(payload) {
      const auth = await api("/api/auth/login", {
        method: "POST",
        body: JSON.stringify(payload)
      });
      storeSession(auth);
      setToken(auth.accessToken);
      setUser(auth.user);
      return auth;
    },
    logout() {
      clearSession();
      setToken(null);
      setUser(null);
    }
  }), [token, user]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  return useContext(AuthContext);
}
