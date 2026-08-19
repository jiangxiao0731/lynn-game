# Shallow Sea Dream — Design Brief (for C#/.NET Godot 4 re-creation)

> This brief is reverse-engineered from the original Godot 4.6 GDScript project at
> `/Users/shawj/Desktop/lynn/shallow-sea-dream`. The goal is an **equivalent** game
> rebuilt from scratch in **C# / .NET for Godot 4**. All values, strings, and identifiers
> below are quoted from the actual source — do not invent additional content.
>
> Original project's `config/name` is `"test"`; the working title is **潮汐微光 / Shallow Sea Dream**
> (`ui/shallow_sea_dream_ui_1920x1280` and audio filenames use the slug).

---

## 1. Premise & Story

**Setting.** A polluted coastal seabed ("潮湾苗圃 / Tide-Bay Nursery"). Long ago the ocean turned all
life's emotions into "微光潮汐 / shimmer-tide" to keep the ecosystem in balance. Now plastic, oil
slicks, chemical waste and noise pollution have sunk into the deep and spawned "污染侵入体 / pollution
invaders." The player is **微光 / "Shimmer,"** a juvenile jellyfish awakened by an ancient current —
the last uncorrupted light in this sea.

**Theme & tone.** Environmental / eco-fable; gentle, melancholic, hopeful. The narrative reframes
"enemies" as victims: *"所谓「敌人」只是被污染扭曲的生态产物，净化后会回归自然"* ("the so-called
enemies are merely pollution-warped products of the ecosystem; once purified they return to nature").
The verb is **purify (净化)**, never "kill."

**Language(s).** Primary in-game language is **Simplified Chinese** (all dialogue, tutorial, and most
runtime strings are hardcoded zh). A bilingual **EN/ZH** translation table exists at
`locale/translations.csv` (compiled to `translations.zh.translation` / `translations.en.translation`)
but only ~19 keys are migrated; most player-facing text in `character_body_2d.gd` is still inline zh.
The C# rebuild should route all strings through the translation table (see §12).

**Story beats (from `场景/tutorial.gd` PAGES and Dialogic `.dtl` timelines):**

- **Tutorial / Ch.1 intro (3 pages):**
  - P1 "第一章·潮湾苗圃": lore + the "enemies are victims" framing. Caption "原始水母 · 微光".
  - P2 "操作指引": the control list (see §5).
  - P3 "潮汐赋予水元素": the tide grants water power; button "化为水元素" triggers a base→water
    transform animation, then loads the game. Lists Level 1 tasks: 1) find 岚婆婆 (Granny Lan),
    2) collect shards, 3) defeat the brood mother.
- **NPC dialogue (Dialogic timelines in `addons/dialogic/timelines/`):**
  - `npc1giving.dtl` — Granny Lan NPC, branching choice ("愿意 / 我再想想"): explains different
    pollution needs different purification; Ch.1 teaches the **water** element; tells you to absorb
    shards and press **1** near the brood mother. Foreshadows ice & electric in deeper waters.
  - `starfish.dtl` — Starfish flavor NPC: lonely survivor; plastic dimmed its kin; "深处有更暗的
    东西在脉动."
  - `seaweed.dtl` — Seaweed flavor NPC: warns of the brood mother's low-frequency "breathing";
    "它原本只是一群小水母，被废液浸泡得彼此粘连"; advises gathering shards then striking its thinnest
    membrane with water.
  - `jellyfish1Giving.dtl` — two-character scene (CharacterBoay2D ↔ box): the jellyfish confronts an
    abandoned gasoline barrel ("我原本是人类工厂里装汽油的容器… 他们说，把我直接扔进海里最省事").
  - `boss_defeated.dtl` — post-victory: the brood mother's shell cracks, trapped shimmer returns to
    the water, a new element orb floats up — *"通往下一片海域的潮汐钥匙"* (tidal key to the next sea).

**Key named entity:** the boss **潮涡巢母 / "Tide-Vortex Brood Mother"** (`MONSTER_BOSS_NAME`).

---

## 2. Art & Visual Style

- **Resolution / window** (`project.godot [display]`): viewport **1920×1080**, `mode=2`
  (maximized), stretch `mode="viewport"`, aspect `expand`. Renderer: D3D12 on Windows.
  `default_texture_filter=0` (**nearest** — crisp, no smoothing).
- **Mixed art direction (3 layers, deliberately unified by `scripts/palette.gd`):**
  1. **Hand-drawn raster sprites** (`资源/*.PNG`, `.png`) — jellyfish forms, elements, boss,
     barrel, coins, maps. Soft illustrative "storybook" look. These are sprite-sheet atlases
     (see §4 / §11).
  2. **Procedural geometry** (drawn in code) — minimap markers, HP ring, skill burst rings,
     boss arena pulses.
  3. **Stock UI panels** (`资源/ui_panels/`, `ui/.../assets/base/`) — ocean-themed 9-slice
     frames, bars, buttons.
- **Palette (single source of truth — `Palette` class, port verbatim):**
  - `DEEP_SEA` `Color(0.04,0.12,0.18)` — backgrounds/fog.
  - `SURFACE_LIGHT` `Color(0.56,0.82,0.96)` — borders/rim light.
  - `COASTAL_CYAN` `Color(0.52,0.94,1.0)` — purification/healing/friendly.
  - `POLLUTED_TEAL` `Color(0.42,0.86,0.78)`, `POLLUTED_TEAL_BRIGHT` `Color(0.56,0.92,0.72)` —
    enemy/pollution attacks (kept on the blue-green axis, never yellow).
  - `WARNING_AMBER` `Color(0.98,0.74,0.32)` — **player-damage only**, the one warm accent.
  - Element accents: `ELEMENT_WATER` `Color(0.30,0.70,1.0)`, `ELEMENT_ICE` `Color(0.70,0.92,1.0)`,
    `ELEMENT_ELECTRIC` `Color(0.90,1.0,0.30)`, plus lighter `*_IMPACT` variants.
- **Theme:** `title.theme.tres` (uid `c8vu0bdsnbftc`) — Button theme using `资源/IMG_2030.PNG` as a
  9-slice StyleBoxTexture atlas (normal/hover/pressed regions), cyan font `Color(0.43,0.84,1)`,
  font size 40, custom font `FontFile` (TTF `资源/Aa水玉圆体.ttf`, a rounded Chinese display face;
  also referenced via `gui/theme/custom_font`).
- **Shader:** `资源/vision_mask.gdshader` (canvas_item) — fog-of-war darkness. Uniforms:
  `light_center`, `secondary_light_center`, `viewport_size`, `radius=170`, `secondary_radius=190`,
  `softness=95`, `darkness=0.92`, `secondary_enabled`. Renders black with alpha that smoothsteps
  from the lit radius outward; supports a second light (e.g. objective marker) via `min()` of both.
- **In-scene StyleBoxFlat conventions** (tutorial/title): cream modal `Color(0.98,0.945,0.835)`,
  ink border `Color(0.165,0.29,0.37)`, gold button `Color(0.98,0.78,0.32)`, 18–24 px corner radii,
  soft drop shadows. Rounded, friendly, high-contrast against deep-sea bg.

---

## 3. Game Modes & Core Loop

- **Genre:** single-player 2D **top-down exploration / light action-adventure** with a fog-of-war
  maze, an NPC-driven narrative, resource collection, and a single boss fight. Story-driven, not
  score-driven.
- **Modes:** one continuous campaign. Flow = **Title → Tutorial → Level 1 (game_scene1)**. No menus
  beyond title; a `settings.gd` controller exists but its scene was never built (see §8/§12).
- **Core loop (Level 1):** explore the dark maze under a vision mask → talk to NPCs (objective
  advances) → collect water shards → reach **5 shards** → fight the brood mother with water skill →
  purify it → pick up the dropped next-level element → reach the exit → settlement screen.
- **Win condition:** complete the objective chain through `OBJECTIVE_COMPLETE` (boss purified + exit
  reached) → SettlementPanel. **Lose condition:** player HP reaches 0 → FailurePanel (restart or
  quit to title).

---

## 4. Mechanics (from `scripts/character_body_2d.gd` + monster/util scripts)

**Player controller** is a `CharacterBody2D` (in group `"player"`); the same ~3000-line script also
acts as the level/HUD controller. The C# rebuild should split this god-class (see §12).

### Movement
- `const SPEED := 300.0`. 8-directional via `Move_Left/Move_Right/Move_Up/Move_Down` (arrow
  keys + joypad). Uses `move_and_slide()`. Horizontal facing flips the sprite. Walk vs Idle
  animation chosen by velocity.

### Forms / elements (transformation)
- Four forms: `FORM_BASE="base"`, `FORM_WATER="water"`, `FORM_ICE="ice"`, `FORM_ELECTRIC="electric"`.
- Labels: 基础水母 / 水元素水母 / 冰元素水母 / 电元素水母 (`FORM_LABELS`).
- Per-form animation sheets (`FORM_SHEET_PATHS`): water=`可爱的水母动画帧.png`,
  ice=`冰雪章鱼精动画帧.png`, electric=`电气水母游动动画.png`. Each sheet is a **2×3 grid of
  512×512 cells = 6 frames**, sliced into "Idle" (speed 6) and "Walk" (speed 9) animations
  (`PlayerUtils.create_form_frames`).
- The player carries separate AnimatedSprite2D children per element (AnimatedSprite2D / WaterSprite2D
  / IceSprite2D / ElectricSprite2D); active form is shown, others hidden, with per-form modulate
  matching the palette element colors.

### Skills (elemental purification)
- Actions `skill_water` (key **1**), `skill_ice` (key **2**), `skill_electric` (key **3**)
  (`SKILL_ACTION_TO_FORM`; fallback keys KEY_1/KEY_2/KEY_3 + keypad in `SKILL_FALLBACK_KEYS`).
- **Charges (energy) per element:** `element_charges` dict `{water, ice, electric}`, gained by
  picking up shards (`ELEMENT_CHARGES_PER_PICKUP := 1`). Casting consumes one charge of that form;
  zero charges → "no shards" feedback (`STATUS_NEED_ENERGY`, plays `no_shards.ogg`).
- **Targeting:** must be near a pollution body whose profile has been viewed before casting
  (`STATUS_NEED_TARGET`); target search radius `SKILL_TARGET_SEARCH_RADIUS := 420.0`.
- **Damage / effectiveness** (`PlayerUtils.get_skill_damage`): if the element matches the monster's
  weakness, `DAMAGE_WATER_EFFECTIVE := 52`; otherwise `DAMAGE_INEFFECTIVE := 18`. (Only water is
  mechanically effective in Level 1; ice/electric are unlock-only.) Feedback strings
  `SKILL_RELEASED_TEMPLATE` / `SKILL_INEFFECTIVE_TEMPLATE` ("{form} released…" / "limited effect").
- Water attack visuals: `WATER_ATTACK_SHEET_PATH = 元素攻击精灵图.png`, projectile travels to target
  over `WATER_ATTACK_PROJECTILE_TIME := 0.24`, reach radius `WATER_ATTACK_TARGET_RADIUS := 650.0`,
  frame size 256×256.
- **Store/recall form (`store_element`, key R):** retracts the current form back to base
  (plays `store_form.ogg`).

### Shards / elements pickup
- Shards spawn at `WATER_ELEMENT_SPAWN_POINTS` (a full-map grid) in group `"element"`; respawn
  interval `0.1`. Pickup distance `ELEMENT_PICKUP_DISTANCE := 100.0`; spawn clearance/jitter
  constants keep them off walls. Element form is inferred from node name
  (`PlayerUtils.get_element_form`: contains "冰"→ice, "电"→electric, else water).
- Picking up the **first** water element sets `_has_collected_first_water_element` and shows
  `STATUS_FIRST_WATER` (one-time tutorial line). Pickup shows a toast (`pickup_toast.gd`).

### Health
- `max_health := 120`, `current_health := 120`. HP shown three ways: `HealthBar` ProgressBar,
  `HealthLabel` ("生命 %d / %d"), and the circular **HpRing** (`hp_ring.gd`, teal fill arc, sand bg,
  starts at 12 o'clock clockwise, `max_value` synced to max_health). Damage from boss attacks
  reduces HP and uses `WARNING_AMBER` flash + camera shake; `DAMAGE_RECEIVED_TEMPLATE`
  ("Jellyfish took {amount} pollution damage"). HP 0 → defeat / FailurePanel (`player_defeat.ogg`).

### Boss combat (`scripts/pollution_monster.gd`, base `pollution_monster_base.gd`)
- Boss node `潮涡巢母` (group `"monster"`), `monster_name="潮涡巢母"`,
  `effective_elements=["water"]`, `attack_damage=18`, `max_health` 100 default / set to **156** by
  the controller. `is_skill_effective(element)` = element in `effective_elements`.
- **Autonomous attack:** when player within `BOSS_AUTO_ATTACK_RANGE := 360.0`, the boss attacks every
  `BOSS_AUTO_ATTACK_INTERVAL := 2.4`s; `perform_attack()` returns damage applied to the player via
  `_take_damage`. Attack effect is a sprite sheet (2×3 of 512×512, "attack" anim, speed 9, non-loop).
- Boss has a multi-ring arena presentation (outer/mid/core radii) with a pulsing tween; collision
  radius 58. Boss HP bar `BOSS_HEALTH_BAR_SIZE 176×16`. On defeat: `boss_defeated.dtl` plays,
  `_first_boss_defeated=true`, drops the **electric** next-level element
  (`_spawn_next_level_element_drop(FORM_ELECTRIC, …)`).
- **Shard gate to boss:** `SHARD_THRESHOLD_FOR_BOSS := 5` water shards required before the boss
  objective is satisfiable (HUD shows "x/5").

### Other interactables
- **Oil/gasoline barrel** `汽油桶` (group `"box"`, `汽油桶.gd` is empty stub) — interact target for
  the jellyfish↔box dialogue; barrel hint shown after first boss defeat.
- **NPCs** in groups `"npc"` (Granny Lan main NPC) and `"flavor_npc"` (StarfishNPC, SeaweedNPC —
  procedural Panel+Label glyph nodes, not sprites).

---

## 5. Controls / Input (from `project.godot [input]`)

| Action | Keys | Joypad |
|---|---|---|
| `Move_Left` | ← (4194319) | axis 0 − |
| `Move_Right` | → (4194321) | axis 0 + |
| `Move_Up` | ↑ (4194320) | axis 1 − |
| `Move_Down` | ↓ (4194322) | axis 1 + |
| `skill_water` | 1 | button 2 |
| `skill_ice` | 2 | button 3 |
| `skill_electric` | 3 | button 9 |
| `store_element` (retract form) | R | button 10 |
| `e` (interact) | E | button 0 |
| `restart_level` | T | button 4 |
| `pause_game` | Esc | button 6 |
| `advance_dialog` | Space / Enter | button 0 |
| `dialogic_default_action` | Enter / Space / X / left-click | — |

Tutorial in-game control sheet (verbatim): `←↑→↓ 移动 / 1·2·3 释放 水·冰·电 / E 互动 / R 收回当前形态
/ T 重新开始当前关卡 / Esc 暂停·继续`. `pause_game` toggles a paused state + PauseFeedbackPanel
("游戏已暂停") and plays `pause_toggle.ogg`.

---

## 6. Levels / Scenes (`场景/`)

- **`title.tscn`** — **main scene** (`run/main_scene="res://场景/title.tscn"`). Full-screen
  background `资源/IMG_2030.PNG`, "Begin" button (text "潜入海洋", relabeled 继续游戏·开始游戏 by
  `title.gd`), "Exit" button ("返回岸上"). Fade in/out, save-aware label, optional NewGameButton,
  plays `title_theme.ogg`. → loads `tutorial.tscn`.
- **`tutorial.tscn`** — 3-page lore/controls modal over a deep-sea background, with a procedural
  jellyfish "stage" (Panel circle + "✿" glyph) that tweens base→water on the final page, then
  loads `game_scene1.tscn`. Script `tutorial.gd`.
- **`game_scene1.tscn`** — the only gameplay level (~5500-line scene). Contains: `VisionMask`
  CanvasLayer (FogRect + shader), large `HUD` CanvasLayer (see §8), a `地图` (map) Sprite2D with a
  `StaticBody2D` wall layout of **~48 CollisionPolygon2D walls** + 4 boundary edges (the maze),
  `CharacterBody2D` player (with 4 form sprites, SkillEffect, Area2D, Camera2D, BubbleMarker),
  `水元素` water-element pickup, `潮涡巢母` boss, `汽油桶` barrel, `NPC` (Granny Lan), `StarfishNPC`,
  `SeaweedNPC`. Map art: `场景/ChatGPT Image 2026年3月25日 00_06_29.png` and `污染的水道迷宫.png`.
- **Objective chain (constants):** `OBJECTIVE_FIND_NPC=0` → `OBJECTIVE_TALK_STARFISH=1` →
  `OBJECTIVE_TALK_SEAWEED=2` → `OBJECTIVE_COLLECT_SHARDS=3` → `OBJECTIVE_DEFEAT_BOSS=4` →
  `OBJECTIVE_EXIT_LEVEL=5` → `OBJECTIVE_COMPLETE=6`. Exit reached within `EXIT_REACH_DISTANCE := 78.0`
  of `EXIT_POINT = (900,286)`; plays `exit_reached.ogg` → Settlement. Objective advances play
  `objective_advance.ogg`.
- Note: there is no level 2 scene; ice/electric and "next sea" are foreshadowed and unlocked-for-save
  only.

---

## 7. Characters (`人物/` + scene/Dialogic)

- **Player — 微光 / Shimmer**, a juvenile jellyfish; 4 elemental forms (§4). Base portrait asset
  `人物/IMG_2028 3_副本.png`; form sheets in `资源/`.
- **Granny Lan (岚婆婆) — main NPC** (`npc` group, `npc.gd` empty stub): quest-giver via
  `npc1giving.dtl`, branching dialogue, teaches water, points to boss. Minimap yellow.
- **Starfish NPC** (`flavor_npc`): procedural glyph node; lore via `starfish.dtl`.
- **Seaweed NPC** (`flavor_npc`): procedural glyph node; warns about boss via `seaweed.dtl`.
- **Gasoline barrel (汽油桶)** (`box` group): pollution object, `jellyfish1Giving.dtl` confrontation.
- **Brood Mother (潮涡巢母)** — boss; behaviors in §4 (autonomous ranged pollution attacks, water
  weakness, multi-ring arena, purification on defeat).
- Dialogic characters: `CharacterBoay2D.dch`, `box.dch`, `npc.dch`.

---

## 8. UI / HUD / Menus

Title & tutorial UI: see §6. **In-game HUD** (`game_scene1.tscn` HUD CanvasLayer, driven by the
player script + `hud_theme.gd`/`hud_utils.gd`):
- **TaskPanel** — objective checklist with 6 stage rows (check glyph + text per stage,
  `OBJECTIVE_CHECKLIST_NAMES`), TaskTitle/TaskLabel, ElementLabel (shows unlocked next-level element),
  shard progress "x/5".
- **MinimapPanel** — `MapCanvas` (procedural `minimap_canvas.gd`): draws map texture + colored marker
  dots (🟢player green, 🟡NPC yellow, 🔵element blue, 🔴boss red, 🟤barrel brown, ⚪exit white),
  pulsing player dot, objective highlight; throttled to 20 fps. Legend label
  (`PlayerUtils.get_minimap_legend_text`).
- **MonsterPanel (怪物图鉴 / Monster Codex)** — title (`MONSTER_TITLE_FALLBACK`) + info
  (`MONSTER_DEFAULT_BODY` / `get_monster_info()` showing type/HP/weakness when near a monster).
- **PlayerHealthPanel** — HpRing, center avatar glyph, HP value/label, ProfilePortrait + name/stats,
  HealthBar, plus (vestigial RPG) ExpBar/ExpLabel, KeyHintLabel.
- **StatusPanel** — StatusTitle + StatusLabel (contextual hints, default `STATUS_DEFAULT_HINT`) +
  ControlsLabel.
- **SkillDock** — three slots (Water/Ice/Electric), each with key label, name, and live charge count.
- **Transient panels:** PauseFeedbackPanel (`UI_PAUSE_LABEL` "游戏已暂停"), DialogueHintPanel,
  NoticePopupPanel (title/body/hint), **FailurePanel** (FailureLabel + RestartButton +
  QuitToTitleButton, `RESTART_HINT` "按 T 重新开始"), **SettlementPanel** (title/summary/button),
  **IntroOverlay** (chapter/story/control/mission intro + BeginLevelButton), PickupToastLayer
  (`pickup_toast.gd`), BossAttackFlash ColorRect.
- **Settings** (`settings.gd`, scene not built): sliders for Master/Music/SFX, fullscreen toggle,
  reset-progress (with `ConfirmationDialog`), back-to-title; plus accessibility state
  (colorblind_mode, text_scale, reduced_motion, fog_assist) persisted to `user://settings.cfg`.

---

## 9. Audio (`scripts/AudioManager.gd`, autoload; files in `assets/audio/`)

- **Buses:** Master, Music, SFX, Dialogue (independent volumes; defaults Master 0.9, Music 0.32,
  SFX 1.0, Dialogue 0.9 linear). SFX uses a 6-player pool; streams cached; OGG music loops.
- **Music:** `title_theme.ogg` (title), `level1_underwater_ambient.ogg` (Level 1).
- **SFX:** element_pickup, player_hit, water_attack, no_shards, boss_attack, boss_hit, boss_defeat,
  ui_confirm, ui_select, key_tick, pause_toggle, objective_advance, dialogue_start, exit_reached,
  player_defeat, store_form (all `.ogg`). Asset licenses tracked in `assets/audio/LICENSES.md`.
- `play_dialogue(stream, speaker, line_text)` emits `dialogue_line_started` for future captions
  (accessibility hook). Dialogue itself is driven by **Dialogic** addon (bubble style).

---

## 10. Progression / Save

- **`SaveManager.gd`** (autoload): JSON at `user://save.json`, `SAVE_VERSION=1`. Signals
  `save_completed`, `load_completed`. `has_save()`, `save_state()`, `load_state()`, `reset()`.
- **Persisted fields** (written by the player script's progress-persist routine):
  `version`, `saved_at` (ISO-8601), `current_form`, `current_health` (clamped to [1,max] on load),
  `element_charges` {water,ice,electric}, `objective_stage`, `next_level_element_unlocked`
  (""/"ice"/"electric"), `first_boss_defeated` (bool), `first_water_collected` (bool).
- **Resume:** title shows 继续游戏; on game scene load, `_restore_progress_if_saved()` reapplies
  state and shows a resume notice ("进度已恢复…碎片:%d"). NewGame / failure / explicit reset call
  `SaveManager.reset()`.
- **Unlocks:** defeating the boss unlocks **electric** for the (planned) next level; ice/electric are
  inert in Level 1 except as unlock flags. Dialogic also has its own autosave (`save/autosave_delay=60`).
- **Settings** persist separately to `user://settings.cfg` (`ConfigFile`).

---

## 11. Asset Inventory (concise)

- **`资源/` (~42 raster images + font/shader):** jellyfish form sheets (water/ice/electric, 2×3×512),
  cute jellyfish frames, ice-octopus frames, electric jellyfish swim frames, element-attack sheet
  (`元素攻击精灵图.png`), ice-monster ice-attack, element orb icons (IMG_2088/2089/2090),
  six-coin row, UI atlas `IMG_2030.PNG`, plus `IMG_20xx.PNG` set; font `Aa水玉圆体.ttf`;
  `vision_mask.gdshader`; `bubble.tres`. Subfolder **`ui_panels/`** (~13 panels): confirm button
  normal/pressed, text frames, monster info frames, ocean UI sheet/reference, panel banner/card/
  info/intro, story_main_frame, task_frame.
- **`ui/shallow_sea_dream_ui_1920x1280/assets/`** (~32 PNGs): `base/` UI kit (HP/energy/affection
  bars, top resource bar, bottom nav, badges, inventory cards 01–06, dialogue textbox/choice panel,
  buttons gold/coral/seafoam/lavender, icons settings/bag/shop/log/mail/book, large story/feedback
  panels) and `content/` (avatars, enemies, icons_interaction/resources/status, portraits,
  story_covers). Note: this kit is largely **unused** by the current scene — a richer UI than what
  shipped; treat as optional design reference.
- **`场景/`:** map images (ChatGPT-generated maze, `污染的水道迷宫.png`). **`人物/`:** base jellyfish
  portrait. **`assets/audio/`:** 16 SFX + 2 music OGG (see §9).
- **`tests/unit/`:** GUT tests `test_save_manager.gd`, `test_skill_effectiveness.gd` (framework: GUT).

---

## 12. Notable Details to Preserve + Suggested Optimizations

**Preserve (these define the game's identity):**
- The **"purify, not kill"** narrative framing and all zh dialogue/lore verbatim.
- The exact **palette** and the rule that `WARNING_AMBER` is player-damage-only.
- **Fog-of-war vision mask** (single + optional secondary light) — central to the maze mood.
- Water-only effectiveness in Level 1 (`52` vs `18` damage) and the **5-shard** boss gate.
- Form transform flow (base→water in tutorial; electric drop on boss defeat) and save unlock flags.
- HUD composition: objective checklist, minimap markers + legend, monster codex, HP ring, skill dock.
- Input map, autoloads (Events bus, AudioManager, SaveManager, Dialogic), and 1920×1080 nearest-filter.

**Optimizations / cleanups for the C# rebuild:**
- **Split the 147 KB god-class** `character_body_2d.gd`: separate Player movement,
  SkillSystem, ObjectiveManager, HudController, ElementSpawner, BossController into discrete
  C# nodes/classes communicating via the existing **`Events` signal bus** (already defined in
  `Events.gd` — port to a C# autoload with C# events/signals).
- **Finish localization:** route every inline zh string through the `translations.csv` table
  (`Tr()`), so EN actually works; expand the table to cover dialogue and tutorial pages.
- Replace string-name heuristics (`get_element_form` parsing node names for "冰"/"电") with typed
  enums / exported metadata.
- Replace the duplicated `@export` blocks between `pollution_monster.gd` and
  `pollution_monster_base.gd` with proper inheritance / a `MonsterData` Resource.
- Consider authoring the **Settings scene** that `settings.gd` already supports, and wire the
  accessibility knobs (fog_assist → `fog_radius_scale`, text_scale, reduced_motion).
- Drop the vestigial Exp bar/labels unless an XP system is intended.
- Build the unused `ui/shallow_sea_dream_ui_1920x1280` kit into the HUD for a more polished look,
  or prune it to reduce asset weight.
- For a pure-C# project, reimplement the 5 small Dialogic timelines as a thin C# dialogue runner +
  data files rather than depending on the GDScript Dialogic addon.
- Decompose the ~48 hand-placed wall CollisionPolygon2D nodes into a TileMap or data-driven layout
  for maintainability.

**Asset-generation constraint (HARD — do not deviate):**
- Use **Gemini only** for all art. The project budget assumes only `GOOGLE_API_KEY` is set;
  `XAI_API_KEY` (Grok) and `TRIPO3D_API_KEY` are NOT available.
- For **animated sprites**, do NOT use `asset_gen.py video` (Grok video). Instead have Gemini
  generate a **grid sprite-sheet** (one image, N frames in a grid, e.g. 4×2 for a jellyfish
  idle/pulse) and slice it with `grid_slice.py` into an `AnimatedSprite2D` / SpriteFrames.
  This also matches the original game, which used hand-drawn sprite-sheet atlases, not video.
- No 3D (Tripo3D) — the game is 2D top-down; all assets are 2D PNG sprites/atlases.
