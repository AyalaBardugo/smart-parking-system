using Microsoft.VisualStudio.TestTools.UnitTesting;
using Home_Task.Entities;
using Home_Task.Services;
using Home_Task.Models;
using System.Linq;

namespace Home_Task.Tests;

[TestClass]
public class ParkingSystemManagerTests
{
    private ParkingSystemManager _manager;

    [TestInitialize]
    public void Setup()
    {
        _manager = new ParkingSystemManager();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _manager?.Dispose();
    }

    // BASIC FUNCTIONALITY TESTS

    [TestMethod]
    public void AddParkingLot_ValidInputs_CreatesLot()
    {
        // Act
        _manager.AddParkingLot("TestLot", 10m, 5);

        // Assert
        Assert.AreEqual(5, _manager.TotalSystemCapacity);
        Assert.AreEqual(0, _manager.TotalOccupiedSpots);
        Assert.AreEqual(5, _manager.TotalAvailableSpots);
    }

    [TestMethod]
    public void AddParkingLot_MultipleLots_SumsCapacityCorrectly()
    {
        // Act
        _manager.AddParkingLot("Lot1", 10m, 3);
        _manager.AddParkingLot("Lot2", 15m, 5);
        _manager.AddParkingLot("Lot3", 20m, 2);

        // Assert
        Assert.AreEqual(10, _manager.TotalSystemCapacity); // 3 + 5 + 2
        Assert.AreEqual(0, _manager.TotalOccupiedSpots);
        Assert.AreEqual(10, _manager.TotalAvailableSpots);
    }

    [TestMethod]
    public async Task ParkVehicleAsync_ValidRequest_Success()
    {
        // Arrange
        _manager.AddParkingLot("TestLot", 10m, 5);
        var car = Vehicle.CreateCar("TEST123");

        // Act
        var result = await _manager.ParkVehicleAsync(car, "TestLot");

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.AssignedSpotId);
        Assert.IsNotNull(result.Session);
        Assert.AreEqual(1, _manager.TotalOccupiedSpots);
        Assert.AreEqual(4, _manager.TotalAvailableSpots);
    }

    [TestMethod]
    public async Task UnparkVehicleAsync_ValidRequest_Success()
    {
        // Arrange
        _manager.AddParkingLot("TestLot", 12m, 5);
        var car = Vehicle.CreateCar("TEST123");
        await _manager.ParkVehicleAsync(car, "TestLot");
        await Task.Delay(10);

        // Act
        var result = await _manager.UnparkVehicleAsync("TestLot", "TEST123");

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(result.TotalFee > 0);
        Assert.IsNotNull(result.CompletedSession);
        Assert.AreEqual(0, _manager.TotalOccupiedSpots);
        Assert.AreEqual(5, _manager.TotalAvailableSpots);
    }

    // INPUT VALIDATION TESTS

    [TestMethod]
    public void AddParkingLot_EmptyName_ThrowsException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentException>(() =>
            _manager.AddParkingLot("", 10m, 5));
    }

    [TestMethod]
    public void AddParkingLot_NullName_ThrowsException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            _manager.AddParkingLot(null, 10m, 5));
    }

    [TestMethod]
    public void AddParkingLot_DuplicateName_ThrowsException()
    {
        // Arrange
        _manager.AddParkingLot("SameName", 10m, 3);

        // Act & Assert
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            _manager.AddParkingLot("SameName", 15m, 5));
    }

    [TestMethod]
    public void AddParkingLot_CaseInsensitiveDuplicate_ThrowsException()
    {
        // Arrange
        _manager.AddParkingLot("TestLot", 10m, 3);

        // Act & Assert
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            _manager.AddParkingLot("testlot", 15m, 5));
    }

    [TestMethod]
    public void AddParkingLot_ZeroSpots_ThrowsException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentException>(() =>
            _manager.AddParkingLot("ZeroLot", 10m, 0));
    }

    [TestMethod]
    public void AddParkingLot_NegativeRate_ThrowsException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentException>(() =>
            _manager.AddParkingLot("NegativeRate", -10m, 5));
    }

    // PARKING VALIDATION TESTS

    [TestMethod]
    public async Task ParkVehicleAsync_NonExistentLot_ReturnsFailure()
    {
        // Arrange
        var car = Vehicle.CreateCar("TEST123");

        // Act
        var result = await _manager.ParkVehicleAsync(car, "NonExistentLot");

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ParkingErrorType.ParkingLotNotFound, result.ErrorType);
    }

    [TestMethod]
    public async Task ParkVehicleAsync_AlreadyParkedSameLot_ReturnsFailure()
    {
        // Arrange
        _manager.AddParkingLot("TestLot", 10m, 5);
        var car = Vehicle.CreateCar("DUPLICATE");
        await _manager.ParkVehicleAsync(car, "TestLot");

        // Act
        var result = await _manager.ParkVehicleAsync(car, "TestLot");

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ParkingErrorType.VehicleAlreadyParked, result.ErrorType);
        Assert.AreEqual(1, _manager.TotalOccupiedSpots);
    }

    [TestMethod]
    public async Task ParkVehicleAsync_AlreadyParkedDifferentLot_ReturnsFailure()
    {
        // Arrange
        _manager.AddParkingLot("LotA", 10m, 5);
        _manager.AddParkingLot("LotB", 15m, 5);
        var car = Vehicle.CreateCar("GLOBAL001");
        await _manager.ParkVehicleAsync(car, "LotA");

        // Act
        var result = await _manager.ParkVehicleAsync(car, "LotB");

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ParkingErrorType.VehicleAlreadyParked, result.ErrorType);
        Assert.AreEqual(1, _manager.TotalOccupiedSpots);
    }

    [TestMethod]
    public async Task ParkVehicleAsync_NullVehicle_ThrowsException()
    {
        // Arrange
        _manager.AddParkingLot("TestLot", 10m, 5);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
            _manager.ParkVehicleAsync(null, "TestLot"));
    }

    // UNPARKING VALIDATION TESTS

    [TestMethod]
    public async Task UnparkVehicleAsync_NonExistentLot_ReturnsFailure()
    {
        // Act
        var result = await _manager.UnparkVehicleAsync("NonExistentLot", "TEST123");

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(UnparkingErrorType.ParkingLotNotFound, result.ErrorType);
    }

    [TestMethod]
    public async Task UnparkVehicleAsync_VehicleNotParked_ReturnsFailure()
    {
        // Arrange
        _manager.AddParkingLot("TestLot", 10m, 5);

        // Act
        var result = await _manager.UnparkVehicleAsync("TestLot", "NONEXISTENT");

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(UnparkingErrorType.VehicleNotFound, result.ErrorType);
    }

    [TestMethod]
    public async Task UnparkVehicleAsync_WrongLot_ReturnsFailure()
    {
        // Arrange
        _manager.AddParkingLot("LotA", 10m, 3);
        _manager.AddParkingLot("LotB", 15m, 3);
        var car = Vehicle.CreateCar("WRONG001");
        await _manager.ParkVehicleAsync(car, "LotA");

        // Act
        var result = await _manager.UnparkVehicleAsync("LotB", "WRONG001");

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(UnparkingErrorType.WrongParkingLot, result.ErrorType);
        Assert.AreEqual(1, _manager.TotalOccupiedSpots);
    }

    [TestMethod]
    public async Task UnparkVehicleAsync_EmptyLicensePlate_ReturnsFailure()
    {
        // Arrange
        _manager.AddParkingLot("TestLot", 10m, 3);

        // Act
        var result = await _manager.UnparkVehicleAsync("TestLot", "");

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(UnparkingErrorType.InvalidLicensePlate, result.ErrorType);
    }

    // REMOVE PARKING LOT TESTS

    [TestMethod]
    public void RemoveParkingLot_EmptyLot_Success()
    {
        // Arrange
        _manager.AddParkingLot("ToRemove", 10m, 3);
        Assert.AreEqual(3, _manager.TotalSystemCapacity);

        // Act
        var result = _manager.RemoveParkingLot("ToRemove");

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(0, _manager.TotalSystemCapacity);
    }

    [TestMethod]
    public async Task RemoveParkingLot_WithParkedVehicles_ThrowsException()
    {
        // Arrange
        _manager.AddParkingLot("CantRemove", 10m, 3);
        var car = Vehicle.CreateCar("PARKED001");
        await _manager.ParkVehicleAsync(car, "CantRemove");

        // Act & Assert
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            _manager.RemoveParkingLot("CantRemove"));
    }

    [TestMethod]
    public void RemoveParkingLot_NonExistentLot_ReturnsFalse()
    {
        // Act
        var result = _manager.RemoveParkingLot("DoesNotExist");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task RemoveParkingLot_CleansGlobalRegistry()
    {
        // Arrange
        _manager.AddParkingLot("ToRemove", 10m, 2);
        _manager.AddParkingLot("ToKeep", 15m, 2);
        var car1 = Vehicle.CreateCar("CLEANUP001");
        var car2 = Vehicle.CreateCar("CLEANUP002");
        
        await _manager.ParkVehicleAsync(car1, "ToRemove");
        await _manager.ParkVehicleAsync(car2, "ToKeep");
        await _manager.UnparkVehicleAsync("ToRemove", "CLEANUP001");

        // Act
        _manager.RemoveParkingLot("ToRemove");

        // Now car1 should be able to park elsewhere
        var result = await _manager.ParkVehicleAsync(car1, "ToKeep");

        // Assert
        Assert.IsTrue(result.IsSuccess, "Vehicle should be able to park after lot removal");
        
        // car2 should still be tracked as parked
        var car2Result = await _manager.ParkVehicleAsync(car2, "ToKeep");
        Assert.IsFalse(car2Result.IsSuccess, "Vehicle2 should still be tracked as parked");
    }

    // DRIVE FUNCTIONALITY TESTS

    [TestMethod]
    public async Task Drive_SingleVehicle_CompletesSuccessfully()
    {
        // Arrange
        _manager.AddParkingLot("DriveTest", 15m, 5);
        var car = Vehicle.CreateCar("DRIVE001");

        // Act
        var result = await _manager.Drive(car, "DriveTest");

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual(0, _manager.TotalOccupiedSpots);
    }

    [TestMethod]
    public async Task Drive_NonExistentLot_Fails()
    {
        // Arrange
        var car = Vehicle.CreateCar("FAIL001");

        // Act
        var result = await _manager.Drive(car, "NonExistentLot");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task Drive_MultipleConcurrentVehicles_AllComplete()
    {
        // Arrange
        _manager.AddParkingLot("ConcurrentTest", 20m, 10);
        var vehicles = Enumerable.Range(1, 5)
            .Select(i => Vehicle.CreateCar($"CONCURRENT{i:D3}"))
            .ToArray();

        // Act
        var tasks = vehicles.Select(v => _manager.Drive(v, "ConcurrentTest")).ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.IsTrue(results.All(r => r), "All drives should complete successfully");
        Assert.AreEqual(0, _manager.TotalOccupiedSpots);
    }

    [TestMethod]
    public async Task Drive_SameVehicleMultipleTimes_OnlyOneSucceeds()
    {
        // Arrange
        _manager.AddParkingLot("DuplicateTest", 15m, 5);
        var car = Vehicle.CreateCar("DUPLICATE001");

        // Act
        var task1 = _manager.Drive(car, "DuplicateTest");
        var task2 = _manager.Drive(car, "DuplicateTest");
        var results = await Task.WhenAll(task1, task2);

        // Assert
        var successCount = results.Count(r => r);
        Assert.AreEqual(1, successCount, "Only one drive should succeed for same vehicle");
        Assert.AreEqual(0, _manager.TotalOccupiedSpots);
    }

    [TestMethod]
    public async Task Drive_CapacityLimited_HandlesCorrectly()
    {
        // Arrange
        _manager.AddParkingLot("CapacityTest", 25m, 2);
        var vehicles = Enumerable.Range(1, 4)
            .Select(i => Vehicle.CreateCar($"CAPACITY{i:D3}"))
            .ToArray();

        // Act
        var tasks = vehicles.Select(v => _manager.Drive(v, "CapacityTest")).ToArray();
        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.IsTrue(results.All(r => r), "All drives should eventually complete");
        Assert.AreEqual(0, _manager.TotalOccupiedSpots);
    }

    // GLOBAL REGISTRY TESTS

    [TestMethod]
    public async Task GlobalRegistry_PreventsCrossLotParking()
    {
        // Arrange
        _manager.AddParkingLot("LotA", 10m, 3);
        _manager.AddParkingLot("LotB", 15m, 3);
        var car = Vehicle.CreateCar("GLOBAL001");
        await _manager.ParkVehicleAsync(car, "LotA");

        // Act
        var result = await _manager.ParkVehicleAsync(car, "LotB");

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(ParkingErrorType.VehicleAlreadyParked, result.ErrorType);
    }

    [TestMethod]
    public async Task GlobalRegistry_AllowsReparkingAfterExit()
    {
        // Arrange
        _manager.AddParkingLot("LotA", 10m, 3);
        _manager.AddParkingLot("LotB", 15m, 3);
        var car = Vehicle.CreateCar("REPARK001");
        
        await _manager.ParkVehicleAsync(car, "LotA");
        await _manager.UnparkVehicleAsync("LotA", "REPARK001");

        // Act
        var result = await _manager.ParkVehicleAsync(car, "LotB");

        // Assert
        Assert.IsTrue(result.IsSuccess, "Should be able to park in different lot after exit");
    }

    [TestMethod]
    public async Task GlobalRegistry_CaseInsensitive()
    {
        // Arrange
        _manager.AddParkingLot("TestLot", 10m, 5);
        var car1 = Vehicle.CreateCar("CasE123");
        var car2 = Vehicle.CreateCar("case123");
        await _manager.ParkVehicleAsync(car1, "TestLot");

        // Act
        var result = await _manager.ParkVehicleAsync(car2, "TestLot");

        // Assert
        Assert.IsFalse(result.IsSuccess, "Should treat different case as same vehicle");
        Assert.AreEqual(ParkingErrorType.VehicleAlreadyParked, result.ErrorType);
    }

    // COMPLEX WORKFLOW TESTS

    [TestMethod]
    public async Task CompleteFlow_ParkUnparkMultipleVehicles_WorksCorrectly()
    {
        // Arrange
        _manager.AddParkingLot("FlowTest", 15m, 3);
        var vehicles = new[]
        {
            Vehicle.CreateCar("FLOW001"),
            Vehicle.CreateMotorcycle("FLOW002"),
            Vehicle.CreateTruck("FLOW003")
        };

        // Act - Park all vehicles
        foreach (var vehicle in vehicles)
        {
            var parkResult = await _manager.ParkVehicleAsync(vehicle, "FlowTest");
            Assert.IsTrue(parkResult.IsSuccess, $"Failed to park {vehicle.LicensePlate}");
        }

        Assert.AreEqual(3, _manager.TotalOccupiedSpots);
        Assert.AreEqual(0, _manager.TotalAvailableSpots);

        // Act - Unpark all vehicles
        foreach (var vehicle in vehicles)
        {
            await Task.Delay(10);
            var unparkResult = await _manager.UnparkVehicleAsync("FlowTest", vehicle.LicensePlate);
            Assert.IsTrue(unparkResult.IsSuccess, $"Failed to unpark {vehicle.LicensePlate}");
            Assert.IsTrue(unparkResult.TotalFee > 0);
        }

        // Assert
        Assert.AreEqual(0, _manager.TotalOccupiedSpots);
        Assert.AreEqual(3, _manager.TotalAvailableSpots);
    }

    [TestMethod]
    public async Task ComplexFlow_MixedOperationsMultipleLots_SystemStable()
    {
        // Arrange
        _manager.AddParkingLot("LotA", 10m, 2);
        _manager.AddParkingLot("LotB", 20m, 2);
        
        var tasks = new List<Task>();
        
        // Mix of drive and manual operations
        tasks.Add(_manager.Drive(Vehicle.CreateCar("DRIVE001"), "LotA"));
        tasks.Add(_manager.Drive(Vehicle.CreateCar("DRIVE002"), "LotB"));
        
        tasks.Add(Task.Run(async () =>
        {
            var car = Vehicle.CreateCar("MANUAL001");
            await _manager.ParkVehicleAsync(car, "LotA");
            await Task.Delay(20);
            await _manager.UnparkVehicleAsync("LotA", "MANUAL001");
        }));

        // Act
        await Task.WhenAll(tasks);

        // Assert
        Assert.AreEqual(0, _manager.TotalOccupiedSpots);
        Assert.AreEqual(4, _manager.TotalAvailableSpots);
    }

    // SYSTEM STATUS TESTS

    [TestMethod]
    public async Task SystemStatus_ConsistentThroughoutOperations()
    {
        // Arrange
        _manager.AddParkingLot("StatusTest", 10m, 2);
        var car1 = Vehicle.CreateCar("STATUS001");
        var car2 = Vehicle.CreateCar("STATUS002");

        // Act & Assert - Step by step verification
        Assert.AreEqual(0, _manager.TotalOccupiedSpots);
        Assert.AreEqual(2, _manager.TotalAvailableSpots);

        await _manager.ParkVehicleAsync(car1, "StatusTest");
        Assert.AreEqual(1, _manager.TotalOccupiedSpots);
        Assert.AreEqual(1, _manager.TotalAvailableSpots);

        await _manager.ParkVehicleAsync(car2, "StatusTest");
        Assert.AreEqual(2, _manager.TotalOccupiedSpots);
        Assert.AreEqual(0, _manager.TotalAvailableSpots);

        await _manager.UnparkVehicleAsync("StatusTest", "STATUS001");
        Assert.AreEqual(1, _manager.TotalOccupiedSpots);
        Assert.AreEqual(1, _manager.TotalAvailableSpots);

        await _manager.UnparkVehicleAsync("StatusTest", "STATUS002");
        Assert.AreEqual(0, _manager.TotalOccupiedSpots);
        Assert.AreEqual(2, _manager.TotalAvailableSpots);
    }

    [TestMethod]
    public void PrintSystemStatus_DoesNotThrow()
    {
        // Arrange
        _manager.AddParkingLot("StatusLot", 10m, 3);

        // Act & Assert - Should not throw exception
        _manager.PrintSystemStatus();
    }

    // EDGE CASES AND STRESS TESTS

    [TestMethod]
    public async Task EdgeCase_VeryLongNames_HandledCorrectly()
    {
        // Arrange
        var longLotName = "VeryLongParkingLotNameThatExceedsNormalLength123456789";
        var longLicensePlate = "VERYLONGLICENSEPLATENUMBER123456789ABCDEFGHIJK";
        
        _manager.AddParkingLot(longLotName, 25m, 3);
        var car = Vehicle.CreateCar(longLicensePlate);

        // Act
        var parkResult = await _manager.ParkVehicleAsync(car, longLotName);
        var unparkResult = await _manager.UnparkVehicleAsync(longLotName, longLicensePlate);

        // Assert
        Assert.IsTrue(parkResult.IsSuccess);
        Assert.IsTrue(unparkResult.IsSuccess);
    }

    [TestMethod]
    public async Task EdgeCase_UnicodeCharacters_HandledCorrectly()
    {
        // Arrange
        var unicodeLotName = "חניון-בעברית-α-β-γ";
        var unicodeLicense = "אבג-123-ñü";
        
        _manager.AddParkingLot(unicodeLotName, 30m, 2);
        var car = Vehicle.CreateCar(unicodeLicense);

        // Act
        var parkResult = await _manager.ParkVehicleAsync(car, unicodeLotName);
        var unparkResult = await _manager.UnparkVehicleAsync(unicodeLotName, unicodeLicense);

        // Assert
        Assert.IsTrue(parkResult.IsSuccess);
        Assert.IsTrue(unparkResult.IsSuccess);
    }

    [TestMethod]
    public async Task StressTest_ManyLotsAndVehicles_SystemStable()
    {
        // Arrange - Create multiple lots
        for (int i = 1; i <= 5; i++)
        {
            _manager.AddParkingLot($"Lot{i}", i * 10m, 2);
        }

        var vehicles = Enumerable.Range(1, 15)
            .Select(i => Vehicle.CreateCar($"STRESS{i:D3}"))
            .ToArray();

        // Act - Mix of operations across different lots
        var tasks = new List<Task>();
        
        for (int i = 0; i < vehicles.Length; i++)
        {
            var lotName = $"Lot{(i % 5) + 1}";
            tasks.Add(_manager.Drive(vehicles[i], lotName));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.AreEqual(0, _manager.TotalOccupiedSpots, "All vehicles should complete");
        Assert.AreEqual(10, _manager.TotalAvailableSpots, "All spots should be available");
    }

    [TestMethod]
    public async Task StressTest_RapidParkUnparkCycles_DataConsistent()
    {
        // Arrange
        _manager.AddParkingLot("RapidTest", 20m, 3);
        var vehicle = Vehicle.CreateCar("RAPID001");

        // Act - Rapid park/unpark cycles
        for (int cycle = 0; cycle < 10; cycle++)
        {
            var parkResult = await _manager.ParkVehicleAsync(vehicle, "RapidTest");
            Assert.IsTrue(parkResult.IsSuccess, $"Park cycle {cycle} should succeed");
            Assert.AreEqual(1, _manager.TotalOccupiedSpots);

            var unparkResult = await _manager.UnparkVehicleAsync("RapidTest", "RAPID001");
            Assert.IsTrue(unparkResult.IsSuccess, $"Unpark cycle {cycle} should succeed");
            Assert.AreEqual(0, _manager.TotalOccupiedSpots);
        }

        // Assert
        Assert.AreEqual(3, _manager.TotalAvailableSpots);
    }

    // DISPOSAL TESTS

    [TestMethod]
    public void Dispose_ReleasesResources()
    {
        // Arrange
        _manager.AddParkingLot("DisposeLot", 15m, 5);

        // Act & Assert - Should not throw
        _manager.Dispose();
        _manager.Dispose(); // Multiple dispose calls should be safe
    }

    [TestMethod]
    public async Task Dispose_WithActiveOperations_HandlesGracefully()
    {
        // Arrange
        _manager.AddParkingLot("DisposeTest", 10m, 3);
        var car = Vehicle.CreateCar("DISPOSE001");
        
        // Start operation
        var driveTask = _manager.Drive(car, "DisposeTest");
        
        // Act - Dispose while operation might be running
        _manager.Dispose();
        
        // Wait for operation to complete
        var result = await driveTask;
        
        // Assert - Should handle gracefully
        Assert.IsTrue(true, "Disposal during operations should not crash");
    }
}