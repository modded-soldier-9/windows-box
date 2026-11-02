# Windows Box plugins

Plugins are **reviewed and merged into this repository** under `plugins/`.
There is **no sandbox** — the app will not install anything that is not on the
in-repo allow-list, and `plugins/deny-list.txt` can disable a plugin immediately.

## How to contribute

1. Open a PR that adds `plugins/<id>/plugin.yaml` (and optional docs).
2. Maintainers review for safety and correctness.
3. Once merged, the plugin id is allow-listed and appears as one-click installable in the app.

## Ratings and reviews

Use GitHub issue labels on this repository:

| Label | Meaning |
|-|-|
| `plugin-review` | General review discussion for a plugin |
| `plugin-rating:5` … `plugin-rating:1` | Community rating signal |
| `plugin-report` | Safety / malware / break report (triage ASAP) |

Reference the plugin id in the issue title: `[plugin.protected-service-drift] …`.

## Deny-list (kill switch)

Edit `plugins/deny-list.txt` (one plugin id per line). The app loads this file and
refuses to register denied plugins even if they remain in the tree.

## Auto-update

Plugin catalog refresh is **tied to the app update toggle**:
`CheckForUpdatesEnabled` must be on. Otherwise the app only uses the plugins
shipped with the installed build (offline).
