using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Blueberry.Desktop.WindowsApp.Bluetooth
{
    /// <summary>
    /// Wraps and makes use of the <see cref="BluetoothLEAdvertisementWatcher"/>
    /// for easier consumption
    /// </summary>
    public class DnaBluetoothLEAdvertisementWatcher
    {
        #region Private Members

        /// <summary>
        /// The underlying bluetooth watcher class
        /// </summary>
        private readonly BluetoothLEAdvertisementWatcher mWatcher;

        /// <summary>
        /// A list of discovered devices
        /// </summary>
        private readonly Dictionary<string, DnaBluetoothLEDevice> mDiscoveredDevices = new Dictionary<string, DnaBluetoothLEDevice>();

        /// <summary>
        /// The details about GATT services
        /// </summary>
        private readonly GattServiceIds mGattServiceIds;

        /// <summary>
        /// A thread lock object for this class
        /// </summary>
        private readonly object mThreadLock = new object();

        #endregion

        #region Public Properties

        /// <summary>
        /// Indicates if this watcher is listening for advertisements
        /// </summary>
        public bool Listening => mWatcher.Status == BluetoothLEAdvertisementWatcherStatus.Started;

        /// <summary>
        /// A list of discovered devices
        /// </summary>
        public IReadOnlyCollection<DnaBluetoothLEDevice> DiscoveredDevices
        {
            get
            {
                // Clean up any timeouts
                CleanupTimeouts();

                // Practice thread-safety kids!
                lock (mThreadLock)
                {
                    // Convert to read-only list
                    return mDiscoveredDevices.Values.ToList().AsReadOnly();
                }
            }
        }

        /// <summary>
        /// The timeout in seconds that a device is removed from the <see cref="DiscoveredDevices"/>
        /// list if it is not re-advertised within this time
        /// </summary>
        public int HeartbeatTimeout { get; set; } = 30;

        #endregion

        #region Public Events

        /// <summary>
        /// Fired when the bluetooth watcher stops listening
        /// </summary>
        public event Action StoppedListening = () => { };

        /// <summary>
        /// Fired when the bluetooth watcher starts listening
        /// </summary>
        public event Action StartedListening = () => { };

        /// <summary>
        /// Fired when a device is discovered
        /// </summary>
        public event Action<DnaBluetoothLEDevice> DeviceDiscovered = (device) => { };

        /// <summary>
        /// Fired when a new device is discovered
        /// </summary>
        public event Action<DnaBluetoothLEDevice> NewDeviceDiscovered = (device) => { };

        /// <summary>
        /// Fired when a device name changes
        /// </summary>
        public event Action<DnaBluetoothLEDevice> DeviceNameChanged = (device) => { };

        /// <summary>
        /// Fired when a device is removed for timing out
        /// </summary>
        public event Action<DnaBluetoothLEDevice> DeviceTimeout = (device) => { };

        #endregion

        #region Constructor

        /// <summary>
        /// The default constructor
        /// </summary>
        public DnaBluetoothLEAdvertisementWatcher(GattServiceIds gattIds)
        {
            // Null guard
            mGattServiceIds = gattIds ?? throw new ArgumentNullException(nameof(gattIds));

            // Create bluetooth listener
            mWatcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };

            // Listen out for new advertisements
            mWatcher.Received += WatcherAdvertisementReceivedAsync;

            // Listen out for when the watcher stops listening
            mWatcher.Stopped += (watcher, e) =>
            {
                // Inform listeners
                StoppedListening();
            };
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Listens out for watcher advertisements
        /// </summary>
        /// <param name="sender">The watcher</param>
        /// <param name="args">The arguments</param>
        private async void WatcherAdvertisementReceivedAsync(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            // Cleanup Timeouts
            CleanupTimeouts();

            // Get BLE device info
            var device = await GetBluetoothLEDeviceAsync(
                args.BluetoothAddress, 
                args.Timestamp, 
                args.RawSignalStrengthInDBm);

            // Null guard
            if (device == null)
                return;

            // Is new discovery?
            var newDiscovery = false;
            var existingName = default(string);

            // Lock your doors
            lock (mThreadLock)
            {
                // Check if this is a new discovery
                newDiscovery = !mDiscoveredDevices.ContainsKey(device.DeviceId);

                // If this is not new...
                if (!newDiscovery)
                    // Store the old name
                    existingName = mDiscoveredDevices[device.DeviceId].Name;
            }

            // Name changed?
            var nameChanged =
                // If it already exists
                !newDiscovery &&
                // And is not a blank  name
                !string.IsNullOrEmpty(device.Name) &&
                // And the name is different
                existingName != device.Name;

            lock (mThreadLock)
            {
                // Add/update the device in the dictionary
                mDiscoveredDevices[device.DeviceId] = device;
            }

            // Inform listeners
            DeviceDiscovered(device);

            // If name changed...
            if (nameChanged)
                // Inform listeners
                DeviceNameChanged(device);

            // If new discovery...
            if (newDiscovery)
                // Inform listeners
                NewDeviceDiscovered(device);
        }

        /// <summary>
        /// Connects to the BLE device and extracts more information from the
        /// <see cref="https://docs.microsoft.com/en-us/uwp/api/windows.devices.bluetooth.bluetoothledevice"/>
        /// </summary>
        /// <param name="address">The BT address of the device to connect to</param>
        /// <param name="broadcastTime">The time the broadcast message was received</param>
        /// <param name="rssi">The signal strength in dB</param>
        /// <returns></returns>
        private async Task<DnaBluetoothLEDevice> GetBluetoothLEDeviceAsync(ulong address, DateTimeOffset broadcastTime, short rssi)
        {
            // Get bluetooth device info
            var device = await BluetoothLEDevice.FromBluetoothAddressAsync(address).AsTask();

            // Null guard
            if (device == null)
                return null;

            // Get GATT services that are available
            var gatt = await device.GetGattServicesAsync().AsTask();

            // If we have any services...
            if (gatt.Status == GattCommunicationStatus.Success)
            {
                // Loop each GATT service
                foreach (var service in gatt.Services)
                {
                    // This ID contains the GATT Profile Assigned number we want!
                    // TODO: Get more info and connect
                    var gattProfileId = service.Uuid;

                    if (service.Uuid.ToString("N").Substring(4, 4) == "1808")
                    {
                        System.Diagnostics.Debugger.Break();
                    }
                }
            }

            // Return the new device information
            return new DnaBluetoothLEDevice
            (
                // Device Id
                deviceId: device.DeviceId,
                // Bluetooth Address
                address: device.BluetoothAddress,
                // Device Name
                name: device.Name,
                // Broadcast Time
                broadcastTime: broadcastTime,
                // Signal Strength
                rssi: rssi,
                // Is Connected?
                connected: device.ConnectionStatus == BluetoothConnectionStatus.Connected,
                // Can Pair?
                canPair: device.DeviceInformation.Pairing.CanPair,
                // Is Paired?
                paired: device.DeviceInformation.Pairing.IsPaired
            ); ;
        }

        /// <summary>
        /// Prune any timed out devices that we have not heard off
        /// </summary>
        private void CleanupTimeouts()
        {
            lock (mThreadLock)
            {
                // The date in time that if less than means a device has timed out
                var threshold = DateTime.UtcNow - TimeSpan.FromSeconds(HeartbeatTimeout);

                // Any devices that have not sent a new broadcast within the heartbeat time
                mDiscoveredDevices.Where(f => f.Value.BroadcastTime < threshold).ToList().ForEach(device =>
                {
                    // Remove device
                    mDiscoveredDevices.Remove(device.Key);

                    // Inform listeners
                    DeviceTimeout(device.Value);
                });
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts listening for advertisements
        /// </summary>
        public void StartListening()
        {
            lock (mThreadLock)
            {
                // If already listening...
                if (Listening)
                    // Do nothing more
                    return;

                // Start the underlying watcher
                mWatcher.Start();
            }
            
            // Inform listeners
            StartedListening();
        }

        /// <summary>
        /// Stops listening for advertisements
        /// </summary>
        public void StopListening()
        {
            lock (mThreadLock)
            {
                // If we are no currently listening...
                if (!Listening)
                    // Do nothing more
                    return;

                // Stop listening
                mWatcher.Stop();

                // Clear any devices
                mDiscoveredDevices.Clear();
            }
        }

        #endregion
    }
}
