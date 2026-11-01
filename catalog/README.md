# Catalogs

Machine-readable optimization catalogs for Windows Box.

## Services

- YAML entries: [`services/`](services/)
- JSON Schema (editor validation): [`schemas/service.schema.json`](schemas/service.schema.json)
- Human research notes: [`../docs/catalogs/services.md`](../docs/catalogs/services.md)

Validate:

```bash
dotnet run --project ../src/WindowsBox.Cli -- catalog validate --catalog services
```

Rules of thumb:

- Prefer **Manual** over **Disabled**
- `risk: 0` ⇒ `touch_policy: never`
- Protected / Medic / Defender / firewall stay hands-off
- Every entry needs `sources`
