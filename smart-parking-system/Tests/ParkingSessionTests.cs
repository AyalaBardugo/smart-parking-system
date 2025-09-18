using Microsoft.VisualStudio.TestTools.UnitTesting;
using Home_Task.Entities;

namespace Home_Task.Tests;

[TestClass]
public class ParkingSessionTests
{
    // BASIC FUNCTIONALITY TESTS

    [TestMethod]
    public void Constructor_ValidInputs_InitializesCorrectly()
    {
        // Arrange
        var vehicle = Vehicle.CreateCar("SESSION001");
        var spot = new ParkingSpot("A-01");
        var beforeCreation = DateTime.Now;

        // Act
        var session = new ParkingSession(vehicle, spot);

        // Assert
        Assert.AreEqual(vehicle, session.Vehicle);
        Assert.AreEqual(spot, session.ParkingSpot);
        Assert.IsTrue(session.IsActive);
        Assert.IsNull(session.ExitTime);
        Assert.IsTrue(session.EntryTime >= beforeCreation);
        Assert.IsTrue(session.EntryTime <= DateTime.Now);
        Assert.IsNotNull(session.SessionId);
        Assert.IsTrue(session.SessionId.StartsWith("T"));
    }

    [TestMethod]
    public void EndSession_ActiveSession_Success()
    {
        // Arrange
        var vehicle = Vehicle.CreateCar("SESSION002");
        var spot = new ParkingSpot("A-01");
        var session = new ParkingSession(vehicle, spot);
        var beforeEnd = DateTime.Now;

        // Act
        var result = session.EndSession();

        // Assert
        Assert.IsTrue(result);
        Assert.IsFalse(session.IsActive);
        Assert.IsNotNull(session.ExitTime);
        Assert.IsTrue(session.ExitTime >= beforeEnd);
        Assert.IsTrue(session.Duration.TotalMilliseconds > 0);
    }

    [TestMethod]
    public void EndSession_AlreadyEndedSession_Fails()
    {
        // Arrange
        var vehicle = Vehicle.CreateCar("SESSION003");
        var spot = new ParkingSpot("A-01");
        var session = new ParkingSession(vehicle, spot);
        session.EndSession(); // End first time
        var firstExitTime = session.ExitTime;

        // Act
        var result = session.EndSession(); // Try again

        // Assert
        Assert.IsFalse(result);
        Assert.IsFalse(session.IsActive);
        Assert.AreEqual(firstExitTime, session.ExitTime); // Should not change
    }

    // DURATION CALCULATION TESTS

    [TestMethod]
    public void Duration_ActiveSession_UpdatesCorrectly()
    {
        // Arrange
        var vehicle = Vehicle.CreateCar("DURATION001");
        var spot = new ParkingSpot("A-01");
        var session = new ParkingSession(vehicle, spot);

        // Act
        Thread.Sleep(10); // Small delay
        var duration = session.Duration;

        // Assert
        Assert.IsTrue(duration.TotalMilliseconds > 0);
        Assert.IsNull(session.ExitTime); // Still active
    }

    [TestMethod]
    public void Duration_EndedSession_RemainsFixed()
    {
        // Arrange
        var vehicle = Vehicle.CreateCar("DURATION002");
        var spot = new ParkingSpot("A-01");
        var session = new ParkingSession(vehicle, spot);
        Thread.Sleep(10);
        session.EndSession();

        // Act
        var duration1 = session.Duration;
        Thread.Sleep(10);
        var duration2 = session.Duration;

        // Assert
        Assert.AreEqual(duration1, duration2); // Should not change
    }

    [TestMethod]
    public void Duration_VeryShortSession_CalculatedCorrectly()
    {
        // Arrange
        var vehicle = Vehicle.CreateCar("SHORT001");
        var spot = new ParkingSpot("A-01");
        var session = new ParkingSession(vehicle, spot);

        // Act - End immediately
        session.EndSession();
        var duration = session.Duration;

        // Assert
        Assert.IsTrue(duration.TotalMilliseconds >= 0);
        Assert.IsTrue(duration.TotalSeconds < 1); // Should be very short
    }

    [TestMethod]
    public void Duration_ActiveSessionProgression_IncreasesOverTime()
    {
        // Arrange
        var vehicle = Vehicle.CreateCar("PROGRESS001");
        var spot = new ParkingSpot("A-01");
        var session = new ParkingSession(vehicle, spot);

        // Act
        var duration1 = session.Duration;
        Thread.Sleep(20);
        var duration2 = session.Duration;

        // Assert
        Assert.IsTrue(duration2 > duration1, "Duration should increase over time for active session");
    }

    // INPUT VALIDATION TESTS

    [TestMethod]
    public void Constructor_NullVehicle_ThrowsException()
    {
        // Arrange
        var spot = new ParkingSpot("A-01");

        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new ParkingSession(null, spot));
    }

    [TestMethod]
    public void Constructor_NullSpot_ThrowsException()
    {
        // Arrange
        var vehicle = Vehicle.CreateCar("TEST123");

        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new ParkingSession(vehicle, null));
    }

    [TestMethod]
    public void Constructor_BothNull_ThrowsException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(() =>
            new ParkingSession(null, null));
    }

    // EQUALITY TESTS

    [TestMethod]
    public void Equals_SameSession_AreEqual()
    {
        // Arrange
        var vehicle = Vehicle.CreateCar("EQUAL001");
        var spot = new ParkingSpot("A-01");
        var session = new ParkingSession(vehicle, spot);

        // Act & Assert
        Assert.AreEqual(session, session);
        Assert.IsTrue(session.Equals(session));
    }

    [TestMethod]
    public void Equals_DifferentSessions_NotEqual()
    {
        // Arrange
        var vehicle1 = Vehicle.CreateCar("EQUAL001");
        var vehicle2 = Vehicle.CreateCar("EQUAL002");
        var spot = new ParkingSpot("A-01");
        var session1 = new ParkingSession(vehicle1, spot);
        var session2 = new ParkingSession(vehicle2, spot);

        // Act & Assert
        Assert.AreNotEqual(session1, session2);
        Assert.IsFalse(session1.Equals(session2));
    }

    [TestMethod]
    public void Equals_NullComparison_NotEqual()
    {
        // Arrange
        var vehicle = Vehicle.CreateCar("NULL001");
        var spot = new ParkingSpot("A-01");
        var session = new ParkingSession(vehicle, spot);

        // Act & Assert
        Assert.AreNotEqual(session, null);
        Assert.IsFalse(session.Equals(null));
    }

    // SESSION ID TESTS

    [TestMethod]
    public void SessionId_MultipleInstances_AreUnique()
    {
        // Arrange
        var vehicle = Vehicle.CreateCar("UNIQUE001");
        var spot = new ParkingSpot("A-01");

        // Act
        var session1 = new ParkingSession(vehicle, spot);
        var session2 = new ParkingSession(vehicle, spot);
        var session3 = new ParkingSession(vehicle, spot);

        // Assert
        Assert.AreNotEqual(session1.SessionId, session2.SessionId);
        Assert.AreNotEqual(session2.SessionId, session3.SessionId);
        Assert.AreNotEqual(session1.SessionId, session3.SessionId);
    }

    [TestMethod]
    public void SessionId_Format_StartsWithT()
    {
        // Arrange
        var vehicle = Vehicle.CreateCar("FORMAT001");
        var spot = new ParkingSpot("A-01");

        // Act
        var session = new ParkingSession(vehicle, spot);

        // Assert
        Assert.IsTrue(session.SessionId.StartsWith("T"));
        Assert.IsTrue(session.SessionId.Length > 1);
    }

    [TestMethod]
    public void SessionId_Sequential_IncreasingNumbers()
    {
        // Arrange
        var vehicle = Vehicle.CreateCar("SEQ001");
        var spot = new ParkingSpot("A-01");

        // Act - Create multiple sessions
        var sessions = Enumerable.Range(0, 5)
            .Select(_ => new ParkingSession(vehicle, spot))
            .ToArray();

        // Assert - Session IDs should be sequential
        for (int i = 1; i < sessions.Length; i++)
        {
            var current = sessions[i].SessionId;
            var previous = sessions[i - 1].SessionId;
            
            // Extract numbers from session IDs
            var currentNum = int.Parse(current.Substring(1));
            var previousNum = int.Parse(previous.Substring(1));
            
            Assert.IsTrue(currentNum > previousNum, 
                $"Session ID {current} should be greater than {previous}");
        }
    }

    // TOSTRING TESTS

    [TestMethod]
    public void ToString_ActiveSession_ReturnsCorrectFormat()
    {
        // Arrange
        var vehicle = Vehicle.CreateCar("STRING001");
        var spot = new ParkingSpot("A-01");
        var session = new ParkingSession(vehicle, spot);

        // Act
        var result = session.ToString();

        // Assert
        Assert.IsTrue(result.Contains("STRING001"));
        Assert.IsTrue(result.Contains("A-01"));
        Assert.IsTrue(result.Contains("Active"));
        Assert.IsTrue(result.Contains("Now"));
        Assert.IsTrue(result.Contains(session.SessionId));
    }

    [TestMethod]
    public void ToString_EndedSession_ReturnsCorrectFormat()
    {
        // Arrange
        var vehicle = Vehicle.CreateCar("STRING002");
        var spot = new ParkingSpot("A-01");
        var session = new ParkingSession(vehicle, spot);
        session.EndSession();

        // Act
        var result = session.ToString();

        // Assert
        Assert.IsTrue(result.Contains("STRING002"));
        Assert.IsTrue(result.Contains("A-01"));
        Assert.IsTrue(result.Contains("Completed"));
        Assert.IsFalse(result.Contains("Now")); // Should show actual exit time
        Assert.IsTrue(result.Contains(session.SessionId));
    }

    [TestMethod]
    public void ToString_DifferentVehicleTypes_CorrectFormat()
    {
        // Arrange
        var motorcycle = Vehicle.CreateMotorcycle("MOTO001");
        var spot = new ParkingSpot("B-05");
        var session = new ParkingSession(motorcycle, spot);

        // Act
        var result = session.ToString();

        // Assert
        Assert.IsTrue(result.Contains("Motorcycle: MOTO001"));
        Assert.IsTrue(result.Contains("B-05"));
    }

    // HASHCODE TESTS

    [TestMethod]
    public void GetHashCode_SameSession_ConsistentResults()
    {
        // Arrange
        var vehicle = Vehicle.CreateCar("HASH001");
        var spot = new ParkingSpot("A-01");
        var session = new ParkingSession(vehicle, spot);

        // Act
        var hash1 = session.GetHashCode();
        var hash2 = session.GetHashCode();

        // Assert
        Assert.AreEqual(hash1, hash2, "Hash code should be consistent");
    }

    [TestMethod]
    public void GetHashCode_DifferentSessions_DifferentHashCode()
    {
        // Arrange
        var vehicle1 = Vehicle.CreateCar("HASH001");
        var vehicle2 = Vehicle.CreateCar("HASH002");
        var spot = new ParkingSpot("A-01");
        var session1 = new ParkingSession(vehicle1, spot);
        var session2 = new ParkingSession(vehicle2, spot);

        // Act & Assert
        Assert.AreNotEqual(session1.GetHashCode(), session2.GetHashCode(), 
            "Different sessions should have different hash codes");
    }

    // TIMING EDGE CASES

    [TestMethod]
    public void EntryTime_Precision_WithinReasonableRange()
    {
        // Arrange
        var beforeCreation = DateTime.Now;
        var vehicle = Vehicle.CreateCar("TIMING001");
        var spot = new ParkingSpot("A-01");

        // Act
        var session = new ParkingSession(vehicle, spot);
        var afterCreation = DateTime.Now;

        // Assert
        Assert.IsTrue(session.EntryTime >= beforeCreation);
        Assert.IsTrue(session.EntryTime <= afterCreation);
        var creationWindow = afterCreation - beforeCreation;
        Assert.IsTrue(creationWindow.TotalSeconds < 1, "Creation should be nearly instantaneous");
    }

    [TestMethod]
    public void ExitTime_Precision_WithinReasonableRange()
    {
        // Arrange
        var vehicle = Vehicle.CreateCar("TIMING002");
        var spot = new ParkingSpot("A-01");
        var session = new ParkingSession(vehicle, spot);
        
        var beforeEnd = DateTime.Now;

        // Act
        session.EndSession();
        var afterEnd = DateTime.Now;

        // Assert
        Assert.IsTrue(session.ExitTime >= beforeEnd);
        Assert.IsTrue(session.ExitTime <= afterEnd);
        var endWindow = afterEnd - beforeEnd;
        Assert.IsTrue(endWindow.TotalSeconds < 1, "End should be nearly instantaneous");
    }

    // COMPLEX WORKFLOW TESTS

    [TestMethod]
    public void CompleteWorkflow_CreateEndCheck_WorksCorrectly()
    {
        // Arrange
        var vehicle = Vehicle.CreateCar("WORKFLOW001");
        var spot = new ParkingSpot("A-01");

        // Act & Assert - Step by step
        var session = new ParkingSession(vehicle, spot);
        
        // Initial state
        Assert.IsTrue(session.IsActive);
        Assert.IsNull(session.ExitTime);
        Assert.IsTrue(session.Duration.TotalMilliseconds >= 0);

        // Wait a bit
        Thread.Sleep(5);
        var midDuration = session.Duration;
        Assert.IsTrue(midDuration.TotalMilliseconds > 0);

        // End session
        var endResult = session.EndSession();
        Assert.IsTrue(endResult);
        Assert.IsFalse(session.IsActive);
        Assert.IsNotNull(session.ExitTime);

        // Duration should be fixed now
        var finalDuration = session.Duration;
        Thread.Sleep(5);
        var laterDuration = session.Duration;
        Assert.AreEqual(finalDuration, laterDuration);
    }

    [TestMethod]
    public void SessionCreation_WithDifferentVehicleTypes_AllWork()
    {
        // Arrange
        var vehicles = new[]
        {
            Vehicle.CreateCar("CAR001"),
            Vehicle.CreateMotorcycle("MOTO001"),
            Vehicle.CreateTruck("TRUCK001")
        };
        var spot = new ParkingSpot("A-01");

        // Act & Assert
        foreach (var vehicle in vehicles)
        {
            var session = new ParkingSession(vehicle, spot);
            
            Assert.AreEqual(vehicle, session.Vehicle);
            Assert.AreEqual(spot, session.ParkingSpot);
            Assert.IsTrue(session.IsActive);
            Assert.IsNotNull(session.SessionId);
            
            // End session
            Assert.IsTrue(session.EndSession());
            Assert.IsFalse(session.IsActive);
        }
    }

    // EDGE CASE: RAPID OPERATIONS

    [TestMethod]
    public void RapidSessionCreation_MaintainsUniqueIds()
    {
        // Arrange
        var vehicle = Vehicle.CreateCar("RAPID001");
        var spot = new ParkingSpot("A-01");
        var sessionIds = new HashSet<string>();

        // Act - Create many sessions rapidly
        for (int i = 0; i < 100; i++)
        {
            var session = new ParkingSession(vehicle, spot);
            sessionIds.Add(session.SessionId);
        }

        // Assert
        Assert.AreEqual(100, sessionIds.Count, "All session IDs should be unique");
    }

    [TestMethod]
    public void RapidEndSession_OnlyFirstSucceeds()
    {
        // Arrange
        var vehicle = Vehicle.CreateCar("RAPID002");
        var spot = new ParkingSpot("A-01");
        var session = new ParkingSession(vehicle, spot);

        // Act - Try to end multiple times rapidly
        var results = Enumerable.Range(0, 10)
            .Select(_ => session.EndSession())
            .ToArray();

        // Assert
        Assert.AreEqual(1, results.Count(r => r), "Only first EndSession should succeed");
        Assert.IsFalse(session.IsActive);
    }
}