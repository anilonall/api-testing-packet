import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { cleanup, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { PrimeReactProvider } from '@primereact/core/config'
import { AuthPage } from './App'
import { useAuth } from './stores'

function renderRegistration() {
  const queryClient = new QueryClient({ defaultOptions: { mutations: { retry: false } } })
  return render(<PrimeReactProvider><QueryClientProvider client={queryClient}><MemoryRouter initialEntries={['/auth/register']}><Routes><Route path="/auth/:mode" element={<AuthPage />} /><Route path="/dashboard" element={<div>Workspace dashboard</div>} /></Routes></MemoryRouter></QueryClientProvider></PrimeReactProvider>)
}

async function fillValidForm() {
  const user = userEvent.setup()
  await user.type(screen.getByLabelText('Display name'), 'Ada Tester')
  await user.type(screen.getByLabelText('Email'), 'ada@example.test')
  await user.type(screen.getByLabelText('Password'), 'correct horse battery staple')
  return user
}

describe('registration', () => {
  beforeEach(() => { sessionStorage.clear(); useAuth.getState().clear() })
  afterEach(() => { cleanup(); vi.restoreAllMocks() })

  it('submits valid details, stores authentication, and redirects', async () => {
    const response = { accessToken: 'access-token', accessTokenExpiresAt: '2030-01-01T00:00:00Z', user: { id: 'user-1', email: 'ada@example.test', displayName: 'Ada Tester' } }
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(JSON.stringify(response), { status: 200, headers: { 'Content-Type': 'application/json' } }))
    renderRegistration()
    const user = await fillValidForm()
    await user.click(screen.getByRole('button'))
    expect(await screen.findByText('Workspace dashboard')).toBeInTheDocument()
    expect(useAuth.getState().accessToken).toBe('access-token')
    expect(fetchMock).toHaveBeenCalledWith('/api/auth/register', expect.objectContaining({ method: 'POST', credentials: 'include' }))
  })

  it('shows client-side validation without sending a request', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch')
    renderRegistration()
    await userEvent.click(screen.getByRole('button'))
    expect(await screen.findByText('Use at least 12 characters')).toBeInTheDocument()
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('shows a backend registration error', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(JSON.stringify({ title: 'An account with this email already exists.' }), { status: 409, headers: { 'Content-Type': 'application/json' } }))
    renderRegistration()
    const user = await fillValidForm()
    await user.click(screen.getByRole('button'))
    await waitFor(() => expect(screen.getByText('An account with this email already exists.')).toBeInTheDocument())
  })
})
