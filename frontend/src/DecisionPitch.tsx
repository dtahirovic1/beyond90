import { PixelJersey } from './PixelJersey'
import { squadNumber } from './jerseyNumber'
export type DecisionAnalysis = {
 snapshot: { eventId: string; matchContext: {minute:number; score:Record<string,number>|null}; decision: { playerId: string; selectedOptionId: string; bodyPartUsed: string | null; options: {id: string; type: string; receiverId?: string}[] }; pitch: { lengthMeters:number; attackDirection: Record<string,string> }; preDecision: {bodySetup?:string|null;ballHeight?:string|null;ball: {x:number;y:number};players:{id:string;teamId:string;x:number;y:number}[]} };
 options: {optionId:string;type:string;receiverId:string|null;distanceMeters:number|null;nearestOpponentMeters:number|null;corridorClearanceMeters:number|null;spaceAheadMeters:number|null;goalOpeningDegrees:number|null;corridor:string;reason:string;progressionMeters:number|null;risk:number|null;benefit:number|null;techniqueNote?:string;setupRiskAdjustment?:number|null}[];
 verdict: {label:string;preferredOptionId:string|null;explanation:string;matchSituation:string;rating?:number|null;ratingLabel?:string;confidence?:string;components?:{riskControl:number;attackingValue:number;optionSelection:number}|null}|null;
 visiblePlayers:number;visibleOpponents:number;coverage:string;
}
export function DecisionPitch({analysis,players,optionId,onOptionChange}:{analysis:DecisionAnalysis;players:{id:string;name:string;teamId:string}[];optionId:string;onOptionChange:(id:string)=>void}) {
 const {snapshot,options}=analysis
 const option=options.find(o=>o.optionId===optionId)!
 const actor=snapshot.preDecision.players.find(p=>p.id===snapshot.decision.playerId)!
 const receiver=snapshot.preDecision.players.find(p=>p.id===option.receiverId)
 const x=(v:number)=>28+v*8.44, y=(v:number)=>26+v*4.28
 const start=snapshot.preDecision.ball
 const target=receiver ?? (option.type==='shot' ? {x:snapshot.pitch.attackDirection[actor.teamId]==='left_to_right'?100:0,y:50}:option.type==='dribble'?{x:Math.max(0,Math.min(100,start.x+(snapshot.pitch.attackDirection[actor.teamId]==='left_to_right'?1:-1)*Math.min(10,option.spaceAheadMeters??0)/snapshot.pitch.lengthMeters*100)),y:start.y}:null)
 const names=new Map(players.map(p=>[p.id,p.name]))
 const actionName=(o:DecisionAnalysis['options'][number])=>o.type==='pass'?`Pass to ${names.get(o.receiverId!)?.split(' ').at(-1)??'teammate'}`:o.type==='dribble'?'Carry forward':o.type==='shot'?'Shoot':'Tackle'
 const played=options.find(o=>o.optionId===snapshot.decision.selectedOptionId)!
 const viewingPlayed=option.optionId===played.optionId
 const alternatives=options.filter(o=>o.optionId!==played.optionId)
 const laneLabel=(o:DecisionAnalysis['options'][number])=>o.corridor==='Blocked'?'Defender close to the path':o.corridor==='Contested'?'Defender near the path':o.corridor==='Open'?'No visible defender close to the path':'Not enough position data'
 const placed:{x:number;y:number}[]=[]
 const markers=snapshot.preDecision.players.map(p=>{
   const px=x(p.x),py=y(p.y)
   const candidates=[[0,0],[0,-105],[0,105],[-100,0],[100,0],[-100,-105],[100,105]].map(([dx,dy])=>({x:Math.max(78,Math.min(822,px+dx)),y:Math.max(90,Math.min(385,py+dy))}))
   const pos=candidates.find(c=>placed.every(v=>Math.abs(c.x-v.x)>=90||Math.abs(c.y-v.y)>=100))??candidates[0]
   placed.push(pos);return {...p,px,py,...pos}
 })
 return <div className="decision-scene">
  <div className="played-action"><span className="eyebrow">PLAYED</span><strong>{actionName(played)}</strong><span>{names.get(actor.id)}</span></div>
  <div className="path-caption" aria-live="polite">{viewingPlayed?'Showing played option':'Previewing alternative'}: <strong>{actionName(option)}</strong>{target&&<small>Dashed line = option path, not a replay.</small>}</div>
  <svg className="pitch-svg" viewBox="0 0 900 480" role="img" aria-label="Player positions at the selected action. Shirt callouts connect to exact position dots.">
   <rect width="900" height="480" fill="#23312f"/><rect x="28" y="26" width="844" height="428" className="pitch-outline"/>
   <path d="M450 26v428M28 136h120v208H28M28 190h46v100H28M872 136H752v208h120M872 190h-46v100h46" className="pitch-line"/><circle cx="450" cy="240" r="72" className="pitch-line"/>
   {target&&<path d={`M${x(start.x)} ${y(start.y)}L${x(target.x)} ${y(target.y)}`} stroke="#d2ed83" strokeWidth="2" strokeDasharray="7 7" fill="none"/>}
   {markers.map(p=><g key={p.id}>
    <line x1={p.px} y1={p.py} x2={p.x} y2={p.y} className="location-leader"/><circle cx={p.px} cy={p.py} r="4" fill={p.id===actor.id?'#d2ed83':'#f5f2df'}/>
    <g transform={`translate(${p.x} ${p.y})`}>
     {p.id===actor.id&&<rect x="-43" y="-51" width="86" height="108" rx="5" className="marker-ring"/>}
     <svg x="-40" y="-48" width="80" height="88" viewBox="0 0 80 88"><PixelJersey team={p.teamId} name={names.get(p.id)??p.id} number={squadNumber(p.id)}/></svg>
     <text y="49" textAnchor="middle" fill="#fffdf5" stroke="#152321" strokeWidth="2" paintOrder="stroke fill" fontSize="11" fontWeight="700">{names.get(p.id)?.split(' ').at(-1)}</text>
    </g>
   </g>)}
   <g transform={`translate(${x(start.x)} ${y(start.y)})`}><circle r="8" fill="#fffdf5" stroke="#152321" strokeWidth="2"/><path d="M0 -4L4 -1 2 4H-2L-4 -1Z" fill="#152321"/></g>
  </svg>
  <div className="pitch-coordinates"><span>{analysis.visiblePlayers} recorded positions · Partial scene</span></div>
  <div className="options-heading"><strong>{alternatives.length?'Compare options':'Played option'}</strong><span>{alternatives.length?'Select an option, then analyse it.':'No alternatives recorded for this action.'}</span></div>
  <div className="decision-options" aria-label="Compare played action and alternatives">{[played,...alternatives].map(o=><button key={o.optionId} aria-pressed={optionId===o.optionId} className={optionId===o.optionId?'is-selected':''} onClick={()=>onOptionChange(o.optionId)}><small className="option-kind">{o.optionId===played.optionId?'PLAYED':'ALTERNATIVE'}</small><strong>{actionName(o)}</strong></button>)}</div>

  <details key={optionId} className="decision-details"><summary>View details for {actionName(option)}</summary>
   <p>{laneLabel(option)}.</p>
   <dl className="option-measurements">
    {option.distanceMeters!==null&&<><dt>{option.type==='dribble'?'Distance to goal line':'Distance'}</dt><dd>{option.distanceMeters} m</dd></>}
    {option.progressionMeters!==null&&option.type!=='shot'&&<><dt>Direction</dt><dd>{Math.abs(option.progressionMeters)} m {option.progressionMeters<0?'backward':'forward'}</dd></>}
    {option.corridorClearanceMeters!==null&&<><dt>Nearest defender to path</dt><dd>{option.corridorClearanceMeters} m</dd></>}
    {option.type==='pass'&&option.nearestOpponentMeters!==null&&<><dt>Nearest defender to receiver</dt><dd>{option.nearestOpponentMeters} m</dd></>}
    {option.spaceAheadMeters!==null&&<><dt>Space ahead (up to 10 m)</dt><dd>{option.spaceAheadMeters} m</dd></>}
    {option.goalOpeningDegrees!==null&&<><dt>Goal opening</dt><dd>{option.goalOpeningDegrees}°</dd></>}
    <dt>Body part used for played action</dt><dd>{snapshot.decision.bodyPartUsed?.replaceAll('_',' ')??'Unknown'}</dd>
    <dt>Body ready for</dt><dd>{snapshot.preDecision.bodySetup?.replaceAll('_',' ')??'Unknown'}</dd>
    <dt>Ball height</dt><dd>{snapshot.preDecision.ballHeight??'Unknown'}</dd>
   </dl>
   <p>{option.techniqueNote}</p>

   <small>Only {analysis.visiblePlayers} players are shown. Movement and unseen opponents may change the assessment.</small>
  </details>
 </div>
}
