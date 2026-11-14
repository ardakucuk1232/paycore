# 0001. Layered architecture with a separate worker host

- Status: accepted
- Date: 2026-11-13

## Context

Paycore handles money movement, so business rules (payment state transitions, ledger balance, refund limits) must be testable without a database or HTTP server. The system also runs background work: outbox dispatch, webhook delivery, subscription renewal and bank reconciliation.

## Decision

- The backend follows Clean Architecture with a service layer: `Domain`, `Application`, `Infrastructure`, and the hosts `Api`, `Worker` and `Migrator`.
- `Domain` has no package dependencies. `Application` depends only on `Domain`. `Infrastructure` implements the ports that `Application` defines.
- Interfaces exist only where a dependency crosses a layer boundary (for example the bank gateway). There is no generic repository; EF Core's `DbContext` already provides unit of work and change tracking.
- No MediatR or CQRS. Application services are plain classes called from endpoints and background jobs.
- Background work runs in a separate `Worker` process that shares `Infrastructure` with the API. The two scale and fail independently, but they share one database and one deployment unit, so this is not a microservice split.
- A project is added to the solution in the phase that gives it real content, not as an empty placeholder.

## Consequences

- Domain rules are covered by fast unit tests; persistence and HTTP behavior are covered by integration tests against real PostgreSQL and Redis (Testcontainers).
- Two hosts mean two container images and two sets of configuration.
- Without a mediator, cross-cutting behavior (validation, auditing) is applied explicitly in services and middleware rather than through pipeline behaviors.
