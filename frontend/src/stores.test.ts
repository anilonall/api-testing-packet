import { beforeEach, describe, expect, it } from 'vitest'
import { useAuth, useUi } from './stores'

describe('client state', () => {
  beforeEach(() => { sessionStorage.clear(); localStorage.clear(); useAuth.getState().clear() })
  it('stores and clears an authenticated session', () => {
    useAuth.getState().setAuth({ accessToken: 'access', accessTokenExpiresAt: new Date().toISOString(), user: { id: '1', email: 'dev@example.test', displayName: 'Developer' } })
    expect(useAuth.getState().user?.email).toBe('dev@example.test')
    expect(sessionStorage.getItem('atp.auth')).not.toContain('undefined')
    useAuth.getState().clear()
    expect(useAuth.getState().accessToken).toBeNull()
  })
  it('keeps the active environment while editor tabs change', () => {
    useUi.getState().setEnvironment('development')
    useUi.getState().setEditorTab('Body')
    expect(useUi.getState().environmentId).toBe('development')
    expect(useUi.getState().editorTab).toBe('Body')
  })
})
