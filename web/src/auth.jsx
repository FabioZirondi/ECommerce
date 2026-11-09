import { createContext, useContext, useMemo, useState } from "react";
import { api, clearSession, getStoredUser, getToken, storeSession, storeUser } from "./api.js";
import { setNearPlace } from "./search.js";

const AuthContext = createContext(null);

export function isSeller(user) {
  return (user?.roles ?? []).some((role) => String(role).toLowerCase() === "seller");
}

export function AuthProvider({ children }) {
  const [token, setToken] = useState(getToken());
  const [user, setUser] = useState(getStoredUser());

  const value = useMemo(() => ({
    token,
    user,
    isAuthenticated: Boolean(token),
    isSeller: isSeller(user),
    async register(payload) {
      const auth = await api("/api/auth/register", {
        method: "POST",
        body: JSON.stringify(payload)
      });
      storeSession(auth);
      if (auth.user?.city || auth.user?.zipCode) {
        setNearPlace({ city: auth.user.city, state: auth.user.state, zipCode: auth.user.zipCode });
      }
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
      if (auth.user?.city || auth.user?.zipCode) {
        setNearPlace({ city: auth.user.city, state: auth.user.state, zipCode: auth.user.zipCode });
      }
      setToken(auth.accessToken);
      setUser(auth.user);
      return auth;
    },
    async saveDeliveryCep(zipCode) {
      const updated = await api("/api/users/me/address", {
        method: "PATCH",
        body: JSON.stringify({ zipCode })
      });
      storeUser(updated);
      setNearPlace({ city: updated.city, state: updated.state, zipCode: updated.zipCode });
      setUser(updated);
      return updated;
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
