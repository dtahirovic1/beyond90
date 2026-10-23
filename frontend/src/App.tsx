import { type CSSProperties, useEffect, useMemo, useRef, useState } from 'react'
import './App.css'
import { PixelJersey, squadNumber } from './PixelJersey'

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
type Audience = 'Player' | 'Coach' | 'Fan'
type GeneratedInsight = {
  source?: 'Demo' | 'Foundry' | 'DemoFallback'
  insightId: string
  audience: Audience
  eventIds: string[]
  metricEvidence: { metricKey: string; value: number | null; eventIds: string[] }[]
  text: string
  caveats: string[]
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
  const [audience, setAudience] = useState<Audience>('Fan')
  const [hasEntered, setHasEntered] = useState(false)
  const [insight, setInsight] = useState<GeneratedInsight | null>(null)
  const [insightEventId, setInsightEventId] = useState<string | null>(null)
  const [insightLoading, setInsightLoading] = useState(false)
  const [insightError, setInsightError] = useState<string | null>(null)
  const insightRequest = useRef<AbortController | null>(null)

  useEffect(() => {
    insightRequest.current?.abort()
    setInsightLoading(false)
    setInsight(null)
    setInsightError(null)
    return () => insightRequest.current?.abort()
  }, [selectedEventId, audience])

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
        setError('Unable to load the demo match. Please try again.')
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false)
      })

    return () => controller.abort()
  }, [reload])

  const playerNames = useMemo(() => new Map(data?.details.players.map((player) => [player.id, player.name])), [data])
  const selectedEvent = data?.events.find((event) => event.eventId === selectedEventId) ?? null

  function enterAs(role: Audience) {
    setAudience(role)
    setHasEntered(true)
  }

  async function generateInsight() {
    if (!selectedEventId) return
    insightRequest.current?.abort()
    const controller = new AbortController()
    insightRequest.current = controller
    setInsightLoading(true)
    setInsightError(null)
    setInsight(null)
    const requestedEventId = selectedEventId
    try {
      const response = await fetch('/api/insights/generate', {
        signal: controller.signal,
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ matchId, eventIds: [requestedEventId], audience }),
      })
      if (!response.ok) throw new Error(`Server returned ${response.status}`)
      const result = await response.json() as GeneratedInsight
      if (controller.signal.aborted) return
      setInsightEventId(requestedEventId)
      setInsight(result)
    } catch {
      if (!controller.signal.aborted) setInsightError('Unable to generate an insight. Please try again.')
    } finally {
      if (!controller.signal.aborted) setInsightLoading(false)
    }
  }

  if (!hasEntered) {
    return <WelcomeScreen players={data?.details.players ?? []} apiStatus={error ? 'offline' : loading ? 'connecting' : 'connected'} onEnter={enterAs} />
  }
  if (loading) return <LoadingScreen />
  if (error || !data) {
    return (
      <main id="top" className="app-shell">
        <TopBar apiStatus="offline" onChangeRole={() => setHasEntered(false)} />
        <section className="system-state" role="alert">
          <span className="eyebrow">SIGNAL LOST</span>
          <h1>Match unavailable</h1>
          <p>{error ?? 'Match data is temporarily unavailable.'}</p>
          <button className="pixel-button" onClick={() => setReload((value) => value + 1)}>Try again</button>
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
      <TopBar apiStatus="connected" audience={audience} onChangeRole={() => setHasEntered(false)} />

      <section className="match-intro">
        <div>
          <div className="eyebrow"><span className="status-dot" /> BEYOND90 / MATCH INTELLIGENCE</div>
          <h1>{details.homeTeam.name}<span className="versus"> vs </span>{details.awayTeam.name}</h1>
        </div>
        <div className="scenario-badge">
          <span className="badge-mark">SIM</span>
          <span><strong>SYNTHETIC SCENARIO</strong><small>Synthetic simulation — not a real match</small></span>
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
          <Pitch events={events} players={data.details.players} selectedEventId={selectedEventId} onSelect={setSelectedEventId} />
          <div className="event-readout" aria-live="polite">
            {selectedEvent ? <>
              <div className="readout-time">{String(selectedEvent.minute).padStart(2, '0')}<small>′</small>{String(selectedEvent.second).padStart(2, '0')}<small>″</small></div>
              <div className="readout-copy"><strong>{selectedEvent.type}<span>{selectedEvent.outcome ?? 'OUTCOME NOT RECORDED'}</span></strong><small>{playerNames.get(selectedEvent.playerId) ?? 'Unknown player'} · {teamShortName(selectedEvent.teamId)}</small></div>
              <span className="coordinate">X {selectedEvent.x} / Y {selectedEvent.y}</span>
            </> : <span>Select an event on the pitch to inspect it.</span>}
          </div>
          <section className="insight-tool" aria-labelledby="insight-heading">
            <div className="insight-controls">
              <div><span className="eyebrow">AI / MATCH EXPLANATION</span><h3 id="insight-heading">Event insight</h3></div>
              <label className="audience-picker">VIEW AS
                <select value={audience} onChange={(event) => { setAudience(event.target.value as Audience); setInsight(null) }}>
                  <option value="Fan">Fan</option><option value="Player">Player</option><option value="Coach">Coach</option>
                </select>
              </label>
            </div>
            <button className="pixel-button insight-button" onClick={generateInsight} disabled={!selectedEvent || insightLoading}>
              {insightLoading ? 'GENERATING…' : 'GENERATE INSIGHT FOR SELECTED EVENT'}
            </button>
            {insightError && <p className="insight-error" role="alert">{insightError}</p>}
            {insight && insightEventId === selectedEventId && insight.audience === audience && <div className="insight-result" aria-live="polite">
              <span className="eyebrow">{insight.source === 'Foundry' ? 'AZURE AI' : insight.source === 'DemoFallback' ? 'DEMO FALLBACK — AI UNAVAILABLE' : 'LOCAL DEMO'}</span>
              <p>{insight.text}</p>
              <div className="insight-evidence">{insight.metricEvidence.map((evidence) => <span key={evidence.metricKey}>{evidence.metricKey}: {evidence.value ?? '—'}</span>)}</div>
              {insight.caveats.map((caveat) => <small className="insight-caveat" key={caveat}>{caveat}</small>)}
            </div>}
          </section>
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

      <footer className="app-footer"><span>BEYOND90</span><span>ALL MATCH EVENTS &amp; STATS SHOWN ARE SYNTHETIC</span><span>DATA SOURCE: LOCAL DEMO API</span></footer>
    </main>
  )
}

function TopBar({ apiStatus, audience, onChangeRole }: { apiStatus: 'connecting' | 'connected' | 'offline'; audience?: Audience; onChangeRole?: () => void }) {
  const statusLabel = { connecting: 'CONNECTING', connected: 'API CONNECTED', offline: 'API OFFLINE' }[apiStatus]
  return <header className="topbar">
    <a className="wordmark" href="#top" aria-label="Beyond90 home">BEYOND<span>90</span></a>
    <span className="topbar-caption">AI FOOTBALL INTELLIGENCE</span>
    <div className={`topbar-status status-${apiStatus}`}><span className="signal-bars"><i /><i /><i /></span><span>{statusLabel}</span><span className="topbar-divider">/</span><span className="status-demo">DEMO MODE</span></div>
    {audience && onChangeRole && <button className="role-switch" onClick={onChangeRole}>VIEWING AS {audience.toUpperCase()} <span>↗</span></button>}
  </header>
}

function WelcomeScreen({ apiStatus, players, onEnter }: { apiStatus: 'connecting' | 'connected' | 'offline'; players: Player[]; onEnter: (role: Audience) => void }) {
  const roles: { role: Audience; description: string; number: string }[] = [
    { role: 'Coach', number: '01', description: 'Review match events and coaching insights.' },
    { role: 'Player', number: '02', description: 'Review player actions and individual feedback.' },
    { role: 'Fan', number: '03', description: 'Explore key moments and match explanations.' },
  ]
  return <main id="top" className="welcome-shell">
    <header className="welcome-nav">
      <a className="wordmark" href="#top" aria-label="Beyond90 home">BEYOND<span>90</span><i /></a>
      <span className="nav-tagline">AI Football Intelligence</span>
      <span className="edition-tag">MATCHROOM / 001</span>
    </header>
    <section className="entry-layout" aria-labelledby="welcome-title">
      <div className="match-poster">
        <div className="poster-top"><span><i /> FEATURED MATCH</span><span>SYNTHETIC SCENARIO</span></div>
        <div className="poster-title"><h2>Arsenal<br /><em>vs Chelsea</em></h2><span className="poster-index">01</span></div>
        <div className="kit-collection">
          {(['arsenal', 'chelsea'] as const).map((team) => <div className={'club-row club-' + team} key={team}>
            <div className="club-label"><span className="club-color" /><strong>{team === 'arsenal' ? 'Arsenal' : 'Chelsea'}</strong><span>{team === 'arsenal' ? 'NORTH LONDON' : 'WEST LONDON'}</span></div>
            <div className="jersey-lineup">
              {players.filter((player) => player.teamId === team).map((player) => <figure className="locker-player" key={player.id}>
                <PixelJersey team={team} name={player.name} number={squadNumber(player.id)} />
                <figcaption><strong>{player.name}</strong><span>{player.position ?? 'Player'}</span></figcaption>
              </figure>)}
              {players.length === 0 && <div className="kit-placeholder"><PixelJersey team={team} name={team} number="" /><span>{apiStatus === 'offline' ? 'Squad available when match reconnects' : 'Loading the squad…'}</span></div>}
            </div>
          </div>)}
        </div>
        <div className="poster-bottom"><span>Synthetic match data</span><span>ARS <b>×</b> CHE</span></div>
      </div>
      <div className="entry-copy">
        <span className="entry-kicker"><span /> CHOOSE YOUR VIEW</span>
        <h1 id="welcome-title">The Game<br />Doesn't End<br />at <em>Full Time.</em></h1>
        <div className="entry-roles" aria-label="Choose your perspective">{roles.map((role) => <button key={role.role} className="entry-role" onClick={() => onEnter(role.role)}>
          <span className="entry-role-index">{role.number}</span>
          <span className="entry-role-copy"><strong>{role.role}</strong><small>{role.description}</small></span>
          <span className="entry-role-arrow" aria-hidden="true">↗</span>
        </button>)}</div>
        <div className="entry-note"><span className="entry-note-dot" /> No account required.</div>
      </div>
    </section>
    <footer className="welcome-footer"><span>DEMO MATCH</span><span>BEYOND90 <b>©</b> FOOTBALL INTELLIGENCE</span></footer>
  </main>
}
function Pitch({ events, players, selectedEventId, onSelect }: { events: MatchEvent[]; players: Player[]; selectedEventId: string | null; onSelect: (eventId: string) => void }) {
  const [replay, setReplay] = useState(0)
  const selected = events.find((event) => event.eventId === selectedEventId)
  const animatedAction = selected?.type === 'Pass' || selected?.type === 'Shot'
  const playerEvents = players.map((player) => ({
    player,
    event: selected?.playerId === player.id ? selected : [...events].reverse().find((event) => event.playerId === player.id),
  })).filter((item): item is { player: Player; event: MatchEvent } => Boolean(item.event))
  const placed: { x: number; y: number }[] = []
  const positions = playerEvents.map((item) => {
    const eventX = 28 + item.event.x / 100 * 844
    const eventY = 26 + item.event.y / 100 * 428
    const candidates = [[0, 0], [0, -105], [0, 105], [-100, 0], [100, 0], [-100, -105], [100, 105]]
    const options = candidates.map(([dx, dy]) => ({ x: Math.min(822, Math.max(78, eventX + dx)), y: Math.min(385, Math.max(90, eventY + dy)) }))
    const position = options.find((candidate) => placed.every((previous) => Math.abs(candidate.x - previous.x) >= 90 || Math.abs(candidate.y - previous.y) >= 100)) ?? options[0]
    placed.push(position)
    return { ...item, ...position, eventX, eventY }
  })
  return <div className="pitch-wrap">
    <svg className="pitch-svg" viewBox="0 0 900 480" role="group" aria-label="Recorded event locations; these are different moments, not a simultaneous player formation">
      <rect width="900" height="480" fill="#23312f" />
      <path className="pitch-mow" d="M240 26h140v428H240zm280 0h140v428H520z" />
      <rect x="28" y="26" width="844" height="428" className="pitch-outline" />
      <path d="M450 26v428M28 136h120v208H28M28 190h46v100H28M872 136H752v208h120M872 190h-46v100h46" className="pitch-line" />
      <circle cx="450" cy="240" r="72" className="pitch-line" />
      <circle cx="450" cy="240" r="3" className="pitch-dot" />
      <path d="M148 190a58 58 0 0 1 0 100M752 190a58 58 0 0 0 0 100" className="pitch-line" />
      {positions.map(({ player, event, x, y, eventX, eventY }) => <g key={player.id}>
        {(Math.abs(x - eventX) > 1 || Math.abs(y - eventY) > 1) && <>
          <line x1={eventX} y1={eventY} x2={x} y2={y} className="location-leader" />
          <circle cx={eventX} cy={eventY} r="3" className="event-location" />
        </>}
        <g className="event-marker" transform={'translate(' + x + ' ' + y + ')'} role="button" tabIndex={0} aria-label={player.name + ', ' + event.type + ' at ' + event.minute + ':' + String(event.second).padStart(2, '0')} aria-pressed={event.eventId === selectedEventId} onClick={() => onSelect(event.eventId)} onKeyDown={(keyEvent) => { if (keyEvent.key === 'Enter' || keyEvent.key === ' ') { keyEvent.preventDefault(); onSelect(event.eventId) } }}>
          <rect x="-43" y="-51" width="86" height="108" rx="5" className="marker-hit-area" />
          {event.eventId === selectedEventId && <rect x="-43" y="-51" width="86" height="108" rx="5" className="marker-ring" />}
          <svg x="-40" y="-48" width="80" height="88" viewBox="0 0 80 88" aria-hidden="true"><PixelJersey team={player.teamId} name={player.name} number={squadNumber(player.id)} /></svg>
          <text x="0" y="49" textAnchor="middle" fill="#f3f2e9" fontSize="11" fontWeight="700">{player.name.split(' ').at(-1)} · {event.minute}′</text>
        </g>
      </g>)}
      {selected && <EventBall key={`${selected.eventId}-${replay}`} event={selected} />}
    </svg>
    <div className="pitch-coordinates"><span>Recorded actions · different moments</span><span>Choose a shirt to inspect its event ↗</span></div>
    {animatedAction && <div className="ball-controls"><span>Illustrative path · destination not recorded</span><button className="replay-action" onClick={() => setReplay((value) => value + 1)}>↻ Replay {selected.type.toLowerCase()}</button></div>}
  </div>
}

function EventBall({ event }: { event: MatchEvent }) {
  const startX = 28 + event.x / 100 * 844
  const startY = 26 + event.y / 100 * 428
  const direction = event.teamId === 'arsenal' ? 1 : -1
  const moving = event.type === 'Pass' || event.type === 'Shot'
  // Visual illustration only: the source has no ball destination or recipient.
  const endX = event.type === 'Shot' ? (direction > 0 ? 864 : 36) : Math.max(42, Math.min(858, startX + direction * 135))
  const endY = event.type === 'Shot' ? 240 : Math.max(42, Math.min(438, startY - 38))
  const motionStyle = { '--ball-dx': `${endX - startX}px`, '--ball-dy': `${endY - startY}px` } as CSSProperties
  return <g className="ball-illustration" pointerEvents="none" aria-hidden="true">
    {moving && <path className="ball-trail" d={`M${startX} ${startY}L${endX} ${endY}`} />}
    <g transform={`translate(${startX} ${startY})`}>
      <g className={moving ? `ball-flight ${event.type === 'Shot' ? 'ball-shot' : ''}` : ''} style={motionStyle}>
        <ellipse cy="11" rx="10" ry="3" fill="#000" opacity=".35" />
        <g className={moving ? 'ball-spin' : ''}>
          <circle r="9" fill="#fffdf4" stroke="#111b20" strokeWidth="1.5" />
          <path d="M0 -4 4 -1 2.5 4H-2.5L-4 -1ZM-3 -8H3L1 -6H-1ZM7 -5 9 0 6 1 5 -2ZM5 7 0 9 -1 6 2 5ZM-8 4-9-1-6-2-5 1Z" fill="#17212a" />
        </g>
      </g>
    </g>
  </g>
}
function Metric({ label, value, detail, accent }: { label: string; value: string; detail: string; accent: string }) {
  return <div className={`metric-block metric-${accent}`}><span className="metric-label">{label}</span><strong>{value}</strong><small>{detail}</small><div className="metric-bars" aria-hidden="true"><i /><i /><i /><i /><i /><i /><i /></div></div>
}

function LoadingScreen() {
  return <main id="top" className="app-shell"><TopBar apiStatus="connecting" /><section className="loading-state" aria-live="polite"><span className="loading-pulse" /><span className="eyebrow">ESTABLISHING DATA LINK</span><strong>Loading synthetic match feed…</strong><div className="loading-line"><i /></div></section></main>
}

export default App
