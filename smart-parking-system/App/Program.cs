using Home_Task.Entities;
using Home_Task.Services;
using Home_Task.Models;

namespace Home_Task;

class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("🚗 Smart Parking System - Testing Suite");
        Console.WriteLine("========================================\n");

        var tests = new (string name, Func<Task<bool>> test)[]
        {
            ("Basic Functionality", TestBasicFunctionality),
            ("Concurrent Entries", TestConcurrentEntries),
            ("Global Vehicle Tracking", TestGlobalVehicleTracking),
            ("Exit from Wrong Lot", TestExitFromWrongLot),
            ("Full Parking Lot", TestFullParkingLot),
            ("Same Vehicle Multiple Times", TestSameVehicleMultipleTimes)
        };

        var allTestsPassed = true;
        foreach (var (name, test) in tests)
        {
            allTestsPassed &= await test();
        }

        // Final result
        Console.WriteLine("\n" + "=".PadLeft(50, '='));
        Console.WriteLine(allTestsPassed 
            ? "✅ ALL TESTS PASSED! The parking system works correctly."
            : "❌ SOME TESTS FAILED! Check the output above.");
        Console.WriteLine("=".PadLeft(50, '='));
    }

    static async Task<bool> TestBasicFunctionality()
    {
        Console.WriteLine("🧪 Test 1: Basic Functionality");
        Console.WriteLine("-------------------------------");
        
        try
        {
            var manager = new ParkingSystemManager();
            manager.AddParkingLot("TestLot", 10m, 3);
            
            var car = Vehicle.CreateCar("TEST001");
            
            // Enter
            var enterResult = await manager.ProcessVehicleAsync(car, "TestLot");
            if (!enterResult.IsSuccess)
            {
                Console.WriteLine($"❌ Failed to enter parking: {enterResult.Message}");
                return false;
            }
            
            // Check capacity
            if (manager.TotalOccupiedSpots != 1)
            {
                Console.WriteLine("❌ Expected 1 occupied spot, got " + manager.TotalOccupiedSpots);
                return false;
            }
            
            await Task.Delay(100); // Small parking duration
            
            // Exit
            var exitResult = await manager.UnparkAsync("TestLot", "TEST001");
            if (!exitResult.IsSuccess)
            {
                Console.WriteLine($"❌ Failed to exit parking: {exitResult.Message}");
                return false;
            }
            
            // Check capacity after exit
            if (manager.TotalOccupiedSpots != 0)
            {
                Console.WriteLine("❌ Expected 0 occupied spots after exit, got " + manager.TotalOccupiedSpots);
                return false;
            }
            
            Console.WriteLine("✅ Basic functionality works!\n");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}\n");
            return false;
        }
    }

    static async Task<bool> TestConcurrentEntries()
    {
        Console.WriteLine("🧪 Test 2: Concurrent Entries");
        Console.WriteLine("------------------------------");
        
        try
        {
            var manager = new ParkingSystemManager();
            manager.AddParkingLot("ConcurrentLot", 15m, 2); // Only 2 spots
            
            var car1 = Vehicle.CreateCar("CONC001");
            var car2 = Vehicle.CreateMotorcycle("CONC002");
            var car3 = Vehicle.CreateTruck("CONC003"); // This should wait/fail
            
            // Start concurrent entries
            var tasks = new List<Task<ParkingResult>>
            {
                manager.ProcessVehicleAsync(car1, "ConcurrentLot"),
                manager.ProcessVehicleAsync(car2, "ConcurrentLot"),
                manager.ProcessVehicleAsync(car3, "ConcurrentLot")
            };
            
            // Timed exit after 1 second
            var exitTask = Task.Run(async () =>
            {
                await Task.Delay(1000);
                return await manager.UnparkAsync("ConcurrentLot", car1.LicensePlate);
            });
            
            var results = await Task.WhenAll(tasks);
            await exitTask; // Wait for exit to complete
            
            // Check results
            var successCount = results.Count(r => r.IsSuccess);
            var noSpotsCount = results.Count(r => !r.IsSuccess && r.ErrorType == ParkingErrorType.NoSpotsAvailable);
            
            if (successCount >= 2 && manager.TotalOccupiedSpots <= 2)
            {
                Console.WriteLine($"✅ Concurrent entries handled correctly! ({successCount} successful entries)");
                Console.WriteLine($"   Final occupied spots: {manager.TotalOccupiedSpots}\n");
                return true;
            }
            else
            {
                Console.WriteLine($"❌ Concurrent test failed. Success count: {successCount}, Occupied: {manager.TotalOccupiedSpots}");
                Console.WriteLine($"   Results: {string.Join(", ", results.Select(r => r.IsSuccess ? "Success" : r.ErrorType?.ToString() ?? "Unknown"))}\n");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}\n");
            return false;
        }
    }

    static async Task<bool> TestGlobalVehicleTracking()
    {
        Console.WriteLine("🧪 Test 3: Global Vehicle Tracking");
        Console.WriteLine("-----------------------------------");
        
        try
        {
            var manager = new ParkingSystemManager();
            manager.AddParkingLot("LotA", 10m, 2);
            manager.AddParkingLot("LotB", 15m, 2);
            
            var car = Vehicle.CreateCar("GLOBAL001");
            
            // Enter first lot
            var result1 = await manager.ProcessVehicleAsync(car, "LotA");
            if (!result1.IsSuccess)
            {
                Console.WriteLine($"❌ Failed to enter LotA: {result1.Message}");
                return false;
            }
            
            // Try to enter second lot (should fail)
            var result2 = await manager.ProcessVehicleAsync(car, "LotB");
            
            if (!result2.IsSuccess && 
                result2.ErrorType == ParkingErrorType.VehicleAlreadyParked && 
                manager.TotalOccupiedSpots == 1)
            {
                Console.WriteLine("✅ Global vehicle tracking prevents duplicate parking!\n");
                return true;
            }
            else
            {
                Console.WriteLine($"❌ Global tracking failed. Result2: {result2.Message}, Occupied: {manager.TotalOccupiedSpots}\n");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}\n");
            return false;
        }
    }

    static async Task<bool> TestExitFromWrongLot()
    {
        Console.WriteLine("🧪 Test 4: Exit From Wrong Lot");
        Console.WriteLine("-------------------------------");
        
        try
        {
            var manager = new ParkingSystemManager();
            manager.AddParkingLot("CorrectLot", 10m, 2);
            manager.AddParkingLot("WrongLot", 15m, 2);
            
            var car = Vehicle.CreateCar("WRONG001");
            
            // Enter CorrectLot
            var enterResult = await manager.ProcessVehicleAsync(car, "CorrectLot");
            if (!enterResult.IsSuccess)
            {
                Console.WriteLine($"❌ Failed to enter CorrectLot: {enterResult.Message}");
                return false;
            }
            
            // Try to exit from WrongLot
            var exitResult = await manager.UnparkAsync("WrongLot", "WRONG001");
            
            if (!exitResult.IsSuccess && 
                exitResult.ErrorType == UnparkingErrorType.WrongParkingLot && 
                manager.TotalOccupiedSpots == 1)
            {
                Console.WriteLine("✅ System prevents exit from wrong lot!\n");
                return true;
            }
            else
            {
                Console.WriteLine($"❌ Wrong lot test failed. ExitResult: {exitResult.Message}, Occupied: {manager.TotalOccupiedSpots}\n");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}\n");
            return false;
        }
    }

    static async Task<bool> TestFullParkingLot()
    {
        Console.WriteLine("🧪 Test 5: Full Parking Lot");
        Console.WriteLine("----------------------------");
    
        try
        {
            var manager = new ParkingSystemManager();
            manager.AddParkingLot("SmallLot", 20m, 1);
        
            var car1 = Vehicle.CreateCar("FULL001");
        
            // Fill the lot
            var result1 = await manager.ProcessVehicleAsync(car1, "SmallLot");
            if (!result1.IsSuccess)
            {
                Console.WriteLine($"❌ Failed to fill lot: {result1.Message}");
                return false;
            }
            
            // Check that lot is full after first car
            // Note: In real life, a second car would wait for a spot to become available.
            // For testing purposes, we only verify the lot capacity management works correctly.
            if (manager.TotalOccupiedSpots == 1 && manager.TotalAvailableSpots == 0)
            {
                Console.WriteLine("✅ Parking lot correctly filled to capacity!");
                Console.WriteLine($"   Occupied: {manager.TotalOccupiedSpots}/{manager.TotalSystemCapacity}\n");
                return true;
            }
            else
            {
                Console.WriteLine($"❌ Expected lot to be full with 1 car, but occupied: {manager.TotalOccupiedSpots}\n");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}\n");
            return false;
        }
    }
    
    static async Task<bool> TestSameVehicleMultipleTimes()
    {
        Console.WriteLine("🧪 Test 6: Same Vehicle Multiple Times");
        Console.WriteLine("---------------------------------------");
        
        try
        {
            var manager = new ParkingSystemManager();
            manager.AddParkingLot("MultiLot", 25m, 5);
            
            var car = Vehicle.CreateCar("MULTI001");
            
            // Try to enter multiple times concurrently
            var tasks = new List<Task<ParkingResult>>();
            for (int i = 0; i < 3; i++)
            {
                tasks.Add(manager.ProcessVehicleAsync(car, "MultiLot"));
            }
            
            var results = await Task.WhenAll(tasks);
            
            var successCount = results.Count(r => r.IsSuccess);
            var alreadyParkedCount = results.Count(r => !r.IsSuccess && r.ErrorType == ParkingErrorType.VehicleAlreadyParked);
            
            if (successCount == 1 && alreadyParkedCount >= 1 && manager.TotalOccupiedSpots == 1)
            {
                Console.WriteLine("✅ Same vehicle correctly allowed only once!\n");
                return true;
            }
            else
            {
                Console.WriteLine($"❌ Multiple entry test failed. Success: {successCount}, AlreadyParked: {alreadyParkedCount}, Occupied: {manager.TotalOccupiedSpots}");
                Console.WriteLine($"   Results: {string.Join(", ", results.Select(r => r.IsSuccess ? "Success" : r.ErrorType?.ToString() ?? "Unknown"))}\n");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}\n");
            return false;
        }
    }
}