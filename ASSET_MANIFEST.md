# Asset Manifest — supplied hand-painted source only

The running project contains **no AI-generated runtime image from the former
Godogen/Gemini pass**. Every PNG under `assets/` is now either a byte-for-byte copy
of artwork preserved under the project-local `source_art/` directory or a
deterministic crop, resize, colour treatment, or composite of those supplied files.

`tools/sync_folder_art.sh` is the canonical reproducible mapping. It calls only
ImageMagick and local file copies; it never contacts an image-generation service.

## Player and action art

| Runtime files | Supplied source | Treatment |
|---|---|---|
| `jellyfish_base.png`, `jellyfish_water.png` and `_raw/` copies | `游戏素材库/可爱的水母动画帧.png` | Direct 3×2 sheet copy; background keyed only in memory |
| `jellyfish_ice.png` and `_raw/` | `游戏素材库/冰雪章鱼精动画帧.png` | Direct 3×2 sheet copy |
| `jellyfish_electric.png` and `_raw/` | `shallow-sea-dream/assets/art/monsters/electric_jellyfish_idle_sheet.png` | Direct 3×2 sheet copy |
| `water_attack.png` and `_raw/` | `shallow-sea-dream/资源/元素攻击精灵图.png` | Direct supplied action sheet copy |

## Pollution and guardians

| Runtime files | Supplied source | Treatment |
|---|---|---|
| `brood_mother.png` and `_raw/` | `assets/art/monsters/tar_monster_idle_sheet.png` | Direct 3×2 copy for chapter-one purification guardian |
| `invader_1.png` and `_raw/` | `游戏人物/塑料瓶小怪.PNG` | Direct transparent copy |
| `invader_2.png` and `_raw/` | `游戏人物/化学气小怪.PNG` | Direct transparent copy |
| `invader_3.png` and `_raw/` | `游戏人物/汽油小怪.PNG` | Direct transparent copy |
| `legacy/ice_monster_idle_sheet.png` | `assets/art/monsters/ice_monster_idle_sheet.png` | Direct 3×2 copy |
| `legacy/factory_monster_idle_sheet.png` | `assets/art/monsters/factory_monster_idle_sheet.png` | Direct 3×2 copy |
| `legacy/electric_jellyfish_idle_sheet.png` | matching legacy art file | Direct 3×2 copy |
| `legacy/tar_monster_idle_sheet.png` | matching legacy art file | Direct 3×2 copy |

## Characters and dialogue portraits

| Runtime id | Supplied source | Treatment |
|---|---|---|
| `npc_npc1giving` | `资源/lan_grandma_idle_sheet.png` | First hand-painted frame isolated and enlarged |
| `npc_starfish` | `资源/dialogue_portraits/starfish_dialogue.png` | Direct transparent copy |
| `npc_seaweed` | `资源/dialogue_portraits/seaweed_dialogue.png` | Direct transparent copy |
| `npc_barrel`, `npc_hermit` | `资源/未命名作品 3.png` | Supplied rusty can centred on transparent portrait canvas |
| `npc_lantern` | `资源/IMG_2098.PNG` | Supplied fish-in-glass painting centred on portrait canvas |
| `npc_shoal` | `资源/IMG_2098.PNG` | Three scaled copies composed as a shoal |
| `npc_boss` | `游戏人物/汽油大怪.PNG` | Supplied oil creature centred on portrait canvas |

The corresponding `assets/img/_raw/` copies contain the same source-derived art.

## World paintings, reefs and icons

| Runtime files | Supplied source | Treatment |
|---|---|---|
| `title_bg.png` | `资源/IMG_2030.PNG` | Lower key-art crop, resized and ocean-tinted |
| `zone_shallows.png`, `zone_sediment.png`, `zone_depths.png` | `资源/IMG_2030.PNG` | Three contiguous left/centre/right crops with identical colour treatment |
| `maze_map.png` | derived `zone_sediment.png` | Runtime compatibility copy |
| `wall_tile.png` | `资源/IMG_2030.PNG` | Dark ruin-and-water crop used inside organic reef polygons |
| `icon_memory.png`, `water_shard.png` | `资源/IMG_2088.PNG` | Supplied water-orb painting, trimmed and centred |
| `icon_note.png` | `资源/IMG_2027.PNG` | Supplied carved sand-and-water tile crop |

## Exclusions and verification

- Previous generated per-frame inspection directories are excluded from the runtime;
  the current sync script does not delete files or directories.
- The separate CraftPix/pixel-art cave and seabed libraries are intentionally not
  mixed into this painterly runtime.
- No source PNG is destructively edited. Runtime background keying and tinting are
  performed on copies or in memory.
- To rebuild this exact set without any external folder dependency, run
  `./tools/sync_folder_art.sh`, then Godot import.
