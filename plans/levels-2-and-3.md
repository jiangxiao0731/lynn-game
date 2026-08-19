# Levels 2–3: Frozen Trench and Broken Current

## Outcome

Extend the existing portfolio demo into a continuous three-chapter game without generating new assets. Level 1 remains the organic nursery maze. Level 2 becomes an ice-locked rescue maze; Level 3 becomes a polluted power-network maze with a restorative ending. Each chapter must be playable, visually distinct but painterly, and transition to the next through the existing settlement panel.

The user's explicit request to complete Levels 2 and 3 and improve the portfolio demo supersedes the older `DESIGN_BRIEF.md` statement that the C# rebuild contains no Level 2. Legacy L2/L3 scenes are evidence and asset sources, not a requirement to reintroduce their pixel-tile/HUD presentation.

## Constraints

- Reuse only existing project and legacy-project images. Do not invoke asset generation.
- Keep the current painterly underwater storybook direction; do not import legacy pixel tiles.
- Preserve the large protagonist, professional HUD, character-bound dialogue bubbles, and readable NPC identity treatment.
- Keep protection, rescue, and purification as the verbs. Monsters are distressed ecosystem guardians rather than targets to kill.
- Preserve Level 1's current maze layout and behavior except for its next-level transition/save metadata.
- Godot 4 C# classes remain `partial`; scene scripts are attached through `.tscn` resources.

## Player-facing design

### Chapter 2 — 霜骨海沟

- Three connected maze regions: 冻潮入口 → 冰脊回廊 → 霜心育场.
- Meet 灯笼鱼·盏 beside a frozen current. Its speech bubble anchors beside the NPC.
- Activate three thaw anchors by approaching and pressing E. Each anchor visibly changes from opaque ice-blue to warm cyan and opens a shortcut gate.
- Collect eight ice fragments distributed along main and optional maze branches. Pickups grant Ice charges and switch the jellyfish to Ice form.
- Purify the 霜壳守望者 with Ice releases. Wrong elements remain possible but visibly inefficient.
- Restored state: frost veil recedes, current ribbons animate, trapped shoal silhouettes return, exit beacon brightens.
- Settlement transition unlocks Electric and enters Level 3.

### Chapter 3 — 断流灯塔

- Three connected maze regions: 沉船电网 → 废热管廊 → 断流灯塔.
- Meet 迷途鱼群 near the dead relay field.
- Reconnect three relays by approaching and pressing E. Each relay turns amber-white, draws a live cable to the next node, and dissolves a pollution gate.
- Collect eight electric sparks along the routes. Pickups grant Electric charges and switch the jellyfish to Electric form.
- Purify the 废热炉心 with Electric releases. Use the existing factory-monster painting for the guardian; use electric-jellyfish art as restored fauna/NPC visual support.
- Restored state: black pollution veil clears, the three cable lines pulse in sequence, sea lights return across the map, and the factory guardian becomes a quiet reef heater rather than disappearing violently.
- Finale: nearby characters speak beside themselves; the last shot shows the relit sea and a concise conservation message before returning to title.

## Technical design

### Shared chapter runtime

Add a reusable `ChapterLevelController` for Levels 2 and 3. A `ChapterId` export selects the authored titles, zones, required element, guardian art/name, NPC identity, objective strings, colors, spawn points, obstacle rectangles, anchors/relays, exit, next scene, and narrative timeline IDs. `ChapterRuntime` exposes only presentation context needed by the reusable HUD.

Reuse `Player`, `SkillSystem`, `HudController`, `DialogueRunner`, `SettlementPanel`, and the global event bus. Avoid duplicating the Level 1 coordinator.

### Generic objective and pickups

Keep the numeric 0–6 objective state stable for HUD/save compatibility (six active tasks plus Complete), but chapter stages mean:

1. meet guide;
2. restore first node;
3. restore remaining nodes;
4. collect elemental fragments;
5. purify guardian;
6. reach exit.

The shared chapter controller owns 12 authored, unique, non-respawning pickups and requires any 8. It only counts the configured element and switches the player into that form on pickup; Level 1 keeps its existing spawner.

### Guardian

Parameterize `BossController` with effective element, sprite path, display size, name, next unlocked element, and whether to emit a next-element unlock. Reuse the existing health/soft-attack rhythm. Slice the existing 3×2 monster sheets in memory with soft background keying so no new files are generated.

### HUD and dialogue

Add session-level chapter presentation data so the HUD asks the current objective using chapter-specific labels, metadata (`冰晶` / `电火花`), zone, and guardian name. `DialogueRunner` gains mappings for the two chapter guardians and reuses existing NPC anchors/portraits where possible.

### Save and transitions

Bump save version and add `current_level`. Old saves default to Level 1. Level 1 settlement loads `game_scene2.tscn`; Level 2 loads `game_scene3.tscn`; Level 3 returns to title after the finale. Persistence is chapter-boundary-only: doors, unique pickups, restoration nodes and guardian HP restart coherently at the chapter entrance.

## Files

- Modify: `BossController.cs`, `SkillSystem.cs`, `HudController.cs`, `GameStrings.cs`, `SaveManager.cs`, `SettlementPanel.cs`, `NarrativeData.cs`, `GameSceneController.cs`, `TitleController.cs`.
- Add: `scripts/ChapterRuntime.cs`, `scripts/ChapterLevelController.cs`.
- Add: `scenes/game_scene2.tscn`, `scenes/game_scene3.tscn`.
- Reuse/copy: legacy hand-painted ice/electric/factory/tar monster sheets into `assets/sprites/legacy/`.

## Implementation sequence

1. Add chapter/session data and generic objective display.
2. Upgrade save schema and settlement transition target.
3. Parameterize pickups, skills, and guardian without regressing Level 1.
4. Implement shared map construction, maze collisions, zone restoration, guide interactions, three chapter nodes, exit detection, narrative beats, persistence, and debug smoke hooks.
5. Create the two thin scene files with different `ChapterId` exports.
6. Wire Level 1 completion to Level 2.
7. Import assets, build C#, run headless validation, then run each level in a real window and inspect screenshots.

## Verification

- `dotnet build` passes with zero errors.
- `godot --headless --editor --path . --quit` imports all new assets/scenes without script/resource errors.
- `godot --headless --path . --scene res://scenes/game_scene2.tscn --quit-after 180 -- --skip-opening --smoke-complete` exercises L2 objectives and transition readiness.
- Equivalent L3 smoke run exercises relays, pickups, guardian, exit, and finale readiness.
- Real-window visual pass at 1920×1080 verifies: protagonist scale, NPC contrast/nameplate, speech-bubble anchoring, no HUD overlap, maze readability, guardian framing, and restored-state contrast.
- Regression run opens Level 1, confirms maze remains and settlement target points to Level 2.

## Adversarial review resolution

- Campaign route: new game → tutorial → L1; Continue routes by `current_level`; each settlement atomically saves the next chapter.
- Progression: Water → Ice → Electric → finale. The L1 maze is preserved; its former Electric unlock is intentionally corrected to Ice.
- Chapter controllers own L2/L3 finale and settlement timing; the HUD only auto-opens settlement for L1.
- Persistence is intentionally chapter-boundary-only; no partial door, node, pickup or guardian state is advertised as resumable.
- Dialogue reuses the existing stable anchors (`LanternNPC`, `ShoalNPC`, `BroodMother`) supported by the character-bound bubble runtime.
- Legacy ice/factory sheets are keyed in memory and tinted into the chapter palette; purified guardians stop attacking and shift to cyan.
- Smoke mode drives nodes → unique pickups → guardian → exit completion through gameplay methods, prints `CHAPTER_SMOKE_OK ... stage=Complete`, and skips save mutation.

## Failure handling and rollback

- Existing Level 1 scene/controller remain the fallback runtime; new chapters are isolated behind new scene paths.
- Legacy image imports are additive and can be removed without touching authored Level 1 art.
- Save loader accepts missing `current_level`/`collected_count`; invalid values clamp to Level 1 defaults.
- If runtime soft-keying fails on a source sheet, guardian falls back to the existing procedural blob instead of crashing.
