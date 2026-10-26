# Player attributes implementation plan

Implement the approved player-profile charts using Amir’s nullable attributes. Preserve Leeds and City navigation and merge both contributions.

1. Resolve merge conflicts, keeping the shared player model, Leeds values, complete validated snapshots and City flow.
2. Add a typed profile component with technical rating bars (0–100), an eight-axis attribute radar, a labelled tracked-squad average and a complete 25-field profile. Unknown or invalid values remain unavailable; season totals and weak foot retain their own units.
3. Add clearly marked synthetic City attribute fixtures for all three players, consistent across scenarios. Never infer distances or event grades from ratings.
4. Verify missing-value rendering, data binding, comparison calculations, route integration and backend fixture/API serialization with tests. Build and inspect desktop/mobile in browser.
5. Review the integrated diff, finish the merge commit and report verification and any limitations. Do not push without a user request.

## Verification record
- Integrated origin/main 07d8f07 with City work b8822a4; kept Amir’s complete opponent snapshots and corrected the synthetic tackle context to include a nearby opponent.
- Preserved supplied Leeds values and filled the fourth fixture’s missing profiles from the same player’s existing data. Added explicit synthetic City profiles, consistent across all four scenarios.
- Backend: 54 tests passed. Frontend: 6 tests passed; production build passed. Existing Date.now render lint warning remains outside this change.
- Browser: confirmed City and Leeds profiles with API-supplied attributes, desktop charts, mobile 390px chart and no horizontal overflow. Independent code review found no actionable profile defects.
