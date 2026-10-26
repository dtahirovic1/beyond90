import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { afterEach, expect, it } from 'vitest'
import { PlayerProfile } from './PlayerProfile'
import { radarAttributes, ratingValue, squadAverage } from './player'

afterEach(cleanup)
const player = { id: 'mci-9', teamId: 'city', name: 'Erling Haaland', passing: 72, pace: 90, acceleration: 85, sprintSpeed: 94, strength: 93, stamina: 87, dribbling: 80, ballControl: 82, composure: 91, minutesPlayed: 2600, goalsScored: 24, assists: 6, expectedGoals: 22.5, weakFoot: 3 }

it('uses the selected player values and keeps season totals out of rating bars', () => {
  render(<PlayerProfile player={player} squad={[player]} />)
  expect(screen.getByRole('meter', { name: 'Passing' })).toHaveAttribute('aria-valuenow', '72')
  expect(screen.getByText('2,600')).toBeInTheDocument()
  expect(screen.getByText('22.5')).toBeInTheDocument()
  expect(screen.getByText('3 / 5')).toBeInTheDocument()
  expect(screen.getByRole('img', { name: /Erling Haaland attribute radar/ })).toBeInTheDocument()
  expect(screen.queryByRole('meter', { name: 'Minutes played' })).not.toBeInTheDocument()
})

it('never invents zero ratings or a completed polygon when values are missing', () => {
  const { container } = render(<PlayerProfile player={{ id: 'lee-5', teamId: 'leeds', name: 'Tarik' }} squad={[]} />)
  expect(screen.queryAllByRole('meter')).toHaveLength(0)
  expect(container.querySelector('[data-series="player"]')).toBeNull()
  expect(screen.getByText(/Attributes are not available for this player yet/)).toBeInTheDocument()
  expect(screen.getAllByText('—').length).toBeGreaterThan(10)
})

it('averages only valid values from the selected club, including genuine zeros', () => {
  const squad = [player, { id: 'mci-10', teamId: 'city', name: 'Cherki', pace: 0 }, { id: 'lee-9', teamId: 'leeds', name: 'Leeds player', pace: 99 }]
  expect(squadAverage(squad, 'city', 'pace')).toEqual({ value: 45, count: 2 })
  expect(squadAverage(squad, 'city', 'defending')).toEqual({ value: null, count: 0 })
  expect(ratingValue(-1)).toBeNull()
  expect(ratingValue(101)).toBeNull()
  expect(ratingValue(Number.NaN)).toBeNull()
  expect(ratingValue(0)).toBe(0)
  expect(radarAttributes).toHaveLength(8)
})

it('lets the reader hide the squad comparison', () => {
  render(<PlayerProfile player={player} squad={[player, { ...player, id: 'mci-10' }]} />)
  expect(screen.getByRole('checkbox', { name: 'Compare with tracked squad' })).toBeChecked()
  fireEvent.click(screen.getByRole('checkbox', { name: 'Compare with tracked squad' }))
  expect(screen.getByRole('checkbox', { name: 'Compare with tracked squad' })).not.toBeChecked()
})
