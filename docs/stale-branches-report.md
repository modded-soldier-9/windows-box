# Stale merged branches report (m2-14)

Generated as part of Ron's Stage 4 item 14. **Nothing was deleted** — this is a
list only so maintainers can prune remotes when ready.

## Merged into `origin/main` (safe to delete remotely)

These remote branches are fully contained in `main` as of this report:

| Branch | Notes |
|-|-|
| `origin/cursor/m0-foundation-a433` | Merged via PR #1 / #2 |
| `origin/cursor/m1-shared-engine-3b89` | Merged via PR #3 |
| `origin/cursor/m1-undo-verifier-3b89` | Merged via PR #4 |
| `origin/cursor/m1-elevator-3b89` | Merged via PR #5 |
| `origin/cursor/m1-oss-docs-wiki-3b89` | Merged via PR #6 |
| `origin/cursor/m1-network-privacy-3b89` | Merged into main (stacked) |
| `origin/cursor/m1-services-module-3b89` | Merged into main (stacked) |
| `origin/cursor/m1-signing-wpf-gate-3b89` | Merged into main (stacked) |
| `origin/cursor/m1-startup-module-3b89` | Merged into main (stacked) |
| `origin/cursor/m1-verify-task-3b89` | Merged into main (stacked) |
| `origin/cursor/m1-winui-shell-3b89` | Merged into main (stacked) |

Suggested cleanup (maintainer only):

```bash
git push origin --delete cursor/m0-foundation-a433
# …repeat for each row above
```

## Active M2 stack (do **not** delete)

Stacked feature branches for this roadmap — not merged yet:

- `cursor/m2-1-b33c` … `cursor/m2-14-b33c`

## Method

```bash
git fetch origin --prune
git branch -r --merged origin/main
```
