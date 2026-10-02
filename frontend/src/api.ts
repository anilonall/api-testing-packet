import { useAuth } from './stores'
import type { AuthResponse } from './types'

const base = import.meta.env.VITE_API_URL ?? ''

export async function api<T>(path: string, init: RequestInit = {}, retry = true): Promise<T> {
  const state = useAuth.getState()
  const response = await fetch(`${base}${path}`, { ...init, credentials: 'include', headers: { 'Content-Type': 'application/json', ...(state.accessToken ? { Authorization: `Bearer ${state.accessToken}` } : {}), ...init.headers } })
  if (response.status === 401 && retry && state.user) {
    const refreshed = await fetch(`${base}/api/auth/refresh`, { method: 'POST', credentials: 'include', headers: { 'Content-Type': 'application/json' }, body: '{}' })
    if (refreshed.ok) { useAuth.getState().setAuth(await refreshed.json() as AuthResponse); return api<T>(path, init, false) }
    useAuth.getState().clear()
  }
  if (!response.ok) { const problem = await response.json().catch(() => ({ title: response.statusText })) as { title?: string; detail?: string; errors?: Record<string, string[]> }; throw new Error(problem.detail ?? problem.title ?? Object.values(problem.errors ?? {}).flat().join(' ') ?? `Request failed (${response.status})`) }
  if (response.status === 204) return undefined as T
  return response.json() as Promise<T>
}

export const post = <T>(path: string, value: unknown) => api<T>(path, { method: 'POST', body: JSON.stringify(value) })
export const put = <T>(path: string, value: unknown) => api<T>(path, { method: 'PUT', body: JSON.stringify(value) })
