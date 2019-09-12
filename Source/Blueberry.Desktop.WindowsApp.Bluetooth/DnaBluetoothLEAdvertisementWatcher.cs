using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Bluetooth.Advertisement;

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
        private readonly Dictionary<ulong, DnaBluetoothLEDevice> mDiscoveredDevices = new Dictionary<ulong, DnaBluetoothLEDevice>();

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
        public DnaBluetoothLEAdvertisementWatcher()
        {
            // Create bluetooth listener
            mWatcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };

            // Listen out for new advertisements
            mWatcher.Received += WatcherAdvertisementReceived;

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
        private void WatcherAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            // Cleanup Timeouts
            CleanupTimeouts();

            // Prepare variables to be set within the locked block
            DnaBluetoothLEDevice device = default;
            bool nameChanged = default;
            bool newDiscovery = default;

            lock (mThreadLock)
            {
                // It might happen that in the meantime we have stopped listening,
                // but this method is still being executed, because it was waiting
                // to get this lock. In that case, we do nothing more
                if (!Listening)
                    return;

                // Is new discovery?
                newDiscovery = !mDiscoveredDevices.ContainsKey(args.BluetoothAddress);

                // Name changed?
                nameChanged =
                    // If it already exists
                    !newDiscovery &&
                    // And is not a blank  name
                    !string.IsNullOrEmpty(args.Advertisement.LocalName) &&
                    // And the name is different
                    mDiscoveredDevices[args.BluetoothAddress].Name != args.Advertisement.LocalName;

                // Get the name of the device
                var name = args.Advertisement.LocalName;

                // If new name is blank, and we already have a device...
                if (string.IsNullOrEmpty(name) && !newDiscovery)
                    // Don't override what could be an actual name already
                    name = mDiscoveredDevices[args.BluetoothAddress].Name;

                // Create new device info class
                device = new DnaBluetoothLEDevice
                (
                    // Bluetooth address
                    address: args.BluetoothAddress,
                    // Name
                    name: name,
                    // Broadcast Time
                    broadcastTime: args.Timestamp,
                    // Signal Strength
                    rssi: args.RawSignalStrengthInDBm
                );

                // Add/update the device in the dictionary
                mDiscoveredDevices[args.BluetoothAddress] = device;
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