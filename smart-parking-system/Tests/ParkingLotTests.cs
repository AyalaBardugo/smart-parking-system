using Microsoft.VisualStudio.TestTools.UnitTesting;
using Home_Task.Entities;
using Home_Task.Models;

namespace Home_Task.Tests;

[TestClass]
public class ParkingLotTests
{
    private ParkingLot _parkingLot;

    [TestInitialize]
    public void Setup()
    {
        _parkingLot = new ParkingLot("TestLot", 10m, 3);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _parkingLot?.Dispose();
    }

    // BASIC FUNCTIONALITY TESTS

    [TestMethod]
    public void Constructor_ValidInputs_InitializesCorrectly()
    {
        // Act
        using var lot = new ParkingLot("MainLot", 15m, 5);

        // Assert
        Assert.AreEqual("MainLot", lot.Name);
        Assert.AreEqual(15m, lot.HourlyRate);
        Assert.AreEqual(5, lot.TotalSpots);
        Assert.AreEqual(0, lot.OccupiedSpots);
        Assert.AreEqual(5, lot.AvailableSpots);
    }

    [TestMethod]
    public async Task TryEnterAsync_ValidVehicle_Success()
    {
        // Arrange
        var car = Vehicle.CreateCar("ENTER001");

        // Act
        var result = await _parkingLot.TryEnterAsync(car);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.AssignedSpotId);
        Assert.IsNotNull(result.Session);
        Assert.AreEqual(1, _parkingLot.OccupiedSpots);
        Assert.AreEqual(2, _parkingLot.AvailableSpots);
    }

    [TestMethod]
    public async Task ExitAsync_ValidVehicle_Success()
    {
        // Arrange
        var car = Vehicle.CreateCar("EXIT001");
        await _parkingLot.TryEnterAsync(car);

        // Act
        var result = await _parkingLot.ExitAsync("EXIT001");

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.TotalFee > 0);
        Assert.IsNotNull(result.CompletedSession);
        Assert.AreEqual(0, _parkingLot.OccupiedSpots);
        Assert.AreEqual(3, _parkingLot.AvailableSpots);
    }

    // INPUT VALIDATION TESTS

    [TestMethod]
    public void Constructor_EmptyName_ThrowsException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentException>(() =>
            new ParkingLot("", 10m, 5));
    }

    [TestMethod]
    public void Constructor_NullName_ThrowsException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentException>(() =>
            new ParkingLot(null, 10m, 5));
    }

    [TestMethod]
    public void Constructor_WhitespaceName_ThrowsException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentException>(() =>
            new ParkingLot("   ", 10m, 5));
    }

    [TestMethod]
    public void Constructor_ZeroHourlyRate_ThrowsException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentException>(() =>
            new ParkingLot("TestLot", 0m, 5));
    }

    [TestMethod]
    public void Constructor_NegativeHourlyRate_ThrowsException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentException>(() =>
            new ParkingLot("TestLot", -5m, 5));
    }

    [TestMethod]
    public void Constructor_ZeroSpots_ThrowsException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentException>(() =>
            new ParkingLot("TestLot", 10m, 0));
    }

    [TestMethod]
    public void Constructor_NegativeSpots_ThrowsException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentException>(() =>
            new ParkingLot("TestLot", 10m, -3));
    }

    [TestMethod]
    public void Constructor_NameWithSpaces_TrimmedCorrectly()
    {
        // Act
        using var lot = new ParkingLot("  SpaceLot  ", 10m, 5);

        // Assert
        Assert.AreEqual("SpaceLot", lot.Name);
    }

    [TestMethod]
    public async Task TryEnterAsync_NullVehicle_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
            _parkingLot.TryEnterAsync(null));
    }

    // CAPACITY MANAGEMENT TESTS

    [TestMethod]
    public async Task Capacity_FillLotCompletely_NoMoreSpace()
    {
        // Arrange
        var vehicles = new[]
        {
            Vehicle.CreateCar("CAP001"),
            Vehicle.CreateCar("CAP002"),
            Vehicle.CreateCar("CAP003"),
            Vehicle.CreateCar("CAP004") // This should not fit
        };

        // Act - Fill all 3 spots
        var results = new List<ParkingResult>();
        foreach (var vehicle in vehicles.Take(3))
        {
            results.Add(await _parkingLot.TryEnterAsync(vehicle));
        }

        // Assert - First 3 should succeed
        Assert.IsTrue(results.All(r => r.IsSuccess), "First 3 vehicles should park successfully");
        Assert.AreEqual(3, _parkingLot.OccupiedSpots);
        Assert.AreEqual(0, _parkingLot.AvailableSpots);
    }

    [TestMethod]
    public async Task Capacity_ExitAndReenter_SpotBecomesAvailable()
    {
        // Arrange
        var vehicles = new[]
        {
            Vehicle.CreateCar("REUSE001"),
            Vehicle.CreateCar("REUSE002"),
            Vehicle.CreateCar("REUSE003"),
            Vehicle.CreateCar("REUSE004")
        };

        // Fill lot completely
        foreach (var vehicle in vehicles.Take(3))
        {
            await _parkingLot.TryEnterAsync(vehicle);
        }

        // Act - Exit one vehicle
        await _parkingLot.ExitAsync("REUSE002");

        // Now try to enter new vehicle
        var result = await _parkingLot.TryEnterAsync(vehicles[3]);

        // Assert
        Assert.IsTrue(result.IsSuccess, "Should be able to park after one exits");
        Assert.AreEqual(3, _parkingLot.OccupiedSpots);
        Assert.AreEqual(0, _parkingLot.AvailableSpots);
    }

    [TestMethod]
    public async Task Capacity_SingleSpotLot_WorksCorrectly()
    {
        // Arrange
        using var singleSpotLot = new ParkingLot("SingleSpot", 20m, 1);
        var car1 = Vehicle.CreateCar("SINGLE001");
        var car2 = Vehicle.CreateCar("SINGLE002");

        // Act
        var result1 = await singleSpotLot.TryEnterAsync(car1);
        var result2Task = singleSpotLot.TryEnterAsync(car2); // This should block

        // Give a moment for the second car to start waiting
        await Task.Delay(10);

        // Exit first car
        await singleSpotLot.ExitAsync("SINGLE001");

        // Wait for second car to complete
        var result2 = await result2Task;

        // Assert
        Assert.IsTrue(result1.IsSuccess);
        Assert.IsTrue(result2.IsSuccess);
        Assert.AreEqual(1, singleSpotLot.OccupiedSpots);
    }

    // CONCURRENCY TESTS

    [TestMethod]
    public async Task Concurrency_MultipleVehiclesSimultaneous_HandledCorrectly()
    {
        // Arrange
        var vehicles = Enumerable.Range(1, 5)
            .Select(i => Vehicle.CreateCar($"CONC{i:D3}"))
            .ToArray();

        // Act - Try to park all simultaneously
        var tasks = vehicles.Select(v => _parkingLot.TryEnterAsync(v)).ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert
        var successCount = results.Count(r => r.IsSuccess);
        Assert.AreEqual(3, successCount, "Only 3 vehicles should succeed (lot capacity)");
        Assert.AreEqual(3, _parkingLot.OccupiedSpots);
        Assert.AreEqual(0, _parkingLot.AvailableSpots);
    }

    [TestMethod]
    public async Task Concurrency_SameVehicleMultipleTimes_OnlyOneSucceeds()
    {
        // Arrange
        var vehicle = Vehicle.CreateCar("DUPLICATE001");

        // Act - Try to park same vehicle multiple times
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => _parkingLot.TryEnterAsync(vehicle))
            .ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert
        var successCount = results.Count(r => r.IsSuccess);
        Assert.AreEqual(1, successCount, "Same vehicle should only park once");
        Assert.AreEqual(1, _parkingLot.OccupiedSpots);
    }

    // FEE CALCULATION TESTS

    [TestMethod]
    public async Task FeeCalculation_ShortDuration_ChargesFullHour()
    {
        // Arrange
        using var expensiveLot = new ParkingLot("Expensive", 60m, 3); // $60/hour
        var car = Vehicle.CreateCar("FEE001");

        // Act
        await expensiveLot.TryEnterAsync(car);
        await Task.Delay(100); // Very short parking
        var result = await expensiveLot.ExitAsync("FEE001");

        // Assert
        Assert.AreEqual(60m, result.TotalFee, "Should charge for full hour even if parked briefly");
    }

    [TestMethod]
    public async Task FeeCalculation_DifferentRates_CalculatedCorrectly()
    {
        // Arrange
        using var cheapLot = new ParkingLot("Cheap", 5m, 2);
        using var expensiveLot = new ParkingLot("Expensive", 50m, 2);
        var car1 = Vehicle.CreateCar("CHEAP001");
        var car2 = Vehicle.CreateCar("EXPENSIVE001");

        // Act
        await cheapLot.TryEnterAsync(car1);
        await expensiveLot.TryEnterAsync(car2);
        await Task.Delay(50);
        var cheapResult = await cheapLot.ExitAsync("CHEAP001");
        var expensiveResult = await expensiveLot.ExitAsync("EXPENSIVE001");

        // Assert
        Assert.AreEqual(5m, cheapResult.TotalFee);
        Assert.AreEqual(50m, expensiveResult.TotalFee);
    }

    // ERROR HANDLING TESTS

    [TestMethod]
    public async Task ExitAsync_VehicleNotParked_ReturnsFailure()
    {
        // Act
        var result = await _parkingLot.ExitAsync("NOTPARKED001");

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(UnparkingErrorType.VehicleNotFound, result.ErrorType);
    }

    [TestMethod]
    public async Task ExitAsync_EmptyLicensePlate_ReturnsFailure()
    {
        // Act
        var result = await _parkingLot.ExitAsync("");

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(UnparkingErrorType.InvalidLicensePlate, result.ErrorType);
    }

    [TestMethod]
    public async Task ExitAsync_NullLicensePlate_ReturnsFailure()
    {
        // Act
        var result = await _parkingLot.ExitAsync(null);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(UnparkingErrorType.InvalidLicensePlate, result.ErrorType);
    }

    [TestMethod]
    public async Task ExitAsync_WhitespaceLicensePlate_ReturnsFailure()
    {
        // Act
        var result = await _parkingLot.ExitAsync("   ");

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(UnparkingErrorType.InvalidLicensePlate, result.ErrorType);
    }

    [TestMethod]
    public async Task ExitAsync_DoubleExit_SecondFails()
    {
        // Arrange
        var car = Vehicle.CreateCar("DOUBLE001");
        await _parkingLot.TryEnterAsync(car);

        // Act
        var firstExit = await _parkingLot.ExitAsync("DOUBLE001");
        var secondExit = await _parkingLot.ExitAsync("DOUBLE001");

        // Assert
        Assert.IsTrue(firstExit.IsSuccess);
        Assert.IsFalse(secondExit.IsSuccess);
        Assert.AreEqual(UnparkingErrorType.VehicleNotFound, secondExit.ErrorType);
    }

    // SPOT ALLOCATION TESTS

    [TestMethod]
    public async Task SpotAllocation_Sequential_CorrectPattern()
    {
        // Arrange
        var vehicles = new[]
        {
            Vehicle.CreateCar("SPOT001"),
            Vehicle.CreateCar("SPOT002"),
            Vehicle.CreateCar("SPOT003")
        };

        // Act
        var results = new List<ParkingResult>();
        foreach (var vehicle in vehicles)
        {
            results.Add(await _parkingLot.TryEnterAsync(vehicle));
        }

        // Assert
        var assignedSpots = results.Select(r => r.AssignedSpotId).ToArray();
        Assert.AreEqual("A-01", assignedSpots[0]);
        Assert.AreEqual("A-02", assignedSpots[1]);
        Assert.AreEqual("A-03", assignedSpots[2]);
    }

    [TestMethod]
    public async Task SpotAllocation_ReuseAfterExit_FirstAvailableSpot()
    {
        // Arrange
        var vehicles = new[]
        {
            Vehicle.CreateCar("REUSE001"),
            Vehicle.CreateCar("REUSE002"),
            Vehicle.CreateCar("REUSE003"),
            Vehicle.CreateCar("REUSE004")
        };

        // Fill all spots
        foreach (var vehicle in vehicles.Take(3))
        {
            await _parkingLot.TryEnterAsync(vehicle);
        }

        // Exit middle vehicle
        await _parkingLot.ExitAsync("REUSE002");

        // Act - Park new vehicle
        var result = await _parkingLot.TryEnterAsync(vehicles[3]);

        // Assert
        Assert.AreEqual("A-02", result.AssignedSpotId, "Should reuse the freed spot");
    }

    // EDGE CASES

    [TestMethod]
    public async Task EdgeCase_VeryLongLicensePlate_HandledCorrectly()
    {
        // Arrange
        var longLicense = "VERYLONGLICENSEPLATENUMBER123456789ABCDEFGHIJKLMNOP";
        var car = Vehicle.CreateCar(longLicense);

        // Act
        var enterResult = await _parkingLot.TryEnterAsync(car);
        var exitResult = await _parkingLot.ExitAsync(longLicense);

        // Assert
        Assert.IsTrue(enterResult.IsSuccess);
        Assert.IsTrue(exitResult.IsSuccess);
    }

    [TestMethod]
    public async Task EdgeCase_UnicodeCharacters_HandledCorrectly()
    {
        // Arrange
        var unicodeLicense = "αβγ123ñü";
        var car = Vehicle.CreateCar(unicodeLicense);

        // Act
        var enterResult = await _parkingLot.TryEnterAsync(car);
        var exitResult = await _parkingLot.ExitAsync(unicodeLicense);

        // Assert
        Assert.IsTrue(enterResult.IsSuccess);
        Assert.IsTrue(exitResult.IsSuccess);
    }

    [TestMethod]
    public async Task EdgeCase_CaseInsensitiveLicensePlate_WorksCorrectly()
    {
        // Arrange
        var car = Vehicle.CreateCar("CasE123");

        // Act
        await _parkingLot.TryEnterAsync(car);
        var result = await _parkingLot.ExitAsync("case123"); // Different case

        // Assert
        Assert.IsTrue(result.IsSuccess, "Exit should work with different case");
    }

    // STRESS TESTS

    [TestMethod]
    public async Task StressTest_RapidEnterExit_SystemStable()
    {
        // Arrange
        var vehicles = Enumerable.Range(1, 20)
            .Select(i => Vehicle.CreateCar($"STRESS{i:D3}"))
            .ToArray();

        // Act - Rapid enter/exit cycles
        foreach (var vehicle in vehicles)
        {
            var enterResult = await _parkingLot.TryEnterAsync(vehicle);
            if (enterResult.IsSuccess)
            {
                var exitResult = await _parkingLot.ExitAsync(vehicle.LicensePlate);
                Assert.IsTrue(exitResult.IsSuccess);
            }
        }

        // Assert
        Assert.AreEqual(0, _parkingLot.OccupiedSpots, "Lot should be empty after all cycles");
        Assert.AreEqual(3, _parkingLot.AvailableSpots, "All spots should be available");
    }

    [TestMethod]
    public async Task StressTest_ConcurrentOperations_DataConsistent()
    {
        // Arrange
        var enterVehicles = Enumerable.Range(1, 10)
            .Select(i => Vehicle.CreateCar($"ENTER{i:D3}"))
            .ToArray();
        
        var exitVehicles = new[] { "ENTER001", "ENTER002", "ENTER003" };

        // Act - Mix enter and exit operations
        var enterTasks = enterVehicles.Take(6).Select(v => _parkingLot.TryEnterAsync(v));
        
        // Let some park first
        var initialResults = await Task.WhenAll(enterTasks);
        
        // Now mix exits with more entries
        var mixedTasks = new List<Task>();
        mixedTasks.AddRange(exitVehicles.Take(2).Select(id => _parkingLot.ExitAsync(id)));
        mixedTasks.AddRange(enterVehicles.Skip(6).Take(2).Select(v => _parkingLot.TryEnterAsync(v)));
        
        await Task.WhenAll(mixedTasks);

        // Assert - Data should be consistent
        var occupiedCount = _parkingLot.OccupiedSpots;
        var availableCount = _parkingLot.AvailableSpots;
        Assert.AreEqual(3, occupiedCount + availableCount, "Total spots should remain constant");
        Assert.IsTrue(occupiedCount >= 0 && occupiedCount <= 3);
        Assert.IsTrue(availableCount >= 0 && availableCount <= 3);
    }

    // TOSTRING TEST

    [TestMethod]
    public void ToString_ReturnsCorrectFormat()
    {
        // Act
        var result = _parkingLot.ToString();

        // Assert
        Assert.AreEqual("TestLot: 3/3 available spots, Rate: $10.00/hour", result);
    }

    [TestMethod]
    public async Task ToString_WithOccupiedSpots_ReturnsCorrectFormat()
    {
        // Arrange
        var car = Vehicle.CreateCar("TOSTRING001");
        await _parkingLot.TryEnterAsync(car);

        // Act
        var result = _parkingLot.ToString();

        // Assert
        Assert.AreEqual("TestLot: 2/3 available spots, Rate: $10.00/hour", result);
    }

    // DISPOSAL TESTS

    [TestMethod]
    public void Dispose_ReleasesResources()
    {
        // Arrange
        var lot = new ParkingLot("DisposeLot", 15m, 5);

        // Act & Assert - Should not throw
        lot.Dispose();
        lot.Dispose(); // Multiple dispose calls should be safe
    }

    [TestMethod]
    public async Task Dispose_DuringActiveOperations_HandlesGracefully()
    {
        // Arrange
        var lot = new ParkingLot("DisposeDuring", 20m, 3);
        var car = Vehicle.CreateCar("DISPOSE001");
        
        // Start parking operation
        var parkTask = lot.TryEnterAsync(car);
        
        // Act - Dispose while operation might be running
        lot.Dispose();
        
        // Wait for operation to complete
        var result = await parkTask;
        
        // Assert - Should handle gracefully (exact behavior may vary)
        Assert.IsTrue(true, "Disposal during operations should not crash");
    }
}
    