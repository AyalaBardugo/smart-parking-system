namespace Home_Task.Models;

using Home_Task.Entities;

public class ParkingResult
{
    public bool IsSuccess { get; }
    public string Message { get; }
    public string? AssignedSpotId { get; }
    public ParkingSession? Session { get; }
    public ParkingErrorType? ErrorType { get; }

    private ParkingResult(bool success, string message, string? assignedSpotId = null, 
        ParkingSession? session = null, ParkingErrorType? errorType = null)
    {
        IsSuccess = success;              
        Message = message;
        AssignedSpotId = assignedSpotId;
        Session = session;
        ErrorType = errorType;
    }

    public static ParkingResult Success(string spotId, ParkingSession session) =>
        new(true, $"Vehicle {session.Vehicle.LicensePlate} parked in spot {spotId}", spotId, session);

    public static ParkingResult NoSpotsAvailable() =>
        new(false, "No free spot found", errorType: ParkingErrorType.NoSpotsAvailable);

    public static ParkingResult VehicleAlreadyParked(string licensePlate) =>
        new(false, $"Vehicle {licensePlate} is already parked", errorType: ParkingErrorType.VehicleAlreadyParked);

    public static ParkingResult SpotAssignmentFailed(string spotId) =>
        new(false, $"Spot {spotId} assignment failed", errorType: ParkingErrorType.SpotAssignmentFailed);

    public static ParkingResult ParkingLotNotFound(string lotName) =>
        new(false, $"Parking lot '{lotName}' not found", errorType: ParkingErrorType.ParkingLotNotFound);

    public override string ToString() => Message;
}

public enum ParkingErrorType
{
    NoSpotsAvailable,
    VehicleAlreadyParked,
    SpotAssignmentFailed,
    ParkingLotNotFound
}