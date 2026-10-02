import { create } from 'zustand'
import type { AuthResponse, User } from './types'

type AuthState = { user: User | null; accessToken: string | null; setAuth: (value: AuthResponse) => void; clear: () => void }
const stored = sessionStorage.getItem('atp.auth')
const initial = stored ? JSON.parse(stored) as Pick<AuthState, 'user' | 'accessToken'> : { user: null, accessToken: null }
export const useAuth = create<AuthState>((set) => ({ ...initial, setAuth: (value) => set(() => { const state = { user: value.user, accessToken: value.accessToken }; sessionStorage.setItem('atp.auth', JSON.stringify(state)); return state }), clear: () => set(() => { sessionStorage.removeItem('atp.auth'); return { user: null, accessToken: null } }) }))

type UiState = { theme: 'dark' | 'light'; environmentId: string | null; editorTab: string; setTheme: (theme: 'dark' | 'light') => void; setEnvironment: (id: string | null) => void; setEditorTab: (tab: string) => void }
export const useUi = create<UiState>((set) => ({ theme: (localStorage.getItem('atp.theme') as 'dark' | 'light') ?? 'dark', environmentId: localStorage.getItem('atp.environment'), editorTab: 'Params', setTheme: (theme) => set(() => { localStorage.setItem('atp.theme', theme); return { theme } }), setEnvironment: (environmentId) => set(() => { if (environmentId) localStorage.setItem('atp.environment', environmentId); else localStorage.removeItem('atp.environment'); return { environmentId } }), setEditorTab: (editorTab) => set({ editorTab }) }))
