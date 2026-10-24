import { useEffect, useRef, useState } from 'react'
import './App.css'
import { PixelJersey } from './PixelJersey'
import { DecisionPitch, type DecisionAnalysis } from './DecisionPitch'
import { squadNumber } from './jerseyNumber'

type Audience = 'Player' | 'Coach'
type Player = { id: string; teamId: string; name: string; position?: string }
type Result = { home:number; away:number }
type Team = { id: string; name: string }
type MatchSummary = { id: string; homeTeam: string; awayTeam: string; scenarioLabel: string; eventCount: number }
type MatchEvent = { eventId: string; sequence: number; minute: number; second: number; teamId: string; playerId: string; type: string; x: number; y: number; outcome: string | null; underPressure: boolean | null; scoreBefore: Record<string,number> | null }
type Details = { id: string; homeTeam: Team; awayTeam: Team; teams: Team[]; players: Player[]; scenarioLabel: string; result:Result|null }
type KeyMoment = { eventIds: string[]; score: number; reasonCode: string; events: MatchEvent[] }
type Stats = { events: number; passes: number; completedPasses: number; passAccuracy: number | null; shots: number; goals: number; recoveries: number; tacklesWon: number; turnovers: number; pressureEvents: number }
type Report = { matchCount: number; stats: Stats; observations: string[]; coverage: string; matches: { id: string; scenarioLabel: string; opponent: string; result:Result|null; stats: Stats }[] }
type Insight = { source?: string; text: string; caveats: string[] }

async function getJson<T>(url: string, signal: AbortSignal): Promise<T> {
  const response = await fetch(url, { signal })
  if (!response.ok) throw new Error('Unable to load this view. Please try again.')
  return response.json() as Promise<T>
}
function useResource<T>(url: string) {
  const [state, setState] = useState<{ url: string; data: T | null; error: string | null; loading: boolean }>({ url, data: null, error: null, loading: true })
  const [attempt, setAttempt] = useState(0)
  useEffect(() => {
    const controller = new AbortController()
    getJson<T>(url, controller.signal).then(data => { if (!controller.signal.aborted) setState({ url, data, error: null, loading: false }) })
      .catch(() => { if (!controller.signal.aborted) setState({ url, data: null, error: 'Unable to load this view. Please try again.', loading: false }) })
    return () => controller.abort()
  }, [url, attempt])
  return { ...(state.url === url ? state : { data: null, error: null, loading: true }), retry: () => { setState({ url, data: null, error: null, loading: true }); setAttempt(value => value + 1) } }
}
function Loading() { return <div className="view-state" role="status">Loading matchroom…</div> }
function Failure({ retry }: { retry: () => void }) { return <div className="view-state" role="alert"><h2>Data unavailable</h2><p>Please check the connection and try again.</p><button className="pixel-button" onClick={retry}>Try again</button></div> }
function Brand() { return <a className="wordmark" href="#" aria-label="Beyond90 home">BEYOND<span>90</span></a> }
function Footer() { return <footer className="app-footer"><span>BEYOND90</span><span>SYNTHETIC SCENARIOS · NOT REAL PLAYER PERFORMANCE</span><span>LEEDS UNITED / 2026–27</span></footer> }
function percent(value: number | null) { return value === null ? '—' : `${value}%` }

function App() {
  const [route, setRoute] = useState(location.hash.slice(1))
  useEffect(() => { const update = () => { setRoute(location.hash.slice(1)); window.scrollTo(0, 0) }; window.addEventListener('hashchange', update); return () => window.removeEventListener('hashchange', update) }, [])
  const club = useResource<{ name: string; players: Player[] }>('/api/reports/club')
  const matches = useResource<MatchSummary[]>('/api/matches')
  const parts = route.split('/')
  const playerId = parts[0] === 'player' ? parts[1] : undefined
  const player = club.data?.players.find(p => p.id === playerId)
  const audience: Audience = parts[0] === 'coach' ? 'Coach' : 'Player'
  const matchId = parts[0] === 'coach' && parts[1] === 'match' ? parts[2] : parts[0] === 'player' && parts[2] === 'match' ? parts[3] : undefined
  if (!route) return <Welcome players={club.data?.players ?? []} loading={club.loading} error={Boolean(club.error)} retry={club.retry} />
  return <main className="app-shell" id="top">
    <header className="topbar"><Brand /><span className="topbar-caption">LEEDS UNITED / MATCHROOM</span><a className="role-switch" href="#">Change role ↗</a></header>
    <nav className="workspace-nav" aria-label="Breadcrumb"><a href="#">Home</a><span>/</span><a href={audience === 'Coach' ? '#coach' : '#players'}>{audience === 'Coach' ? 'Club overview' : 'Players'}</a>{player && <><span>/</span><a href={`#player/${player.id}`}>{player.name}</a></>}{matchId && <><span>/</span><span>Match review</span></>}</nav>
    {club.loading || matches.loading ? <Loading /> : club.error || matches.error ? <Failure retry={() => { club.retry(); matches.retry() }} /> :
      parts[0] === 'players' ? <PlayerPicker players={club.data!.players} /> :
      (parts[0] === 'coach' || player) ? matchId ? <MatchReview key={`${audience}-${playerId}-${matchId}`} matchId={matchId} player={player} audience={audience} /> :
        <Overview key={playerId ?? 'coach'} player={player} audience={audience} players={club.data!.players} /> :
        <div className="view-state"><h1>View not found</h1><a className="pixel-button" href="#">Return home</a></div>}
    <Footer />
  </main>
}

function Welcome({ players, loading, error, retry }: { players: Player[]; loading: boolean; error: boolean; retry: () => void }) {
  return <main className="welcome-shell"><header className="welcome-nav"><Brand /><span className="nav-tagline">AI Football Intelligence</span><span className="edition-tag">LEEDS / 2026–27</span></header>
    <section className="entry-layout"><div className="match-poster leeds-poster">
      <div className="poster-top"><span><i /> CLUB MATCHROOM</span><span>SYNTHETIC SCENARIOS</span></div>
      <div className="poster-title"><h2>Leeds<br /><em>United.</em></h2><span className="poster-index">LU</span></div>
      <div className="club-row"><div className="club-label"><span className="club-color leeds-color" /><strong>Player analysis</strong><span>2026 / 27</span></div>
        <div className="jersey-lineup">{players.map(p => <figure className="locker-player" key={p.id}><PixelJersey team={p.teamId} name={p.name} number={squadNumber(p.id)} /><figcaption><strong>{p.name}</strong><span>{p.position}</span></figcaption></figure>)}</div>
        {loading && <Loading />}{error && <Failure retry={retry} />}
      </div><div className="poster-opponents"><span>SCENARIO OPPONENTS</span><div><b>Arsenal</b><b>Chelsea</b><b>Liverpool</b></div></div>
      <div className="poster-bottom"><span>Real players. Simulated match events.</span><span>LEEDS <b>×</b> BEYOND90</span></div>
    </div><div className="entry-copy"><span className="entry-kicker"><span /> CHOOSE YOUR VIEW</span><h1>The Game<br />Doesn't End<br />at <em>Full Time.</em></h1>
      <div className="entry-roles">{([{role:'Player',href:'#players',description:'Choose your player. Review performance across matches.'},{role:'Coach',href:'#coach',description:'Review the club, compare matches and inspect decisions.'}] as const).map((item,i) => <a className="entry-role" key={item.role} href={item.href}><span className="entry-role-index">0{i+1}</span><span className="entry-role-copy"><strong>{item.role}</strong><small>{item.description}</small></span><span className="entry-role-arrow">↗</span></a>)}</div><div className="entry-note"><span className="entry-note-dot" /> No account required.</div>
    </div></section><Footer /></main>
}
function PlayerPicker({ players }: { players: Player[] }) {
  return <><section className="overview-heading"><span className="eyebrow">PLAYER / LEEDS UNITED</span><h1>Choose your player.</h1><p>Start with a performance overview, then explore an individual match.</p></section>
    <div className="player-picker">{players.map(p => <a className="player-card" href={`#player/${p.id}`} key={p.id}><div className="player-card-top"><span>{p.position}</span><b>#{squadNumber(p.id)}</b></div><PixelJersey team={p.teamId} name={p.name} number={squadNumber(p.id)} /><h2>{p.name}</h2><div className="player-card-bottom"><span>View player overview</span><span>↗</span></div></a>)}</div></>
}
function Overview({ player, audience, players }: { player?: Player; audience: Audience; players: Player[] }) {
  return <><section className="overview-hero"><div><span className="eyebrow">{player ? 'PLAYER OVERVIEW' : 'COACH / CLUB OVERVIEW'} · LEEDS UNITED</span><h1>{player ? player.name : 'Leeds United'}</h1><p>{player ? `${player.position} · #${squadNumber(player.id)} · 2026/27` : 'Performance across the available match scenarios'}</p><span className="sample-pill">Synthetic event samples</span></div>
    {player ? <PixelJersey team="leeds" name={player.name} number={squadNumber(player.id)} /> : <div className="club-hero-shirts">{players.map(p => <PixelJersey key={p.id} team="leeds" name={p.name} number={squadNumber(p.id)} />)}</div>}</section>
    <Performance key={player?.id ?? 'club'} player={player} audience={audience} />
    {!player && <section className="tracked-section"><div className="section-title"><h2>Tracked players</h2><span>3 individual profiles</span></div><div className="tracked-grid">{players.map(p => <a href={`#player/${p.id}`} key={p.id}><PixelJersey team="leeds" name={p.name} number={squadNumber(p.id)} /><div><strong>{p.name}</strong><small>{p.position}</small></div><span>↗</span></a>)}</div></section>}
  </>
}

function Performance({ player, audience, matchId }: { player?: Player; audience: Audience; matchId?: string }) {
  const query = new URLSearchParams({ ...(player ? { playerId: player.id } : {}), ...(matchId ? { matchId } : {}) })
  const report = useResource<Report>(`/api/reports?${query}`)
  if (report.loading) return <Loading />
  if (report.error || !report.data) return <Failure retry={report.retry} />
  const { stats, matches, coverage, observations } = report.data
  return <><div className="section-title"><h2>{matchId ? 'Performance in this match' : 'Performance overview'}</h2><span>{player ? player.name : 'Tracked Leeds players'} · {report.data.matchCount} {report.data.matchCount === 1 ? 'scenario' : 'scenarios'}</span></div>
    <div className="performance-grid"><Stat label="Pass completion" value={percent(stats.passAccuracy)} detail={`${stats.completedPasses} / ${stats.passes} recorded passes`} /><Stat label="Shots / goals" value={`${stats.shots} / ${stats.goals}`} detail="In the recorded event sample" /><Stat label="Recoveries" value={stats.recoveries} detail={`Tackles won: ${stats.tacklesWon}`} /><Stat label="Turnovers" value={stats.turnovers} detail={`${stats.events} total recorded actions`} /></div>
    <p className="coverage-note">{coverage}</p>
    <section className="overview-analysis"><div><span className="eyebrow">DATA REVIEW</span><h2>{player ? 'Your performance review' : 'Club performance review'}</h2><div className="observation-list">{observations.map((text,i) => <div key={text}><span>0{i+1}</span><p>{text}</p></div>)}</div></div><div className="ai-review"><AiInsight key={query.toString()} endpoint="/api/reports/insight" body={{ playerId: player?.id, matchId, audience }} title={player ? 'Player analysis' : 'Coaching analysis'} label="Generate AI review" /></div></section>
    {!matchId && <section className="match-library"><div className="section-title"><div><span className="eyebrow">MATCH LIBRARY</span><h2>Explore a match</h2></div><span>Compare recorded samples</span></div><div className="match-cards">{matches.map((match,i) => <a className="match-card" href={player ? `#player/${player.id}/match/${match.id}` : `#coach/match/${match.id}`} key={match.id}>
      <div className="match-card-top"><span>{match.scenarioLabel}</span><span>SIM</span></div><h3>Leeds United<br /><span>vs {match.opponent}</span></h3>{match.result&&<div className="match-result">{match.result.home} – {match.result.away}<small>FINAL · SIMULATED</small></div>}<div className="match-card-metrics"><div><strong>{percent(match.stats.passAccuracy)}</strong><small>Pass completion</small></div><div><strong>{match.stats.shots}</strong><small>Shots</small></div><div><strong>{match.stats.events}</strong><small>Actions</small></div></div><div className="sample-chart" aria-label={`Pass completion: ${percent(match.stats.passAccuracy)}`}><i style={{width:`${match.stats.passAccuracy ?? 0}%`}} /></div><div className="match-card-bottom"><span>Review match 0{i+1}</span><b>↗</b></div></a>)}</div></section>}
  </>
}
function Stat({ label, value, detail }: { label: string; value: string | number; detail: string }) { return <div className="stat-tile"><span>{label}</span><strong>{value}</strong><small>{detail}</small></div> }
function AiInsight({ endpoint, body, title, label }: { endpoint: string; body: object; title: string; label: string }) {
  const [insight, setInsight] = useState<Insight | null>(null)
  const [error, setError] = useState(false)
  const [loading, setLoading] = useState(false)
  const request = useRef<AbortController | null>(null)
  useEffect(() => () => request.current?.abort(), [])
  async function generate() {
    request.current?.abort(); const controller = new AbortController(); request.current = controller
    setLoading(true); setError(false); setInsight(null)
    try { const response = await fetch(endpoint, { method:'POST', headers:{'Content-Type':'application/json'}, body:JSON.stringify(body), signal:controller.signal }); if (!response.ok) throw new Error(); const result = await response.json() as Insight; if (!controller.signal.aborted) setInsight(result) }
    catch { if (!controller.signal.aborted) setError(true) }
    finally { if (!controller.signal.aborted) setLoading(false) }
  }
  return <><span className="eyebrow">AI / {title.toUpperCase()}</span><h3>{title}</h3><p className="ai-description">Review the recorded actions and statistics with feedback tailored to your view.</p><button className="pixel-button insight-button" onClick={generate} disabled={loading}>{loading ? 'Generating…' : label}</button>{error && <p className="insight-error" role="alert">Unable to generate a review. Please try again.</p>}{insight && <div className="insight-result" aria-live="polite"><span className="eyebrow">{insight.source === 'Foundry' ? 'AZURE AI' : insight.source === 'DemoFallback' ? 'LOCAL SUMMARY · AI UNAVAILABLE' : 'LOCAL SUMMARY'}</span><p>{insight.text}</p>{insight.caveats.map(c => <small key={c} className="insight-caveat">{c}</small>)}</div>}</>
}

function MatchReview({ matchId, player, audience }: { matchId: string; player?: Player; audience: Audience }) {
  const details = useResource<Details>(`/api/matches/${encodeURIComponent(matchId)}`)
  const feed = useResource<MatchEvent[]>(`/api/matches/${encodeURIComponent(matchId)}/events`)
  const recap = useResource<{ keyMoments: KeyMoment[] }>(`/api/matches/${encodeURIComponent(matchId)}/recap${player ? `?playerId=${encodeURIComponent(player.id)}` : ''}`)
  const decisions = useResource<DecisionAnalysis[]>(`/api/matches/${encodeURIComponent(matchId)}/decisions`)
  const [eventId,setEventId] = useState<string | null>(null)
  if (details.loading || feed.loading || recap.loading || decisions.loading) return <Loading />
  if (details.error || feed.error || recap.error || decisions.error || !details.data || !feed.data || !recap.data) return <Failure retry={() => {details.retry();feed.retry();recap.retry();decisions.retry()}} />
  const d = details.data; const events = feed.data.filter(e => !player || e.playerId === player.id)
  const detailedIds = new Set(decisions.data?.map(d => d.snapshot.eventId))
  const keyEvents = events.filter(e => detailedIds.has(e.eventId) || recap.data!.keyMoments.some(m => m.eventIds.includes(e.eventId)))
  const selected = events.find(e => e.eventId === eventId) ?? events.find(e => detailedIds.has(e.eventId)) ?? events[0]
  const scene = decisions.data?.find(d => d.snapshot.eventId === selected?.eventId)
  const names = new Map(d.players.map(p => [p.id,p.name]))
  return <><section className="match-intro"><div><span className="eyebrow">{d.scenarioLabel} / {audience.toUpperCase()} REVIEW</span><h1>{d.homeTeam.name}<span className="versus">vs</span>{d.awayTeam.name}</h1><p className="intro-subtitle">{player ? `${player.name} · Individual match review` : 'Tracked Leeds players · Match review'}</p></div><div>{d.result&&<div className="match-result">{d.result.home} – {d.result.away}<small>FINAL · SIMULATED</small></div>}<span className="sample-pill">Synthetic match scenario</span></div></section>
    <Performance player={player} audience={audience} matchId={matchId} />
    <div className="section-title"><h2>Decision review</h2><span>{player ? 'Select one of your recorded actions' : 'Select a player action'}</span></div>
    <div className="dashboard-grid"><section className="panel pitch-panel"><div className="panel-heading"><h2>Event map</h2><div className="legend"><span>Leeds United</span><span>{d.awayTeam.name}</span></div></div>
      {scene ? <DecisionPitch key={scene.snapshot.eventId} analysis={scene} players={d.players} /> : <Pitch events={selected ? [selected] : []} players={d.players} selectedEventId={selected?.eventId ?? null} onSelect={setEventId} focusPlayerId={player?.id} />}

      {selected ? <><div className="event-readout"><div className="readout-time">{String(selected.minute).padStart(2,'0')}:{String(selected.second).padStart(2,'0')}</div><div className="readout-copy"><strong>{selected.type}<span>{selected.outcome ?? 'Outcome unknown'}</span></strong><small>{names.get(selected.playerId)}{selected.scoreBefore&&` · Before action: ${d.homeTeam.name} ${selected.scoreBefore[d.homeTeam.id]}–${selected.scoreBefore[d.awayTeam.id]} ${d.awayTeam.name}`}</small></div></div><section className="insight-tool"><AiInsight key={selected.eventId} endpoint="/api/insights/generate" body={{matchId,eventIds:[selected.eventId],audience}} title="Event insight" label="Analyse selected decision" /></section></> : <p className="empty-state">No recorded actions for this player.</p>}
    </section><section className="panel moments-panel"><div className="panel-heading"><h2>Key decisions</h2></div>{keyEvents.length===0&&<p className="empty-state">Explore the recorded actions below.</p>}<ol className="moment-list">{keyEvents.map((e,i)=><li key={e.eventId}><button className={`moment-button ${e.eventId===selected?.eventId?'is-selected':''}`} onClick={()=>setEventId(e.eventId)}><span className="moment-index">{String(i+1).padStart(2,'0')}</span><span className="moment-content"><strong>{e.type} · {e.outcome??'Outcome unknown'}</strong><small>{e.minute}′ · {names.get(e.playerId)}</small><small className="decision-badge">{detailedIds.has(e.eventId)?'Compare options':'Event summary'}</small></span></button></li>)}</ol><p className="panel-footnote">Detailed decisions and notable recorded events, in match order. Select an action, then an option to see its path.</p></section>
      <section className="panel events-panel"><div className="panel-heading"><h2>{player ? 'Your match actions' : 'Match events'}</h2><span>{events.length} actions</span></div><div className="event-table-wrap"><table className="event-table"><thead><tr><th>Time</th><th>Player</th><th>Action</th><th>Outcome</th><th>Pressure</th></tr></thead><tbody>{events.map(e => <tr key={e.eventId} className={e.eventId === selected?.eventId ? 'is-selected' : ''}><td><button className="time-link" aria-label={`Select ${e.type} by ${names.get(e.playerId)} at ${e.minute}:${String(e.second).padStart(2,'0')}`} onClick={() => setEventId(e.eventId)}>{e.minute}:{String(e.second).padStart(2,'0')}</button></td><td>{names.get(e.playerId)}</td><td>{e.type}{detailedIds.has(e.eventId)&&<small className="decision-badge">Compare options</small>}</td><td>{e.outcome ?? 'Unknown'}</td><td>{e.underPressure === null ? 'Unknown' : e.underPressure ? 'Yes' : 'No'}</td></tr>)}</tbody></table></div></section>
    </div>
  </>
}
function Pitch({ events, players, selectedEventId, onSelect, focusPlayerId }: { events: MatchEvent[]; players: Player[]; selectedEventId: string | null; onSelect: (eventId: string) => void; focusPlayerId?: string }) {
  const selected = events.find((event) => event.eventId === selectedEventId)
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
    <svg className="pitch-svg" viewBox="0 0 900 480" role="group" aria-label="Selected action location; other player positions are unavailable">
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
        <g className="event-marker" transform={'translate(' + x + ' ' + y + ')'} role="button" aria-disabled={Boolean(focusPlayerId && player.id !== focusPlayerId)} tabIndex={focusPlayerId && player.id !== focusPlayerId ? -1 : 0} aria-label={player.name + ', ' + event.type + ' at ' + event.minute + ':' + String(event.second).padStart(2, '0')} aria-pressed={event.eventId === selectedEventId} onClick={() => { if (!focusPlayerId || player.id === focusPlayerId) onSelect(event.eventId) }} onKeyDown={(keyEvent) => { if ((!focusPlayerId || player.id === focusPlayerId) && (keyEvent.key === 'Enter' || keyEvent.key === ' ')) { keyEvent.preventDefault(); onSelect(event.eventId) } }}>
          <rect x="-43" y="-51" width="86" height="108" rx="5" className="marker-hit-area" />
          {event.eventId === selectedEventId && <rect x="-43" y="-51" width="86" height="108" rx="5" className="marker-ring" />}
          <svg x="-40" y="-48" width="80" height="88" viewBox="0 0 80 88" aria-hidden="true"><PixelJersey team={player.teamId} name={player.name} number={squadNumber(player.id)} /></svg>
          <text x="0" y="49" textAnchor="middle" fill="#f3f2e9" fontSize="10" fontWeight="700" stroke="#152321" strokeWidth="2" paintOrder="stroke fill">{player.name.split(' ').at(-1)} · {event.minute}′</text>
        </g>
      </g>)}
      {selected && <EventBall key={selected.eventId} event={selected} />}
    </svg>
    <div className="pitch-coordinates"><span>Single action · spatial snapshot unavailable</span><span>{focusPlayerId ? 'Your actions are listed below' : 'Choose a shirt to inspect its event'}</span></div>
  </div>
}

function EventBall({ event }: { event: MatchEvent }) {
  return <g transform={`translate(${28+event.x*8.44} ${26+event.y*4.28})`} pointerEvents="none" aria-hidden="true"><circle r="9" fill="#fffdf4" stroke="#111b20" strokeWidth="1.5"/><path d="M0 -4 4 -1 2.5 4H-2.5L-4 -1Z" fill="#17212a"/></g>
}

export default App
