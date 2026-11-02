# Security Policy

## Supported versions

| Version | Supported |
|-|-|
| `main` (pre-release / M1) | Yes |
| Signed release tags | Yes, once published |
| Unsigned CI artifacts | Best-effort only |

## Reporting a vulnerability

**Do not open a public GitHub issue for security bugs.**

Email the maintainers (Ron / repo owners) via GitHub Security Advisories on this
repository: **Security → Advisories → Report a vulnerability**.

Please include:

1. Impact (privilege escalation, unexpected host mutation, supply chain, etc.)
2. Reproduction steps (VM only — never ask us to run tweaks on a production host)
3. Affected commit SHA or release tag
4. Whether a fix is already proposed

We aim to acknowledge within 72 hours.

## Hard rules for this project

- The app must **never phone home**.
- No resident Windows service, tray icon, or always-on updater.
- Elevator elevates **only for a batch** and exits.
- Unit tests use **fakes** — never mutate a CI runner or developer host.
- Signing secrets stay in CI secrets / SignPath / Azure Trusted Signing — never commit PFXes.

## Supply chain

Report compromised dependencies, malicious catalog PRs, or signing pipeline
issues through the same private channel.
