import { useEffect, useMemo, useState } from 'react'
import './App.css'

const matchId = 'b90-demo-001'

type Team = { id: string; name: string }
type Player = { id: string; teamId: string; name: string; position?: string }
type MatchDetails = {
  id: string
  homeTeam: Team
  awayTeam: Team
  teams: Team[]
  players: Player[]
  eventCount: number
  dataStatus: string
}
type MatchEvent = {
  eventId: string
  sequence: number
  minute: number
  second: number
  teamId: string
  playerId: string
  type: string
  x: number
  y: number
  outcome: string | null
  underPressure: boolean | null
}
type MatchMetrics = {
  attemptedPasses: number
  completedPasses: number
  passCompletionPercentage: number | null
  turnoversByZone: Record<string, number>
  shotsByInterval: Record<string, number>
  pressureEventCount: number
}
type KeyMoment = {
  eventIds: string[]
  score: number
  reasonCode: string
  metricEvidence: { metricKey: string; value: number | null; eventIds: string[] }[]
  events: MatchEvent[]
}
type MatchData = {
  details: MatchDetails
  events: MatchEvent[]
  metrics: MatchMetrics
  keyMoments: KeyMoment[]
}

async function getJson<T>(url: string, signal: AbortSignal): Promise<T> {
  const response = await fetch(url, { signal })
  if (!response.ok) throw new Error(`Server returned ${response.status} for ${url}`)
  return response.json() as Promise<T>
}

function App() {
  const [data, setData] = useState<MatchData | null>(null)
  const [selectedEventId, setSelectedEventId] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [reload, setReload] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    setLoading(true)
    setError(null)

    Promise.all([
      getJson<MatchDetails>(`/api/matches/${matchId}`, controller.signal),
      getJson<MatchEvent[]>(`/api/matches/${matchId}/events`, controller.signal),
      getJson<MatchMetrics>(`/api/matches/${matchId}/metrics`, controller.signal),
      getJson<{ keyMoments: KeyMoment[] }>(`/api/matches/${matchId}/recap`, controller.signal),
    ])
      .then(([details, events, metrics, recap]) => {
        setData({ details, events, metrics, keyMoments: recap.keyMoments })
        setSelectedEventId(events[0]?.eventId ?? null)
      })
      .catch((reason: unknown) => {
        if (reason instanceof DOMException && reason.name === 'AbortError') return
        setError('Ne mogu dohvatiti demo utakmicu. Provjeri je li backend pokrenut pa pokušaj ponovo.')
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false)
      })

    return () => controller.abort()
  }, [reload])

  const playerNames = useMemo(() => new Map(data?.details.players.map((player) => [player.id, player.name])), [data])
  const selectedEvent = data?.events.find((event) => event.eventId === selectedEventId) ?? null

  if (loading) return <LoadingScreen />
  if (error || !data) {
    return (
      <main id="top" className="app-shell">
        <TopBar apiStatus="offline" />
        <section className="system-state" role="alert">
          <span className="eyebrow">SIGNAL LOST</span>
          <h1>Utakmica nije dostupna</h1>
          <p>{error ?? 'Backend trenutno ne vraća podatke za demo utakmicu.'}</p>
          <button className="pixel-button" onClick={() => setReload((value) => value + 1)}>Pokušaj ponovo</button>
          <code>dotnet run --project backend/Beyond90.Api</code>
        </section>
      </main>
    )
  }

  const { details, events, metrics, keyMoments } = data
  const teamShortName = (teamId: string) => details.teams.find((team) => team.id === teamId)?.name ?? teamId
  const passAccuracy = metrics.passCompletionPercentage == null ? '—' : `${Math.round(metrics.passCompletionPercentage)}%`
  const turnovers = Object.entries(metrics.turnoversByZone)

  return (
    <main id="top" className="app-shell">
      <TopBar apiStatus="connected" />

      <section className="match-intro">
        <div>
          <div className="eyebrow"><span className="status-dot" /> BEYOND90 / MATCH INTELLIGENCE</div>
          <h1>{details.homeTeam.name}<span className="versus"> vs </span>{details.awayTeam.name}</h1>
          <p className="intro-subtitle">The game doesn’t end at full time.</p>
        </div>
        <div className="scenario-badge">
          <span className="badge-mark">SIM</span>
          <span><strong>SYNTHETIC SCENARIO</strong><small>{details.dataStatus}</small></span>
        </div>
      </section>

      <section className="match-strip" aria-label="Match summary">
        <div className="team-name home"><span className="team-chip arsenal-chip">ARS</span>{details.homeTeam.name}</div>
        <div className="match-state"><span>DEMO MATCH</span><strong>EVENT FEED</strong><small>{events.length} recorded moments</small></div>
        <div className="team-name away"><span className="team-chip chelsea-chip">CHE</span>{details.awayTeam.name}</div>
      </section>

      <div className="dashboard-grid">
        <section className="panel pitch-panel" aria-labelledby="pitch-heading">
          <div className="panel-heading">
            <div><span className="eyebrow">01 / POSITIONAL DATA</span><h2 id="pitch-heading">Event map</h2></div>
            <div className="legend"><span><i className="legend-dot arsenal-dot" />Arsenal</span><span><i className="legend-dot chelsea-dot" />Chelsea</span></div>
          </div>
          <Pitch events={events} selectedEventId={selectedEventId} onSelect={setSelectedEventId} />
          <div className="event-readout" aria-live="polite">
            {selectedEvent ? <>
              <div className="readout-time">{String(selectedEvent.minute).padStart(2, '0')}<small>′</small>{String(selectedEvent.second).padStart(2, '0')}<small>″</small></div>
              <div className="readout-copy"><strong>{selectedEvent.type}<span>{selectedEvent.outcome ?? 'OUTCOME NOT RECORDED'}</span></strong><small>{playerNames.get(selectedEvent.playerId) ?? 'Unknown player'} · {teamShortName(selectedEvent.teamId)}</small></div>
              <span className="coordinate">X {selectedEvent.x} / Y {selectedEvent.y}</span>
            </> : <span>Select an event on the pitch to inspect it.</span>}
          </div>
        </section>

        <section className="panel moments-panel" aria-labelledby="moments-heading">
          <div className="panel-heading">
            <div><span className="eyebrow">02 / AUTOMATIC RECAP</span><h2 id="moments-heading">Key moments</h2></div>
            <span className="count-badge">{keyMoments.length.toString().padStart(2, '0')}</span>
          </div>
          {keyMoments.length > 0 ? <ol className="moment-list">
            {keyMoments.slice(0, 6).map((moment, index) => {
              const event = moment.events[0]
              return <li key={`${moment.eventIds.join('-')}-${index}`}>
                <button className={`moment-button ${event?.eventId === selectedEventId ? 'is-selected' : ''}`} onClick={() => event && setSelectedEventId(event.eventId)}>
                  <span className="moment-index">{String(index + 1).padStart(2, '0')}</span>
                  <span className="moment-content">
                    <strong>{moment.reasonCode.replaceAll('_', ' ')}</strong>
                    <small>{event ? `${String(event.minute).padStart(2, '0')}′ · ${event.type} · ${playerNames.get(event.playerId) ?? 'Unknown player'}` : moment.eventIds.join(', ')}</small>
                    <small className="moment-evidence">{moment.metricEvidence.map((item) => `${item.metricKey}: ${item.value ?? '—'}`).join(' · ')}</small>
                  </span>
                  <span className="moment-score">{moment.score}<small> IMPACT</small></span>
                </button>
              </li>
            })}
          </ol> : <div className="empty-state">No key moments are available for this match.</div>}
          <p className="panel-footnote">Ranked from the synthetic event sequence. Not real match analysis.</p>
        </section>

        <section className="panel stats-panel" aria-labelledby="stats-heading">
          <div className="panel-heading">
          <div><span className="eyebrow">03 / MATCH METRICS</span><h2 id="stats-heading">Match stats</h2></div>
            <span className="metric-source">BACKEND CALCULATED</span>
          </div>
          <div className="metric-grid">
            <Metric label="Pass accuracy" value={passAccuracy} detail={`${metrics.completedPasses} / ${metrics.attemptedPasses} completed`} accent="cyan" />
            <Metric label="Pressure events" value={String(metrics.pressureEventCount).padStart(2, '0')} detail="Across both teams" accent="blue" />
            <div className="turnover-metric">
              <span className="metric-label">Turnovers by zone</span>
              <div className="zone-bars">{turnovers.map(([zone, count]) => {
                const largestZoneCount = Math.max(1, ...turnovers.map(([, value]) => value))
                return <div className="zone-row" key={zone}><span>{zone}</span><div className="bar-track"><i style={{ width: `${(count / largestZoneCount) * 100}%` }} /></div><strong>{count}</strong></div>
              })}</div>
            </div>
          </div>
        </section>

        <section className="panel events-panel" aria-labelledby="events-heading">
          <div className="panel-heading">
            <div><span className="eyebrow">04 / SEQUENCE LOG</span><h2 id="events-heading">Match events</h2></div>
            <span className="count-badge">{events.length.toString().padStart(2, '0')}</span>
          </div>
          <div className="event-table-wrap">
            <table className="event-table">
              <thead><tr><th>Time</th><th>Team</th><th>Action</th><th>Player</th><th>Result</th></tr></thead>
              <tbody>{events.map((event) => <tr key={event.eventId} className={event.eventId === selectedEventId ? 'is-selected' : ''}>
                <td><button className="time-link" onClick={() => setSelectedEventId(event.eventId)}>{String(event.minute).padStart(2, '0')}:{String(event.second).padStart(2, '0')}</button></td>
                <td><span className={`team-mini ${event.teamId === details.homeTeam.id ? 'arsenal-text' : 'chelsea-text'}`}>{teamShortName(event.teamId)}</span></td>
                <td>{event.type}</td>
                <td>{playerNames.get(event.playerId) ?? 'Unknown player'}</td>
                <td><span className="outcome-tag">{event.outcome ?? '—'}</span></td>
              </tr>)}</tbody>
            </table>
          </div>
        </section>
      </div>

      <footer className="app-footer"><span>BEYOND90 // THE GAME DOESN’T END AT FULL TIME</span><span>ALL MATCH EVENTS &amp; STATS SHOWN ARE SYNTHETIC</span><span>DATA SOURCE: LOCAL DEMO API</span></footer>
    </main>
  )
}

function TopBar({ apiStatus }: { apiStatus: 'connecting' | 'connected' | 'offline' }) {
  const statusLabel = { connecting: 'CONNECTING', connected: 'API CONNECTED', offline: 'API OFFLINE' }[apiStatus]
  return <header className="topbar">
    <a className="wordmark" href="#top" aria-label="Beyond90 home">BEYOND<span>90</span></a>
    <span className="topbar-caption">AI FOOTBALL INTELLIGENCE</span>
    <div className={`topbar-status status-${apiStatus}`}><span className="signal-bars"><i /><i /><i /></span><span>{statusLabel}</span><span className="topbar-divider">/</span><span className="status-demo">DEMO MODE</span></div>
  </header>
}

function Pitch({ events, selectedEventId, onSelect }: { events: MatchEvent[]; selectedEventId: string | null; onSelect: (eventId: string) => void }) {
  return <div className="pitch-wrap">
    <svg className="pitch-svg" viewBox="0 0 900 480" role="img" aria-label="Tactical pitch showing the synthetic match event positions">
      <defs>
        <pattern id="pitch-grid" width="30" height="30" patternUnits="userSpaceOnUse"><path d="M 30 0 L 0 0 0 30" fill="none" stroke="rgba(25, 101, 139, .22)" strokeWidth="1" /></pattern>
        <filter id="marker-glow" x="-100%" y="-100%" width="300%" height="300%"><feGaussianBlur stdDeviation="5" result="blur" /><feMerge><feMergeNode in="blur" /><feMergeNode in="SourceGraphic" /></feMerge></filter>
      </defs>
      <rect x="0" y="0" width="900" height="480" fill="url(#pitch-grid)" />
      <rect x="28" y="26" width="844" height="428" rx="4" className="pitch-outline" />
      <line x1="450" y1="26" x2="450" y2="454" className="pitch-line" />
      <circle cx="450" cy="240" r="72" className="pitch-line" />
      <circle cx="450" cy="240" r="3" className="pitch-dot" />
      <rect x="28" y="136" width="120" height="208" className="pitch-line" />
      <rect x="28" y="190" width="46" height="100" className="pitch-line" />
      <rect x="752" y="136" width="120" height="208" className="pitch-line" />
      <rect x="826" y="190" width="46" height="100" className="pitch-line" />
      <path d="M 148 190 A 58 58 0 0 1 148 290" className="pitch-line" />
      <path d="M 752 190 A 58 58 0 0 0 752 290" className="pitch-line" />
      {events.map((event) => {
        const x = 28 + (event.x / 100) * 844
        const y = 26 + (event.y / 100) * 428
        const teamClass = event.teamId === 'arsenal' ? 'event-marker-arsenal' : 'event-marker-chelsea'
        const isSelected = event.eventId === selectedEventId
        const label = `${event.eventId}, ${event.minute} minutes ${event.second} seconds, ${event.type}, x ${event.x}, y ${event.y}`
        return <g key={event.eventId} className={`event-marker ${teamClass} ${isSelected ? 'is-selected' : ''}`} transform={`translate(${x} ${y})`} role="button" tabIndex={0} aria-label={label} aria-pressed={isSelected} onClick={() => onSelect(event.eventId)} onKeyDown={(keyEvent) => { if (keyEvent.key === 'Enter' || keyEvent.key === ' ') { keyEvent.preventDefault(); onSelect(event.eventId) } }}>
          <circle r={isSelected ? 16 : 11} className="marker-hit-area" />
          <circle r={isSelected ? 7 : 5} className="marker-core" filter={isSelected ? 'url(#marker-glow)' : undefined} />
          {isSelected && <circle r="17" className="marker-ring" />}
        </g>
      })}
    </svg>
    <div className="pitch-coordinates"><span>← ARSENAL ATTACK</span><span>CHELSEA ATTACK →</span></div>
  </div>
}

function Metric({ label, value, detail, accent }: { label: string; value: string; detail: string; accent: string }) {
  return <div className={`metric-block metric-${accent}`}><span className="metric-label">{label}</span><strong>{value}</strong><small>{detail}</small><div className="metric-bars" aria-hidden="true"><i /><i /><i /><i /><i /><i /><i /></div></div>
}

function LoadingScreen() {
  return <main id="top" className="app-shell"><TopBar apiStatus="connecting" /><section className="loading-state" aria-live="polite"><span className="loading-pulse" /><span className="eyebrow">ESTABLISHING DATA LINK</span><strong>Loading synthetic match feed…</strong><div className="loading-line"><i /></div></section></main>
}

export default App
