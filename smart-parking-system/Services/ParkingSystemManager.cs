namespace Home_Task.Services;

using Home_Task.Entities;
using Home_Task.Models;
using System.Collections.Concurrent;

public sealed class ParkingSystemManager : IDisposable
{
    private readonly Dictionary<string, ParkingLot> _parkingLots = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _globalVehicleRegistry = 
        new(StringComparer.OrdinalIgnoreCase);
    
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

        if (lot.OccupiedSpots > 0)
            throw new InvalidOperationException($"Cannot remove lot '{name}' - {lot.OccupiedSpots} vehicles still parked");

        lot.Dispose();
        return _parkingLots.Remove(name);
        
    }

    public async Task<ParkingResult> ProcessVehicleAsync(Vehicle vehicle, string parkingLotName)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        ArgumentException.ThrowIfNullOrWhiteSpace(parkingLotName);

        if (!_parkingLots.TryGetValue(parkingLotName, out var lot))
            return ParkingResult.ParkingLotNotFound(parkingLotName);

        var licensePlate = vehicle.LicensePlate;

        // Check global registry - prevent parking in multiple lots
        if (_globalVehicleRegistry.TryGetValue(licensePlate, out var currentLot))
            return ParkingResult.VehicleAlreadyParked(licensePlate);

        var result = await lot.TryEnterAsync(vehicle);
    
        if (result.IsSuccess)
            // Safe to add - already verified vehicle is not in global registry above
            _globalVehicleRegistry.TryAdd(licensePlate, parkingLotName);

        return result;
    }

    public async Task<UnparkingResult> UnparkAsync(string parkingLotName, string licensePlate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parkingLotName);
        ArgumentException.ThrowIfNullOrWhiteSpace(licensePlate);

        if (!_parkingLots.TryGetValue(parkingLotName, out var lot))
            return UnparkingResult.ParkingLotNotFound(parkingLotName);

        // Validate vehicle is in the correct lot
        if (_globalVehicleRegistry.TryGetValue(licensePlate, out var registeredLot) &&
            !string.Equals(registeredLot, parkingLotName, StringComparison.OrdinalIgnoreCase))
        {
            return UnparkingResult.WrongParkingLot(licensePlate, registeredLot, parkingLotName);
        }

        var result = await lot.ExitAsync(licensePlate);
    
        if (result.IsSuccess)
            // Safe to remove - already verified vehicle exists in global registry above
            _globalVehicleRegistry.TryRemove(licensePlate, out _);

        return result;
    }
    
    
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

    public void Dispose()
    {
        foreach (var lot in _parkingLots.Values)
            lot.Dispose();
        
        _parkingLots.Clear();
        _globalVehicleRegistry.Clear();
    }
}