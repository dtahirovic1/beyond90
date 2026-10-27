# Decision evidence and execution profiles

**Goal:** Replace visible numerical decision grades with concrete spatial evidence and connect synthetic player attributes to option-specific execution context.

**Approved design:** The conversation approves qualitative risk, measured space/progression, bounded comparison of visible options, and separate synthetic execution profiles. Profiles inform explanations, never alter geometry or predict success. Unknown/aerial context must not appear safe. AI explains supplied evidence without numerical grades.

**Architecture:** Enrich calculated options with a shared backend execution profile. Serve the same data to the interface and AI. Keep existing comparison heuristics internal; remove legacy numerical grades from the calculator, public responses and AI input. Show selected-option evidence before generating AI, with an accessible toggle highlighting the measured corridor and nearest defender.

**Tech stack:** ASP.NET Core, React/TypeScript, SVG, xUnit, Vitest.

- [x] Add regression tests for relevant profile selection, invalid/missing values, foot context and unchanged geometry.
- [x] Implement shared option enrichment and grounded AI payload; remove numeric decision grades from public responses.
- [x] Render spatial evidence and execution profile, including unknown states and selectable pitch evidence.
- [x] Verify backend/frontend tests, production build and relevant UI behavior; update README.

Verification: 58 backend tests and 8 frontend tests passed; production build and lint passed. Browser checked option-specific attributes, qualitative evidence and pitch highlighting. AI request contract checked with a captured mock request; no new live-model quality evaluation claimed.
