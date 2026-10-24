import { useState } from 'react'
import { PixelJersey } from './PixelJersey'
import { squadNumber } from './jerseyNumber'
export type DecisionAnalysis = {
 snapshot: { eventId: string; matchContext: {minute:number; score:Record<string,number>|null}; decision: { playerId: string; selectedOptionId: string; bodyPartUsed: string | null; options: {id: string; type: string; receiverId?: string}[] }; pitch: { lengthMeters:number; attackDirection: Record<string,string> }; preDecision: {ball: {x:number;y:number};players:{id:string;teamId:string;x:number;y:number}[]} };
 options: {optionId:string;type:string;receiverId:string|null;distanceMeters:number|null;nearestOpponentMeters:number|null;corridorClearanceMeters:number|null;spaceAheadMeters:number|null;goalOpeningDegrees:number|null;corridor:string;reason:string;progressionMeters:number|null;risk:number|null;benefit:number|null}[];
 verdict: {label:string;preferredOptionId:string|null;explanation:string;matchSituation:string}|null;
 visiblePlayers:number;visibleOpponents:number;coverage:string;
}
export function DecisionPitch({analysis,players}:{analysis:DecisionAnalysis;players:{id:string;name:string;teamId:string}[]}) {
 const {snapshot,options}=analysis
 const [optionId,setOptionId]=useState(snapshot.decision.selectedOptionId)
 const option=options.find(o=>o.optionId===optionId)!
 const actor=snapshot.preDecision.players.find(p=>p.id===snapshot.decision.playerId)!
 const receiver=snapshot.preDecision.players.find(p=>p.id===option.receiverId)
 const x=(v:number)=>28+v*8.44, y=(v:number)=>26+v*4.28
 const start=snapshot.preDecision.ball
 const target=receiver ?? (option.type==='shot' ? {x:snapshot.pitch.attackDirection[actor.teamId]==='left_to_right'?100:0,y:50}:option.type==='dribble'?{x:Math.max(0,Math.min(100,start.x+(snapshot.pitch.attackDirection[actor.teamId]==='left_to_right'?1:-1)*Math.min(10,option.spaceAheadMeters??0)/snapshot.pitch.lengthMeters*100)),y:start.y}:null)
 const names=new Map(players.map(p=>[p.id,p.name]))
 const placed:{x:number;y:number}[]=[]
 const markers=snapshot.preDecision.players.map(p=>{
   const px=x(p.x),py=y(p.y)
   const candidates=[[0,0],[0,-105],[0,105],[-100,0],[100,0],[-100,-105],[100,105]].map(([dx,dy])=>({x:Math.max(78,Math.min(822,px+dx)),y:Math.max(90,Math.min(385,py+dy))}))
   const pos=candidates.find(c=>placed.every(v=>Math.abs(c.x-v.x)>=90||Math.abs(c.y-v.y)>=100))??candidates[0]
   placed.push(pos);return {...p,px,py,...pos}
 })
 return <div className="decision-scene">
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
  <div className="pitch-coordinates"><span>Same moment · {analysis.visiblePlayers} visible players</span><span>Body part: {snapshot.decision.bodyPartUsed?.replaceAll('_',' ')??'Unknown'}</span></div>
  <div className="decision-options" aria-label="Available decision options">{options.map(o=><button key={o.optionId} aria-pressed={optionId===o.optionId} className={optionId===o.optionId?'is-selected':''} onClick={()=>setOptionId(o.optionId)}><strong>{o.type==='pass'?`Pass to ${names.get(o.receiverId!)?.split(' ').at(-1)??o.receiverId}`:o.type==='dribble'?'Carry forward':o.type==='shot'?'Shoot':'Tackle'}</strong><small>{o.optionId===snapshot.decision.selectedOptionId?'Chosen action':'Alternative'} · {o.corridor}{analysis.verdict?.preferredOptionId===o.optionId?' · Stronger visible option':''}</small></button>)}</div>
  {analysis.verdict&&<div className="decision-verdict"><span className="eyebrow">PROVISIONAL DECISION ASSESSMENT</span><h3>{analysis.verdict.label}</h3><p>{analysis.verdict.explanation}</p><small>{analysis.verdict.matchSituation}</small></div>}
  <div className="decision-details"><strong>{option.corridor} · visible opponents only</strong>
   <p>{option.distanceMeters!==null&&`${option.type==='dribble'?'Distance to goal line':'Distance'}: ${option.distanceMeters} m. `}{option.corridorClearanceMeters!==null&&`Nearest defender to corridor: ${option.corridorClearanceMeters} m. `}{option.spaceAheadMeters!==null&&`Space ahead within 10 m: ${option.spaceAheadMeters} m. `}{option.goalOpeningDegrees!==null&&`Goal opening: ${option.goalOpeningDegrees}°. `}</p>
   <p>{option.progressionMeters!==null&&option.type!=='shot'&&`Forward progress: ${option.progressionMeters} m. `}{option.type==='pass'&&option.nearestOpponentMeters!==null&&`Receiver’s nearest visible opponent: ${option.nearestOpponentMeters} m. `}</p>
   <small>{option.reason}</small>
   <small>{analysis.coverage} Paths illustrate options, not recorded ball flight.</small>
  </div>
 </div>
}
