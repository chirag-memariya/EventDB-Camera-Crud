CRUD operations and each of the six new event endpoints
1. Register a New Camera
POST /cameras

```json

{
  "location": "Building A - Entrance",
  "model": "Matrix IPC-HFW5442",
  "ipAddress": "192.168.1.101"
}
```

2. Get Camera Details
GET /cameras/{cameraId}
No body.

3. Update Camera Properties
PUT /cameras/{cameraId}

```json

{
  "location": "Building A - Lobby",
  "model": "Matrix IPC-HFW5842T",
  "ipAddress": "192.168.1.102",
  "isActive": true
}
```

4. Decommission (Delete) Camera
DELETE /cameras/{cameraId}
No body.

Added Event Endpoints
All event bodies include a timestampUtc in ISO 8601 UTC.

5. Motion Detected
POST /cameras/{cameraId}/events/motion-detected

```json

{
  "timestampUtc": "2025-07-16T08:30:00Z",
  "area": "Entrance Door",
  "sensitivity": "High"
}
```

6. Stream On
POST /cameras/{cameraId}/events/stream-on

```json

{
  "timestampUtc": "2025-07-16T08:31:00Z",
  "startedBy": "auto" 
}
```

7. Stream Off
POST /cameras/{cameraId}/events/stream-off

```json

{
  "timestampUtc": "2025-07-16T10:00:00Z",
  "reason": "Scheduled shutdown"
}
```

8. Alarm On
POST /cameras/{cameraId}/events/alarm-on

```json

{
  "timestampUtc": "2025-07-16T09:45:00Z",
  "alarmType": "Tamper",
  "severity": "Critical"
}
```

9. Alarm Off
POST /cameras/{cameraId}/events/alarm-off

```json

{
  "timestampUtc": "2025-07-16T09:50:00Z",
  "clearedBy": "operator123"
}
```

10. Config Changed
POST /cameras/{cameraId}/events/config-changed

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

