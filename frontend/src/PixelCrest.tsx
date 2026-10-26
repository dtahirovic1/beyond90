/** Hand-drawn club emblems on an integer pixel grid, matching the shirt artwork. */
export function PixelCrest({ team }: { team: string }) {
  const city = team === 'city'
  return <svg className="pixel-crest" viewBox="0 0 64 72" role="img" aria-label={`${city ? 'Manchester City' : 'Leeds United'} pixel crest`} shapeRendering="crispEdges">
    {city ? <>
      <path d="M22 4h20v3h8v4h6v7h4v8h3v20h-3v8h-4v7h-6v4h-8v3H22v-3h-8v-4H8v-7H4v-8H1V26h3v-8h4v-7h6V7h8Z" fill="#83c9ea" />
      <path d="M23 9h18v3h9v6h5v9h3v18h-3v9h-5v6h-9v3H23v-3h-9v-6H9v-9H6V27h3v-9h5v-6h9Z" fill="#182f51" />
      <path d="M23 13h18v3h8v6h5v27h-5v7h-8v3H23v-3h-8v-7h-5V22h5v-6h8Z" fill="#f6f2dd" />
      <path d="M17 27h30v17h-3v6h-6v5H26v-5h-6v-6h-3Z" fill="#8ecdeb" stroke="#243e5a" strokeWidth="2" />
      <path d="M31 25h2v14h-2zM29 27v10H20v-2h3v-3h3v-5ZM35 27v10h9v-2h-3v-3h-3v-5ZM20 39h24v3h-4v3H24v-3h-4Z" fill="#d5a84e" />
      <path d="M30 46h4v3h3v4h-3v3h-4v-3h-3v-4h3Z" fill="#c14248" />
      <text x="32" y="23" textAnchor="middle" fill="#182f51" fontSize="5" fontWeight="900">MANCHESTER</text><text x="32" y="62" textAnchor="middle" fill="#182f51" fontSize="5" fontWeight="900">CITY</text>
    </> : <>
      <path d="M7 5h50v35h-3v7h-4v6h-5v5h-5v5h-5v4h-6v-4h-5v-5h-5v-5h-5v-6h-4v-7H7Z" fill="#e9bd38" />
      <path d="M11 9h42v30h-3v7h-4v6h-6v5h-5v5h-6v-5h-5v-5h-6v-6h-4v-7h-3Z" fill="#164a83" />
      <path d="M15 13h34v25h-3v7h-4v5h-6v5h-8v-5h-6v-5h-4v-7h-3Z" fill="#fff8e3" />
      <path d="M29 13h6v42h-6Z" fill="#e9bd38" />
      <text x="32" y="26" textAnchor="middle" fontSize="10" fontWeight="900" fill="#164a83">LUFC</text>
      <path d="M24 33h5v-3h6v3h5v5h-5v4h-6v-4h-5Z" fill="#fff" stroke="#164a83" strokeWidth="2" />
      <path d="M30 34h4v4h-4Z" fill="#e9bd38" />
    </>}
  </svg>
}
