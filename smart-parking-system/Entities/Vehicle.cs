namespace Home_Task.Entities;

public class Vehicle : IEquatable<Vehicle>
{
    public string LicensePlate { get; }
    public VehicleType VehicleType { get; }
    
    private Vehicle(string licensePlate, VehicleType vehicleType)
    {
        LicensePlate = string.IsNullOrWhiteSpace(licensePlate) 
            ? throw new ArgumentException("License plate cannot be empty", nameof(licensePlate))
            : licensePlate.Trim();
        
        VehicleType = vehicleType;
    }
    
    public static Vehicle CreateCar(string licensePlate) => new(licensePlate, VehicleType.Car);
    public static Vehicle CreateMotorcycle(string licensePlate) => new(licensePlate, VehicleType.Motorcycle);
    public static Vehicle CreateTruck(string licensePlate) => new(licensePlate, VehicleType.Truck);

    public override string ToString() => $"{VehicleType}: {LicensePlate}";

    public bool Equals(Vehicle? other) => 
        other is not null && 
        string.Equals(LicensePlate, other.LicensePlate, StringComparison.OrdinalIgnoreCase);
    
    public override bool Equals(object? obj) => Equals(obj as Vehicle);
    
    public override int GetHashCode() => 
        StringComparer.OrdinalIgnoreCase.GetHashCode(LicensePlate);
}