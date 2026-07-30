# Repository Guide

## Scope

Keep this application a compact equipment checkout system. Do not add authentication, roles, email, microservices, or external infrastructure without an approved design discussion.

## Architecture boundaries

- Put pure entities and lifecycle rules in `ItEquipmentCheckout.Core`.
- Put HTTP, Razor Pages, persistence, DTOs, and migrations in `ItEquipmentCheckout.Web`.
- Never expose EF Core entities directly from API controllers.
- Enforce critical invariants in both domain code and database constraints where possible.

## Working agreement

- Use English in source, comments, documentation, configuration, and commits.
- Use Conventional Commits.
- Never commit secrets, real employee data, local SQLite files, or build artifacts.
- Add unit tests for domain rules and integration tests for API or persistence behavior.
- Run formatting, Release build, tests, migration validation, and dependency audit before submitting changes.
