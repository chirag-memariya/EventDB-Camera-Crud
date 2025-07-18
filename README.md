# EventStoreDB

## 1: Executive Summary - What is EventStoreDB?

EventStoreDB is a specialized **NoSQL database** engineered for **Event Sourcing** and Event-Driven Architectures. Unlike traditional databases that store only the current state, EventStoreDB persists an immutable, ordered log of all "events" – facts that have occurred within your business domain. This fundamental difference provides a complete audit trail, enables the reconstruction of any past system state, and facilitates the development of highly scalable, decoupled systems. It is not a relational database (SQL) or a traditional document store.

## 2: How EventStoreDB Works

* **Data Format:** Events are stored in a raw, serialized format, commonly JSON or Protobuf, along with essential metadata (e.g., event type, timestamp, event ID).
* **Streams:** Events are logically organized into append-only "streams," which are ordered sequences of events pertaining to a specific entity or aggregate (e.g., all events for `camera-123`).
* **Core Principle:** Instead of overwriting or deleting data, every change in the system is recorded as a new, immutable event. This append-only nature preserves a full, verifiable history of all system activities.

## 3: Why Use EventStoreDB? Key Benefits

* **Full Auditability:** Every system change is recorded as an immutable event, providing an undeniable, granular audit trail crucial for compliance, debugging, and forensic analysis.
* **Time Travel:** The complete event log allows for the reconstruction of the system's state at any point in the past, enabling historical analysis, debugging, and the ability to re-evaluate past business decisions.
* **Scalability:** Designed for high write throughput, EventStoreDB is ideal for high-volume event data. Its append-only nature and efficient indexing support fast reads of individual streams.
* **Decoupled Architectures (Microservices):** EventStoreDB acts as a central hub for events, enabling different services to react to changes asynchronously and independently. This fosters loosely coupled, more resilient architectures.
* **Business Insight:** Events capture the true intent and sequence of business actions, leading to richer data for advanced analytics, business intelligence, and informed decision-making.
* **Future-Proofing:** The immutable event log provides immense flexibility. As business requirements evolve, you can easily adapt by replaying events to build new read models or re-evaluate past domain logic without costly data migrations.

## 4: Key Use Cases

* **Core of Event-Sourced Systems:** Serves as the durable log and central repository for all domain events, driving the application's state.
* **Real-time Event Processing:** Powers responsive, event-driven applications that react instantaneously to business changes.
* **Complex Business Logic:** Simplifies the management of complex business processes and long-running sagas by modeling state transitions as events.
* **Fraud Detection & Anomaly Analysis:** The detailed, historical event data is crucial for identifying patterns, detecting anomalies, and implementing real-time fraud detection.
* **Data Auditing & Compliance:** The immutable history naturally meets stringent regulatory requirements for data integrity and traceability.

## 5: Open Source vs. Enterprise (KurrentDB)

EventStoreDB offers both open-source and commercial versions.

| Feature             | Open Source EventStoreDB                                                               | Enterprise EventStoreDB (now part of KurrentDB)                                                |
| :------------------ | :------------------------------------------------------------------------------------- | :--------------------------------------------------------------------------------------------- |
| **Licensing** | Event Store License (based on BSD 3-Clause, shifting to ESLv2 for newer versions)      | Commercial License                                                                             |
| **Core Functionality** | Full event sourcing capabilities, immutable log, projections, subscriptions, clustering. | Same core functionality plus enterprise-focused features.                                      |
| **Enterprise Features** | Limited/None                                                                           | LDAP integration, advanced monitoring, correlation event sequence visualization, management CLI. |
| **Support** | Community forum, GitHub issues                                                         | Dedicated support portal, SLAs, professional services.                                         |
| **Deployment** | Self-hostable, Docker containers                                                       | Self-hostable, Event Store Cloud (SaaS offering).                                              |
| **Target Audience** | Developers, small to medium teams                                                      | Large enterprises, mission-critical systems requiring robust support and advanced features.    |

**Key Takeaway for Managers:** While the open-source version is powerful and suitable for many use cases, the enterprise offering provides critical features and support for production environments where stability, security, advanced operational capabilities, and guaranteed uptime are paramount.

## 6: Integration and Ecosystem (.NET)

* **Robust .NET Client SDK:** EventStoreDB offers a well-documented and robust .NET client SDK, ensuring seamless integration for .NET applications and adherence to common .NET patterns like dependency injection.
* **Complementary to Other Databases:** EventStoreDB serves as the "source of truth." For optimized querying and reporting, read models (projections) are typically built in other databases (e.g., PostgreSQL, SQL Server, Elasticsearch, document stores) by consuming events from EventStoreDB.
* **Integrates with Messaging Systems:** Events from EventStoreDB can be published to message brokers like Apache Kafka, RabbitMQ, Azure Service Bus, or AWS SQS/SNS for broader distribution to other services or for building real-time data pipelines.

## 7: Architectural Considerations

* **Architectural Shift:** Adopting Event Sourcing with EventStoreDB requires a paradigm shift in thinking about data and system design. It moves away from traditional CRUD operations as the primary interaction model.
* **Learning Curve:** There is an initial learning curve for the development team to fully grasp Event Sourcing principles and EventStoreDB's specific concepts (streams, events, projections).
* **Benefits Outweigh Complexity:** For suitable use cases, the long-term benefits in terms of flexibility, auditability, scalability, and enhanced business insight significantly outweigh the initial architectural complexity.

## EventStoreDB vs. Traditional Logging: A Fundamental Distinction

It's crucial to understand the fundamental difference between "Event Sourcing" (which EventStoreDB facilitates) and more general "log activity handled by an event handler" (traditional logging/audit logging). While both involve recording events, their purpose, immutability, and role in defining system state are profoundly different.

### 1. Log Activity Handled by an Event Handler (Traditional Logging / Audit Logging)

* **Purpose:** Primarily for **observability, auditing, debugging, and diagnostics**. It records *what happened* for later review.
* **Immutability:** Logs are usually appended, but they are **not the system's source of truth**. If the primary database state is corrupted, the log entries won't help you reconstruct it.
* **Role in State:** Logs **do not define or rebuild the application's state**. They merely record observations about state changes that occurred in a separate system (e.g., a traditional SQL database).
* **Example (Camera):**
    * You have a `Camera` table in a SQL database with `IsActive`, `HasMotion`, `LastMotionTime` columns.
    * When motion is detected, your application updates this `Camera` table.
    * An "event handler" (or simple logging code) writes a line to a file, a logging service (ELK stack, Splunk), or an `AuditLog` table:
        ```
        2025-07-15 10:05:30Z | INFO | CAM-001 | Motion detected.
        2025-07-15 10:06:15Z | INFO | CAM-001 | Motion ceased.
        2025-07-15 10:30:00Z | INFO | CAM-001 | Settings changed: sensitivity=high.
        ```
    * These logs describe what happened to the state stored elsewhere. If your `Camera` table gets corrupted, these logs tell you what might have happened, but they don't contain enough information to rebuild the table's state from scratch.

### 2. Event Sourcing (with EventStoreDB)

* **Purpose:** The **events are the source of truth for the application's state**. They are the fundamental, atomic business facts from which the current and any past state of the system are derived.
* **Immutability:** Events are **strictly immutable and append-only**. Once an event is recorded, it is never changed or deleted.
* **Role in State:** The sequence of events **defines and allows for the complete reconstruction of the application's state**. If your derived "read models" (e.g., a denormalized `Camera` table for querying) are lost, you can rebuild them entirely and accurately from the event log.
* **Example (Camera):**
    * Instead of a `Camera` table being the primary store, your system primarily interacts with EventStoreDB.
    * When motion is detected, you don't update a `HasMotion` flag. You **record a `MotionDetected` event** in EventStoreDB:
        ```json
        {
          "eventId": "...",
          "eventType": "MotionDetected",
          "data": {
            "cameraId": "CAM-001",
            "timestamp": "2025-07-15T10:05:30Z",
            "detectionConfidence": 0.95
          }
        }
        ```
    * When motion ceases, you record a `MotionCeased` event:
        ```json
        {
          "eventId": "...",
          "eventType": "MotionCeased",
          "data": {
            "cameraId": "CAM-001",
            "timestamp": "2025-07-15T10:06:15Z"
          }
        }
        ```
    * To get the current "state" (e.g., is motion currently detected?), a service would **project** these events. It would process `MotionDetected` to set `HasMotion = TRUE` and `MotionCeased` to set `HasMotion = FALSE`. This projected state might reside in a separate read model (e.g., a SQL table for fast querying), but the **source for that state is always the EventStoreDB**.
    * If that SQL table is lost, you simply re-run the projection from the beginning of the EventStoreDB stream, and your state is perfectly restored.

### Key Differences Summarized:

| Feature                   | Log Activity (Traditional Logging)             | Event Sourcing                                             |
| :------------------------ | :--------------------------------------------- | :--------------------------------------------------------- |
| **Primary Role** | Observability, debugging, audit trail (secondary) | **Source of Truth for application state** |
| **Immutability** | Generally append-only, but not strict; can be purged. | **Strictly immutable and append-only**. Never modified.    |
| **Reconstruction** | Cannot reconstruct system state if primary data is lost. | **Can always reconstruct current and past system state.** |
| **Granularity** | Often coarse-grained (e.g., "User updated profile"). | Fine-grained, semantic business facts (e.g., "EmailAddressChanged"). |
| **Data Format** | Text lines, structured logs (JSON, XML).       | Structured, semantic business events (JSON, Protobuf).     |
| **Purpose of "Event"** | A record *about* a change that happened elsewhere. | **The change itself *is* the event; it is the data.** |
| **Examples** | Application logs, web server access logs, database audit logs. | Bank transaction logs, order history, IoT sensor readings. |

In the camera example, "log activity handled by an event handler" would be like taking a snapshot of the camera's state every few seconds or when something changes, and writing that snapshot to a simple log file. Event Sourcing, on the other hand, records the *actions* that lead to those snapshots, providing a much richer and perfectly reconstructible history.

## How EventStoreDB Maps Your Entities (e.g., Cameras)

In Event Sourcing, your "entities" (like cameras) are not stored as rows in a table but as **streams of events**. EventStoreDB maps entities to streams as follows:

### 1. Stream = Entity Instance

* **Stream Name:** You choose a unique identifier for each entity. For a camera with ID `123`, your stream name might be `"camera-123"`.
* **Events:** Each change or business fact (e.g., `CameraCreated`, `ConfigUpdated`, `MotionDetected`, `StreamOn`) related to that specific camera is appended, in order, to its corresponding stream.

| Camera Entity     | EventStoreDB Stream     |
| :---------------- | :---------------------- |
| `Camera { id: 123 }` | Stream `"camera-123"` |

### 2. Event Metadata Carries the Link

Every event recorded in EventStoreDB carries essential metadata that links it back to the specific entity and its position in the timeline:

* **Stream Name:** (e.g., `"camera-123"`)
* **Stream Revision:** The event's zero-based sequence number within that specific stream. This is crucial for maintaining order and for optimistic concurrency.
* **Global Position:** The event's position within the `$all` stream (the global log of all events in the database).
* **Custom Metadata:** You can include additional metadata (e.g., `who triggered it`, `timestamp`, `correlation IDs`) relevant to your domain.

This metadata ensures you always know "which camera" and "where in its timeline" any given event belongs.

### 3. Rebuilding Entity State

To get the current state of your `Camera` object (or any entity):

1.  You read all events from that camera's stream (e.g., `"camera-123"`) starting from the beginning.
2.  You apply each event, in the order they were recorded, to an initial, empty instance of your camera aggregate/entity. Each event's application updates the state of the entity.

```csharp
var events = client.ReadStreamAsync(Direction.Forwards, "camera-123", StreamPosition.Start);
var camera = new CameraAggregate(); // Your domain model
await foreach (var ev in events)
    camera.Apply(ev.EventType, ev.Data); // Apply the event's data to the camera's state
// At this point, 'camera' now holds the full, up-to-date state of camera 123.
```

### 4. Concurrency & Versioning

The **stream revision** acts as an optimistic concurrency token. When you append new events to a stream, you can assert that you are writing on a specific expected revision (typically the last revision you read). If another process has written to the stream in the meantime, the append operation will fail, preventing lost updates and ensuring data consistency.

### 5. Projections & Read Models

While EventStoreDB is the source of truth, it's not optimized for complex queries across all entities or for denormalized views (e.g., "list all active cameras"). For such needs, you create **projections** (or external processes):

* These projections listen to events from EventStoreDB.
* They transform and store the derived, denormalized state into a "read model" – typically a traditional database (e.g., PostgreSQL, SQL Server, Elasticsearch, a document database) that is optimized for specific query patterns.
* This provides fast lookups by camera ID or other criteria without replaying the entire event stream each time.

### Putting It All Together for a Camera System

* **Create camera:** Append a `CameraCreated` event to a new stream named `"camera-XXX"` (where `XXX` is the camera's ID).
* **Update configuration:** Append a `ConfigChanged` event to the same `"camera-XXX"` stream.
* **Motion detected:** Append a `MotionDetected` event to the `"camera-XXX"` stream.
* **Read current state:** Replay all events for `"camera-XXX"` from revision `0` to the latest.
* **Concurrency:** Use the stream revision you last read to ensure no concurrent writes conflict when appending new events.
* **List all cameras:** Either read from the `$all` stream (less efficient for large datasets) and filter on stream names, or, more commonly, maintain a projection/read-model that tracks every new camera stream creation and stores their current state in a separate query-optimized database.

In short: Streams are your primary "tables" (or aggregates), events are your immutable and ordered "rows" representing changes, and your camera entity's identity is simply the stream name you choose.

---

## API Endpoints for Camera Management with Event Sourcing

Here's how CRUD operations and specific event capturing would look in an API using EventStoreDB for Event Sourcing. All event bodies include a `timestampUtc` in ISO 8601 UTC format.

### Traditional CRUD-like Operations (often result in events)

These API endpoints typically trigger the creation of one or more events in EventStoreDB.

1.  **Register a New Camera**
    * **Method:** `POST`
    * **Path:** `/cameras`
    * **Description:** Creates a new camera entity. Internally, this would likely append a `CameraCreated` event to a new stream (e.g., `camera-{new-camera-id}`).
    * **Request Body Example:**
        ```json
        {
          "location": "Building A - Entrance",
          "model": "Matrix IPC-HFW5442",
          "ipAddress": "192.168.1.101"
        }
        ```

2.  **Get Camera Details**
    * **Method:** `GET`
    * **Path:** `/cameras/{cameraId}`
    * **Description:** Retrieves the current state of a camera. This operation would typically read from a **read model** (e.g., a SQL database) that is kept up-to-date by projecting events from EventStoreDB.
    * **Request Body:** None.

3.  **Update Camera Properties**
    * **Method:** `PUT`
    * **Path:** `/cameras/{cameraId}`
    * **Description:** Updates properties of an existing camera. Internally, this would trigger one or more `ConfigChanged` events or specific property-change events to the camera's stream.
    * **Request Body Example:**
        ```json
        {
          "location": "Building A - Lobby",
          "model": "Matrix IPC-HFW5842T",
          "ipAddress": "192.168.1.102",
          "isActive": true
        }
        ```

4.  **Decommission (Delete) Camera**
    * **Method:** `DELETE`
    * **Path:** `/cameras/{cameraId}`
    * **Description:** Marks a camera as decommissioned. In Event Sourcing, you typically don't "delete" data. Instead, a `CameraDecommissioned` event would be appended to the stream, and read models would be updated to reflect its inactive status. The stream remains in EventStoreDB as part of the immutable history.
    * **Request Body:** None.

### Event-Driven Endpoints (Capturing Business Facts)

These endpoints directly represent significant business events or actions occurring in the system. Each call appends a specific event to the corresponding camera's stream in EventStoreDB.

5.  **Motion Detected**
    * **Method:** `POST`
    * **Path:** `/cameras/{cameraId}/events/motion-detected`
    * **Description:** Records that motion was detected by the specified camera.
    * **Request Body Example:**
        ```json
        {
          "timestampUtc": "2025-07-16T08:30:00Z",
          "area": "Entrance Door",
          "sensitivity": "High"
        }
        ```

6.  **Stream On**
    * **Method:** `POST`
    * **Path:** `/cameras/{cameraId}/events/stream-on`
    * **Description:** Records that the camera's video stream has been turned on.
    * **Request Body Example:**
        ```json
        {
          "timestampUtc": "2025-07-16T08:31:00Z",
          "startedBy": "auto"
        }
        ```

7.  **Stream Off**
    * **Method:** `POST`
    * **Path:** `/cameras/{cameraId}/events/stream-off`
    * **Description:** Records that the camera's video stream has been turned off.
    * **Request Body Example:**
        ```json
        {
          "timestampUtc": "2025-07-16T10:00:00Z",
          "reason": "Scheduled shutdown"
        }
        ```

8.  **Alarm On**
    * **Method:** `POST`
    * **Path:** `/cameras/{cameraId}/events/alarm-on`
    * **Description:** Records that an alarm has been triggered by the camera.
    * **Request Body Example:**
        ```json
        {
          "timestampUtc": "2025-07-16T09:45:00Z",
          "alarmType": "Tamper",
          "severity": "Critical"
        }
        ```

9.  **Alarm Off**
    * **Method:** `POST`
    * **Path:** `/cameras/{cameraId}/events/alarm-off`
    * **Description:** Records that an active alarm on the camera has been cleared.
    * **Request Body Example:**
        ```json
        {
          "timestampUtc": "2025-07-16T09:50:00Z",
          "clearedBy": "operator123"
        }
        ```

10. **Config Changed**
    * **Method:** `POST`
    * **Path:** `/cameras/{cameraId}/events/config-changed`
    * **Description:** Records that one or more configuration settings of the camera have been modified.
    * **Request Body Example:**
        ```json
        {
          "timestampUtc": "2025-07-16T11:00:00Z",
          "changes": {
            "resolution": "1920x1080",
            "frameRate": 30,
            "bitrate": "2Mbps",
            "nightMode": true
          },
          "changedBy": "adminUser"
        }
        ```