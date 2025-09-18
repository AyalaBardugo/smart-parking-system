namespace Home_Task.Services;

using Home_Task.Entities;
using Home_Task.Models;
using System.Collections.Concurrent;

public sealed class ParkingSystemManager : IDisposable
{
    private readonly ConcurrentDictionary<string, ParkingLot> _parkingLots = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _globalVehicleRegistry = new(StringComparer.OrdinalIgnoreCase);
    
    private static readonly ThreadLocal<Random> ThreadLocalRandom = new(() => new Random());
    
    public int TotalSystemCapacity => _parkingLots.Values.Sum(p => p.TotalSpots);
    public int TotalOccupiedSpots => _parkingLots.Values.Sum(p => p.OccupiedSpots);
    public int TotalAvailableSpots => _parkingLots.Values.Sum(p => p.AvailableSpots);

    public void AddParkingLot(string name, decimal hourlyRate, int totalSpots)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        
        if (_parkingLots.ContainsKey(name))
            throw new InvalidOperationException($"Parking lot '{name}' already exists");

        _parkingLots[name] = new ParkingLot(name, hourlyRate, totalSpots);
        
        Console.WriteLine($"Added parking lot: {name} ({totalSpots} spots, {hourlyRate:C}/hour)");
    }

    public bool RemoveParkingLot(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        
        if (!_parkingLots.TryGetValue(name, out var lot)) return false;

        // Cannot remove lot with parked vehicles
        if (lot.OccupiedSpots > 0)
            throw new InvalidOperationException($"Cannot remove lot '{name}' - {lot.OccupiedSpots} vehicles still parked");

        if (!_parkingLots.TryRemove(name, out var removedLot)) return false;

        // Remove all vehicles from this lot from global registry
        var vehiclesToRemove = _globalVehicleRegistry
            .Where(kvp => string.Equals(kvp.Value, name, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var licensePlate in vehiclesToRemove)
        {
            _globalVehicleRegistry.TryRemove(licensePlate, out _);
        }

        removedLot.Dispose();
        return true;
    }

    // Entry point for vehicle simulation flow
    public Task<bool> Drive(Vehicle vehicle, string lotName)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        ArgumentException.ThrowIfNullOrWhiteSpace(lotName);
        return DriveVehicleAsync(vehicle, lotName);
    }

    private async Task<bool> DriveVehicleAsync(Vehicle vehicle, string lotName)
    {
        var random = ThreadLocalRandom.Value!;
        var parkingTime = TimeSpan.FromSeconds(random.Next(1, 6));

        Console.WriteLine($"[Veh {vehicle.LicensePlate}] Flow started (managedThread={Environment.CurrentManagedThreadId})");

        // STEP 1: Attempt to park
        var enterResult = await ParkVehicleAsync(vehicle, lotName);
        if (!enterResult.IsSuccess)
        {
            Console.WriteLine($"[Veh {vehicle.LicensePlate}] Failed to enter {lotName}: {enterResult.Message}");
            return false;
        }

        var entryTime = enterResult.Session!.EntryTime;
        Console.WriteLine($"[Veh {vehicle.LicensePlate}] Entered {lotName} at {entryTime:HH:mm:ss}, Spot: {enterResult.AssignedSpotId}");

        Console.WriteLine($"[Veh {vehicle.LicensePlate}] Parking for {parkingTime.TotalSeconds:F0} seconds");
        
        // STEP 2: Simulate parking duration
        await Task.Delay(parkingTime);

        // STEP 3: Exit and calculate fee
        var exitResult = await UnparkVehicleAsync(lotName, vehicle.LicensePlate);
        if (!exitResult.IsSuccess)
        {
            Console.WriteLine($"[Veh {vehicle.LicensePlate}] Exit failed: {exitResult.Message}");
            return false;
        }

        var exitTime = exitResult.CompletedSession!.ExitTime;
        Console.WriteLine($"[Veh {vehicle.LicensePlate}] Exited at {exitTime:HH:mm:ss}, Fee: {exitResult.TotalFee:C}");
        Console.WriteLine($"[Veh {vehicle.LicensePlate}] Flow completed");
        return true;
    }
 
    public async Task<ParkingResult> ParkVehicleAsync(Vehicle vehicle, string parkingLotName)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        ArgumentException.ThrowIfNullOrWhiteSpace(parkingLotName);

        if (!_parkingLots.TryGetValue(parkingLotName, out var lot))
            return ParkingResult.ParkingLotNotFound(parkingLotName);

        var licensePlate = vehicle.LicensePlate;

        if (!_globalVehicleRegistry.TryAdd(licensePlate, parkingLotName))
            return ParkingResult.VehicleAlreadyParked(licensePlate);

        var result = await lot.TryEnterAsync(vehicle);

        return result;
    }

    // Handle vehicle exit with validation
    public async Task<UnparkingResult> UnparkVehicleAsync(string parkingLotName, string licensePlate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parkingLotName);
        if (string.IsNullOrWhiteSpace(licensePlate))
            return UnparkingResult.InvalidLicensePlate();

        if (!_parkingLots.TryGetValue(parkingLotName, out var lot))
            return UnparkingResult.ParkingLotNotFound(parkingLotName);

        // Validate vehicle is in the correct lot
        if (_globalVehicleRegistry.TryGetValue(licensePlate, out var registeredLot) &&
            !string.Equals(registeredLot, parkingLotName, StringComparison.OrdinalIgnoreCase))
        {
            return UnparkingResult.WrongParkingLot(licensePlate, registeredLot, parkingLotName);
        }

        var result = await lot.ExitAsync(licensePlate);
    
        if (result.IsSuccess) _globalVehicleRegistry.TryRemove(licensePlate, out _);
        return result;
    }
    
    // Display system-wide status for debugging/monitoring
    public void PrintSystemStatus()
    {
        Console.WriteLine("\n" + "═".PadLeft(50, '═'));
        Console.WriteLine("           PARKING SYSTEM STATUS");
        Console.WriteLine("═".PadLeft(50, '═'));
        
        foreach (var (name, lot) in _parkingLots)
        {
            Console.WriteLine($"🏢 {name}:");
            Console.WriteLine($"   📊 {lot}");
        }

        Console.WriteLine($"── System: {TotalOccupiedSpots}/{TotalSystemCapacity} occupied ({TotalAvailableSpots} free)");
        
        if (_globalVehicleRegistry.Any())
        {
            Console.WriteLine("── Currently Parked Vehicles:");
            foreach (var (plate, lotName) in _globalVehicleRegistry)
            {
                Console.WriteLine($"    {plate} → {lotName}");
            }
        }
        
        Console.WriteLine("═".PadLeft(50, '═'));
    }

    // Dispose all parking lots and clear registries
    public void Dispose()
    {
        foreach (var lot in _parkingLots.Values)
            lot.Dispose();
        
        _parkingLots.Clear();
        _globalVehicleRegistry.Clear();
    }
}