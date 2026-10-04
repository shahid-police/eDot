import { useSyncExternalStore } from "react";

// The access token is kept in memory only. The refresh token lives in an
// HttpOnly cookie that scripts cannot read, so a page reload restores the
// session through /auth/refresh rather than from browser storage.
let session = null;
const listeners = new Set();

function emit() {
  listeners.forEach((listener) => listener());
}

export function getSession() {
  return session;
}

export function setSession(next) {
  session = next;
  emit();
}

function subscribe(listener) {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

export function useSession() {
  return useSyncExternalStore(subscribe, getSession, () => null);
}
