/**
 * Unit Test: Token Storage Service
 */
import { tokenStorage } from '@/services/storage/tokenStorage';

describe('tokenStorage', () => {
  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
  });

  it('should store and retrieve access token with remember=true', () => {
    tokenStorage.setAccessToken('sample-jwt-token-123', true);
    expect(tokenStorage.getAccessToken()).toBe('sample-jwt-token-123');
    expect(localStorage.getItem('bpm_access_token')).toBe('sample-jwt-token-123');
  });

  it('should store in sessionStorage when remember=false', () => {
    tokenStorage.setAccessToken('session-only-token', false);
    expect(tokenStorage.getAccessToken()).toBe('session-only-token');
    expect(sessionStorage.getItem('bpm_access_token')).toBe('session-only-token');
    expect(localStorage.getItem('bpm_access_token')).toBeNull();
  });

  it('should clear all tokens upon clearTokens()', () => {
    tokenStorage.setAccessToken('test-token');
    tokenStorage.setRefreshToken('refresh-token');
    tokenStorage.setUser('{"username":"test"}');

    tokenStorage.clearTokens();

    expect(tokenStorage.getAccessToken()).toBeNull();
    expect(tokenStorage.getRefreshToken()).toBeNull();
    expect(tokenStorage.getUser()).toBeNull();
  });
});
