import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { PrimeReactProvider } from '@primereact/core/config'
import Aura from '@primeuix/themes/aura'
import 'primeicons/primeicons.css'
import './index.css'
import App from './App'

const queryClient = new QueryClient({ defaultOptions: { queries: { staleTime: 15_000, retry: 1 } } })
createRoot(document.getElementById('root')!).render(<StrictMode><PrimeReactProvider theme={{ preset: Aura }}><QueryClientProvider client={queryClient}><BrowserRouter><App /></BrowserRouter></QueryClientProvider></PrimeReactProvider></StrictMode>)
