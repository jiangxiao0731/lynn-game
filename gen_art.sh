#!/usr/bin/env zsh
# Compatibility entry point retained for old documentation and shortcuts.
# Runtime art now comes exclusively from source artwork preserved in this project.

set -euo pipefail
SCRIPT_DIR="${0:A:h}"
PROJECT="$SCRIPT_DIR"
cd "$PROJECT"
exec ./tools/sync_folder_art.sh
