namespace Home_Task.Entities;

using Home_Task.Models;
using System.Collections.Concurrent;

public class ParkingLot : IDisposable
{
    public string Name { get; }
    public decimal HourlyRate { get; }
    public int TotalSpots { get; }
    
    private volatile int _occupiedSpots;
    public int OccupiedSpots => _occupiedSpots;
    public int AvailableSpots => TotalSpots - OccupiedSpots;
    
    private readonly SemaphoreSlim _capacity;
    private readonly ConcurrentQueue<ParkingSpot> _availableSpots;
    private readonly ConcurrentDictionary<string, ParkingSession> _activeSessions;

    public ParkingLot(string name, decimal hourlyRate, int totalSpots)
    {
        Name = string.IsNullOrWhiteSpace(name) 
            ? throw new ArgumentException("Parking lot name cannot be empty", nameof(name))
            : name.Trim();

        HourlyRate = hourlyRate <= 0
            ? throw new ArgumentException("Hourly rate must be positive", nameof(hourlyRate))
            : hourlyRate;

        TotalSpots = totalSpots <= 0
            ? throw new ArgumentException("Total spots must be positive", nameof(totalSpots))
            : totalSpots;

        _capacity = new SemaphoreSlim(totalSpots, totalSpots);
        _availableSpots = new ConcurrentQueue<ParkingSpot>(CreateParkingSpots(totalSpots));
        _activeSessions = new ConcurrentDictionary<string, ParkingSession>(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<ParkingSpot> CreateParkingSpots(int totalSpots) =>
        Enumerable.Range(1, totalSpots)
                  .Select(i => new ParkingSpot($"A-{i:D2}"));

    public async Task<ParkingResult> TryEnterAsync(Vehicle vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);

        await _capacity.WaitAsync().ConfigureAwait(false);

        var success = false;
        try
        {
            if (!_availableSpots.TryDequeue(out var freeSpot))
                return ParkingResult.NoSpotsAvailable();

            if (!freeSpot.Assign(vehicle))
            {
                _availableSpots.Enqueue(freeSpot);
                return ParkingResult.SpotAssignmentFailed(freeSpot.SpotId);
            }

            var session = new ParkingSession(vehicle, freeSpot);
            var licensePlate = vehicle.LicensePlate;

            _activeSessions.TryAdd(licensePlate, session);
            Interlocked.Increment(ref _occupiedSpots);
        
            success = true; 
            return ParkingResult.Success(freeSpot.SpotId, session);
        }
        finally
        {
            if (!success) 
                _capacity.Release();
        }
    }

    public Task<UnparkingResult> ExitAsync(string licensePlate)
    {
        if (string.IsNullOrWhiteSpace(licensePlate))
            return Task.FromResult(UnparkingResult.InvalidLicensePlate());

        if (!_activeSessions.TryRemove(licensePlate, out var session))
            return Task.FromResult(UnparkingResult.VehicleNotFound(licensePlate));

        var success = false;
        try
        {
            if (!session.IsActive)
            {
                _activeSessions.TryAdd(licensePlate, session);
                return Task.FromResult(UnparkingResult.SessionAlreadyEnded(licensePlate));
            }

            session.EndSession();
            var fee = CalculateParkingFee(session);

            session.ParkingSpot.Release();
            _availableSpots.Enqueue(session.ParkingSpot);

            Interlocked.Decrement(ref _occupiedSpots);
            success = true;

            return Task.FromResult(UnparkingResult.Success(fee, session.Duration, session));
        }
        finally
        {
            if (success)
                _capacity.Release();
        }
    }

    private decimal CalculateParkingFee(ParkingSession session)
    {
        var hours = Math.Ceiling(session.Duration.TotalHours);
        var fee = HourlyRate * (decimal)hours;
        return Math.Round(fee, 2, MidpointRounding.AwayFromZero);
    }

    public override string ToString() =>
        $"{Name}: {AvailableSpots}/{TotalSpots} available spots, Rate: {HourlyRate:C}/hour";

    public void Dispose() => _capacity.Dispose();
}
