# Shared engine

1. **Detect** — `ISystemProbe` (edition, build, laptop, MDM, features, …)
2. **Preview / plan** — diff, risk 0–4, expected gain MB, dependency checks
3. **Snapshot** — measured priors + registry export (never assumed defaults)
4. **Apply** — `IServiceController` / Elevator batch
5. **Verify** — tweaks stuck? `ApplyVerifier` + `StartupVerifier`
6. **Rollback** — replay priors; offline `.reg`/`.cmd` kit
7. **Drift** — compare journal targets to live state (e.g. after Windows Update)

Adapters implement interfaces; **fakes** power Linux CI. Never run applies against a real host from unit tests.
