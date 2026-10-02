import { afterEach, describe, expect, it, vi } from 'vitest'
import { api } from './api'
import { useAuth } from './stores'

describe('API client', () => {
  afterEach(() => { vi.unstubAllGlobals(); useAuth.getState().clear() })
  it('refreshes an expired access token and retries once', async () => {
    useAuth.getState().setAuth({ accessToken: 'old', accessTokenExpiresAt: '', user: { id: '1', email: 'a@b.test', displayName: 'A' } })
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(null, { status: 401 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ accessToken: 'new', accessTokenExpiresAt: '', user: useAuth.getState().user }), { status: 200 }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ ok: true }), { status: 200 }))
    vi.stubGlobal('fetch', fetchMock)
    await expect(api<{ ok: boolean }>('/api/test')).resolves.toEqual({ ok: true })
    expect(fetchMock).toHaveBeenCalledTimes(3)
    expect(useAuth.getState().accessToken).toBe('new')
  })
})
