// ParkingSession.cs
namespace Home_Task.Entities;

public sealed class ParkingSession : IEquatable<ParkingSession>
{
    private static int _sessionCounter;

    public string SessionId { get; }
    public Vehicle Vehicle { get; }
    public ParkingSpot ParkingSpot { get; }
    public DateTime EntryTime { get; }
    public DateTime? ExitTime { get; private set; }

    public bool IsActive => ExitTime is null;
    public TimeSpan Duration => (ExitTime ?? DateTime.Now) - EntryTime;

    public ParkingSession(Vehicle vehicle, ParkingSpot parkingSpot)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        ArgumentNullException.ThrowIfNull(parkingSpot);

        Vehicle = vehicle;
        ParkingSpot = parkingSpot;

        SessionId = $"T{Interlocked.Increment(ref _sessionCounter):D6}";
        EntryTime = DateTime.Now;
    }

    // Thread safety: Each session belongs to a specific vehicle/spot combination
    // Only one thread should access each session instance
    public bool EndSession()
    {
        if (!IsActive) return false;

        ExitTime = DateTime.Now;
        return true;
    }

    public bool Equals(ParkingSession? other) =>
        other is not null && SessionId == other.SessionId;

    public override bool Equals(object? obj) => Equals(obj as ParkingSession);

    public override int GetHashCode() => SessionId.GetHashCode();

    public override string ToString()
    {
        var status = IsActive ? "Active" : "Completed";
        var durationStr = Duration.ToString(@"hh\:mm");        
        return $"Ticket {SessionId}: {Vehicle} in {ParkingSpot.SpotId} " +
               $"({EntryTime:HH:mm} - {(IsActive ? "Now" : ExitTime?.ToString("HH:mm"))}) " +
               $"[{durationStr}] - {status}";
    }
}