DOTNET ?= dotnet
SOLUTION := ItEquipmentCheckout.slnx
CONFIGURATION ?= Release

.PHONY: setup format lint test build run up down logs clean migration

setup:
	$(DOTNET) tool restore
	$(DOTNET) restore $(SOLUTION) --locked-mode

format:
	$(DOTNET) format $(SOLUTION)

lint:
	$(DOTNET) format $(SOLUTION) --verify-no-changes --no-restore
	$(DOTNET) build $(SOLUTION) --configuration $(CONFIGURATION) --no-restore

test:
	$(DOTNET) test $(SOLUTION) --configuration $(CONFIGURATION) --no-restore \
		--collect:"XPlat Code Coverage"

build:
	$(DOTNET) publish src/ItEquipmentCheckout.Web \
		--configuration $(CONFIGURATION) \
		--output artifacts/app

run:
	$(DOTNET) run --project src/ItEquipmentCheckout.Web

up:
	docker compose up --build --detach
	docker compose ps

down:
	docker compose down

logs:
	docker compose logs --follow app

clean:
	docker compose down --volumes --remove-orphans
	$(DOTNET) clean $(SOLUTION)
	rm -rf artifacts TestResults coverage

migration:
	test -n "$(name)"
	$(DOTNET) ef migrations add "$(name)" \
		--project src/ItEquipmentCheckout.Web \
		--startup-project src/ItEquipmentCheckout.Web \
		--output-dir Data/Migrations
