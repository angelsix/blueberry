using System;

namespace Blueberry.Desktop.WindowsApp.Bluetooth
{
    /// <summary>
    /// Information about a BLE device
    /// </summary>
    public class DnaBluetoothLEDevice
    {
        #region Public Properties

        /// <summary>
        /// The time of the broadcast advertisement message of the device
        /// </summary>
        public DateTimeOffset BroadcastTime { get; }

        /// <summary>
        /// The address of the device
        /// </summary>
        public ulong Address { get; }

        /// <summary>
        /// The name of the device
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The signal strength in dB
        /// </summary>
        public short SignalStrengthInDB { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public DnaBluetoothLEDevice(ulong address, string name, short rssi, DateTimeOffset broadcastTime)
        {
            Address = address;
            Name = name;
            SignalStrengthInDB = rssi;
            BroadcastTime = broadcastTime;
        }

        #endregion

        /// <summary>
        /// User friendly ToString
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{ (string.IsNullOrEmpty(Name) ? "[No Name]" : Name ) } {Address} ({SignalStrengthInDB})";
        }
    }
}
