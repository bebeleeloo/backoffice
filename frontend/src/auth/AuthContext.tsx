import { createContext, useCallback, useEffect, useState, type ReactNode } from "react";
import { apiClient } from "../api/client";
import type { AuthResponse, UserProfile } from "../api/types";

interface AuthState {
  user: UserProfile | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  permissions: string[];
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
  refreshProfile: () => Promise<void>;
}

export const AuthContext = createContext<AuthState>({
  user: null, isAuthenticated: false, isLoading: true, permissions: [],
  login: async () => {}, logout: () => {}, refreshProfile: async () => {},
});

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserProfile | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const fetchMe = useCallback(async () => {
    try {
      const { data } = await apiClient.get<UserProfile>("/auth/me");
      setUser(data);
    } catch {
      localStorage.removeItem("accessToken");
      localStorage.removeItem("refreshToken");
      setUser(null);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    if (localStorage.getItem("accessToken")) {
      fetchMe();
    } else {
      setIsLoading(false);
    }
  }, [fetchMe]);

  const login = async (username: string, password: string) => {
    const { data } = await apiClient.post<AuthResponse>("/auth/login", { username, password });
    localStorage.setItem("accessToken", data.accessToken);
    localStorage.setItem("refreshToken", data.refreshToken);
    await fetchMe();
  };

  const logout = () => {
    localStorage.removeItem("accessToken");
    localStorage.removeItem("refreshToken");
    setUser(null);
  };

  return (
    <AuthContext.Provider
      value={{
        user, isAuthenticated: !!user, isLoading,
        permissions: user?.permissions ?? [],
        login, logout, refreshProfile: fetchMe,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}
