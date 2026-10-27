import { PixelJersey } from './PixelJersey'
import { squadNumber } from './jerseyNumber'
import { useRef, useState } from 'react'
import { DecisionEvidence } from './DecisionEvidence'
export type DecisionAnalysis = {
 snapshot: { eventId: string; matchContext: {minute:number; score:Record<string,number>|null}; decision: { playerId: string; selectedOptionId: string; bodyPartUsed: string | null; options: {id: string; type: string; receiverId?: string}[] }; pitch: { lengthMeters:number; widthMeters?:number; attackDirection: Record<string,string> }; preDecision: {bodySetup?:string|null;ballHeight?:string|null;ball: {x:number;y:number};players:{id:string;teamId:string;x:number;y:number}[]} };
 options: {optionId:string;type:string;receiverId:string|null;distanceMeters:number|null;nearestOpponentMeters:number|null;corridorClearanceMeters:number|null;spaceAheadMeters:number|null;goalOpeningDegrees:number|null;corridor:string;reason:string;progressionMeters:number|null;riskLevel?:string;comparison?:string;techniqueNote?:string;executionProfile?:{attributes:{key:string;label:string;value:number|null;relevance:string}[];footContext:string;note:string}}[];
 verdict: {label:string;preferredOptionId:string|null;explanation:string;matchSituation:string;confidence?:string}|null;
 visiblePlayers:number;visibleOpponents:number;coverage:string;
}
export function DecisionPitch({analysis,players,optionId,onOptionChange}:{analysis:DecisionAnalysis;players:{id:string;name:string;teamId:string}[];optionId:string;onOptionChange:(id:string)=>void}) {
 const {snapshot,options}=analysis
 const [evidenceKey,setEvidenceKey]=useState<string|null>(null)
 const pitchRef=useRef<SVGSVGElement>(null)
 const highlighted=evidenceKey===`${snapshot.eventId}/${optionId}`
 const option=options.find(o=>o.optionId===optionId)!
 const actor=snapshot.preDecision.players.find(p=>p.id===snapshot.decision.playerId)!
 const receiver=snapshot.preDecision.players.find(p=>p.id===option.receiverId)
 const x=(v:number)=>28+v*8.44, y=(v:number)=>26+v*4.28
 const start=snapshot.preDecision.ball
 const direction=snapshot.pitch.attackDirection[actor.teamId]==='left_to_right'?1:-1
 const corridorTarget=receiver ?? (option.type==='shot'?{x:direction===1?100:0,y:50}:option.type==='dribble'?{x:Math.max(0,Math.min(100,actor.x+direction*10/snapshot.pitch.lengthMeters*100)),y:actor.y}:null)
 const width=snapshot.pitch.widthMeters??68
 const nearest= corridorTarget ? snapshot.preDecision.players.filter(p=>p.teamId!==actor.teamId).map(p=>{
   const dx=(corridorTarget.x-actor.x)*snapshot.pitch.lengthMeters/100,dy=(corridorTarget.y-actor.y)*width/100
   const px=(p.x-actor.x)*snapshot.pitch.lengthMeters/100,py=(p.y-actor.y)*width/100
   const t=dx*dx+dy*dy===0?0:(px*dx+py*dy)/(dx*dx+dy*dy)
   const bounded=Math.max(0,Math.min(1,t))
   return {p,t,distance:Math.hypot(px-bounded*dx,py-bounded*dy),point:{x:actor.x+bounded*(corridorTarget.x-actor.x),y:actor.y+bounded*(corridorTarget.y-actor.y)}}
 }).filter(item=>option.type!=='dribble'||item.t>=0&&item.t<=1).sort((a,b)=>a.distance-b.distance)[0]:undefined
 const canHighlight=Boolean(corridorTarget&&nearest&&option.riskLevel&&option.riskLevel!=='Unknown')
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
  <svg ref={pitchRef} className="pitch-svg" viewBox="0 0 900 480" role="img" aria-label="Player positions at the selected action. Shirt callouts connect to exact position dots.">
   <rect width="900" height="480" fill="#23312f"/><rect x="28" y="26" width="844" height="428" className="pitch-outline"/>
   <path d="M450 26v428M28 136h120v208H28M28 190h46v100H28M872 136H752v208h120M872 190h-46v100h46" className="pitch-line"/><circle cx="450" cy="240" r="72" className="pitch-line"/>
   {target&&<path d={`M${x(start.x)} ${y(start.y)}L${x(target.x)} ${y(target.y)}`} stroke="#d2ed83" strokeWidth="2" strokeDasharray="7 7" fill="none"/>}
   {highlighted&&canHighlight&&corridorTarget&&nearest&&<g aria-label="Measured ground corridor and nearest defender">
    <line x1={x(actor.x)} y1={y(actor.y)} x2={x(corridorTarget.x)} y2={y(corridorTarget.y)} stroke="#f2ba74" strokeWidth="12" opacity=".35"/>
    <line x1={x(nearest.p.x)} y1={y(nearest.p.y)} x2={x(nearest.point.x)} y2={y(nearest.point.y)} stroke="#f2ba74" strokeWidth="3"/>
    <circle cx={x(nearest.p.x)} cy={y(nearest.p.y)} r="12" fill="none" stroke="#f2ba74" strokeWidth="3"/>
   </g>}
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
  <DecisionEvidence option={option} playerName={names.get(actor.id)??'Player'} highlighted={highlighted} canHighlight={canHighlight} onHighlight={()=>{setEvidenceKey(highlighted?null:`${snapshot.eventId}/${optionId}`);if(!highlighted)pitchRef.current?.scrollIntoView({block:'center',behavior:'auto'})}}/>

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
