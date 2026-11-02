# Modules

## Services (flagship)

- Apply-ready YAML (`catalog/services`, ~88) + full inventory (`docs/catalogs/services-full.json`, 290)
- Manual before Disabled; policy route for protected services; per-user templates require confirm
- Profiles: safe, balanced, gamer, laptop, office, privacy, developer, minimal

## Startup

Autoruns-inspired locations: Run keys, startup folders, scheduled tasks, services, drivers, shell extensions, Winlogon/AppInit — each with risk 0–4 and Hide Microsoft filter.

## Network privacy

`wbox netcheck` — one-shot “who phoned home” TCP snapshot; process exits.

## Verify task (opt-in)

`\WindowsBox\Verify` runs `wbox verify --quiet` ~10 min after Windows Update event 19 and 2–5 min after logon. Notify only on drift; re-apply is opt-in.
