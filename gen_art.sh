#!/usr/bin/env bash
# gen_art.sh — generate the minimal Level 1 art set with Gemini (godogen tools).
# Non-interactive, fixed prompt table, no per-asset loops. Animated sprites use a
# Gemini GRID sheet sliced by grid_slice.py (NO Grok video, NO Tripo3D).
#
# Usage:  GOOGLE_API_KEY=... ./gen_art.sh
# The key is read from the environment ONLY — never written to any file.

set -euo pipefail

PROJ="/Users/shawj/Desktop/lynn/shallow-sea-dream-cs"
PY="$PROJ/.venv/bin/python"
GEN="$PROJ/.claude/skills/godogen/tools/asset_gen.py"
SLICE="$PROJ/.claude/skills/godogen/tools/grid_slice.py"

if [ -z "${GOOGLE_API_KEY:-}" ]; then
  cat <<'EOF'
GOOGLE_API_KEY is not set.

Set it and re-run (the key is read from the environment only and is never
written to disk):

  export GOOGLE_API_KEY="your-key-here"
  ./gen_art.sh

EOF
  exit 1
fi

if [ ! -x "$PY" ]; then
  echo "Project venv python not found at: $PY"
  echo "Create it, e.g.:  python3 -m venv .venv && .venv/bin/pip install -r .claude/skills/godogen/tools/requirements.txt"
  exit 1
fi

cd "$PROJ"
mkdir -p assets/img assets/sprites

STYLE="soft illustrative storybook undersea art, deep-sea teal and cyan palette, gentle melancholic eco-fable mood, crisp clean edges, no yellow"

img() {  # img <gemini-size> <aspect> <output> <prompt...>
  local size="$1" aspect="$2" out="$3"; shift 3
  local log; log="$(mktemp)"
  echo ">> $out"
  if "$PY" "$GEN" image --model gemini --size "$size" --aspect-ratio "$aspect" \
        --prompt "$*" -o "$out" >/dev/null 2>"$log"; then
    echo "   ok"
  else
    echo "   FAILED — see below:"; tail -15 "$log"
  fi
  rm -f "$log"
}

# --- Animated form sheets: 2x3 grid (6 frames). Generate sheet, then slice. ---
gen_form_sheet() {  # gen_form_sheet <name> <desc>
  local name="$1" desc="$2"
  local sheet="assets/sprites/jellyfish_${name}.png"
  img 1K "2:3" "$sheet" \
    "$STYLE. A 2x3 grid sprite sheet (6 cells) of a $desc. Each cell is one frame of a gentle swimming pulse animation, same character, consistent size and centering, solid dark teal background, clear separation between cells."
  # Re-slice into individual frames for inspection (runtime re-slices the full sheet).
  [ -f "$sheet" ] && "$PY" "$SLICE" "$sheet" -o "assets/sprites/_${name}_frames" --grid 2x3 >/dev/null 2>&1 || true
}

gen_form_sheet base     "translucent juvenile jellyfish (微光/Shimmer), pale cyan glow, soft tendrils"
gen_form_sheet water    "water-element jellyfish, blue glow #4db3ff, flowing water tendrils"
gen_form_sheet ice      "ice-element jellyfish, frosty pale blue #b3eaff, crystalline edges"
gen_form_sheet electric "electric-element jellyfish, bright #e6ff4d energy arcs"

# Water attack burst sheet (2x3).
img 1K "2:3" "assets/sprites/water_attack.png" \
  "$STYLE. A 2x3 grid sprite sheet (6 cells) of a water purification burst: a cyan water droplet expanding into a splash across the 6 frames, solid dark teal background, clear cell separation."
[ -f assets/sprites/water_attack.png ] && "$PY" "$SLICE" assets/sprites/water_attack.png -o assets/sprites/_water_attack_frames --grid 2x3 >/dev/null 2>&1 || true

# --- Boss (single large sprite) ---
img 1K "1:1" "assets/sprites/brood_mother.png" \
  "$STYLE. 潮涡巢母 the Tide-Vortex Brood Mother: a huge ominous mass of small jellyfish fused together by pollution, polluted teal, slow low-frequency bloom, one thin pale translucent membrane weak-point, centered, solid dark background."

# --- Pollution invaders (single sprites) ---
img 1K "1:1" "assets/sprites/invader_1.png" "$STYLE. A drifting plastic-bag pollution invader creature, teal-tinted, sad warped form, centered on solid dark teal background."
img 1K "1:1" "assets/sprites/invader_2.png" "$STYLE. An oil-slick pollution invader creature, iridescent dark teal, centered on solid dark teal background."
img 1K "1:1" "assets/sprites/invader_3.png" "$STYLE. A chemical-foam pollution invader creature, sickly pale teal, centered on solid dark teal background."

# --- Single images / props ---
img 1K "1:1" "assets/img/water_shard.png" "$STYLE. A single glowing blue water shard crystal, faceted, soft cyan glow, centered on solid dark background."
img 1K "1:1" "assets/img/wall_tile.png"   "$STYLE. A seamless tileable top-down rocky coral seabed wall tile, dark teal, uniform lighting, no shadows."

# --- NPC portraits (filenames match DialogueData timeline ids) ---
img 1K "1:1" "assets/img/npc_npc1giving.png" "$STYLE. Portrait of 岚婆婆 Granny Lan, an elderly wise sea-anemone spirit, warm cyan, kindly gentle expression, centered."
img 1K "1:1" "assets/img/npc_starfish.png"   "$STYLE. Portrait of a lonely surviving starfish character, dimmed colors, gentle melancholic expression, centered."
img 1K "1:1" "assets/img/npc_seaweed.png"    "$STYLE. Portrait of a tall swaying seaweed spirit character, cautious worried expression, centered."

# --- NEW NPC / speaker portraits (story-first overhaul) ---
img 1K "1:1" "assets/img/npc_hermit.png"  "$STYLE. Portrait of 寄居蟹老郑 an old weary hermit crab living in a discarded human can, practical melancholy expression, centered."
img 1K "1:1" "assets/img/npc_lantern.png" "$STYLE. Portrait of 灯笼鱼·盏 a deep-sea lanternfish whose light organ is dimmed by oil, hopeful gentle glow, centered."
img 1K "1:1" "assets/img/npc_shoal.png"   "$STYLE. Portrait of 迷途鱼群 a small lost shoal of fish huddled together, anxious searching expression, centered."
img 1K "1:1" "assets/img/npc_barrel.png"  "$STYLE. Portrait of 汽油桶 a rusted human gasoline barrel sunk on the seabed, faded factory label, sorrowful, centered."
img 1K "1:1" "assets/img/npc_boss.png"    "$STYLE. Close portrait of 潮涡巢母 the Tide-Vortex Brood Mother, a tender weeping mass of fused small jellyfish, pained not evil, centered."

# --- Narrative icons: memory vignette + lore-note fragment (story-first overhaul) ---
img 1K "1:1" "assets/img/icon_memory.png" "$STYLE. A single glowing 微光潮汐记忆 memory mote: a soft cyan luminous pearl with faint tidal swirl, transparent background, centered."
img 1K "1:1" "assets/img/icon_note.png"   "$STYLE. A single 残片笔记 lore fragment: a worn torn scrap / shard with faint markings, muted teal-brown, transparent background, centered."

# --- New-zone backgrounds (story-first overhaul, 2K for crispness) ---
img 2K "16:9" "assets/img/zone_shallows.png" "$STYLE. Top-down 浅滩 shallows of the tidal nursery, lighter teal seabed with soft coral and scattered shimmer, hopeful, game level background."
img 2K "16:9" "assets/img/zone_sediment.png" "$STYLE. Top-down 沉积带 sediment zone, murky settling silt over buried debris and empty shells, somber, game level background."
img 2K "16:9" "assets/img/zone_depths.png"   "$STYLE. Top-down 巢母深处 brood depths, darkest polluted water with a low ominous bloom and a thin membrane glow, game level background."

# --- Large backgrounds (2K for crispness) ---
img 2K "16:9" "assets/img/maze_map.png"  "$STYLE. Top-down view of a polluted water-channel maze, dark teal seabed with scattered debris and pollution, 1920x1080 game level background."
img 2K "16:9" "assets/img/title_bg.png"  "$STYLE. Atmospheric undersea title screen background, shimmer-tide light shafts piercing dark water, sense of hope and melancholy, 16:9."

echo
echo "Done. Generated assets are under assets/img and assets/sprites."
echo "Re-run any FAILED line individually if needed. Then open the project in Godot to import."
