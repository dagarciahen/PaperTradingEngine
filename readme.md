# Distributed Paper Trading Engine

An event-driven, distributed paper trading platform that simulates high-volume
order execution against **live cryptocurrency prices** from Binance.

Orders are simulated. Prices are real. The matching engine evaluates simulated
orders against the real market and records the executions — all asynchronously,
fault-tolerantly, and horizontally scalable.

> See [Project Status](#project-status).

---

## Overview

The system is built as three independent microservices communicating
exclusively through a message broker. No service calls another directly.

```
┌───────────────────────┐
│        Binance        │
│   WebSocket (public)  │
└───────────┬───────────┘
            │ @trade stream
            ▼
┌───────────────────────┐
│ Market Data Ingestion │  Worker Service
│  (live price stream)  │
└───────────┬───────────┘
            │ publish LivePriceUpdatedEvent
            ▼
      ┌───────────┐
      │ RabbitMQ  │  exchange: live_prices (fanout)
      └─────┬─────┘
            │
            ├──────────────────────┐
            ▼                      ▼
   ┌─────────────────┐    ┌─────────────────┐
   │ execution_queue │    │ reporting_queue │
   └────────┬────────┘    └─────────────────┘
            │
            ▼
   ┌─────────────────┐
   │ ExecutionWorker │  Worker Service
   │  Matching Engine│  (stateless, N replicas)
   └────────┬────────┘
            │
            ▼
   ┌─────────────────┐
   │   PostgreSQL    │  single source of truth
   └─────────────────┘
            ▲
            │
   ┌─────────────────┐
   │ Order Simulator │  ASP.NET Core Web API
   │       API       │  POST /orders, /orders/burst
   └─────────────────┘
```

---

## Components

### 1. Market Data Ingestion

**Type:** .NET Worker Service (BackgroundService)

Maintains a persistent WebSocket connection to Binance and publishes every
incoming trade as an event.

- Connects to the combined stream endpoint:
  `wss://stream.binance.com:9443/stream?streams=btcusdt@trade/ethusdt@trade`
- Parses each trade into a `LivePriceUpdatedEvent` and publishes it to the
  `live_prices` fanout exchange
- Handles fragmented WebSocket frames with a `MemoryStream` accumulator
- Auto-reconnects on failure with a backoff delay (Binance closes sockets every 24h)


### 2. Order Simulator API

**Type:** ASP.NET Core Web API

Entry point for users and load-testing scripts.

- `POST /orders` — insert a single order with status `Pending`
- `POST /orders/burst` — bulk-insert up to thousands of orders in one round-trip
- `GET /orders/{orderId}` — query order status
- Returns `202 Accepted` immediately; processing happens asynchronously

Bulk inserts use PostgreSQL's `COPY ... FROM STDIN (FORMAT BINARY)` via Npgsql,
which is an order of magnitude faster than row-by-row inserts.

### 3. Execution Worker

**Type:** .NET Worker Service (BackgroundService)

The consumer and matching engine. **Stateless by design**, so it can be scaled
horizontally — run 5 or 10 replicas, and RabbitMQ will round-robin messages
across all of them.

- Consumes `execution_queue` with **manual acknowledgement**
- Evaluates each pending order against the latest live price
- Updates order state and records executions in PostgreSQL

**Manual ack:** messages are acknowledged *after* processing. If a
worker crashes mid-execution, RabbitMQ requeues the message and another replica
picks it up. With auto-ack, the message would be lost permanently.

---

## Infrastructure

### RabbitMQ

Acts as the central nervous system, decoupling data ingestion from heavy
processing. If the execution engine goes down, messages queue up and are not
lost.

- Exchange `live_prices` — **fanout**, durable
- Queue `execution_queue` — durable
- Queue `reporting_queue` — durable

### PostgreSQL

The single source of truth. Stores order history and transactional state under
ACID guarantees.

Schema (`docker/init.sql`), under the `pte` schema:

| Table | Purpose |
|---|---|
| `pte.orders` | Order lifecycle and state |
| `pte.executions` | Execution records (FK to orders) |
| `pte.processed_messages` | Idempotency ledger by `message_id` |

---

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 |
| Services | ASP.NET Core Web API, Generic Host / BackgroundService |
| Messaging | RabbitMQ 3 (RabbitMQ.Client 7.x) |
| Database | PostgreSQL 17 |
| Data access | Dapper + Npgsql |
| Containerization | Docker Compose |

---

## Getting Started

### Prerequisites

- .NET 10 SDK
- Docker Desktop (with WSL2 backend on Windows)

### Run the message broker

```bash
docker compose -f docker/docker-compose.yml up -d
```

RabbitMQ Management UI: http://localhost:15672 (`user` / `password`)

### Run the services

```bash
# Terminal 1 — consumer
dotnet run --project src/ExecutionWorker

# Terminal 2 — producer
dotnet run --project src/MarketDataIngestion
```

You should see live BTC and ETH trades flowing from Binance, published by the
ingestion service and consumed by the execution worker:

```
info: MarketDataIngestion.Worker[0]
      Connected to Binance: wss://stream.binance.com:9443/...
info: MarketDataIngestion.Worker[0]
      BTCUSDT 95123.45 qty 0.00234 @ 14:23:11.402
info: ExecutionWorker.Worker[0]
      [EXECUTION] BTCUSDT 95123.45 qty 0.00234 @ 14:23:11.402
```

---

## Configuration

Configuration follows the standard .NET layering, with environment variables
overriding `appsettings.json`. This is what allows the same binary to run
locally and in Docker without code changes.

```jsonc
{
  "RabbitMQ": {
    "HostName": "localhost",              // "rabbitmq" inside Docker
    "Port": 5672,
    "UserName": "user",
    "Password": "password",
    "LivePricesExchangeName": "live_prices",
    "ExecutionQueueName": "execution_queue",
    "ReportingQueueName": "reporting_queue"
  },
  "Binance": {
    "BaseUrl": "wss://stream.binance.com:9443",
    "Streams": [ "btcusdt@trade", "ethusdt@trade" ]
  }
}
```

To override in Docker, use double underscores:

```yaml
environment:
  RabbitMQ__HostName: rabbitmq
```
---

## Design Decisions


The clearest boundary in this system is that Binance supplies prices while orders remain entirely simulated; paper trading only means anything if the money is fake but the market is real, so inventing prices would make every result meaningless, and for the same reason the public market-data API is used rather than the trading API, which requires credentials and moves real funds. 
Dapper was chosen over EF Core because the bulk-insert path relies on PostgreSQL's `COPY` binary protocol, a level of throughput that EF Core cannot match. Messages ar acknowledged manually rather than automatically, which gives at-least-once delivery: if a worker crashes mid-execution the message is requeued instead of lost, at the cost of requiring idempotent consumers. That guarantee is only useful because the workers themselves are stateless, meaning any replica can process any message, which is what allows horizontal scaling and rolling deploys without coordination. Configuration is externalized rather than hardcoded so the same build artifact runs locally and in Docker with no code changes, and the DTOs modelling Binance's wire format are marked `internal` because that format is
an implementation detail of a single service, not a contract shared across the system.

---

## Project Status

### ✅ Done

- [x] Solution and 5 projects (`Contracts`, `Infrastructure`, `MarketDataIngestion`, `ExecutionWorker`, `OrderSimulatorApi`)
- [x] RabbitMQ in Docker Compose
- [x] `RabbitMQInfrastructure`: connection, channel, durable exchange + queues
- [x] **Real Binance WebSocket integration** — combined streams, reconnect loop, fragmented-frame handling
- [x] `MarketDataIngestion` publishes `LivePriceUpdatedEvent` to `live_prices`
- [x] `ExecutionWorker` consumes `execution_queue` with **manual ack**
- [x] Config externalized with startup validation
- [x] Domain contracts: `OrderType`, `OrderSide`, `OrderStatus`, versioned events
- [x] PostgreSQL schema (`init.sql`): orders, executions, processed_messages
- [x] `OrdersController`: `POST /orders`, `POST /orders/burst`, `GET /orders/{id}`
- [x] Bulk insert via `COPY ... FROM STDIN (FORMAT BINARY)`

### 🚧 In progress

- [ ] `OrderSimulatorApi/Program.cs` — register controllers, repository, and `DatabaseSettings`
- [ ] `OrderRepository` — Dapper queries and interface conformance
- [ ] PostgreSQL service in `docker-compose.yml`

### ❌ Pending

- [ ] `pending_orders_queue` (durable) and `OrderCreatedEvent` publishing from the API
- [ ] **Matching engine** evaluate orders against live prices, mark as `Executed`
- [ ] Idempotency by `message_id` using `pte.processed_messages`
- [ ] Dead-letter queue and retry policy
- [ ] Dockerfiles for all three services
- [ ] Automated tests
- [ ] Load testing with k6 and horizontal scale-out demonstration

---

## Roadmap

1. Complete the order ingestion pipeline (API → `pending_orders_queue` → worker)
2. Implement the matching engine with price caching per symbol
3. Add idempotent consumption and a dead-letter queue
4. Build a load generator simulating hundreds of users with realistic order profiles
5. Benchmark with k6 and demonstrate horizontal scaling
6. Containerize all services and deploy

---

## License

MIT