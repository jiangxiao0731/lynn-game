#!/usr/bin/env zsh
# Rebuild every runtime PNG from the preserved source artwork inside this project.
# This script performs only selection, cropping, compositing, resizing and colour
# treatment. It never calls an image-generation service.

set -euo pipefail

SCRIPT_DIR="${0:A:h}"
PROJECT="${SCRIPT_DIR:h}"
SOURCE_ROOT="$PROJECT/source_art"
LIBRARY="$SOURCE_ROOT/library"
PEOPLE="$SOURCE_ROOT/characters"
RESOURCE="$SOURCE_ROOT/story"
ART="$SOURCE_ROOT/legacy"
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

if [[ "$PWD" != "$PROJECT" ]]; then
  print -u2 "Run this script from $PROJECT"
  exit 2
fi
if ! command -v magick >/dev/null 2>&1; then
  print -u2 "ImageMagick is required (missing: magick)."
  exit 2
fi

required=(
  "$LIBRARY/cute_jellyfish_sheet.png"
  "$LIBRARY/ice_jellyfish_sheet.png"
  "$ART/monsters/electric_jellyfish_idle_sheet.png"
  "$ART/monsters/tar_monster_idle_sheet.png"
  "$ART/monsters/ice_monster_idle_sheet.png"
  "$ART/monsters/factory_monster_idle_sheet.png"
  "$RESOURCE/element_attack_sheet.png"
  "$RESOURCE/key_art.png"
  "$RESOURCE/note_source.png"
  "$RESOURCE/memory_source.png"
  "$RESOURCE/lantern_fish_source.png"
  "$RESOURCE/dialogue_portraits/starfish_dialogue.png"
  "$RESOURCE/dialogue_portraits/seaweed_dialogue.png"
  "$RESOURCE/granny_idle_sheet.png"
  "$RESOURCE/rusty_can.png"
  "$PEOPLE/plastic_bottle_small.png"
  "$PEOPLE/chemical_gas_small.png"
  "$PEOPLE/oil_small.png"
  "$PEOPLE/oil_guardian.png"
)
for file in "${required[@]}"; do
  [[ -f "$file" ]] || { print -u2 "Missing source: $file"; exit 2; }
done

mkdir -p assets/img/_raw assets/sprites/_raw assets/sprites/legacy

copy_form() {
  local source="$1" name="$2"
  cp "$source" "assets/sprites/_raw/jellyfish_${name}.png"
  cp "$source" "assets/sprites/jellyfish_${name}.png"
}

copy_portrait() {
  local source="$1" name="$2"
  cp "$source" "assets/img/npc_${name}.png"
  cp "$source" "assets/img/_raw/npc_${name}.png"
}

# Player forms: all selected sheets are 3 columns × 2 rows.
copy_form "$LIBRARY/cute_jellyfish_sheet.png" base
copy_form "$LIBRARY/cute_jellyfish_sheet.png" water
copy_form "$LIBRARY/ice_jellyfish_sheet.png" ice
copy_form "$ART/monsters/electric_jellyfish_idle_sheet.png" electric

# Pollution and guardians.
cp "$ART/monsters/tar_monster_idle_sheet.png" assets/sprites/brood_mother.png
cp "$ART/monsters/tar_monster_idle_sheet.png" assets/sprites/_raw/brood_mother.png
cp "$PEOPLE/plastic_bottle_small.png" assets/sprites/invader_1.png
cp "$PEOPLE/chemical_gas_small.png" assets/sprites/invader_2.png
cp "$PEOPLE/oil_small.png" assets/sprites/invader_3.png
cp "$PEOPLE/plastic_bottle_small.png" assets/sprites/_raw/invader_1.png
cp "$PEOPLE/chemical_gas_small.png" assets/sprites/_raw/invader_2.png
cp "$PEOPLE/oil_small.png" assets/sprites/_raw/invader_3.png
cp "$RESOURCE/element_attack_sheet.png" assets/sprites/water_attack.png
cp "$RESOURCE/element_attack_sheet.png" assets/sprites/_raw/water_attack.png

# Preserve the original supplied guardian sheets byte-for-byte.
cp "$ART/monsters/ice_monster_idle_sheet.png" assets/sprites/legacy/ice_monster_idle_sheet.png
cp "$ART/monsters/factory_monster_idle_sheet.png" assets/sprites/legacy/factory_monster_idle_sheet.png
cp "$ART/monsters/electric_jellyfish_idle_sheet.png" assets/sprites/legacy/electric_jellyfish_idle_sheet.png
cp "$ART/monsters/tar_monster_idle_sheet.png" assets/sprites/legacy/tar_monster_idle_sheet.png

# NPC portraits and world sprites.
copy_portrait "$RESOURCE/dialogue_portraits/starfish_dialogue.png" starfish
copy_portrait "$RESOURCE/dialogue_portraits/seaweed_dialogue.png" seaweed

# Granny Lan: isolate one hand-painted anemone frame from the supplied 6-frame strip.
magick "$RESOURCE/granny_idle_sheet.png" -crop 64x96+0+0 +repage \
  -trim +repage -resize '180x260' -gravity center -background none -extent 320x320 \
  "$WORK/granny.png"
copy_portrait "$WORK/granny.png" npc1giving

# The rusty discarded can serves both the barrel memory and the hermit-crab home.
magick "$RESOURCE/rusty_can.png" -trim +repage -resize '292x250>' \
  -gravity center -background none -extent 320x320 "$WORK/barrel.png"
copy_portrait "$WORK/barrel.png" barrel
copy_portrait "$WORK/barrel.png" hermit

# One supplied fish-in-glass painting becomes the lantern guide; a small hand-built
# grouping of the same painting reads as the lost shoal without inventing new art.
magick "$RESOURCE/lantern_fish_source.png" -trim +repage -resize '260x260>' \
  -gravity center -background none -extent 320x320 "$WORK/lantern.png"
copy_portrait "$WORK/lantern.png" lantern
magick -size 520x320 canvas:none \
  \( "$WORK/lantern.png" -resize 210x210 \) -geometry +0+90 -composite \
  \( "$WORK/lantern.png" -resize 235x235 \) -geometry +140+12 -composite \
  \( "$WORK/lantern.png" -resize 195x195 \) -geometry +320+105 -composite \
  "$WORK/shoal.png"
copy_portrait "$WORK/shoal.png" shoal

# The supplied large oil creature is the close portrait for the first guardian.
magick "$PEOPLE/oil_guardian.png" -trim +repage -resize '300x300>' \
  -gravity center -background none -extent 320x320 "$WORK/boss.png"
copy_portrait "$WORK/boss.png" boss

# Icons are direct crops or copies from the supplied hand-painted source library.
magick "$RESOURCE/memory_source.png" -trim +repage -resize '300x300>' \
  -gravity center -background none -extent 320x320 assets/img/icon_memory.png
cp assets/img/icon_memory.png assets/img/_raw/icon_memory.png
cp assets/img/icon_memory.png assets/img/water_shard.png
cp assets/img/icon_memory.png assets/img/_raw/water_shard.png

# A carved sand-and-water tile becomes the collectible story fragment.
magick "$RESOURCE/note_source.png" -crop 238x190+76+104 +repage \
  -resize 320x256 -gravity center -background none -extent 320x320 assets/img/icon_note.png
cp assets/img/icon_note.png assets/img/_raw/icon_note.png

# World paintings: three lower-half crops of the supplied JellyGuard key painting.
# Light colour treatment keeps maze silhouettes and the player readable over them.
KEY_ART="$RESOURCE/key_art.png"
magick "$KEY_ART" -crop 1536x624+0+400 +repage -resize 1920x1080! \
  -modulate 72,72,100 -fill '#153f4b' -colorize 16 assets/img/title_bg.png
magick "$KEY_ART" -crop 512x624+0+400 +repage -resize 1200x1080! -blur 0x2.1 \
  -modulate 58,58,100 -fill '#18515f' -colorize 28 assets/img/zone_shallows.png
magick "$KEY_ART" -crop 512x624+512+400 +repage -resize 1200x1080! -blur 0x2.1 \
  -modulate 58,58,100 -fill '#18515f' -colorize 28 assets/img/zone_sediment.png
magick "$KEY_ART" -crop 512x624+1024+400 +repage -resize 1200x1080! -blur 0x2.1 \
  -modulate 58,58,100 -fill '#18515f' -colorize 28 assets/img/zone_depths.png
cp assets/img/zone_sediment.png assets/img/maze_map.png

# Use a dark ruin-and-water crop from the same supplied key painting for reef texture.
magick "$KEY_ART" -crop 330x300+170+360 +repage -resize 512x512! -blur 0x0.6 \
  -modulate 48,46,100 -fill '#173f49' -colorize 35 assets/img/wall_tile.png

# Strip creation timestamps and ancillary chunks from edited PNGs so repeated local
# rebuilds are byte-for-byte stable as well as visually stable.
derived_outputs=(
  assets/img/icon_memory.png assets/img/_raw/icon_memory.png
  assets/img/icon_note.png assets/img/_raw/icon_note.png
  assets/img/water_shard.png assets/img/_raw/water_shard.png
  assets/img/npc_npc1giving.png assets/img/_raw/npc_npc1giving.png
  assets/img/npc_barrel.png assets/img/_raw/npc_barrel.png
  assets/img/npc_hermit.png assets/img/_raw/npc_hermit.png
  assets/img/npc_lantern.png assets/img/_raw/npc_lantern.png
  assets/img/npc_shoal.png assets/img/_raw/npc_shoal.png
  assets/img/npc_boss.png assets/img/_raw/npc_boss.png
  assets/img/title_bg.png assets/img/zone_shallows.png
  assets/img/zone_sediment.png assets/img/zone_depths.png
  assets/img/maze_map.png assets/img/wall_tile.png
)
for output in "${derived_outputs[@]}"; do
  magick "$output" -strip -define png:exclude-chunks=date,time "$WORK/normalized.png"
  mv "$WORK/normalized.png" "$output"
done

print "SOURCE_FOLDER_ART_SYNC_OK"
