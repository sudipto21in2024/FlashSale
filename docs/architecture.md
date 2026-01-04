# BoltTickets Architecture

## Overview
BoltTickets is designed as a **High-Concurrency Flash Sale System**. It decouples the high-write traffic of ticket sales from the persistence layer using a message broker and a distributed cache.

## Tech Stack
- **Framework**: .NET 9 ASP.NET Core
- **Language**: C# 13
- **Frontend**: React (TypeScript, Vite, Tailwind)
- **Database**: PostgreSQL (Entity Framework Core)
- **Cache**: Redis (StackExchange.Redis)
- **Messaging**: Kafka (Confluent.Kafka)
- **Real-time**: SignalR
- **Observability**: OpenTelemetry, Jaeger, Prometheus, Grafana

## Layers (Clean Architecture)

### 1. Domain Layer (`BoltTickets.Domain`)
The heart of the business logic. Contains entities, enums, exceptions, and repository interfaces.
- **Key Entities**: `Ticket` (Aggregate Root), `Booking`.
- **Rules**: Ticket availability logic (`Reserve()`).

### 2. Application Layer (`BoltTickets.Application`)
Orchestrates business logic using CQRS (Command Query Responsibility Segregation).
- **Libraries**: MediatR, FluentValidation.
- **Commands**: `BookTicketCommand`.
- **Interfaces**: `ITicketCacheService`, `IBookingProducer`.

### 3. Infrastructure Layer (`BoltTickets.Infrastructure`)
Implements interfaces defined in Domain/Application.
- **Persistence**: `ApplicationDbContext` (PostgreSQL).
- **Services**: `RedisTicketCacheService` (RedLock), `KafkaBookingProducer`.

### 4. API Layer (`BoltTickets.API`)
The entry point for Clients.
- **Middleware**: Global Exception Handling, Serilog Access Logs.
- **Controllers**: RESTful endpoints.
- **Hubs**: SignalR `TicketHub`.

### 5. Worker Service (`BoltTickets.Worker`)
Background consumer.
- Listens to Kafka `booking-intents`.
- Persists bookings to PostgreSQL.
- Ensures data consistency only after the "rush" is buffered.
