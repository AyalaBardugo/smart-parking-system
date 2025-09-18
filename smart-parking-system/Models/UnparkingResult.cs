namespace Home_Task.Models;

using Home_Task.Entities;

public class UnparkingResult
{
    public bool IsSuccess { get; }
    public string Message { get; }
    public decimal TotalFee { get; }
    public TimeSpan ParkingDuration { get; }
    public ParkingSession? CompletedSession { get; }
    public UnparkingErrorType? ErrorType { get; }

    private UnparkingResult(bool success, string message, decimal totalFee = 0,
                            TimeSpan parkingDuration = default, ParkingSession? completedSession = null,
                            UnparkingErrorType? errorType = null)
    {
        IsSuccess = success;
        Message = message;
        TotalFee = totalFee;
        ParkingDuration = parkingDuration;
        CompletedSession = completedSession;
        ErrorType = errorType;
    }

    public static UnparkingResult Success(decimal totalFee, TimeSpan duration, ParkingSession completedSession) =>
        new(true, $"Vehicle {completedSession.Vehicle.LicensePlate} successfully unparked. Duration: {duration:hh\\:mm}, Fee: {totalFee:C}",
            totalFee, duration, completedSession);

    public static UnparkingResult VehicleNotFound(string licensePlate) =>
        new(false, $"Vehicle {licensePlate} is not currently parked", 
            errorType: UnparkingErrorType.VehicleNotFound);

    public static UnparkingResult SessionAlreadyEnded(string licensePlate) =>
        new(false, $"Parking session for vehicle {licensePlate} has already ended", 
            errorType: UnparkingErrorType.SessionAlreadyEnded);

    public static UnparkingResult InvalidLicensePlate() =>
        new(false, "License plate cannot be empty", 
            errorType: UnparkingErrorType.InvalidLicensePlate);

    public static UnparkingResult WrongParkingLot(string licensePlate, string expectedLot, string attemptedLot) =>
        new(false, $"Vehicle {licensePlate} is parked in {expectedLot}, not in {attemptedLot}",
            errorType: UnparkingErrorType.WrongParkingLot);
    
    public static UnparkingResult ParkingLotNotFound(string lotName) =>
        new(false, $"Parking lot '{lotName}' not found", 
            errorType: UnparkingErrorType.ParkingLotNotFound);

    public string GetReceiptSummary() =>
        IsSuccess ? 
        $"""
        ═══════════════════════════════
                PARKING RECEIPT
        ═══════════════════════════════
        Vehicle: {CompletedSession!.Vehicle.LicensePlate}
        Spot:    {CompletedSession!.ParkingSpot.SpotId}
        From:    {CompletedSession!.EntryTime:yyyy-MM-dd HH:mm}
        To:      {CompletedSession!.ExitTime:yyyy-MM-dd HH:mm}
        Time:    {ParkingDuration:hh\:mm}
        Total:   {TotalFee:C}
        ═══════════════════════════════
        """ : 
        $"Failed: {Message}";

    public override string ToString() => Message;

}

public enum UnparkingErrorType
{
    VehicleNotFound,
    SessionAlreadyEnded,
    InvalidLicensePlate,
    WrongParkingLot,
    ParkingLotNotFound
}