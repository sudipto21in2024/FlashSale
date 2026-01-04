# Booking Flow (Flash Sale)

This document describes the lifecycle of a user purchase during a high-demand event.

## 1. The Request (Synchronous Fast Path)
1. **User Action**: Clicks "Buy Now" on the Frontend.
2. **API Endpoint**: `POST /api/v1/tickets/book`.
3. **Validation**: `BookTicketCommandValidator` checks inputs.
4. **Redis Check (The Gatekeeper)**:
   - `RedisTicketCacheService.TryReserveTicketAsync(ticketId)`.
   - Uses atomic `DECR` or RedLock logic to decrement the available count in memory.
   - **IF** Count < 0: Fails immediately (Fast Fail). User gets "Sold Out".
   - **IF** Count >= 0: Proceeds.
5. **Kafka Publish**:
   - The API does **NOT** write to the DB yet.
   - Publishes a `BookingIntent` event to the `booking-intents` Kafka topic.
6. **Response**: 
   - Returns `202 Accepted` to the UI instantly.
   - UI shows "Processing...".

## 2. The Persistence (Asynchronous Slow Path)
1. **Worker Service**: `BookingWorker` is listening to `booking-intents`.
2. **Consume**: Best-effort processing.
3. **Database Write**:
   - Creates a `Booking` entity with `Confirmed` status.
   - Saves to PostgreSQL via `BookingRepository`.
4. **Concurrency Safety**:
   - Since Redis already guaranteed distinct slots, DB inserts are mostly safe.
   - Optimistic Concurrency on `Ticket` entity (RowVersion) acts as a final safety net if we were updating the Ticket row directly (optional in this flow if relying purely on Redis for count).

## 3. The Update (Real-time Feedback)
1. **SignalR**:
   - The Frontend is listening to `InventoryUpdated`.
   - When the `RedisTicketCacheService` decrements, it could optionally broadcast the new count (or a separate recurring job broadcasts counts every 500ms).
2. **User Notification**:
   - In a full production app, the Worker would publish a "BookingConfirmed" event.
   - The API would listen to this and send a specific SignalR message to *that specific User* saying "Success!".
