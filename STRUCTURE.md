# 潮汐微光 / Shallow Sea Dream — Architecture (C#/.NET Godot 4.7 skeleton)

## Dimension: 2D (top-down)

## Toolchain
- Godot 4.7 (.NET / mono), `config_version=5`
- `Godot.NET.Sdk/4.7.0` (restored from Godot's local nupkgs via `nuget.config`)
- `TargetFramework`: `net10.0` (the only targeting pack installed on this machine)
- Assembly name: `ShallowSeaDream`

## Input Actions

| Action | Keys | Joypad |
|--------|------|--------|
| Move_Left | ← | axis 0 − |
| Move_Right | → | axis 0 + |
| Move_Up | ↑ | axis 1 − |
| Move_Down | ↓ | axis 1 + |
| skill_water | 1 | button 2 |
| skill_ice | 2 | button 3 |
| skill_electric | 3 | button 9 |
| store_element | R | button 10 |
| e | E | button 0 |
| restart_level | T | button 4 |
| pause_game | Esc | button 6 |
| advance_dialog | Space / Enter | button 0 |

## Autoloads (singletons)

| Name | Script | Role |
|------|--------|------|
| Events | res://scripts/Events.cs | Signal bus — decouples all subsystems |
| AudioManager | res://scripts/AudioManager.cs | Music/SFX/Dialogue buses |
| SaveManager | res://scripts/SaveManager.cs | JSON save at user://save.json |

## Scenes

### Title (main scene)
- **File:** res://scenes/title.tscn
- **Root type:** Control → TitleController
- **Children:** Background (ColorRect), Center/Menu with GameTitle, BeginButton, NewGameButton, ExitButton
- **Loads:** tutorial.tscn

### Tutorial
- **File:** res://scenes/tutorial.tscn
- **Root type:** Control → TutorialController
- **Children:** Background, JellyfishStage (Panel + ✿ glyph), Modal (PageTitle, PageBody, NextButton)
- **Loads:** game_scene1.tscn

### GameScene1 (Level 1)
- **File:** res://scenes/game_scene1.tscn
- **Root type:** Node2D → GameSceneController
- **Children:** BackgroundLayer, Map/Walls, Player (4 form AnimatedSprite2D + InteractArea + Camera2D), BroodMother, GasolineBarrel, GrannyLan, StarfishNPC, SeaweedNPC, SkillSystem, ObjectiveManager, ElementSpawner, HUD (instances SettlementPanel + FailurePanel). Fog-of-war (VisionMask/FogRect + vision_mask shader) was REMOVED — the level is always visible. New NPCs (HermitNPC/ShoalNPC/LanternNPC) and lore objects are added at runtime by GameSceneController.

### SettlementPanel (instanced into GameScene1 HUD)
- **File:** res://scenes/settlement_panel.tscn
- **Root type:** Control → SettlementPanel

### FailurePanel (instanced into GameScene1 HUD)
- **File:** res://scenes/failure_panel.tscn
- **Root type:** Control → FailurePanel

## Scripts

### Events (autoload)
- **File:** res://scripts/Events.cs · **Extends:** Node
- **Signals emitted:** PlayerHealthChanged, PlayerDamaged, PlayerDefeated, PlayerFormChanged, ElementChargesChanged, SkillCast, SkillFailed, ElementPickedUp, NextLevelElementUnlocked, ObjectiveAdvanced, ShardProgressChanged, ObjectiveCompleted, BossHealthChanged, BossAttacked, BossDefeated, DialogueStarted, DialogueLineStarted, DialogueFinished, LevelExitReached, GamePaused, StatusHint

### AudioManager (autoload)
- **File:** res://scripts/AudioManager.cs · **Extends:** Node
- PlayMusic / StopMusic / PlaySfx / PlayDialogue

### SaveManager (autoload)
- **File:** res://scripts/SaveManager.cs · **Extends:** Node
- HasSave / SaveState / LoadState / Reset · record `SaveState` (see DESIGN_BRIEF §10)

### Player
- **File:** res://scripts/Player.cs · **Extends:** CharacterBody2D · **Attaches to:** GameScene1:Player
- Movement, facing, form display, health. **Emits to bus:** PlayerFormChanged, PlayerDamaged, PlayerHealthChanged, PlayerDefeated

### SkillSystem
- **File:** res://scripts/SkillSystem.cs · **Extends:** Node · **Attaches to:** GameScene1:SkillSystem
- Charges, target/energy validation, damage. **Emits:** SkillCast, SkillFailed, ElementChargesChanged

### ObjectiveManager
- **File:** res://scripts/ObjectiveManager.cs · **Extends:** Node · **Attaches to:** GameScene1:ObjectiveManager
- **Receives from bus:** ElementPickedUp, BossDefeated, LevelExitReached → **Emits:** ObjectiveAdvanced, ShardProgressChanged, ObjectiveCompleted

### HudController
- **File:** res://scripts/HudController.cs · **Extends:** CanvasLayer · **Attaches to:** GameScene1:HUD
- **Receives from bus:** PlayerHealthChanged, ElementChargesChanged, ShardProgressChanged, ObjectiveAdvanced, BossHealthChanged, StatusHint, PlayerDefeated, ObjectiveCompleted, GamePaused

### ElementSpawner
- **File:** res://scripts/ElementSpawner.cs · **Extends:** Node2D · **Attaches to:** GameScene1:ElementSpawner
- Shard spawn grid + pickup. **Emits:** ElementPickedUp, StatusHint

### BossController
- **File:** res://scripts/BossController.cs · **Extends:** CharacterBody2D · **Attaches to:** GameScene1:BroodMother
- Auto-attack, water weakness, electric drop. **Emits:** BossHealthChanged, BossAttacked, BossDefeated, NextLevelElementUnlocked

### GameSceneController
- **File:** res://scripts/GameSceneController.cs · **Extends:** Node2D · **Attaches to:** GameScene1 (root)
- Pause, restart, exit-reach, save/restore. **Emits:** LevelExitReached, GamePaused

### TitleController / TutorialController / SettlementPanel / FailurePanel
- Control roots for their respective scenes; scene-flow + button handlers.

### Support (non-Node)
- **Palette.cs** — colour constants (WARNING_AMBER = player damage only)
- **ElementForm.cs** — `ElementForm`, `ObjectiveStage` enums + `GameConstants`

## Signal Map (via Events bus)

- ElementSpawner → Events.ElementPickedUp → ObjectiveManager.OnElementPickedUp, HUD
- ObjectiveManager → Events.ShardProgressChanged / ObjectiveAdvanced / ObjectiveCompleted → HUD
- SkillSystem → BossController.ApplyDamage → Events.BossDefeated → ObjectiveManager.OnBossDefeated
- BossController → Events.BossAttacked + Player.TakeDamage → Events.PlayerDefeated → HUD (FailurePanel)
- GameSceneController → Events.LevelExitReached → ObjectiveManager.OnLevelExitReached → Events.ObjectiveCompleted → HUD (SettlementPanel)

## Build Order
1. dotnet build
2. scenes/BuildSettlementPanel.cs → scenes/settlement_panel.tscn
3. scenes/BuildFailurePanel.cs → scenes/failure_panel.tscn
4. scenes/BuildTitle.cs → scenes/title.tscn
5. scenes/BuildTutorial.cs → scenes/tutorial.tscn
6. scenes/BuildGameScene1.cs → scenes/game_scene1.tscn (depends: settlement_panel.tscn, failure_panel.tscn)

## Asset Hints (for the later asset+gameplay pass)

- Jellyfish form sprite-sheets (base/water/ice/electric), 2×3 grid of 512×512 cells
- Element-attack sprite-sheet (water projectile), 256×256 frames
- Boss 潮涡巢母 sprite + multi-ring arena art
- Maze map background (1920×1080+), polluted-water-channel theme
- UI 9-slice panels (cream modal, gold buttons), rounded Chinese display font
- vision_mask fog-of-war canvas_item shader
- Element orb pickup icons (water/ice/electric)
- Audio: title_theme.ogg, level1_underwater_ambient.ogg, 16 SFX (see DESIGN_BRIEF §9)

## Story-first overhaul (narrative systems)

The game was converted from a combat-led skeleton into a story-first eco-fable. Key systems:

### Dialogue system (item 2)
- **`scripts/DialogueRunner.cs`** — narrative box with a speaker name, a character
  **portrait** (`AssetLoader.NpcPortrait(id)` → `npc_{id}.png`, procedural blob fallback),
  a **typewriter** reveal (advance key skips-to-full first, then advances), and
  **branching choices** (buttons that set a story flag and/or jump to a labelled line).
  `Play(timelineId, onComplete)` runs registered timelines; `ShowVignette(title, lines,
  speaker, onComplete)` shows ad-hoc memory/lore text. Records seen conversations + chosen
  flags into MemoryLog.
- **`scripts/DialogueData.cs`** — line model (`DialogueLine` with `PortraitId`, `Choices`,
  `Label`; `DialogueChoice` with `SetFlag`/`GotoLabel`) + the core canon timelines
  (verbatim anchor lore). `Get(id)` falls through to NarrativeData.

### Narrative data (items 3, 5, 6) — where the story lives
- **`scripts/NarrativeData.cs`** — all expanded story content as clean C# data: extra
  timelines (opening, 岚婆婆 mid/after, 海星/海草 deep, NEW NPCs 寄居蟹老郑 / 灯笼鱼·盏 /
  迷途鱼群, boss pre/mid, ending+潮汐钥匙), the **10 微光潮汐记忆 vignettes** (`Memories`),
  the **8 残片笔记 lore notes** (`LoreNotes`), and **monster codex entries**
  (`CodexEntries`). Zone ids 浅滩/沉积带/巢母深处.

### Memory log / 图鉴 (item 5)
- **`scripts/MemoryLog.cs`** — autoload journal persisted to `user://log.json` (parallel
  to save.json). Tracks unlocked memories, conversations seen, codex entries, lore notes
  found, and story flags. Registered in `project.godot` `[autoload]`.
- **`scripts/LogPanel.cs`** — full-screen overlay toggled by the `toggle_log` action
  (J / Tab), instanced by HudController. Lists the four sections from MemoryLog; pauses
  the tree while open.

### Shards = memories (item 3)
- `ElementSpawner.CollectShard` unlocks the next vignette and emits
  `Events.MemoryUnlocked`; `GameSceneController.OnMemoryUnlocked` plays it via the
  dialogue box. Boss gate still uses the 5-shard threshold.

### Soft combat & death (item 4)
- `Player.SoftRespawn` (HP 0) fades out, restores full health, and returns to the last
  safe point (set on zone entry / level start) — no game-over. `HudController` no longer
  auto-shows FailurePanel; it remains only as an optional gentle surface. Boss values in
  `GameConstants` lowered for a gentle, narrative purification (pre/mid/post beats wired
  via `Events.BossHealthChanged` thresholds in GameSceneController).

### Zones & exploration (item 7)
- One widened `game_scene1` (3600×1080) split into three X-band sub-areas
  (浅滩 / 沉积带 / 巢母深处) with passage gaps, per-zone tints + optional backgrounds
  (`AssetLoader.ZoneBackground`), zone-entry beats (`Events.ZoneEntered`), NPCs + lore +
  memories spread across all three, and the exit moved deep into 巢母深处. Optional
  content: repeat-talk follow-ups for 海星/海草, optional NPCs (迷途鱼群), and lore objects.

### New Events signals
`MemoryUnlocked(int)`, `LoreNoteFound(string)`, `ZoneEntered(string)`.

## Scene generation note (IMPORTANT)
Godot C# `--script` SceneTree builders do not execute headless on this setup (hang, `_Initialize` never called); scenes are authored as `.tscn` text directly. The same hang affects `godot --headless --import` and `--editor --quit` (banner only, then timeout). The 5 scenes (title, tutorial, game_scene1, settlement_panel, failure_panel) were translated verbatim from each `scenes/Build*.cs` node tree into `format=3` scene text. The later /godogen gameplay pass must do the same (author `.tscn` text directly) or run the builders from inside the Godot editor.
