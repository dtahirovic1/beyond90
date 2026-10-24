const digits: Record<string, string[]> = {
  '0': ['01110','11011','11011','11011','11011','11011','01110'],
  '1': ['00110','01110','00110','00110','00110','00110','11111'],
  '2': ['11110','00011','00011','01110','11000','11000','11111'],
  '3': ['11110','00011','00011','01110','00011','00011','11110'],
  '4': ['11011','11011','11011','11111','00011','00011','00011'],
  '5': ['11111','11000','11000','11110','00011','00011','11110'],
  '6': ['01111','11000','11000','11110','11011','11011','01110'],
  '7': ['11111','00011','00011','00110','01100','01100','01100'],
  '8': ['01110','11011','11011','01110','11011','11011','01110'],
  '9': ['01110','11011','11011','01111','00011','00011','11110'],
}

export function PixelJersey({ team, name, number, className = '' }: { team: string; name: string; number: string; className?: string }) {
  const leeds = team === 'leeds'
  const kits: Record<string, { base: string; light: string; dark: string; edge: string; sleeve: string; cuff: string }> = {
    leeds: { base:'#f4f3e9',light:'#ffffff',dark:'#c9d1d3',edge:'#697c91',sleeve:'#f4f3e9',cuff:'#d9b745' },
    arsenal: { base:'#cf2c39',light:'#ed4953',dark:'#a82032',edge:'#631d2b',sleeve:'#eee9df',cuff:'#17243c' },
    chelsea: { base:'#205ac6',light:'#3479e6',dark:'#17469d',edge:'#102a61',sleeve:'#205ac6',cuff:'#d7b555' },
    liverpool: { base:'#b92031',light:'#d93b49',dark:'#8e1829',edge:'#561623',sleeve:'#b92031',cuff:'#f5f0df' },
  }
  const kit = kits[team] ?? { base:'#676f79',light:'#818c99',dark:'#454e59',edge:'#222a35',sleeve:'#676f79',cuff:'#f5f0df' }
  const silhouette = 'M28 5H34V8H46V5H52V8H60V11H66V14H72V20H76V35H62V30H60V77H20V30H18V35H4V20H8V14H14V11H20V8H28Z'
  const glyphs = number.slice(0, 2).split('').flatMap((digit, index) =>
    (digits[digit] ?? []).flatMap((row, y) => row.split('').flatMap((cell, x) => cell === '1' ? [{ x: index * 6 + x, y }] : [])),
  )
  const unit = 2.6
  const numberX = (80 - (number.length > 1 ? 11 : 5) * unit) / 2
  const surname = name.endsWith('van Dijk') ? 'VAN DIJK' : name.split(' ').at(-1)?.toUpperCase() ?? ''
  return <svg className={`pixel-jersey ${className}`} viewBox="0 0 80 88" role="img" aria-label={`${name}, ${team === 'leeds' ? 'Leeds United' : team}${number ? `, number ${number}` : ''}`}>
    <g shapeRendering="crispEdges">
      <path d={silhouette} fill="#000" opacity=".35" transform="translate(0 4)" />
      <path d={silhouette} fill={kit.edge} stroke={kit.edge} strokeWidth="2" />
      <path d="M28 8H33V11H47V8H52V11H60V14H66V17H71V21H73V32H62V26H57V74H23V26H18V32H7V21H11V17H17V14H23V11H28Z" fill={kit.base} />
      <path d="M23 13H29V24H23V30H18V32H7V21H11V17H17V14H23ZM51 13H57V14H63V17H69V21H73V32H62V30H57V24H51Z" fill={kit.sleeve} />
      <path d="M7 30H18V33H7ZM62 30H73V33H62Z" fill={kit.cuff} />
      <path d="M25 28H29V70H25ZM29 67H54V71H29Z" fill={kit.light} opacity=".75" />
      <path d="M54 27H58V74H23V71H54ZM20 16H23V29H20ZM57 16H60V29H57Z" fill={kit.dark} />
      <path d="M30 7H34V10H46V7H50V10H47V14H33V10H30Z" fill={leeds ? '#1d457a' : kit.cuff} />
      <path d="M34 7H46V10H34Z" fill="#0f141c" />
      {team === 'liverpool' && <g opacity=".22" fill="#f5f0df"><path d="M25 28h12l-8 12h8l-8 12h8L25 66ZM43 28h12l-8 12h8l-8 12h8L43 66Z" /></g>}
      {leeds && <>
        {[21,30,39,48,57,66].map(y => <g key={y}><path d={`M24 ${y}H56`} stroke="#678ab4" strokeWidth=".5" /><path d={`M24 ${y+2}H56`} stroke="#d9bc55" strokeWidth=".5" /></g>)}
        {[0,4,8].map(x => <path key={x} d={`M${19+x} 12v7M${53+x} 12v7`} stroke="#214d85" strokeWidth="2" />)}
        <path d="M7 28h11v2H7zm55 0h11v2H62z" fill="#214d85" />
      </>}
      <g transform={`translate(${numberX} 33)`}>
        {glyphs.map((p, i) => <rect key={`outline-${i}`} x={p.x * unit - .9} y={p.y * unit - .9} width={unit + 1.8} height={unit + 1.8} fill={leeds ? "#fffdf5" : "#10141c"} />)}
        {glyphs.map((p, i) => <rect key={`shadow-${i}`} x={p.x * unit + 1.3} y={p.y * unit + 1.3} width={unit} height={unit} fill="#9a9f9e" />)}
        {glyphs.map((p, i) => <rect key={i} x={p.x * unit} y={p.y * unit} width={unit} height={unit} fill={leeds ? "#163d70" : "#fff8e8"} />)}
      </g>
    </g>
    <text x="40" y="27" textAnchor="middle" fill={leeds ? "#163d70" : "#fffdf5"} stroke={leeds ? "#fffdf5" : "#10141c"} strokeWidth=".85" strokeLinejoin="round" paintOrder="stroke fill" fontFamily="Arial, sans-serif" fontWeight="900" fontSize="5.6" letterSpacing=".08" textLength={surname.length >= 8 ? 35 : undefined} lengthAdjust="spacingAndGlyphs">{surname}</text>
  </svg>
}
