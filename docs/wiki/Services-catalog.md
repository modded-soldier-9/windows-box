# Services catalog

## Apply-ready (YAML)

- Path: `catalog/services/*.yaml`
- Schema: `catalog/schemas/service.schema.json`
- Validated by `wbox catalog validate` in CI

## Full inventory (290)

- Path: `docs/catalogs/services-full.json`
- Loaded by `FullServicesInventoryLoader` for browse/advise
- Defaults mostly **unverified** until the VM harness harvests them (`honesty_label: unproven`)

## Risk scale

| Risk | Meaning |
|-|-|
| 0 | Never touch |
| 1 | Safe Manual |
| 2 | Disable only if unused |
| 3 | Situational / visible break |
| 4 | Breaks updates, Store, login, or security |
