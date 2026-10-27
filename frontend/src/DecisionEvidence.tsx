import type { DecisionAnalysis } from './DecisionPitch'

type Props = {
  option: DecisionAnalysis['options'][number]
  playerName: string
  highlighted: boolean
  canHighlight: boolean
  onHighlight: () => void
}

export function DecisionEvidence({ option, playerName, highlighted, canHighlight, onHighlight }: Props) {
  const profile = option.executionProfile
  const assessed = option.riskLevel !== 'Unknown' && option.riskLevel != null
  const metres = (value: number | null) => value == null ? 'Unknown' : `${Math.round(value * 10) / 10} m`
  return <section className="decision-evidence" aria-label="Evidence for selected option">
    <div className="evidence-heading"><div><span className="eyebrow">01 / SITUATION ON THE PITCH</span><h3>What the scene supports</h3></div>
      <span className={`risk-tag risk-${option.riskLevel?.toLowerCase() ?? 'unknown'}`}>{assessed ? `${option.riskLevel} visible risk` : 'Risk unknown'}</span></div>
    <div className="evidence-metrics">
      <div><span>{option.type === 'shot' ? 'Distance to goal' : 'Forward progress'}</span><strong>{metres(option.type === 'shot' ? option.distanceMeters : option.progressionMeters)}</strong><small>{option.type === 'shot' ? 'Distance alone does not predict a goal' : 'Negative means a backward option'}</small></div>
      <div><span>{option.type === 'pass' ? 'Defender from receiver' : 'Defender from actor'}</span><strong>{metres(option.nearestOpponentMeters)}</strong><small>Nearest recorded opponent</small></div>
      <div><span>{option.type === 'dribble' ? 'Space ahead' : 'Defender from path'}</span><strong>{assessed ? metres(option.type === 'dribble' ? option.spaceAheadMeters : option.corridorClearanceMeters) : 'Not assessed'}</strong><small>{option.type === 'dribble' ? 'Up to 10 m along the forward corridor' : 'Static ground corridor only'}</small></div>
    </div>
    <p className="spatial-conclusion">{option.comparison ?? 'Insufficient context to compare this option.'}</p>
    {canHighlight && <button type="button" className="evidence-toggle" aria-pressed={highlighted} onClick={onHighlight}>{highlighted ? 'Hide path evidence' : 'Show path evidence on pitch'}</button>}
    <small className="evidence-limit">Rule-based comparison of a partial static scene. Movement, unseen players and offside are unknown. Risk labels are not probabilities.</small>
    <div className="execution-context"><span className="eyebrow">02 / EXECUTION PROFILE</span><h3>{playerName} · this option</h3>
      {profile?.attributes.length ? <ul className="execution-attributes">{profile.attributes.map(a => <li key={a.key}><div><strong>{a.label}</strong><span>{a.value == null ? 'Unavailable' : `${a.value}/100`}</span></div><p>{a.relevance}</p></li>)}</ul> : <p>No relevant attribute data supplied for this technique.</p>}
      {profile && <p className="foot-context">{profile.footContext}</p>}
      <small className="evidence-limit">{profile?.note ?? 'Synthetic profile unavailable. No ability assumptions are made.'}</small>
    </div>
  </section>
}
