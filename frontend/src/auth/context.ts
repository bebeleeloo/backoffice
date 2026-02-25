import { createContext } from "react";
import type { UserProfile } from "../api/types";

export interface AuthState {
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
