import axios, { type AxiosInstance, type AxiosRequestConfig, type AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { tokenStorage } from '../storage/tokenStorage';

// Create singleton Axios client
export const apiClient: AxiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
    Accept: 'application/json'
  }
});

// Refresh token queue handling to prevent race conditions
let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value?: unknown) => void;
  reject: (reason?: unknown) => void;
}> = [];

const processQueue = (error: Error | null, token: string | null = null) => {
  failedQueue.forEach((promise) => {
    if (error) {
      promise.reject(error);
    } else {
      promise.resolve(token);
    }
  });
  failedQueue = [];
};

/**
 * Generate a unique correlation ID for end-to-end tracing
 */
function generateCorrelationId(): string {
  if (typeof crypto !== 'undefined' && crypto.randomUUID) {
    return crypto.randomUUID();
  }
  return 'req-' + Math.random().toString(36).substring(2, 11) + '-' + Date.now();
}

// Request Interceptor: Attach Bearer token and X-Correlation-ID
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = tokenStorage.getAccessToken();
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    if (config.headers && !config.headers['X-Correlation-ID']) {
      config.headers['X-Correlation-ID'] = generateCorrelationId();
    }

    return config;
  },
  (error: AxiosError) => {
    return Promise.reject(error);
  }
);

// Response Interceptor: Auto Refresh Token with Queue Lock
apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as AxiosRequestConfig & { _retry?: boolean };

    // If 401 Unauthorized and request not already retried
    if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            if (originalRequest.headers) {
              originalRequest.headers.Authorization = `Bearer ${token}`;
            }
            return apiClient(originalRequest);
          })
          .catch((err) => Promise.reject(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const currentRefreshToken = tokenStorage.getRefreshToken();
      if (!currentRefreshToken) {
        tokenStorage.clearTokens();
        window.location.href = '/login?expired=1';
        return Promise.reject(error);
      }

      try {
        const response = await axios.post('/api/auth/refresh-token', {
          refreshToken: currentRefreshToken
        });

        const { accessToken, refreshToken } = response.data;
        tokenStorage.setAccessToken(accessToken);
        if (refreshToken) {
          tokenStorage.setRefreshToken(refreshToken);
        }

        if (originalRequest.headers) {
          originalRequest.headers.Authorization = `Bearer ${accessToken}`;
        }

        processQueue(null, accessToken);
        return apiClient(originalRequest);
      } catch (refreshError) {
        processQueue(refreshError as Error, null);
        tokenStorage.clearTokens();
        window.location.href = '/login?expired=1';
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);

export default apiClient;
