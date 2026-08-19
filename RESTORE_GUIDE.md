# Version restore guide

The project keeps named Git restore points in addition to the original physical
snapshot folders. No original folder was deleted, moved, or overwritten while these
restore points were created.

## Named restore points

- `restore/baseline-before-handpaint-20260818`: version before the hand-painted UI pass.
- `restore/handpaint-ui-before-folder-assets-20260818`: hand-painted UI with the earlier runtime artwork.
- `restore/folder-art-self-contained-20260818`: current all-hand-painted version, with source artwork inside the project.
- `restore/english-ocean-learning-20260818`: English portfolio version with the three-chapter ocean learning arc.

List them at any time:

```bash
git tag --list 'restore/*'
```

## Safest rollback preview

Use a Git worktree to open an old version beside the current one. This does not switch,
overwrite, or remove anything in the current folder:

```bash
cd /Users/shawj/Desktop/lynn/shallow-sea-dream-cs
git worktree add ../shallow-sea-dream-restore-preview restore/baseline-before-handpaint-20260818
```

Replace the tag at the end with either of the other restore-point names to preview that
version. Choose a new destination folder if `shallow-sea-dream-restore-preview` already
exists; Git will refuse rather than overwrite it.

## Existing physical snapshots

These folders were deliberately left untouched as a second layer of protection:

- `/Users/shawj/Desktop/lynn/shallow-sea-dream`
- `/Users/shawj/Desktop/lynn/shallow-sea-dream-cs-baseline-before-handpaint-20260818`
- `/Users/shawj/Desktop/lynn/shallow-sea-dream-cs-handpaint-ui-before-folder-assets-20260818`
- `/Users/shawj/Desktop/lynn/shallow-sea-dream-cs`

Do not remove any of them until the current game and all three Git restore points have
been opened and visually approved. Keeping only one physical project folder can be a
later, explicit decision; it is not part of this consolidation step.
