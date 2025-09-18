using Microsoft.VisualStudio.TestTools.UnitTesting;
using Home_Task.Entities;

namespace Home_Task.Tests;

[TestClass]
public class VehicleTests
{
    // BASIC FUNCTIONALITY TESTS

    [TestMethod]
    public void CreateCar_ValidLicense_Success()
    {
        // Act
        var car = Vehicle.CreateCar("ABC123");

        // Assert
        Assert.AreEqual("ABC123", car.LicensePlate);
        Assert.AreEqual(VehicleType.Car, car.VehicleType);
    }

    [TestMethod]
    public void CreateMotorcycle_ValidLicense_Success()
    {
        // Act
        var motorcycle = Vehicle.CreateMotorcycle("MOTO001");

        // Assert
        Assert.AreEqual("MOTO001", motorcycle.LicensePlate);
        Assert.AreEqual(VehicleType.Motorcycle, motorcycle.VehicleType);
    }

    [TestMethod]
    public void CreateTruck_ValidLicense_Success()
    {
        // Act
        var truck = Vehicle.CreateTruck("TRUCK001");

        // Assert
        Assert.AreEqual("TRUCK001", truck.LicensePlate);
        Assert.AreEqual(VehicleType.Truck, truck.VehicleType);
    }

    // EQUALITY AND COMPARISON TESTS

    [TestMethod]
    public void Equals_SameLicenseDifferentCase_AreEqual()
    {
        // Arrange
        var car1 = Vehicle.CreateCar("ABC123");
        var car2 = Vehicle.CreateCar("abc123");

        // Act & Assert
        Assert.AreEqual(car1, car2);
        Assert.AreEqual(car1.GetHashCode(), car2.GetHashCode());
    }

    [TestMethod]
    public void Equals_DifferentLicense_NotEqual()
    {
        // Arrange
        var car1 = Vehicle.CreateCar("ABC123");
        var car2 = Vehicle.CreateCar("XYZ789");

        // Act & Assert
        Assert.AreNotEqual(car1, car2);
    }

    [TestMethod]
    public void Equals_SameLicenseDifferentTypes_AreEqual()
    {
        // Arrange - Same license plate, different vehicle types
        var car = Vehicle.CreateCar("ABC123");
        var motorcycle = Vehicle.CreateMotorcycle("ABC123");

        // Act & Assert
        Assert.AreEqual(car, motorcycle, "Vehicles with same license should be equal regardless of type");
        Assert.AreEqual(car.GetHashCode(), motorcycle.GetHashCode());
    }

    [TestMethod]
    public void Equals_NullComparison_NotEqual()
    {
        // Arrange
        var car = Vehicle.CreateCar("ABC123");

        // Act & Assert
        Assert.AreNotEqual(car, null);
        Assert.IsFalse(car.Equals(null));
    }

    [TestMethod]
    public void Equals_SameInstance_AreEqual()
    {
        // Arrange
        var car = Vehicle.CreateCar("ABC123");

        // Act & Assert
        Assert.AreEqual(car, car);
        Assert.IsTrue(car.Equals(car));
    }

    // INPUT VALIDATION TESTS

    [TestMethod]
    public void CreateCar_EmptyLicense_ThrowsException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentException>(() =>
            Vehicle.CreateCar(""));
    }

    [TestMethod]
    public void CreateMotorcycle_NullLicense_ThrowsException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentException>(() =>
            Vehicle.CreateMotorcycle(null));
    }

    [TestMethod]
    public void CreateTruck_WhitespaceLicense_ThrowsException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentException>(() =>
            Vehicle.CreateTruck("   "));
    }

    [TestMethod]
    public void CreateVehicle_TabsAndSpaces_ThrowsException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentException>(() =>
            Vehicle.CreateCar("\t\n  "));
    }

    // EDGE CASES

    [TestMethod]
    public void LicensePlate_WithLeadingTrailingSpaces_TrimmedCorrectly()
    {
        // Act
        var car = Vehicle.CreateCar("  ABC123  ");

        // Assert
        Assert.AreEqual("ABC123", car.LicensePlate, "License plate should be trimmed");
    }

    [TestMethod]
    public void LicensePlate_VeryLongValid_HandledCorrectly()
    {
        // Arrange
        var longLicense = "VERYLONGLICENSEPLATE123456789";

        // Act
        var car = Vehicle.CreateCar(longLicense);

        // Assert
        Assert.AreEqual(longLicense, car.LicensePlate);
    }

    [TestMethod]
    public void LicensePlate_SpecialCharacters_HandledCorrectly()
    {
        // Arrange
        var specialLicense = "ABC-123_XYZ";

        // Act
        var car = Vehicle.CreateCar(specialLicense);

        // Assert
        Assert.AreEqual(specialLicense, car.LicensePlate);
    }

    [TestMethod]
    public void LicensePlate_NumericOnly_HandledCorrectly()
    {
        // Arrange
        var numericLicense = "123456";

        // Act
        var car = Vehicle.CreateCar(numericLicense);

        // Assert
        Assert.AreEqual(numericLicense, car.LicensePlate);
    }

    [TestMethod]
    public void LicensePlate_SingleCharacter_HandledCorrectly()
    {
        // Arrange
        var singleChar = "A";

        // Act
        var car = Vehicle.CreateCar(singleChar);

        // Assert
        Assert.AreEqual(singleChar, car.LicensePlate);
    }

    // CASE SENSITIVITY EDGE CASES

    [TestMethod]
    public void Equality_MixedCaseVariations_AllEqual()
    {
        // Arrange
        var vehicles = new[]
        {
            Vehicle.CreateCar("abc123"),
            Vehicle.CreateMotorcycle("ABC123"),
            Vehicle.CreateTruck("Abc123"),
            Vehicle.CreateCar("AbC123"),
            Vehicle.CreateMotorcycle("aBc123")
        };

        // Act & Assert
        for (int i = 0; i < vehicles.Length; i++)
        {
            for (int j = i + 1; j < vehicles.Length; j++)
            {
                Assert.AreEqual(vehicles[i], vehicles[j], 
                    $"Vehicle {i} should equal vehicle {j}");
                Assert.AreEqual(vehicles[i].GetHashCode(), vehicles[j].GetHashCode(),
                    $"Hash codes should match for vehicles {i} and {j}");
            }
        }
    }

    // UNICODE AND INTERNATIONAL EDGE CASES

    [TestMethod]
    public void LicensePlate_UnicodeCharacters_HandledCorrectly()
    {
        // Arrange
        var unicodeLicense = "åæø123";

        // Act
        var car = Vehicle.CreateCar(unicodeLicense);

        // Assert
        Assert.AreEqual(unicodeLicense, car.LicensePlate);
    }

    [TestMethod]
    public void LicensePlate_CyrillicCharacters_HandledCorrectly()
    {
        // Arrange
        var cyrillicLicense = "АБВ123";

        // Act
        var car = Vehicle.CreateCar(cyrillicLicense);

        // Assert
        Assert.AreEqual(cyrillicLicense, car.LicensePlate);
    }

    // TOSTRING TESTS

    [TestMethod]
    public void ToString_Car_ReturnsCorrectFormat()
    {
        // Arrange
        var car = Vehicle.CreateCar("ABC123");

        // Act
        var result = car.ToString();

        // Assert
        Assert.AreEqual("Car: ABC123", result);
    }

    [TestMethod]
    public void ToString_Motorcycle_ReturnsCorrectFormat()
    {
        // Arrange
        var motorcycle = Vehicle.CreateMotorcycle("MOTO001");

        // Act
        var result = motorcycle.ToString();

        // Assert
        Assert.AreEqual("Motorcycle: MOTO001", result);
    }

    [TestMethod]
    public void ToString_Truck_ReturnsCorrectFormat()
    {
        // Arrange
        var truck = Vehicle.CreateTruck("TRUCK001");

        // Act
        var result = truck.ToString();

        // Assert
        Assert.AreEqual("Truck: TRUCK001", result);
    }

    // HASHCODE CONSISTENCY TESTS

    [TestMethod]
    public void GetHashCode_SameVehicle_ConsistentResults()
    {
        // Arrange
        var car = Vehicle.CreateCar("HASH123");

        // Act
        var hash1 = car.GetHashCode();
        var hash2 = car.GetHashCode();

        // Assert
        Assert.AreEqual(hash1, hash2, "Hash code should be consistent");
    }

    [TestMethod]
    public void GetHashCode_EqualVehicles_SameHashCode()
    {
        // Arrange
        var car1 = Vehicle.CreateCar("HASH123");
        var car2 = Vehicle.CreateCar("hash123"); // Different case

        // Act & Assert
        Assert.AreEqual(car1.GetHashCode(), car2.GetHashCode(), 
            "Equal vehicles should have same hash code");
    }
}