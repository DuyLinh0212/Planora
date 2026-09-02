import { AuthenticationResponse } from '../api/api.models';

const PREFIX = 'planora.user.';
const ACCESS_TOKEN = `${PREFIX}accessToken`;
const EXPIRES_AT = `${PREFIX}accessTokenExpiresAt`;
const USER = `${PREFIX}user`;

function read(key: string): string | null {
  return localStorage.getItem(key) ?? sessionStorage.getItem(key);
}

export function accessToken(): string | null {
  return read(ACCESS_TOKEN);
}

export function storeSession(response: AuthenticationResponse, remember: boolean): void {
  clearSession();
  const storage = remember ? localStorage : sessionStorage;
  storage.setItem(ACCESS_TOKEN, response.accessToken);
  storage.setItem(EXPIRES_AT, response.accessTokenExpiresAt);
  storage.setItem(
    USER,
    JSON.stringify({
      id: response.userId,
      email: response.email,
      username: response.username,
      displayName: response.displayName,
      avatarUrl: response.avatarUrl,
    }),
  );
}

export function updateTokens(response: AuthenticationResponse): void {
  const remember = localStorage.getItem(ACCESS_TOKEN) !== null;
  storeSession(response, remember);
}

export function hasUsableAccessToken(): boolean {
  const expiresAt = read(EXPIRES_AT);
  return !!accessToken() && !!expiresAt && Date.parse(expiresAt) > Date.now() + 30_000;
}

export function clearSession(): void {
  for (const storage of [localStorage, sessionStorage]) {
    storage.removeItem(ACCESS_TOKEN);
    storage.removeItem(EXPIRES_AT);
    storage.removeItem(USER);
  }
}
