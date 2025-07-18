# Camera Event‑Sourcing API Documentation

This document explains the **Camera Event‑Sourcing API** project, which uses **.NET 8 Minimal APIs** and **EventStoreDB** to manage camera lifecycle and event streams.

---

## 1. Project Overview

- **Purpose**: Provide a CRUD‑style API for cameras (register, update, read, decommission) and record domain events (motion detected, stream on/off, alarm on/off, config changed) via Event Sourcing.
- **Tech Stack**:
  - **.NET 8** Minimal APIs
  - **EventStoreDB** (v21.10) for append‑only event streams
  - **Docker Compose** for local setup
  - **Swagger UI** for API exploration and testing

---

## 2. Architecture

```plaintext
┌────────────────┐       ┌───────────────┐
│ .NET Minimal   │  gRPC │  EventStoreDB │
│ API (C#)       │ ─────▶│  (Container)  │
└────────────────┘       └───────────────┘
       ▲
       │ HTTP/JSON
       │
 ┌───────────┐
 │ Swagger   │
 │ UI        │
 └───────────┘
```

1. **API** receives HTTP calls, serializes domain events, and appends them to per‑camera streams.
2. **EventStoreDB** persists events; the API replays them to reconstruct camera state.
3. **Swagger UI** provides an interactive interface for all endpoints.

---

## 3. Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker & Docker Compose](https://docs.docker.com/compose/)
- Modern browser (e.g., Chrome, Edge)

---

## 4. Getting the Code

```bash
git clone https://github.com/your-org/camera-eventstore-api.git
cd camera-eventstore-api
```

---

## 5. Running Locally (without Docker)

1. **Configure** `Program.cs` connection string:
   ```csharp
   var connectionString = "esdb://localhost:2113?tls=false";
   ```
2. **Install** EventStoreDB locally or use Docker.
3. **Run**:
   ```bash
   dotnet restore
   dotnet build
   dotnet run --project src/Api/Api.csproj
   ```
4. Open [**https://localhost:5001/swagger**](https://localhost:5001/swagger).

---

## 6. Running with Docker Compose

```bash
docker-compose up --build
```

- **Ports**:

  - `2113`: EventStoreDB gRPC
  - `1113`: EventStoreDB TCP (legacy)
  - `2114`: EventStoreDB HTTP UI
  - `5236`: API HTTP

- Browse:

  - **EventStoreDB UI**: `http://localhost:2114`
  - **Swagger UI**:   `http://localhost:5236/swagger`

---

## 7. API Endpoints

| Method | Path                                         | Description                        |
| ------ | -------------------------------------------- | ---------------------------------- |
| POST   | `/cameras`                                   | Register new camera                |
| GET    | `/cameras/{cameraId}`                        | Get camera state                   |
| PUT    | `/cameras/{cameraId}`                        | Update camera properties           |
| DELETE | `/cameras/{cameraId}`                        | Decommission camera                |
| POST   | `/cameras/{cameraId}/events/motion-detected` | Record motion-detected event       |
| POST   | `/cameras/{cameraId}/events/stream-on`       | Record stream start                |
| POST   | `/cameras/{cameraId}/events/stream-off`      | Record stream stop                 |
| POST   | `/cameras/{cameraId}/events/alarm-on`        | Record alarm-on event              |
| POST   | `/cameras/{cameraId}/events/alarm-off`       | Record alarm-off event             |
| POST   | `/cameras/{cameraId}/events/config-changed`  | Record configuration-changed event |

---

## 8. Sample JSON Bodies

See the **Request Samples** section in Swagger UI or `docs/request-samples.md` for full examples.

---

## 9. Testing with Swagger UI

1. Navigate to [**http://localhost:5236/swagger**](http://localhost:5236/swagger)
2. Expand an endpoint, click **Try it out**
3. Paste JSON body, hit **Execute**
4. Inspect responses and status codes

---

## 10. Extending the Project

- **Add new events**: Define an event record class, map a POST endpoint, update `EventSerializer`.
- **Read Models**: Build projections (SQL, MongoDB) by subscribing to streams.
- **Security**: Add JWT, API‑keys, or Identity Server to secure endpoints.

---

## 11. Troubleshooting

- **Connection refused**: Ensure Docker Compose is running and `esdb://eventstore:2113` matches the service name.
- **Invalid GUID**: Ensure path parameters use a valid GUID format (36‑char `/[0-9a-f\-]+/`).

---

## 12. Cleanup

```bash
docker-compose down --volumes
```

