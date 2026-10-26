import { cleanup, render, screen, waitFor } from '@testing-library/react'
import { afterEach, expect, it, vi } from 'vitest'
import App from './App'

afterEach(() => { cleanup(); vi.unstubAllGlobals(); location.hash = '' })

it('starts with both clubs and accessible pixel crests', () => {
  vi.stubGlobal('fetch', vi.fn(() => new Promise(() => {})))
  location.hash = ''
  render(<App />)
  expect(screen.getByRole('link', { name: /Leeds United/ })).toHaveAttribute('href', '#leeds')
  expect(screen.getByRole('link', { name: /Manchester City/ })).toHaveAttribute('href', '#city')
  expect(screen.getAllByRole('img')).toHaveLength(2)
})

it('loads the City squad and keeps player navigation inside City', async () => {
  location.hash = '#city/players'
  vi.stubGlobal('fetch', vi.fn(async (url: string) => ({ ok: true, json: async () => url.includes('/club') ? {
    name: 'Manchester City', players: [
      { id: 'mci-9', teamId: 'city', name: 'Erling Haaland', position: 'Striker' },
      { id: 'mci-10', teamId: 'city', name: 'Rayan Cherki', position: 'Attacking midfielder' },
      { id: 'mci-15', teamId: 'city', name: 'Marc Guéhi', position: 'Centre-back' },
    ],
  } : [] })))
  render(<App />)
  await waitFor(() => expect(screen.getByRole('link', { name: /Erling Haaland/ })).toHaveAttribute('href', '#city/player/mci-9'))
  expect(screen.getByRole('heading', { name: 'Rayan Cherki' })).toBeInTheDocument()
  expect(screen.getByRole('heading', { name: 'Marc Guéhi' })).toBeInTheDocument()
  expect(fetch).toHaveBeenCalledWith('/api/reports/club?teamId=city', expect.anything())
})
