import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { afterEach, expect, it, vi } from 'vitest'
import { DecisionEvidence } from './DecisionEvidence'
import type { DecisionAnalysis } from './DecisionPitch'

afterEach(cleanup)
const option: DecisionAnalysis['options'][number] = {
  optionId:'pass',type:'pass',receiverId:'b',distanceMeters:20,nearestOpponentMeters:4,
  corridorClearanceMeters:1,spaceAheadMeters:null,goalOpeningDegrees:null,corridor:'Blocked',reason:'',
  progressionMeters:-12,riskLevel:'High',comparison:'Another visible option offers a more favourable spatial trade-off under these rules.',
  executionProfile:{attributes:[{key:'passing',label:'Passing',value:0,relevance:'Delivery of the selected pass.'},{key:'vision',label:'Vision',value:null,relevance:'Recognising a passing option.'}],footContext:'Non-dominant foot selected.',note:'Synthetic profile context only.'},
}
it('shows concrete evidence and preserves zero versus unavailable attributes', () => {
  const toggle=vi.fn()
  render(<DecisionEvidence option={option} playerName="Test player" highlighted={false} canHighlight onHighlight={toggle}/> )
  expect(screen.getByText('High visible risk')).toBeInTheDocument()
  expect(screen.getByText('-12 m')).toBeInTheDocument()
  expect(screen.getByText('0/100')).toBeInTheDocument()
  expect(screen.getByText('Unavailable')).toBeInTheDocument()
  expect(screen.queryByText(/Model estimate|\/10$/)).not.toBeInTheDocument()
  fireEvent.click(screen.getByRole('button',{name:'Show path evidence on pitch'}))
  expect(toggle).toHaveBeenCalledOnce()
})
it('switches to unknown aerial context without implying a safe corridor or carrying over attributes', () => {
  const {rerender}=render(<DecisionEvidence option={option} playerName="Test" highlighted={false} canHighlight onHighlight={()=>{}}/> )
  rerender(<DecisionEvidence option={{...option,optionId:'head',type:'shot',riskLevel:'Unknown',executionProfile:{attributes:[],footContext:'No foot comparison for this technique.',note:'No heading attribute supplied.'}}} playerName="Test" highlighted={false} canHighlight={false} onHighlight={()=>{}}/> )
  expect(screen.getByText('Risk unknown')).toBeInTheDocument()
  expect(screen.getByText('Not assessed')).toBeInTheDocument()
  expect(screen.queryByText('Passing')).not.toBeInTheDocument()
  expect(screen.queryByRole('button')).not.toBeInTheDocument()
})
