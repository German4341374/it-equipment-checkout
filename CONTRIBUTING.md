# Contributing

Thank you for improving IT Equipment Checkout.

## Development setup

1. Install the .NET SDK version declared in `global.json`.
2. Run `dotnet tool restore`.
3. Run `dotnet restore ItEquipmentCheckout.slnx --locked-mode`.
4. Create a focused branch from `main`.

## Quality checks

Run these commands before opening a pull request:

```bash
dotnet format ItEquipmentCheckout.slnx --verify-no-changes --no-restore
dotnet build ItEquipmentCheckout.slnx --configuration Release --no-restore
dotnet test ItEquipmentCheckout.slnx --configuration Release --no-build --no-restore
dotnet ef migrations has-pending-model-changes \
  --project src/ItEquipmentCheckout.Web \
  --startup-project src/ItEquipmentCheckout.Web \
  --no-build --configuration Release
```

Add or update tests for behavioral changes. Unit tests belong with pure domain rules; HTTP, persistence, and serialization behavior belongs in integration tests.

## Commit convention

Use Conventional Commits:

```text
<type>(optional-scope): short imperative description
```

Common types are `feat`, `fix`, `docs`, `test`, `refactor`, `build`, and `ci`.

## Database changes

Do not edit generated migration files manually. Change `CheckoutDbContext`, then create and review a migration:

```bash
dotnet ef migrations add DescriptiveName \
  --project src/ItEquipmentCheckout.Web \
  --startup-project src/ItEquipmentCheckout.Web \
  --output-dir Data/Migrations
```

Confirm that existing data has a safe upgrade path and that the model has no pending changes.

## Pull requests

- Keep the change focused and explain the user-visible effect.
- Include tests and documentation where appropriate.
- Never commit local databases, secrets, real employee records, or generated build output.
- Ensure every CI job passes.
