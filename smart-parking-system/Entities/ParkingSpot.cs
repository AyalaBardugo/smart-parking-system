namespace Home_Task.Entities;

public class ParkingSpot : IEquatable<ParkingSpot>
{
    public string SpotId { get; }
    public bool IsOccupied { get; private set; }
    public Vehicle? CurrentVehicle { get; private set; }

    public ParkingSpot(string spotId)
    {
        SpotId = string.IsNullOrWhiteSpace(spotId)
            ? throw new ArgumentException("Spot ID cannot be empty", nameof(spotId))
            : spotId.Trim();
    }

    // Thread safety is ensured by ParkingLot's SemaphoreSlim + ConcurrentQueue
    // Each thread receives a unique ParkingSpot instance from the queue
    public bool Assign(Vehicle vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        
        if (IsOccupied) return false;

        CurrentVehicle = vehicle;
        IsOccupied = true;
        return true;
    }

    public bool Release()
    {
        if (!IsOccupied) return false;

        CurrentVehicle = null;
        IsOccupied = false;
        return true;
    }

    public override string ToString() =>
        IsOccupied
            ? $"Spot {SpotId}: Occupied by {CurrentVehicle}"
            : $"Spot {SpotId}: Available";

    public bool Equals(ParkingSpot? other) =>
        other is not null &&
        string.Equals(SpotId, other.SpotId, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => Equals(obj as ParkingSpot);

    public override int GetHashCode() =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(SpotId);
}