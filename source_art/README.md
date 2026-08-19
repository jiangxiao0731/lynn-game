# Preserved source artwork

This directory makes the current game self-contained. Its 19 image files are
byte-for-byte copies of artwork supplied in the surrounding `lynn` folders; no
source file was moved, renamed in place, edited, or deleted.

The filenames here are normalized copies used by `tools/sync_folder_art.sh`:

- `library/`: player jellyfish animation sheets.
- `characters/`: pollution creatures and the oil guardian.
- `legacy/monsters/`: four hand-painted guardian/monster sheets from the legacy game.
- `story/`: key art, icons, portraits, dialogue characters, and attack artwork.

`assets/` contains the runtime-ready copies and deterministic crops. This directory
contains the preserved inputs. The sync script only reads these inputs and writes
derived runtime files; it does not delete project files or contact a generation API.
