import { useId, useState } from 'react'
import { radarAttributes, ratingValue, squadAverage, technicalAttributes, type Player } from './player'
import './PlayerProfile.css'

function number(value: number | null | undefined) {
  return typeof value === 'number' && Number.isFinite(value) && value >= 0 ? value.toLocaleString('en-GB', { maximumFractionDigits: 1 }) : '—'
}
function point(index: number, value: number, radius = 142) {
  const angle = -Math.PI / 2 + index * Math.PI / 4
  return { x: 230 + Math.cos(angle) * radius * value / 100, y: 205 + Math.sin(angle) * radius * value / 100 }
}
function polygon(values: (number | null)[]) {
  return values.every((v): v is number => v !== null) ? values.map((v, i) => { const p = point(i, v); return `${p.x},${p.y}` }).join(' ') : undefined
}

export function PlayerProfile({ player, squad }: { player: Player; squad: Player[] }) {
  const [compare, setCompare] = useState(true)
  const chartId = useId()
  const own = radarAttributes.map(a => ratingValue(player[a.key]))
  const averages = radarAttributes.map(a => squadAverage(squad, player.teamId, a.key))
  const available = new Set(squad.filter(p => p.teamId === player.teamId && radarAttributes.some(a => ratingValue(p[a.key]) !== null)).map(p => p.id)).size
  const canCompare = available > 1
  const ownPolygon = polygon(own)
  const averagePolygon = polygon(averages.map(a => a.value))
  const noRatings = [...technicalAttributes, ...radarAttributes].every(a => ratingValue(player[a.key]) === null)
  return <section className="player-profile" aria-label={`${player.name} attribute profile`}>
    <div className="profile-heading"><div><span className="eyebrow">SCOUTING PROFILE / 2026–27</span><h2>The player, in detail.</h2></div><span className="profile-data-tag"><i /> Demo attributes</span></div>
    <div className="profile-summary">
      <div className="profile-overall"><span>OVERALL</span><strong>{number(ratingValue(player.overallRating))}</strong><small>Potential <b>{number(ratingValue(player.potential))}</b></small></div>
      <dl className="profile-facts"><div><dt>Position</dt><dd>{player.position || '—'}</dd></div><div><dt>Age</dt><dd>{number(player.age)}</dd></div><div><dt>Preferred foot</dt><dd>{player.dominantFoot || '—'}</dd></div><div><dt>Weak foot</dt><dd>{player.weakFoot != null && player.weakFoot >= 1 && player.weakFoot <= 5 ? `${player.weakFoot} / 5` : '—'}</dd></div></dl>
    </div>
    {noRatings && <p className="profile-empty" role="status">Attributes are not available for this player yet. Missing values are shown as —.</p>}
    <div className="profile-charts">
      <section className="profile-chart-card" aria-labelledby={`${chartId}-technical`}><div className="profile-card-heading"><div><span className="eyebrow">01 / TECHNICAL</span><h3 id={`${chartId}-technical`}>Skill breakdown</h3></div><span>0—100</span></div>
        <div className="rating-bars">{technicalAttributes.map(({ key, label }) => {
          const value = ratingValue(player[key])
          return <div className="rating-row" key={key}><div className="rating-label"><span>{label}</span><strong>{number(value)}</strong></div>{value === null ? <div className="rating-track rating-missing" aria-label={`${label}: unavailable`} /> : <div className="rating-track" role="meter" aria-label={label} aria-valuenow={value} aria-valuemin={0} aria-valuemax={100}><span style={{ width: `${value}%` }} /></div>}</div>
        })}</div><p className="profile-note">Player attribute ratings on a 0–100 scale.</p>
      </section>
      <section className="profile-chart-card radar-card" aria-labelledby={`${chartId}-radar-heading`}><div className="profile-card-heading"><div><span className="eyebrow">02 / ATTRIBUTE RADAR</span><h3 id={`${chartId}-radar-heading`}>Player shape</h3></div><span>8 ATTRIBUTES</span></div>
        <div className="radar-legend"><span><i className="player-swatch" />{player.name}</span>{compare && canCompare && <span><i className="squad-swatch" />Tracked squad average</span>}</div>
        <svg className="attribute-radar" viewBox="0 0 460 420" role="img" aria-labelledby={`${chartId}-title ${chartId}-desc`}>
          <title id={`${chartId}-title`}>{player.name} attribute radar</title><desc id={`${chartId}-desc`}>Eight player attributes on a 0 to 100 scale.{compare && canCompare ? ` Compared with the mean of ${available} tracked players in the same club, including this player. Each average uses available values only.` : ''} Exact values are listed below. Missing ratings are not plotted.</desc>
          {[20, 40, 60, 80, 100].map(v => <polygon key={v} points={polygon(Array<number>(8).fill(v))} className="radar-ring" />)}
          {radarAttributes.map((a, i) => { const edge = point(i, 100); const label = point(i, 100, 174); return <g key={a.key}><line x1="230" y1="205" x2={edge.x} y2={edge.y} className="radar-axis" /><text x={label.x} y={label.y} textAnchor={i === 0 || i === 4 ? 'middle' : i < 4 ? 'start' : 'end'} dominantBaseline="middle">{a.label}</text></g> })}
          {[20, 40, 60, 80, 100].map(v => <text className="radar-scale" key={v} x="235" y={205 - 142 * v / 100 - 3}>{v}</text>)}
          {compare && canCompare && averagePolygon && <polygon points={averagePolygon} className="radar-squad" data-series="squad" />}
          {ownPolygon && <polygon points={ownPolygon} className="radar-player" data-series="player" />}
          {own.map((value, i) => { if (value === null) return null; const p = point(i, value); return <circle key={radarAttributes[i].key} cx={p.x} cy={p.y} r="3.5" className="radar-dot"><title>{radarAttributes[i].label}: {value}</title></circle> })}
        </svg>
        <label className="radar-toggle"><input type="checkbox" checked={compare && canCompare} disabled={!canCompare} onChange={e => setCompare(e.target.checked)} />Compare with tracked squad</label>
        <p className="profile-note">{canCompare ? `Average of ${available} tracked club players, including ${player.name.split(' ').at(-1)}. Available ratings only; not a league benchmark.` : 'Squad comparison needs ratings for at least two tracked club players.'}</p>
        <details className="radar-values"><summary>View exact radar values</summary><table><caption>Attribute ratings / 100</caption><thead><tr><th scope="col">Attribute</th><th scope="col">Player</th>{compare && canCompare && <th scope="col">Squad mean (n)</th>}</tr></thead><tbody>{radarAttributes.map((a, i) => <tr key={a.key}><th scope="row">{a.label}</th><td>{number(own[i])}</td>{compare && canCompare && <td>{number(averages[i].value)} ({averages[i].count})</td>}</tr>)}</tbody></table></details>
      </section>
    </div>
    <section className="profile-season" aria-label="Season attributes"><div className="profile-card-heading"><div><span className="eyebrow">03 / SEASON SNAPSHOT</span><h3>Beyond the attributes.</h3></div><span>2026–27 DEMO</span></div><dl>{([
      ['Minutes played', player.minutesPlayed], ['Goals', player.goalsScored], ['Assists', player.assists], ['Expected goals (xG)', player.expectedGoals], ['Expected assists (xA)', player.expectedAssists],
    ] as const).map(([label, value]) => <div key={label}><dt>{label}</dt><dd>{number(value)}</dd></div>)}</dl></section>
    <p className="profile-disclaimer">Simulated player profiles, not real scouting grades or measured match statistics. Season attributes are supplied profile totals, separate from the recorded match samples below.</p>
  </section>
}
