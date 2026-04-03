export interface LoginRequest {
  username: string;
  password: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export interface RefreshTokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}
