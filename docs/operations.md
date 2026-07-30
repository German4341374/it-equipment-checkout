# Operations guide

## Health

`GET /health` checks that the process can query SQLite:

```bash
curl --fail http://localhost:8080/health
```

Expected response:

```json
{"status":"healthy","checks":{"sqlite":"healthy"}}
```

## Data location and backup

Local runs use `src/ItEquipmentCheckout.Web/Data/checkout.db`. Docker Compose stores the same database in the `checkout_data` named volume.

SQLite backups must copy a consistent database. For a small local deployment, stop the application before copying the database:

```bash
docker compose stop app
docker run --rm \
  --volume it-equipment-checkout_checkout_data:/source:ro \
  --volume "$PWD/backups:/backup" \
  alpine:3.22.1 \
  cp /source/checkout.db /backup/checkout.db
docker compose start app
```

Restore only while the application is stopped:

```bash
docker compose stop app
docker run --rm \
  --volume it-equipment-checkout_checkout_data:/target \
  --volume "$PWD/backups:/backup:ro" \
  alpine:3.22.1 \
  cp /backup/checkout.db /target/checkout.db
docker compose start app
curl --fail http://localhost:8080/health
```

The Compose project name may change the volume prefix. Confirm it with `docker volume ls` before backup or restore.

## Reset demo data

This operation permanently removes the local Docker database:

```bash
docker compose down --volumes
docker compose up --build --detach
```

The startup seed runs only against an empty database.

## Troubleshooting

### Application is unhealthy

1. Read `docker compose ps`.
2. Inspect `docker compose logs app`.
3. Confirm the data volume is writable by the container's non-root user.
4. Confirm no corrupt or incompatible SQLite file was restored.

### A checkout returns HTTP 409

Read the Problem Details `detail` field. Common causes are an existing active checkout or equipment in `Repair` or `Retired`. Inspect `/api/equipment/{id}` and `/api/checkouts?active=true`.

### A create or import reports a uniqueness conflict

Inventory and serial numbers are case-insensitively unique. Search the equipment register before retrying with another identifier.

### Migrations do not match the model

Run:

```bash
dotnet ef migrations has-pending-model-changes \
  --project src/ItEquipmentCheckout.Web \
  --startup-project src/ItEquipmentCheckout.Web
```

Create and review a migration if the command reports pending changes. Never edit the model snapshot by hand.
