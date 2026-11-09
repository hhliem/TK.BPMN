const ACCESS_TOKEN_KEY = 'bpm_access_token';
const REFRESH_TOKEN_KEY = 'bpm_refresh_token';
const USER_KEY = 'bpm_user_profile';

export const tokenStorage = {
  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY) || sessionStorage.getItem(ACCESS_TOKEN_KEY);
  },

  setAccessToken(token: string, remember = true): void {
    if (remember) {
      localStorage.setItem(ACCESS_TOKEN_KEY, token);
    } else {
      sessionStorage.setItem(ACCESS_TOKEN_KEY, token);
    }
  },

  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY) || sessionStorage.getItem(REFRESH_TOKEN_KEY);
  },

  setRefreshToken(token: string, remember = true): void {
    if (remember) {
      localStorage.setItem(REFRESH_TOKEN_KEY, token);
    } else {
      sessionStorage.setItem(REFRESH_TOKEN_KEY, token);
    }
  },

  getUser(): string | null {
    return localStorage.getItem(USER_KEY) || sessionStorage.getItem(USER_KEY);
  },

  setUser(userJson: string, remember = true): void {
    if (remember) {
      localStorage.setItem(USER_KEY, userJson);
    } else {
      sessionStorage.setItem(USER_KEY, userJson);
    }
  },

  clearTokens(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    sessionStorage.removeItem(ACCESS_TOKEN_KEY);
    sessionStorage.removeItem(REFRESH_TOKEN_KEY);
    sessionStorage.removeItem(USER_KEY);
  }
};
