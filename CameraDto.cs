using EventStore.Client;
using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

// --- DTOs for API Requests/Responses ---
public record CameraRegisterRequest(string Location, string Model, string IPAddress);
public record CameraUpdateRequest(string? Location = null, string? Model = null, string? IPAddress = null, bool? IsActive = null);
public record CameraDto(Guid Id, string Location, string Model, string IPAddress, bool IsActive);

// --- Domain Events (Records) ---
// Using records for immutability and conciseness
public record CameraRegisteredEvent(Guid CameraId, string Location, string Model, string IPAddress, DateTime Timestamp);
public record CameraUpdatedEvent(Guid CameraId, string? Location, string? Model, string? IPAddress, bool? IsActive, DateTime Timestamp);
public record CameraDecommissionedEvent(Guid CameraId, DateTime Timestamp);

// --- Request DTOs for new events ---

public record MotionDetectedRequest(
    [property: JsonPropertyName("timestampUtc")] DateTime TimestampUtc,
    [property: JsonPropertyName("area")]        string Area,
    [property: JsonPropertyName("sensitivity")] string Sensitivity
);

public record StreamOnRequest(
    [property: JsonPropertyName("timestampUtc")] DateTime TimestampUtc,
    [property: JsonPropertyName("startedBy")]   string StartedBy
);

public record StreamOffRequest(
    [property: JsonPropertyName("timestampUtc")] DateTime TimestampUtc,
    [property: JsonPropertyName("reason")]      string Reason
);

public record AlarmOnRequest(
    [property: JsonPropertyName("timestampUtc")] DateTime TimestampUtc,
    [property: JsonPropertyName("alarmType")]   string AlarmType,
    [property: JsonPropertyName("severity")]    string Severity
);

public record AlarmOffRequest(
    [property: JsonPropertyName("timestampUtc")] DateTime TimestampUtc,
    [property: JsonPropertyName("clearedBy")]   string ClearedBy
);

public record ConfigChangedRequest(
    [property: JsonPropertyName("timestampUtc")] DateTime TimestampUtc,
    [property: JsonPropertyName("changes")]     Dictionary<string, object> Changes,
    [property: JsonPropertyName("changedBy")]   string ChangedBy
);

// --- Helper for Serializing/Deserializing Events ---
public static class EventSerializer
{
    private static readonly JsonSerializerOptions _options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static EventData ToEventData(object @event)
    {
        var eventType = @event.GetType().Name; // e.g., "CameraRegisteredEvent"
        var data = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), _options);
        var metadata = JsonSerializer.SerializeToUtf8Bytes(new { Timestamp = DateTime.UtcNow }, _options); // Common metadata

        // FIX: Provide all arguments required by the constructor
        // Constructor: EventData(Uuid eventId, string eventType, ReadOnlyMemory<byte> data, ReadOnlyMemory<byte> metadata, string? contentType = null)
        // Or if it's the specific signature from the error (Guid, string, bool, byte[], byte[]):
        // EventData(Guid eventId, string eventType, bool isJson, byte[] data, byte[] metadata)

        // Given the error message, the constructor expected looks like this one:
        // EventData(Guid eventId, string eventType, bool isJson, byte[] data, byte[] metadata)
        return new EventData(
            Uuid.NewUuid(), // Corrected: Use EventStore.Client.Uuid.NewUuid() for the event ID
            eventType,
            data,
            metadata
        );
    }

    public static object? FromResolvedEvent(ResolvedEvent resolvedEvent)
    {
        var eventType = resolvedEvent.Event.EventType;
        var jsonData = Encoding.UTF8.GetString(resolvedEvent.Event.Data.ToArray());

        return eventType switch
        {
            nameof(CameraRegisteredEvent) => JsonSerializer.Deserialize<CameraRegisteredEvent>(jsonData, _options),
            nameof(CameraUpdatedEvent) => JsonSerializer.Deserialize<CameraUpdatedEvent>(jsonData, _options),
            nameof(CameraDecommissionedEvent) => JsonSerializer.Deserialize<CameraDecommissionedEvent>(jsonData, _options),
            _ => null
        };
    }
}

    public record MotionDetectedEvent(
        [property: JsonPropertyName("cameraId")] Guid CameraId,
        [property: JsonPropertyName("timestampUtc")] DateTime TimestampUtc,
        [property: JsonPropertyName("area")] string Area,
        [property: JsonPropertyName("sensitivity")] string Sensitivity
    );

    public record StreamOnEvent(
        [property: JsonPropertyName("cameraId")] Guid CameraId,
        [property: JsonPropertyName("timestampUtc")] DateTime TimestampUtc,
        [property: JsonPropertyName("startedBy")] string StartedBy
    );

    public record StreamOffEvent(
        [property: JsonPropertyName("cameraId")] Guid CameraId,
        [property: JsonPropertyName("timestampUtc")] DateTime TimestampUtc,
        [property: JsonPropertyName("reason")] string Reason
    );

    public record AlarmOnEvent(
        [property: JsonPropertyName("cameraId")] Guid CameraId,
        [property: JsonPropertyName("timestampUtc")] DateTime TimestampUtc,
        [property: JsonPropertyName("alarmType")] string AlarmType,
        [property: JsonPropertyName("severity")] string Severity
    );

    public record AlarmOffEvent(
        [property: JsonPropertyName("cameraId")] Guid CameraId,
        [property: JsonPropertyName("timestampUtc")] DateTime TimestampUtc,
        [property: JsonPropertyName("clearedBy")] string ClearedBy
    );

    public record ConfigChangedEvent(
        [property: JsonPropertyName("cameraId")] Guid CameraId,
        [property: JsonPropertyName("timestampUtc")] DateTime TimestampUtc,
        [property: JsonPropertyName("changes")] Dictionary<string, object> Changes,
        [property: JsonPropertyName("changedBy")] string ChangedBy
    );