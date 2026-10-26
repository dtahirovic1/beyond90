export type Player = {
  id: string;
  teamId: string;
  name: string;
  position?: string;
  dominantFoot?: string | null;
  overallRating?: number | null;
  potential?: number | null;
  weakFoot?: number | null;
  age?: number | null;
  pace?: number | null;
  acceleration?: number | null;
  sprintSpeed?: number | null;
  strength?: number | null;
  stamina?: number | null;
  shooting?: number | null;
  finishing?: number | null;
  passing?: number | null;
  vision?: number | null;
  dribbling?: number | null;
  ballControl?: number | null;
  defending?: number | null;
  interceptions?: number | null;
  composure?: number | null;
  minutesPlayed?: number | null;
  goalsScored?: number | null;
  assists?: number | null;
  expectedGoals?: number | null;
  expectedAssists?: number | null;
}

export type RatingKey = 'pace' | 'acceleration' | 'sprintSpeed' | 'strength' | 'stamina' | 'shooting' | 'finishing' | 'passing' | 'vision' | 'dribbling' | 'ballControl' | 'defending' | 'interceptions' | 'composure'
type Attribute = { key: RatingKey; label: string }
export const technicalAttributes: Attribute[] = [
  { key: 'passing', label: 'Passing' }, { key: 'vision', label: 'Vision' },
  { key: 'shooting', label: 'Shooting' }, { key: 'finishing', label: 'Finishing' },
  { key: 'dribbling', label: 'Dribbling' }, { key: 'ballControl', label: 'Ball control' },
  { key: 'defending', label: 'Defending' }, { key: 'interceptions', label: 'Interceptions' },
  { key: 'composure', label: 'Composure' },
]
export const radarAttributes: Attribute[] = [
  { key: 'acceleration', label: 'Acceleration' }, { key: 'sprintSpeed', label: 'Sprint speed' },
  { key: 'strength', label: 'Strength' }, { key: 'stamina', label: 'Stamina' },
  { key: 'composure', label: 'Composure' }, { key: 'ballControl', label: 'Ball control' },
  { key: 'dribbling', label: 'Dribbling' }, { key: 'pace', label: 'Pace' },
]
export function ratingValue(value: number | null | undefined): number | null {
  return typeof value === 'number' && Number.isFinite(value) && value >= 0 && value <= 100 ? value : null
}
export function squadAverage(squad: Player[], teamId: string, key: RatingKey) {
  const values = squad.filter(p => p.teamId === teamId).map(p => ratingValue(p[key])).filter((v): v is number => v !== null)
  return { value: values.length ? Math.round(values.reduce((sum, v) => sum + v, 0) / values.length * 10) / 10 : null, count: values.length }
}
