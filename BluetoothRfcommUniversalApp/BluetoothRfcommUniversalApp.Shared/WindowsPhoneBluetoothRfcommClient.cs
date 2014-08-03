//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking.Proximity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace BluetoothRfcommUniversalApp
{
    public class WindowsPhoneBluetoothRfcommClient : IBluetoothClient
    {
        
        // Constructor
        /// <param name="BluetoothServiceUuid">Service Uuid </param>
        /// <param name="bluetoothServiceDisplayName">Service display name</param>
        /// <param name="AutoStart">AutoStart define whether the server will start automatically after the application is launched or resumed</param>
        public WindowsPhoneBluetoothRfcommClient(Guid bluetoothServiceUuid, string bluetoothDisplayDisplayName, bool AutoStart = false)
        {
            _BluetoothServiceUuid = bluetoothServiceUuid;
            _BluetoothServiceDisplayName = bluetoothDisplayDisplayName;
            _AutoStart = AutoStart;

            _State = BluetoothClientState.Created;
        }


        // AutoStart property 
        // When AutoStart = true, the server will start automactically
        public bool AutoStart { get { return _AutoStart; }
            set {   
                    _AutoStart = value;
                }
        }
        private bool _AutoStart;

        // Service Uuid: this GUID is unique for this application and is shared 
        // between the Windows 8.X application and the Windows Phone 8 application 
        public Guid BluetoothServiceUuid { get { return _BluetoothServiceUuid; } }
        private Guid _BluetoothServiceUuid;

        // Service Name: it's actually the GUID associated with the service.
        public string BluetoothServiceName { get { return "{" + _BluetoothServiceUuid.ToString() + "}"; } }
        
        // Service Display Name: this name is shared 
        // between the Windows 8.X application and the Windows Phone 8 application 
        public string BluetoothServiceDisplayName { get { return _BluetoothServiceDisplayName; } }
        private string _BluetoothServiceDisplayName;

        // When true, the client is connected with a device (For instance a Windows Phone)
        public bool IsConnected
        {
            get { return GetState() == BluetoothClientState.Connected; }
        }



        // When IsConnected is true, the  properties below contain information about the connected device
        public string ConnectedDeviceDisplayName { get { return _connectedDeviceDisplayName; } }
        private string _connectedDeviceDisplayName;
        public string ConnectedDeviceHostName { get { return _connectedDeviceHostName; } }
        private string _connectedDeviceHostName;
        public string ConnectedDeviceServiceName { get { return _connectedDeviceServiceName; } }
        private string _connectedDeviceServiceName;

        // StreamSocket, DataWriter, DataReader used to exchange message over bluetooth 
        private StreamSocket socket;
        private DataWriter writer;
        private DataReader reader;

        // Exception which occurs when Bluetooth is off
        const uint ERR_BLUETOOTH_OFF = 0x8007048F;      // The Bluetooth radio is off
        // Exception which occurs when the application manifest didn't include the Rfcomm capability
        const uint ERR_MISSING_CAPS = 0x80070005;       // A capability is missing from your manifest
        // Did you add the bluetooth capability in the manifest:
        //    <m2:DeviceCapability Name="bluetooth.rfcomm">
        //        <m2:Device Id="any">
        //          <m2:Function Type="serviceId:34B1CF4D-1069-4AD6-89B6-E161D79BE4D8" />
        //        </m2:Device>
        //    </m2:DeviceCapability>
        // Exception which occurs when starting advertsing
        const uint ERR_NOT_ADVERTISING = 0x8000000E;    // You are currently not advertising your presence using PeerFinder.Start()

        // Return the Server state:
        // - Created: 
        // - Stopped: not listening
        // - Started: listening connection request 
        // - Connected: connected with a bluetooth device
        public BluetoothClientState GetState()
        {
            return _State;
        }

        // Set the Server state 
        public  void SetState(BluetoothClientState value)
        {
            _State = value;
            if (this.StateChanged != null)
            {
                  StateChanged(this, _State);
            }
        }
        private BluetoothClientState _State;
        private string[] StateStringArray = { "Not initialized", "Initialized", "Connected" };
        // Return the StateString
        public string StateString { get { return StateStringArray[(int)GetState()]; } }



        // Bluetooth Client Initialisation method
        /// <summary>
        /// Method used to initialize the Bluetooth Client
        /// </summary>
        /// <param name="BluetoothServiceUuid">Service Uuid </param>
        /// <param name="bluetoothServiceDisplayName">Service display name</param>
        /// <param name="AutoStart">AutoStart define whether the server will start automatically after the application is launched or resumed</param>
        public async Task<BluetoothClientReturnCode> Initialization()
        {
            // TODO
            //if (Microsoft.Devices.Environment.DeviceType == Microsoft.Devices.DeviceType.Emulator)
            //{
            //    return BluetoothClientReturnCode.InitEmulator;
            //}

            try
            {
                PeerFinder.AlternateIdentities["Bluetooth:Paired"] = "";
                var Peers = await PeerFinder.FindAllPeersAsync();
            }
            catch (Exception ex)
            {
                if ((uint)ex.HResult == ERR_BLUETOOTH_OFF)
                {
                    return BluetoothClientReturnCode.InitBluetoothOff;
                }
                else if ((uint)ex.HResult == ERR_MISSING_CAPS)
                {
                    return BluetoothClientReturnCode.InitMissingCaps;
                }
                else if ((uint)ex.HResult == ERR_NOT_ADVERTISING)
                {
                    return BluetoothClientReturnCode.InitNotAdvertising;
                }
                else
                {
                    return BluetoothClientReturnCode.InitError;
                }
            }
            finally
            {
                PeerFinder.Stop();
            }
            SetState(BluetoothClientState.Initialized);
            return BluetoothClientReturnCode.Success;

        }

        /// <summary>
        /// Method used for the connection with the server .
        /// </summary>
        /// <param name="device">Bluetooth device associated with the server.</param>
        public async Task<BluetoothClientReturnCode> Connect(BluetoothDevice device)
        {
            NotifyLog("Information", "Connecting Client...");
            if (socket == null)
                socket = new StreamSocket();
            try
            {
                await socket.ConnectAsync(new Windows.Networking.HostName(device.HostName), BluetoothServiceName);
            }
            catch (Exception ex)
            {
                NotifyLog("Error", "Exception in Connect - " + ex.Message);
                return BluetoothClientReturnCode.ConnectError;
            }
            SetState(BluetoothClientState.Connected);

            // Connected no need for advertising anymore
            PeerFinder.Stop();
            _connectedDeviceDisplayName = device.DisplayName;
            _connectedDeviceHostName = device.HostName;
            _connectedDeviceServiceName = BluetoothServiceName;
            NotifyLog("Information", "Client connected");

            // Reception loop
            ReceiveIncomingCommands();

            return BluetoothClientReturnCode.Success;
        }
        /// <summary>
        /// Method associated with with the reception loop.
        /// </summary>
        async void ReceiveIncomingCommands()
        {
            while (true)
            {
                byte[] CommandBytes = await ReadCommand();
                if (CommandBytes != null)
                {
                    if (CommandReceived != null)
                    {
                        BluetoothCommand bc = BluetoothCommand.CreateBluetoothCommmand(CommandBytes);
                        if (bc != null)
                        {
                             CommandReceived(this, bc);
                        }
                    }
                }
                else
                {
                    // if ReadCommand returns null, 
                    // need to disconnect the client
                    if(reader!=null)
                        Disconnect();
                    return;
                }
            }
        }

        public void NotifyLog(string logtype, string message)
        {
            if(LogReceived!=null)
            {
                LogReceived(this,logtype + ": " + message );
            }
        }


        // Bluetooth Client Disconnect method
        /// <summary>
        /// Method used to disconnect the current connection established with the Bluetooth Client. 
        /// </summary>
        public BluetoothClientReturnCode Disconnect()
        {
            try
            {
                NotifyLog("Information", "Disconnecting Client...");
                SetState(BluetoothClientState.Initialized);

                _connectedDeviceDisplayName = "";
                _connectedDeviceHostName = "";
                _connectedDeviceServiceName = "";

                if (socket != null)
                {
                    socket.Dispose();
                    socket = null;
                }

                if (writer != null)
                {
                    writer.DetachStream(); 
                    writer.Dispose();
                    writer = null;
                }
                if (reader != null)
                {
                    
                    reader.Dispose();
                    reader = null;
                }
                NotifyLog("Information", "Client Disconnected");

            }
            catch (Exception e)
            {
                NotifyLog("Error", "Exception in Disconnect - " + e.Message);
                return BluetoothClientReturnCode.DisconnectException;
            }
            finally
            {
                reader = null;
                writer = null;
                socket = null;
            }
            return BluetoothClientReturnCode.Success;
        }


        // Bluetooth Client SendCommand method
        /// <summary>
        /// Method used to send a Command towards the connected device  
        /// </summary>
        /// <param name="Message">Command to be sent towards the device</param>
        public async Task<BluetoothClientReturnCode> SendCommand(BluetoothCommand Command)
        {
            try
            {
                if (Command.CommandLength == 0)
                    return BluetoothClientReturnCode.SendEmptyMessage;
                if (socket != null)
                {
                    if (writer == null)
                        writer = new DataWriter(socket.OutputStream);
                    NotifyLog("Information", "Sending Command - " + Command.ToString());
                    if (writer != null)
                    {
                        writer.WriteUInt32((uint)Command.CommandLength);
                        writer.WriteBytes(Command.CommandBytes);

                        await writer.StoreAsync();
                        NotifyLog("Information", "Command Sent - " + Command.ToString());                        
                        return BluetoothClientReturnCode.Success;
                    }
                }
                else
                    return BluetoothClientReturnCode.SendNoConnection;
            }
            catch (Exception e)
            {
                NotifyLog("Error", "Exception in SendCommand - " + e.Message);
                return BluetoothClientReturnCode.SendException;
            }
            return BluetoothClientReturnCode.SendError;
        }

        // Bluetooth Client ReadCommand method
        /// <summary>
        /// Method used to receive Command from the connected device  
        /// </summary>
        /// <return>byte[] received, null if error occured</return>
        public async Task<byte[]> ReadCommand()
        {
            try
            {

                if (reader == null)
                    reader = new DataReader(socket.InputStream);
                if (reader != null)
                {
                    uint readLength = await reader.LoadAsync(sizeof(uint));
                    if (readLength < sizeof(uint))
                    {
                        return null;
                    }
                    uint currentLength = reader.ReadUInt32();
                    NotifyLog("Information", "Reading Command - Expected Length: " + currentLength.ToString());

                    readLength = await reader.LoadAsync(currentLength);
                    if (readLength < currentLength)
                    {
                        return null;
                    }
                    byte[] ReceivedBuffer = new byte[currentLength];
                    reader.ReadBytes(ReceivedBuffer);
                    NotifyLog("Information", "Command read - Length: " + currentLength.ToString());
                    return ReceivedBuffer;
                }
            }
            catch (Exception e)
            {
                NotifyLog("Error", "Exception in ReadCommand - " + e.Message);
            }
            return null;
        }

        // Bluetooth Client GetListPairedDevices method
        /// <summary>
        /// Method used to retrieve the list of paired devices  
        /// </summary>
        /// <return>List of Bluetooth devices received, null if error occured</return>
        public async Task<List<BluetoothDevice>> GetListPairedDevices()
        {
            PeerFinder.AlternateIdentities["Bluetooth:PAIRED"] = BluetoothServiceName;
            var peers = await PeerFinder.FindAllPeersAsync();

            // By clearing the backing data, we are effectively clearing the ListBox
            List<BluetoothDevice> list = new List<BluetoothDevice>();

            if (peers.Count != 0)
            {
                foreach (dynamic peer in peers)
                {
                    list.Add(new BluetoothDevice(peer.HostName.DisplayName, peer.ServiceName, peer.DisplayName));
                }
            }
            return list;
        }

        // StateChanged event
        // This event occurs each time the application State changes
        public event TypedEventHandler<IBluetoothClient, BluetoothClientState> StateChanged;
        // CommandReceived event
        // This event occurs each time a command is received 
        public event TypedEventHandler<IBluetoothClient, BluetoothCommand> CommandReceived;
        // LogReceived event
        // This event occurs each time the server wants to log information
        public event TypedEventHandler<IBluetoothClient, string> LogReceived;

    }
}
