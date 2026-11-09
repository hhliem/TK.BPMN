export interface UserProfile {
  id: string;
  username: string;
  email: string;
  fullName: string;
  avatarUrl?: string;
  title?: string;
  departmentId?: string;
  departmentName?: string;
  branchName?: string;
  roles: string[];
  permissions: string[];
}

export interface LoginRequest {
  username: string;
  password: string;
  rememberMe?: boolean;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  user: UserProfile;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}
