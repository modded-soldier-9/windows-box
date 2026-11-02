## Summary

<!-- What does this PR change, and why? -->

## Safety checklist

- [ ] No host mutations in unit tests (fakes / VMs only)
- [ ] Undo path exists (journal priors) or change is explicitly one-way + confirmed
- [ ] Prefer Manual over Disabled for services (risk ≤ 2 for Disabled)
- [ ] No phone-home, no resident agent, no tray / always-on updater
- [ ] Catalog entries cite sources and set risk 0–4 + touch_policy

## Test plan

- [ ] `dotnet test tests/WindowsBox.Tests`
- [ ] `wbox catalog validate` (if catalog touched)
- [ ] Windows CI / AOT publish green (if engine/CLI touched)

## Stacking

Base branch (if stacked):
