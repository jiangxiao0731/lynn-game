# Asset Manifest — Shallow Sea Dream (C# rebuild)

Every asset below is the core campaign set. The game runs with
procedural placeholders until these files exist; code loads each path through
`AssetLoader` and falls back gracefully when missing. `gen_art.sh` produces exactly
this set (Gemini images; animated forms = Gemini GRID sheet → `grid_slice.py`).

Art direction (all prompts share it): soft illustrative "storybook" undersea look,
deep-sea teal/cyan palette (deep `#0a1f2e`, cyan `#85f0ff`, teal `#6bdcc7`), gentle
melancholic eco-fable mood, crisp edges (nearest-filter), **no yellow** except the
single warm amber reserved for player damage (do not use amber in enemy/pollution art).

## Sprite sheets (animated — 2×3 grid of 512×512 = 6 frames, sliced Idle 0–2 / Walk 3–5)

| Path | Grid | Used by | Prompt summary |
|------|------|---------|----------------|
| `assets/sprites/jellyfish_base.png` | 2×3 (512²) | `Player` base form (`AssetLoader.FormSheet(Base)` → `PlaceholderArt.SliceFormSheet`) | translucent juvenile jellyfish "微光/Shimmer", pale cyan, 6 gentle swim-pulse frames |
| `assets/sprites/jellyfish_water.png` | 2×3 (512²) | `Player` water form | water-element jellyfish, blue `#4db3ff` glow, flowing tendrils, 6 pulse frames |
| `assets/sprites/jellyfish_ice.png` | 2×3 (512²) | `Player` ice form (unlock-only) | ice-element jellyfish, frosty `#b3eaff`, crystalline edges, 6 pulse frames |
| `assets/sprites/jellyfish_electric.png` | 2×3 (512²) | `Player` electric form (unlock-only) | electric jellyfish, `#e6ff4d` arcs, 6 pulse frames |
| `assets/sprites/brood_mother.png` | single (1024²) | `BossController` boss sprite | 潮涡巢母 brood mother: huge mass of fused small jellyfish, polluted teal, ominous bloom, thin pale membrane weak-point |
| `assets/sprites/water_attack.png` | 2×3 (256²) | water projectile (`AssetLoader.WaterAttackSheet`) | water purification burst, cyan droplet → splash, 6 frames |
| `assets/sprites/invader_1.png` | single (512²) | pollution invader prop | plastic-bag pollution invader, drifting, teal-tinted |
| `assets/sprites/invader_2.png` | single (512²) | pollution invader prop | oil-slick pollution invader, iridescent dark teal |
| `assets/sprites/invader_3.png` | single (512²) | pollution invader prop | chemical-foam pollution invader, sickly pale teal |

## Single images

| Path | Used by | Prompt summary |
|------|---------|----------------|
| `assets/img/water_shard.png` | `ElementSpawner` shard pickup (`AssetLoader.ShardIcon`) | glowing blue water shard crystal, faceted, soft cyan glow, transparent bg |
| `assets/img/maze_map.png` | `GameSceneController` map background (`AssetLoader.MapBackground`) | top-down polluted-water-channel maze, 1920×1080, dark teal seabed, debris |
| `assets/img/title_bg.png` | `TitleController` background (`AssetLoader.TitleBackground`) | atmospheric undersea title scene, shimmer-tide light shafts, 16:9 |
| `assets/img/wall_tile.png` | wall tile reference (`AssetLoader.WallTile`) | seamless rocky/coral seabed wall tile, dark teal, tileable |
| `assets/img/npc_npc1giving.png` | Granny Lan portrait (`AssetLoader.NpcPortrait`) | 岚婆婆 / Granny Lan: elderly wise sea-anemone spirit, warm cyan, kindly |
| `assets/img/npc_starfish.png` | Starfish NPC portrait | lonely surviving starfish, dimmed colors, gentle |
| `assets/img/npc_seaweed.png` | Seaweed NPC portrait | tall swaying seaweed spirit, cautious expression |

## Story-first overhaul assets (new)

NPC / speaker portraits load via `AssetLoader.NpcPortrait(id)` → `npc_{id}.png`; all fall
back to a procedural tinted blob when missing (DialogueRunner / GameSceneController).

| Path | Used by | Prompt summary |
|------|---------|----------------|
| `assets/img/npc_hermit.png` | 寄居蟹老郑 portrait (`NarrativeData.Hermit`) | weary hermit crab in a discarded can, practical melancholy |
| `assets/img/npc_lantern.png` | 灯笼鱼·盏 portrait (`NarrativeData.Lantern`) | deep-sea lanternfish, oil-dimmed light, hopeful |
| `assets/img/npc_shoal.png` | 迷途鱼群 portrait (`NarrativeData.Shoal`) | small lost shoal huddled together, anxious |
| `assets/img/npc_barrel.png` | 汽油桶 portrait (`DialogueData.JellyfishBox`) | rusted sunk gasoline barrel, faded label, sorrowful |
| `assets/img/npc_boss.png` | 潮涡巢母 dialogue portrait (boss pre/mid) | weeping fused-jellyfish mass, pained not evil |
| `assets/img/icon_memory.png` | 微光潮汐记忆 mote (`AssetLoader.MemoryIcon`, shard sprite) | soft cyan luminous pearl with tidal swirl, transparent bg |
| `assets/img/icon_note.png` | 残片笔记 lore object (`AssetLoader.NoteIcon`) | worn torn lore fragment, muted teal-brown, transparent bg |
| `assets/img/zone_shallows.png` | 浅滩 background (`AssetLoader.ZoneBackground`) | lighter teal nursery shallows, soft coral, hopeful, 16:9 |
| `assets/img/zone_sediment.png` | 沉积带 background (`AssetLoader.ZoneBackground`) | murky settling silt over buried debris/shells, somber, 16:9 |
| `assets/img/zone_depths.png` | 巢母深处 background (`AssetLoader.ZoneBackground`) | darkest polluted water, low bloom, membrane glow, 16:9 |

## Shader

The fog-of-war `vision_mask.gdshader` was REMOVED (story-first overhaul item 1): the
whole level is always visible now. No shader assets are required.

## Notes

### Existing legacy campaign paintings (no generation)

| Path | Source | Grid | Runtime use |
|---|---|---|---|
| `assets/sprites/legacy/ice_monster_idle_sheet.png` | sibling legacy project `assets/art/monsters/` | 3×2 | Chapter 2 guardian; `SliceCreatureSheet`, target 290 px |
| `assets/sprites/legacy/factory_monster_idle_sheet.png` | sibling legacy project `assets/art/monsters/` | 3×2 | Chapter 3 guardian; `SliceCreatureSheet`, target 300 px |
| `assets/sprites/legacy/electric_jellyfish_idle_sheet.png` | sibling legacy project `assets/art/monsters/` | 3×2 | reserved restored-electric fauna |
| `assets/sprites/legacy/tar_monster_idle_sheet.png` | sibling legacy project `assets/art/monsters/` | 3×2 | reserved pollution fauna |

These four images already existed in `/Users/shawj/Desktop/lynn/shallow-sea-dream` and were copied without regeneration. Their presentation backgrounds are removed in memory; source PNGs remain untouched.

- Animated sheets: Gemini generates ONE grid image per prompt; `grid_slice.py --grid 2x3`
  slices into 6 frames. Code re-slices the full sheet at runtime (`SliceFormSheet`), so
  the saved file must be the **whole 2×3 sheet**, not pre-sliced cells.
- Only **water** is mechanically effective in Level 1; ice/electric sheets are for
  form display + unlock flags only.
- Audio (`assets/audio/*.ogg`) is NOT generated here — drop in licensed OGGs named per
  DESIGN_BRIEF §9 (e.g. `level1_underwater_ambient.ogg`, `element_pickup.ogg`).
