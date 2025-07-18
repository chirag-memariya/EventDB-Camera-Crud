using EventStore.Client;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization; // Required for JsonPropertyName

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure EventStoreDB gRPC Client
builder.Services.AddSingleton(provider =>
{
    var connectionString = "esdb://eventstore:2113?tls=false";
    return new EventStoreClient(EventStoreClientSettings.Create(connectionString));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection(); // Recommended for production, optional for dev.


// --- Helper to append any event ---
static async Task<IResult> AppendEvent(Guid cameraId, object @event, EventStoreClient client)
{
    var stream = $"camera-{cameraId}";
    var data   = EventSerializer.ToEventData(@event);
    try
    {
        // use Any for simplicity; in prod you’d read the last revision for concurrency
        await client.AppendToStreamAsync(stream, StreamState.Any, new[] { data });
        return Results.Ok();
    }
    catch (Exception e)
    {
        return Results.Problem(e.Message, statusCode: 500);
    }
}


// --- CRUD-like Operations ---

// 1. CREATE (Register a New Camera)
app.MapPost("/cameras", async (
    [FromBody] CameraRegisterRequest request,
    [FromServices] EventStoreClient client) =>
{
    var cameraId = Guid.NewGuid();
    var @event = new CameraRegisteredEvent(
        cameraId,
        request.Location,
        request.Model,
        request.IPAddress,
        DateTime.UtcNow
    );

    var eventData = EventSerializer.ToEventData(@event);
    var streamName = $"camera-{cameraId}";

    try
    {
        // Append event to the stream, expecting no stream to exist (for new camera)
        await client.AppendToStreamAsync(
            streamName,
            StreamState.NoStream, // Optimistic concurrency check: stream must not exist
            new[] { eventData }
        );
        return Results.Created($"/cameras/{cameraId}", new CameraDto(cameraId, request.Location, request.Model, request.IPAddress, true));
    }
    catch (WrongExpectedVersionException)
    {
        return Results.Conflict($"Camera with ID {cameraId} already exists."); // Highly unlikely for new, random GUID
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error registering camera.");
        return Results.Problem("Failed to register camera.", statusCode: 500);
    }
})
.WithOpenApi();

// 2. READ (Get Current State of a Camera - by replaying events)
app.MapGet("/cameras/{cameraId:guid}", async (
    [FromRoute] Guid cameraId,
    [FromServices] EventStoreClient client) =>
{
    var streamName = $"camera-{cameraId}";
    var readResult = client.ReadStreamAsync(
        Direction.Forwards,
        streamName,
        StreamPosition.Start
    );

    // Reconstruct state from events
    string? location = null;
    string? model = null;
    string? ipAddress = null;
    bool isActive = false; // Default to not active until registered

    long eventCount = 0; // To track if stream exists

    await foreach (var resolvedEvent in readResult)
    {
        eventCount++;
        var domainEvent = EventSerializer.FromResolvedEvent(resolvedEvent);

        switch (domainEvent)
        {
            case CameraRegisteredEvent registered:
                location = registered.Location;
                model = registered.Model;
                ipAddress = registered.IPAddress;
                isActive = true; // Registered means active initially
                break;
            case CameraUpdatedEvent updated:
                location = updated.Location ?? location;
                model = updated.Model ?? model;
                ipAddress = updated.IPAddress ?? ipAddress;
                isActive = updated.IsActive ?? isActive;
                break;
            case CameraDecommissionedEvent _:
                isActive = false; // Decommissioned means not active
                break;
        }
    }

    if (eventCount == 0) // No events in stream means camera doesn't exist
    {
        return Results.NotFound($"Camera with ID {cameraId} not found.");
    }

    return Results.Ok(new CameraDto(cameraId, location!, model!, ipAddress!, isActive));
})
.WithOpenApi();

// 3. UPDATE (Update Camera Properties - by appending a new event)
app.MapPut("/cameras/{cameraId:guid}", async (
    [FromRoute] Guid cameraId,
    [FromBody] CameraUpdateRequest request,
    [FromServices] EventStoreClient client) =>
{
    var streamName = $"camera-{cameraId}";

    // Read the current version to ensure optimistic concurrency
    // var lastEvent = await client.ReadStreamAsync(Direction.Backwards, streamName, StreamPosition.End, maxCount: 1)
    //                             .FirstOrDefaultAsync();
    long expectedVersion;
    try
    {
        var lastEvent = await client.ReadStreamAsync(
            Direction.Backwards,
            streamName,
            StreamPosition.End,
            maxCount: 1
        ).FirstOrDefaultAsync();

        // ResolvedEvent is a struct. If FirstOrDefaultAsync returns a default struct,
        // it means the stream exists but is empty (no events).
        // Check if the EventId is Uuid.Empty to determine if it's a default ResolvedEvent.
        if (lastEvent.Event.EventId == Uuid.Empty) // Corrected: Compare Uuid with Uuid.Empty
        {
            return Results.NotFound($"Camera with ID {cameraId} found but has no events for update.");
        }
        else
        {
            expectedVersion = lastEvent.Event.EventNumber.ToInt64();
        }
    }
    catch (StreamNotFoundException)
    {
        // Stream does not exist at all.
        return Results.NotFound($"Camera with ID {cameraId} not found for update.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error determining expected version for update of camera {CameraId}.", cameraId);
        return Results.Problem("Failed to prepare for camera update.", statusCode: 500);
    }

    var @event = new CameraUpdatedEvent(
        cameraId,
        request.Location,
        request.Model,
        request.IPAddress,
        request.IsActive,
        DateTime.UtcNow
    );

    var eventData = EventSerializer.ToEventData(@event);

    try
    {
        await client.AppendToStreamAsync(
            streamName,
            StreamRevision.FromInt64(expectedVersion), // Corrected: Convert long to StreamRevision
            new[] { eventData }
        );
        return Results.Ok();
    }
    catch (WrongExpectedVersionException)
    {
        return Results.Conflict($"Update failed due to a concurrency conflict. Camera {cameraId} was modified concurrently.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error updating camera {CameraId}.", cameraId);
        return Results.Problem("Failed to update camera.", statusCode: 500);
    }
})
.WithOpenApi();

// 4. DELETE (Decommission a Camera - by appending a "decommissioned" event)
app.MapDelete("/cameras/{cameraId:guid}", async (
    [FromRoute] Guid cameraId,
    [FromServices] EventStoreClient client) =>
{
    var streamName = $"camera-{cameraId}";

    long expectedVersion;
    try
    {
        var lastEvent = await client.ReadStreamAsync(
            Direction.Backwards,
            streamName,
            StreamPosition.End,
            maxCount: 1
        ).FirstOrDefaultAsync();

        // ResolvedEvent is a struct. If FirstOrDefaultAsync returns a default struct,
        // it means the stream exists but is empty (no events).
        // Check if the EventId is Uuid.Empty to determine if it's a default ResolvedEvent.
        if (lastEvent.Event.EventId == Uuid.Empty)
        {
            return Results.NotFound($"Camera with ID {cameraId} found but has no events for decommissioning.");
        }
        else
        {
            expectedVersion = lastEvent.Event.EventNumber.ToInt64();
        }
    }
    catch (StreamNotFoundException)
    {
        // Stream does not exist at all.
        return Results.NotFound($"Camera with ID {cameraId} not found for decommissioning.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error determining expected version for decommission of camera {CameraId}.", cameraId);
        return Results.Problem("Failed to prepare for camera decommissioning.", statusCode: 500);
    }

    // Check if already decommissioned to avoid redundant events
    // This requires replaying the stream or querying a read model if available
    // For simplicity here, we'll just append. A real system would check state first.
    var currentCamera = await GetCurrentCameraState(cameraId, client);
    if (currentCamera == null)
    {
        return Results.NotFound($"Camera with ID {cameraId} not found.");
    }
    if (!currentCamera.IsActive)
    {
        return Results.NoContent(); // Already decommissioned or inactive
    }


    var @event = new CameraDecommissionedEvent(cameraId, DateTime.UtcNow);
    var eventData = EventSerializer.ToEventData(@event);

    try
    {
        await client.AppendToStreamAsync(
            streamName,
            StreamRevision.FromInt64(expectedVersion), // Corrected: Convert long to StreamRevision
            new[] { eventData }
        );
        return Results.NoContent();
    }
    catch (WrongExpectedVersionException)
    {
        return Results.Conflict($"Decommission failed due to a concurrency conflict. Camera {cameraId} was modified concurrently.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error decommissioning camera {CameraId}.", cameraId);
        return Results.Problem("Failed to decommission camera.", statusCode: 500);
    }
})
.WithOpenApi();


// --- NEW CAMERA EVENTS ---

// 5. Motion Detected
app.MapPost("/cameras/{cameraId:guid}/events/motion-detected",
    async ([FromRoute] Guid cameraId,
           [FromBody] MotionDetectedRequest req,
           [FromServices] EventStoreClient client) =>
        await AppendEvent(cameraId, new MotionDetectedEvent(
            cameraId,
            req.TimestampUtc,
            req.Area,
            req.Sensitivity), client)
).WithOpenApi();

// 6. Stream On
app.MapPost("/cameras/{cameraId:guid}/events/stream-on",
    async ([FromRoute] Guid cameraId,
           [FromBody] StreamOnRequest req,
           [FromServices] EventStoreClient client) =>
        await AppendEvent(cameraId, new StreamOnEvent(
            cameraId,
            req.TimestampUtc,
            req.StartedBy), client)
).WithOpenApi();

// 7. Stream Off
app.MapPost("/cameras/{cameraId:guid}/events/stream-off",
    async ([FromRoute] Guid cameraId,
           [FromBody] StreamOffRequest req,
           [FromServices] EventStoreClient client) =>
        await AppendEvent(cameraId, new StreamOffEvent(
            cameraId,
            req.TimestampUtc,
            req.Reason), client)
).WithOpenApi();

// 8. Alarm On
app.MapPost("/cameras/{cameraId:guid}/events/alarm-on",
    async ([FromRoute] Guid cameraId,
           [FromBody] AlarmOnRequest req,
           [FromServices] EventStoreClient client) =>
        await AppendEvent(cameraId, new AlarmOnEvent(
            cameraId,
            req.TimestampUtc,
            req.AlarmType,
            req.Severity), client)
).WithOpenApi();

// 9. Alarm Off
app.MapPost("/cameras/{cameraId:guid}/events/alarm-off",
    async ([FromRoute] Guid cameraId,
           [FromBody] AlarmOffRequest req,
           [FromServices] EventStoreClient client) =>
        await AppendEvent(cameraId, new AlarmOffEvent(
            cameraId,
            req.TimestampUtc,
            req.ClearedBy), client)
).WithOpenApi();

// 10. Config Changed
app.MapPost("/cameras/{cameraId:guid}/events/config-changed",
    async ([FromRoute] Guid cameraId,
           [FromBody] ConfigChangedRequest req,
           [FromServices] EventStoreClient client) =>
        await AppendEvent(cameraId, new ConfigChangedEvent(
            cameraId,
            req.TimestampUtc,
            req.Changes,
            req.ChangedBy), client)
).WithOpenApi();

// Helper to get current camera state for checks (e.g., if already decommissioned)
// In a real CQRS system, this would typically come from a dedicated Read Model
async Task<CameraDto?> GetCurrentCameraState(Guid cameraId, EventStoreClient client)
{
    var streamName = $"camera-{cameraId}";
    var readResult = client.ReadStreamAsync(
        Direction.Forwards,
        streamName,
        StreamPosition.Start
    );

    string? location = null;
    string? model = null;
    string? ipAddress = null;
    bool isActive = false;
    long eventCount = 0;

    await foreach (var resolvedEvent in readResult)
    {
        eventCount++;
        var domainEvent = EventSerializer.FromResolvedEvent(resolvedEvent);

        switch (domainEvent)
        {
            case CameraRegisteredEvent registered:
                location = registered.Location;
                model = registered.Model;
                ipAddress = registered.IPAddress;
                isActive = true;
                break;
            case CameraUpdatedEvent updated:
                location = updated.Location ?? location;
                model = updated.Model ?? model;
                ipAddress = updated.IPAddress ?? ipAddress;
                isActive = updated.IsActive ?? isActive;
                break;
            case CameraDecommissionedEvent _:
                isActive = false;
                break;
        }
    }
    return eventCount > 0 ? new CameraDto(cameraId, location!, model!, ipAddress!, isActive) : null;
}


app.Run();