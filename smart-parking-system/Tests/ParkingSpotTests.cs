using Microsoft.VisualStudio.TestTools.UnitTesting;
using Home_Task.Entities;

namespace Home_Task.Tests;

[TestClass]
public class ParkingSpotTests
{
    // BASIC FUNCTIONALITY TESTS

    [TestMethod]
    public void Constructor_ValidSpotId_InitializesCorrectly()
    {
        // Act
        var spot = new ParkingSpot("A-01");

        // Assert
        Assert.AreEqual("A-01", spot.SpotId);
        Assert.IsFalse(spot.IsOccupied);
        Assert.IsNull(spot.CurrentVehicle);
    }

    [TestMethod]
    public void Assign_EmptySpot_Success()
    {
        // Arrange
        var spot = new ParkingSpot("A-01");
        var car = Vehicle.CreateCar("TEST123");

        // Act
        var result = spot.Assign(car);

        // Assert
        Assert.IsTrue(result);
        Assert.IsTrue(spot.IsOccupied);
        Assert.AreEqual(car, spot.CurrentVehicle);
    }

    [TestMethod]
    public void Release_OccupiedSpot_Success()
    {
        // Arrange
        var spot = new ParkingSpot("A-01");
        var car = Vehicle.CreateCar("TEST123");
        spot.Assign(car);

        // Act
        var result = spot.Release();

        // Assert
        Assert.IsTrue(result);
        Assert.IsFalse(spot.IsOccupied);
        Assert.IsNull(spot.CurrentVehicle);
    }

    // EDGE CASES AND ERROR CONDITIONS

    [TestMethod]
    public void Assign_OccupiedSpot_Fails()
    {
        // Arrange
        var spot = new ParkingSpot("A-01");
        var car1 = Vehicle.CreateCar("FIRST");
        var car2 = Vehicle.CreateCar("SECOND");
        spot.Assign(car1);

        // Act
        var result = spot.Assign(car2);

        // Assert
        Assert.IsFalse(result);
        Assert.AreEqual(car1, spot.CurrentVehicle); // Still first car
        Assert.IsTrue(spot.IsOccupied);
    }

    [TestMethod]
    public void Release_EmptySpot_Fails()
    {
        // Arrange
        var spot = new ParkingSpot("A-01");

        // Act
        var result = spot.Release();

        // Assert
        Assert.IsFalse(result);
        Assert.IsFalse(spot.IsOccupied);
        Assert.IsNull(spot.CurrentVehicle);
    }

    [TestMethod]
    public void Assign_NullVehicle_ThrowsException()
    {
        // Arrange
        var spot = new ParkingSpot("A-01");

        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            spot.Assign(null));
    }

    // INPUT VALIDATION TESTS

    [TestMethod]
    public void Constructor_EmptySpotId_ThrowsException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentException>(() =>
            new ParkingSpot(""));
    }

    [TestMethod]
    public void Constructor_NullSpotId_ThrowsException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentException>(() =>
            new ParkingSpot(null));
    }

    [TestMethod]
    public void Constructor_WhitespaceSpotId_ThrowsException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentException>(() =>
            new ParkingSpot("   "));
    }

    [TestMethod]
    public void Constructor_WithLeadingTrailingSpaces_TrimmedCorrectly()
    {
        // Act
        var spot = new ParkingSpot("  A-01  ");

        // Assert
        Assert.AreEqual("A-01", spot.SpotId);
    }

    // EQUALITY TESTS

    [TestMethod]
    public void Equals_SameSpotId_AreEqual()
    {
        // Arrange
        var spot1 = new ParkingSpot("A-01");
        var spot2 = new ParkingSpot("A-01");

        // Act & Assert
        Assert.AreEqual(spot1, spot2);
        Assert.AreEqual(spot1.GetHashCode(), spot2.GetHashCode());
    }

    [TestMethod]
    public void Equals_SameSpotIdDifferentCase_AreEqual()
    {
        // Arrange
        var spot1 = new ParkingSpot("A-01");
        var spot2 = new ParkingSpot("a-01"); // Different case

        // Act & Assert
        Assert.AreEqual(spot1, spot2);
        Assert.AreEqual(spot1.GetHashCode(), spot2.GetHashCode());
    }

    [TestMethod]
    public void Equals_DifferentSpotId_AreNotEqual()
    {
        // Arrange
        var spot1 = new ParkingSpot("A-01");
        var spot2 = new ParkingSpot("A-02");

        // Act & Assert
        Assert.AreNotEqual(spot1, spot2);
    }

    [TestMethod]
    public void Equals_SameSpotDifferentOccupancyState_AreEqual()
    {
        // Arrange
        var spot1 = new ParkingSpot("A-01");
        var spot2 = new ParkingSpot("A-01");
        var car = Vehicle.CreateCar("TEST123");
        
        spot1.Assign(car); // One occupied, one empty

        // Act & Assert
        Assert.AreEqual(spot1, spot2, "Equality should be based on SpotId only, not occupancy state");
    }

    [TestMethod]
    public void Equals_NullComparison_NotEqual()
    {
        // Arrange
        var spot = new ParkingSpot("A-01");

        // Act & Assert
        Assert.AreNotEqual(spot, null);
        Assert.IsFalse(spot.Equals(null));
    }

    // TOSTRING TESTS

    [TestMethod]
    public void ToString_EmptySpot_ReturnsCorrectFormat()
    {
        // Arrange
        var spot = new ParkingSpot("A-01");

        // Act
        var result = spot.ToString();

        // Assert
        Assert.AreEqual("Spot A-01: Available", result);
    }

    [TestMethod]
    public void ToString_OccupiedSpot_ReturnsCorrectFormat()
    {
        // Arrange
        var spot = new ParkingSpot("A-01");
        var car = Vehicle.CreateCar("ABC123");
        spot.Assign(car);

        // Act
        var result = spot.ToString();

        // Assert
        Assert.AreEqual("Spot A-01: Occupied by Car: ABC123", result);
    }

    [TestMethod]
    public void ToString_DifferentVehicleTypes_CorrectFormat()
    {
        // Arrange
        var spot = new ParkingSpot("B-05");
        var motorcycle = Vehicle.CreateMotorcycle("MOTO001");
        spot.Assign(motorcycle);

        // Act
        var result = spot.ToString();

        // Assert
        Assert.AreEqual("Spot B-05: Occupied by Motorcycle: MOTO001", result);
    }

    // COMPLEX WORKFLOW TESTS

    [TestMethod]
    public void CompleteFlow_AssignReleaseMultipleTimes_WorksCorrectly()
    {
        // Arrange
        var spot = new ParkingSpot("A-01");
        var vehicles = new[]
        {
            Vehicle.CreateCar("CAR001"),
            Vehicle.CreateMotorcycle("MOTO001"),
            Vehicle.CreateTruck("TRUCK001")
        };

        // Act & Assert - Multiple assign/release cycles
        foreach (var vehicle in vehicles)
        {
            // Assign
            var assignResult = spot.Assign(vehicle);
            Assert.IsTrue(assignResult, $"Should assign {vehicle.LicensePlate}");
            Assert.IsTrue(spot.IsOccupied);
            Assert.AreEqual(vehicle, spot.CurrentVehicle);

            // Release
            var releaseResult = spot.Release();
            Assert.IsTrue(releaseResult, $"Should release {vehicle.LicensePlate}");
            Assert.IsFalse(spot.IsOccupied);
            Assert.IsNull(spot.CurrentVehicle);
        }
    }

    [TestMethod]
    public void MultipleAssignAttempts_OnlyFirstSucceeds()
    {
        // Arrange
        var spot = new ParkingSpot("A-01");
        var vehicles = new[]
        {
            Vehicle.CreateCar("FIRST"),
            Vehicle.CreateCar("SECOND"),
            Vehicle.CreateCar("THIRD")
        };

        // Act
        var results = vehicles.Select(v => spot.Assign(v)).ToArray();

        // Assert
        Assert.IsTrue(results[0], "First assign should succeed");
        Assert.IsFalse(results[1], "Second assign should fail");
        Assert.IsFalse(results[2], "Third assign should fail");
        Assert.AreEqual(vehicles[0], spot.CurrentVehicle, "Should contain first vehicle");
    }

    [TestMethod]
    public void MultipleReleaseAttempts_OnlyFirstSucceeds()
    {
        // Arrange
        var spot = new ParkingSpot("A-01");
        var car = Vehicle.CreateCar("TEST123");
        spot.Assign(car);

        // Act
        var result1 = spot.Release();
        var result2 = spot.Release();
        var result3 = spot.Release();

        // Assert
        Assert.IsTrue(result1, "First release should succeed");
        Assert.IsFalse(result2, "Second release should fail");
        Assert.IsFalse(result3, "Third release should fail");
        Assert.IsFalse(spot.IsOccupied);
        Assert.IsNull(spot.CurrentVehicle);
    }

    // SPOT ID EDGE CASES

    [TestMethod]
    public void SpotId_VariousFormats_HandledCorrectly()
    {
        // Arrange & Act
        var spots = new[]
        {
            new ParkingSpot("A-01"),
            new ParkingSpot("LEVEL2-B-15"),
            new ParkingSpot("VIP001"),
            new ParkingSpot("HANDICAP-1"),
            new ParkingSpot("123"),
            new ParkingSpot("α-β-01") // Unicode
        };

        // Assert
        Assert.AreEqual("A-01", spots[0].SpotId);
        Assert.AreEqual("LEVEL2-B-15", spots[1].SpotId);
        Assert.AreEqual("VIP001", spots[2].SpotId);
        Assert.AreEqual("HANDICAP-1", spots[3].SpotId);
        Assert.AreEqual("123", spots[4].SpotId);
        Assert.AreEqual("α-β-01", spots[5].SpotId);
    }

    [TestMethod]
    public void SpotId_VeryLong_HandledCorrectly()
    {
        // Arrange
        var longSpotId = "VERYLONGSPOTIDENTIFIER123456789ABCDEFGHIJKLMNOP";

        // Act
        var spot = new ParkingSpot(longSpotId);

        // Assert
        Assert.AreEqual(longSpotId, spot.SpotId);
    }

    [TestMethod]
    public void SpotId_SingleCharacter_HandledCorrectly()
    {
        // Arrange & Act
        var spot = new ParkingSpot("A");

        // Assert
        Assert.AreEqual("A", spot.SpotId);
    }

    // HASHCODE CONSISTENCY TESTS

    [TestMethod]
    public void GetHashCode_SameSpot_ConsistentResults()
    {
        // Arrange
        var spot = new ParkingSpot("HASH-01");

        // Act
        var hash1 = spot.GetHashCode();
        var hash2 = spot.GetHashCode();

        // Assert
        Assert.AreEqual(hash1, hash2, "Hash code should be consistent");
    }

    [TestMethod]
    public void GetHashCode_EqualSpots_SameHashCode()
    {
        // Arrange
        var spot1 = new ParkingSpot("HASH-01");
        var spot2 = new ParkingSpot("hash-01"); // Different case

        // Act & Assert
        Assert.AreEqual(spot1.GetHashCode(), spot2.GetHashCode(), 
            "Equal spots should have same hash code");
    }

    [TestMethod]
    public void GetHashCode_DifferentSpots_DifferentHashCode()
    {
        // Arrange
        var spot1 = new ParkingSpot("A-01");
        var spot2 = new ParkingSpot("A-02");

        // Act & Assert
        Assert.AreNotEqual(spot1.GetHashCode(), spot2.GetHashCode(), 
            "Different spots should have different hash codes");
    }
}