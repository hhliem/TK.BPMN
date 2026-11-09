import { apiClient } from '@/services/api/apiClient';
import type { LoginRequest, LoginResponse, UserProfile, ChangePasswordRequest } from '../types/auth.types';

export const authApi = {
  async login(payload: LoginRequest): Promise<LoginResponse> {
    const response = await apiClient.post<LoginResponse>('/auth/login', payload);
    return response.data;
  },

  async logout(): Promise<void> {
    try {
      await apiClient.post('/auth/logout');
    } catch {
      // Ignore network errors on logout to allow clean local storage purge
    }
  },

  async getProfile(): Promise<UserProfile> {
    const response = await apiClient.get<UserProfile>('/auth/me');
    return response.data;
  },

  async changePassword(payload: ChangePasswordRequest): Promise<void> {
    await apiClient.post('/auth/change-password', payload);
  }
};
