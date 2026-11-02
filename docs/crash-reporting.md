# Crash reporting

## Local dumps (always)

When Windows Box catches an unhandled exception, it writes a JSON dump under:

`%ProgramData%\WindowsBox\crashes\<id>.json`

Dumps include exception type, message, stack (when available), app version, and OS description.
They never leave the machine unless you opt in to upload.

## Upload (opt-in, off by default)

Settings → Privacy / Crash reports → **Upload crash dumps**.

- Default: **off**
- When off: zero network I/O from the crash reporter
- When on: the dump JSON may be POSTed to the configured crash endpoint

See also `docs/network-safeguards.md`.
