# AtlasBank

[![AtlasBank.Maui CI](https://github.com/queennduji/atlas-bank/actions/workflows/maui-ci.yml/badge.svg)](https://github.com/queennduji/atlas-bank/actions/workflows/maui-ci.yml)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

A banking system modeled as event-driven microservices in .NET – built to practice the kind of
distributed-systems patterns real banking platforms use: an API gateway, service-to-service gRPC,
async domain events over RabbitMQ, database-per-service, and centralized structured logging.

![AtlasBank architecture](docs/architecture.svg)

For the request-flow sequence diagram (idempotency + compensating-transaction
reversal) and the full bounded-context breakdown, see
[`docs/architecture.md`](docs/architecture.md).

## Services

| Service | Responsibility | Talks to |
|---|---|---|
| **API Gateway** | Routes client requests, validates JWTs, rate-limits per IP | YARP reverse proxy → all 7 services |
| **Customer Service** | Customer records | – |
| **Account Service** | Account balances, credit/debit | gRPC (read) → Customer Service |
| **Transaction Service** | Processes deposits/withdrawals/transfers | gRPC (write, no auto-retry – see Resilience below) → Account Service; publishes `TransactionCompletedEvent` to RabbitMQ |
| **Ledger Service** | Double-entry ledger postings | Consumes `TransactionCompletedEvent` |
| **Notification Service** | Customer notifications | gRPC (read) → Account Service, Customer Service; consumes transaction/card events |
| **Card Service** | Card issuance, freeze/unfreeze, spending limits | gRPC (read) → Account Service, Customer Service; publishes `CardIssuedEvent` |
| **Statement Service** | Account statements | gRPC (read) → Account Service, Customer Service, Transaction Service |

## Tech stack

- **.NET 10** / ASP.NET Core, **EF Core** + PostgreSQL (one database per service)
- **YARP** reverse proxy for the API Gateway, with JWT auth via **Keycloak** (OAuth2/OIDC) and per-IP rate limiting
- **gRPC** for synchronous service-to-service calls, **RabbitMQ** for async domain events
- **Resilience**: Polly-based timeout, retry-with-backoff, and circuit breaker on every gRPC
  client – reads get the full pipeline, but a client carrying a non-idempotent write (Account
  Service's Credit/Debit) drops automatic retry entirely, so a lost response after the write
  already landed can't get silently re-applied
- **Optimistic concurrency** on account balances (Postgres `xmin`, no extra locking
  infrastructure) and client-supplied **idempotency keys** on deposit/withdraw/transfer, so a
  retried request is answered with the original result instead of moving money twice
- **Serilog → Seq** for centralized structured logging
- **Docker Compose** for local orchestration
- **xUnit**, **FluentAssertions**, and **Testcontainers** (real PostgreSQL containers) for integration tests

## Clients

Two clients talk to the same API Gateway – sign up, open accounts, move money, issue cards,
and pull statements:

- **[`frontend/`](frontend)** – React + TypeScript, for the browser. See
  [`frontend/README.md`](frontend/README.md).
- **[`src/Clients/AtlasBank.Maui/`](src/Clients/AtlasBank.Maui)** – .NET MAUI, for
  Android/iOS/Mac Catalyst/Windows. OAuth2 Authorization Code + PKCE against Keycloak (no
  embedded WebView, no password ever touching the app's own code), with its API client, DTOs,
  and auth state machine factored into a UI-framework-agnostic library
  ([`AtlasBank.Clients.Core`](src/Clients/AtlasBank.Clients.Core)) that a planned WPF client
  will reuse rather than duplicate. See
  [`src/Clients/AtlasBank.Maui/README.md`](src/Clients/AtlasBank.Maui/README.md).

## Getting started

```bash
git clone https://github.com/queennduji/atlas-bank.git
cd atlas-bank
docker-compose up --build
```

This brings up everything – Keycloak, PostgreSQL, RabbitMQ, Seq, all seven services, the
API Gateway, and the frontend. The frontend is at `http://localhost:3000`, the gateway at
`http://localhost:5000`, Seq's UI at `http://localhost:8081`.

> PostgreSQL replaced SQL Server so the whole stack can run on ARM (e.g. Oracle Cloud's
> free-tier VMs) – SQL Server's Docker image is amd64-only.

For active frontend development, run it outside Docker instead so you get hot reload –
see [`frontend/README.md`](frontend/README.md).

## Testing

```bash
dotnet test
```

Integration tests spin up real PostgreSQL containers via Testcontainers rather than mocking the database.

## License

MIT – see [LICENSE](LICENSE).
