using Blueberry.Desktop.WindowsApp.Bluetooth;
using System;

namespace Blueberry.Desktop.ConsolePlayground
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Hello World!");

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
                Console.ReadLine();

                // Get discovered devices
                var devices = watcher.DiscoveredDevices;

                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($"{devices.Count} devices......");

                foreach (var device in devices)
                    Console.WriteLine(device);
            }
        }
    }
}
