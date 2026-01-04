# Change Data Capture (CDC) Implementation Plan for PostgreSQL

## Overview
This plan outlines the architectural and configuration changes required to implement Change Data Capture (CDC) from PostgreSQL in the BoltTickets system using Debezium and Kafka Connect. The goal is to capture INSERT/UPDATE/DELETE events on the `Bookings` and `Tickets` tables and publish them as events to Kafka topics for event-driven processing (e.g., auditing, analytics, or external system synchronization).

## Current System Context
- **Data Flow**: API → Redis (cache) → Kafka (async persistence) → Worker → PostgreSQL (via EF Core)
- **Key Entities**: `Bookings` (tracks user bookings) and `Tickets` (inventory management)
- **Infrastructure**: PostgreSQL, Kafka, Redis, .NET services
- **CDC Goal**: Capture changes from `Bookings` and `Tickets` tables to enable downstream event processing.

## Recommended CDC Approach
Use **Debezium PostgreSQL Connector** via Kafka Connect. This integrates seamlessly with the existing Kafka infrastructure and provides reliable, non-invasive change capture. Alternatives considered:
- **PostgreSQL Triggers**: Custom triggers to publish events – flexible but requires custom code and maintenance.
- **Application-level CDC**: EF Core change tracking – simple but misses out-of-band changes.

## Detailed Changes Required

### 1. Assess and Define CDC Scope
- **Tables to Monitor**: `public.bookings`, `public.tickets`
- **Event Types**: INSERT, UPDATE, DELETE operations
- **Event Schema**: Use Debezium's default JSON/Avro format (includes before/after states, operation type, timestamp, LSN)
- **Downstream Consumers**: Define use cases (e.g., analytics, audit logs). If internal consumption needed, add Kafka consumers in Worker or new service.

### 2. PostgreSQL Configuration for Logical Replication
- Update `postgresql.conf`:
  - `wal_level = logical`
  - `max_replication_slots = 5` (or higher)
  - `max_wal_senders = 5`
- Create replication user with `REPLICATION` privilege.
- Restart PostgreSQL.
- **Impact**: No code changes; DB-level configuration.

### 3. Kafka Connect Setup with Debezium
- Install Kafka Connect (Docker or standalone).
- Add Debezium PostgreSQL connector plugin.
- Connector Configuration:
  - Name: `bolttickets-postgres-connector`
  - Database: Connection details (host, port, DB name, replication user)
  - Tables: `public.bookings`, `public.tickets`
  - Topics: `cdc.bookings`, `cdc.tickets`
  - Snapshot: `initial` (capture existing data)
  - Heartbeat: Enabled for monitoring
- Deploy via REST API or config file.
- Update `docker-compose.yml`: Add Kafka Connect service with Debezium image.

### 4. Kafka Topic and Schema Management
- Create topics `cdc.bookings` and `cdc.tickets` with appropriate partitions/replication.
- Integrate with Schema Registry for Avro schemas if available.
- Align with existing topic conventions (e.g., `booking-intents`).

### 5. Consumer Implementation for CDC Events
- If processing needed (e.g., cache invalidation):
  - Add consumers in Worker or dedicated service.
  - Topics: `cdc.bookings`, `cdc.tickets`
  - Logic: Parse events, handle operations.
- If external-only, no .NET changes required.

### 6. Monitoring and Observability Updates
- Add metrics: CDC lag, throughput, errors (Prometheus integration).
- Update logging: Structured logs for CDC events.
- Alerts: Replication slot lag, connector health.

### 7. Security and Access Control
- Replication user: Read-only on monitored tables.
- Encrypt Kafka traffic (SSL/TLS).
- Update firewall for PostgreSQL replication ports.

### 8. Testing and Validation
- Test with sample operations: Verify events in Kafka.
- End-to-end: Ensure no data loss.
- Performance: Monitor WAL and Kafka impact.

### 9. Documentation and Maintenance
- Document setup, configs, troubleshooting.
- Backup: Include replication slots.
- Version Control: Store configs in Git.

## Potential Challenges and Mitigations
- **Performance**: Monitor WAL overhead; scale PostgreSQL.
- **Schema Changes**: Test DDL handling.
- **Consistency**: Use timestamps for event ordering.
- **Cost**: Debezium is open-source; provision resources.

## Estimated Effort
- Configuration: 2-3 days
- Testing: 1-2 days
- No breaking changes; additive feature.

## Next Steps
Review this plan. If approved, proceed to implementation in Code mode. If changes needed, refine the todo list.