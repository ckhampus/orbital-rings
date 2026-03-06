# Phase 19: Save/Load Integration - Context

**Gathered:** 2026-03-06
**Status:** Ready for planning

<domain>
## Phase Boundary

Housing assignments persist across save-quit-load cycles, old saves load cleanly, and stale assignments are corrected automatically. This is primarily a verification and gap-closure phase — the save/load plumbing was implemented incrementally across Phases 14, 15, and 17.

Requirements: INFR-04 (save/load housing assignments with backward compatibility), INFR-05 (already complete — save format v3 schema).

</domain>

<decisions>
## Implementation Decisions

### Phase character
- This is a **code audit + fix** phase, not a greenfield implementation phase
- SaveManager already saves HomeSegmentIndex (CollectGameState line 335) and restores via HousingManager.RestoreFromSave (ApplySceneState lines 462-469)
- Phase 19 traces all code paths, fixes any gaps found, and produces a commit even if no code changes are needed

### Verification approach
- Pure code audit — trace all 3 save/load paths explicitly:
  1. Normal save/load with housed citizens (home retention + home timer restart)
  2. v2 deserialization (int? defaults to null → citizens start unhoused → AssignAllUnhoused handles assignment)
  3. Stale reference detection (_housingRoomCapacities.ContainsKey check → skip + log → reassign)
- No runtime testing or Godot launch required — trust prior phase testing
- v2 backward compatibility verified by tracing the deserialization path, not by loading an actual v2 save file
- Success criteria from ROADMAP.md confirmed implicitly through the audit (no separate checklist)

### Mid-activity save behavior
- Citizens resume from walkway on reload — no mid-visit or mid-home-rest state is saved
- Radial snap (mid-drift citizen appears at walkway radius) is acceptable — player won't notice
- Wish badge visibility is correct by default: SpawnCitizenFromSave creates visible citizens, SetWishFromSave creates visible badges
- No new save fields needed (no isAtHome, no radialOffset, no homeRestProgress)

### Fix policy
- Minimal fixes for broken code + light cleanup if it improves clarity
- No architectural changes, no refactoring beyond what's needed
- REQUIREMENTS.md update (marking INFR-04 complete) deferred to milestone closure — not part of this phase's commit

### Commit policy
- Always produce a commit, even if audit finds zero code issues
- Commit documents the verification was done (matches prior phase pattern)

### Claude's Discretion
- Exact audit ordering (which path to trace first)
- Whether to add/improve GD.Print logging in the restore path
- Whether to add comments clarifying the save/load flow for future readers
- Light cleanup scope (better error messages, removing dead code paths)

</decisions>

<specifics>
## Specific Ideas

No specific requirements — this is a verification phase. The existing code from Phases 14 (schema), 15 (RestoreFromSave + stale ref handling), and 17 (EnsureHomeTimer on HomeSegmentIndex setter) should cover all three success criteria.

</specifics>

<code_context>
## Existing Code Insights

### Save Path (already implemented)
- SaveManager.CollectGameState (line 335): reads HomeSegmentIndex via HousingManager.GetHomeForCitizen()
- SavedCitizen.HomeSegmentIndex: int? property, serializes to null when unhoused
- SaveData.Version: already set to 3

### Load Path (already implemented)
- SaveManager.ApplyState: sets HousingManager.StateLoaded = true (prevents InitializeExistingRooms in _Ready)
- SaveManager.ApplySceneState: restores rooms first, then citizens, then calls HousingManager.RestoreFromSave
- HousingManager.RestoreFromSave: calls InitializeExistingRooms(assignCitizens: false), then rebuilds assignments from save data
- AssignCitizen → sets CitizenNode.HomeSegmentIndex → triggers EnsureHomeTimer (lazy timer creation)

### Stale Reference Path (already implemented)
- RestoreFromSave checks _housingRoomCapacities.ContainsKey(homeIndex.Value)
- Missing rooms: citizen skipped with GD.Print warning, remains unhoused
- AssignAllUnhoused runs after all saved assignments, catches any remaining unhoused citizens

### Autosave Event Wiring (already implemented)
- SaveManager subscribes to CitizenAssignedHome and CitizenUnhoused for debounced autosave
- HousingManager._isRestoring flag suppresses events during RestoreFromSave (prevents autosave loops)

### Integration Points
- SaveManager.cs — primary audit target (CollectGameState + ApplySceneState)
- HousingManager.cs — RestoreFromSave method + StateLoaded guard
- CitizenNode.cs — HomeSegmentIndex setter (EnsureHomeTimer) + SpawnCitizenFromSave path

</code_context>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 19-save-load-integration*
*Context gathered: 2026-03-06*
