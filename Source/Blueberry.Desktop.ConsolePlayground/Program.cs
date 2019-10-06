using Blueberry.Desktop.WindowsApp.Bluetooth;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Blueberry.Desktop.ConsolePlayground
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Hello World!");

            var tcs = new TaskCompletionSource<bool>();

            Task.Run(async () =>
            {
                try
                {
                    // New watcher
                    var watcher = new DnaBluetoothLEAdvertisementWatcher(new GattServiceIds());

                    // Hook into events
                    watcher.StartedListening += () =>
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("Started listening");
                    };

                    watcher.StoppedListening += () =>
                    {
                        Console.ForegroundColor = ConsoleColor.Gray;
                        Console.WriteLine("Stopped listening");
                    };

                    watcher.NewDeviceDiscovered += (device) =>
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"New device: {device}");
                    };

                    watcher.DeviceNameChanged += (device) =>
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Device name changed: {device}");
                    };

                    watcher.DeviceTimeout += (device) =>
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Device timeout: {device}");
                    };

                    // Start listening
                    watcher.StartListening();

                    while (true)
                    {
                        // Pause until we press enter
                        var command = Console.ReadLine()?.ToLower().Trim();

                        if (string.IsNullOrEmpty(command))
                        {
                            // Get discovered devices
                            var devices = watcher.DiscoveredDevices;

                            Console.ForegroundColor = ConsoleColor.White;
                            Console.WriteLine($"{devices.Count} devices......");

                            foreach (var device in devices)
                                Console.WriteLine(device);
                        }
                        else if (command == "c")
                        {
                            // Attempt to find contour device
                            var contourDevice = watcher.DiscoveredDevices.FirstOrDefault(
                                f => f.Name.ToLower().Contains("contour"));

                            // If we don't find it...
                            if (contourDevice == null)
                            {
                                // Let the user know
                                Console.WriteLine("No Contour device found for connecting");
                                continue;
                            }

                            // Try and connect
                            Console.WriteLine("Connecting to Contour Device...");

                            try
                            {
                                // Try and connect
                                await watcher.PairToDeviceAsync(contourDevice.DeviceId);
                            }
                            catch (Exception ex)
                            {
                                // Log it out
                                Console.WriteLine("Failed to pair to Contour device.");
                                Console.WriteLine(ex);
                            }
                        }
                        // Q to quit
                        else if (command == "q")
                        {
                            break;
                        }
                    }

                    // Finish console application
                    tcs.TrySetResult(true);
                }
                finally
                {
                    // If anything goes wrong, exit out
                    tcs.TrySetResult(false);
                }
            });

            tcs.Task.Wait();
        }
    }
}
