# Leeds workspace preview

Approved scope: Player selection -> aggregate player overview -> match -> event. Coach aggregate overview -> match. Remove Fan entry. Preserve pixel shirts and dark/cream/lime visual system. Local preview approved for publication.

Three deterministic synthetic event samples (84 events each), with three tracked Leeds players and three real opponent players per scenario. No fabricated full match results, minutes, xG or performance rating. All cards and reports derive from the same API events. Existing match-demo.json remains untouched for Amir and historical tests.

Checks: backend tests, TypeScript/build, fixture consistency and scope tests, browser desktop/mobile navigation, long shirt names, no Fan entry, scoped report totals, error/retry and Azure fallback labels. Live paid AI evaluation is outside this preview check; request serialization is checked locally.

2026/27 roster references checked 24 October 2026:
- https://www.leedsunited.com/en/news/2026-27-squad-numbers-announced
- https://www.premierleague.com/en/news/4725682
- https://www.chelseafc.com/en/news/article/chelsea-squad-numbers-confirmed-for-the-2026-27-season
Kit references (pixel interpretations, not official assets):
- https://shop.leedsunited.com/en/x-9595
- https://www.chelseafc.com/en/news/article/gallery-new-home-kit-honours-the-lion-reign-at-chelsea
- https://www.footballkitarchive.com/arsenal-fc-2026-27-home-kit-441205/

- https://www.liverpoolfc.com/news/lfc-and-adidas-unveil-2026-27-home-kit-inspired-iconic-title-winning-season

Local preview: frontend port 5175, API port 5090. Standard desktop startup remains dotnet run --project backend/Beyond90.Api and npm run dev in frontend. Restart the API after changing the fixture catalogue. Foundry remains configured via User Secrets; no keys added to source.
