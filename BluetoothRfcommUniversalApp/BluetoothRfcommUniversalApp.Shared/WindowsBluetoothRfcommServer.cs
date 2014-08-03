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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Proximity;
using System.ComponentModel;

namespace BluetoothRfcommUniversalApp
{
    public class WindowsBluetoothRfcommServer : IBluetoothServer
    {
        // Constructor
         /// <param name="BluetoothServiceUuid">Service Uuid </param>
        /// <param name="bluetoothServiceDisplayName">Service display name</param>
        /// <param name="AutoStart">AutoStart define whether the server will start automatically after the application is launched or resumed</param>
       public WindowsBluetoothRfcommServer(Guid bluetoothServiceUuid, string bluetoothDisplayDisplayName, bool AutoStart = false)
        {
            _State = BluetoothServerState.Created;
            _BluetoothServiceUuid = bluetoothServiceUuid;
            _BluetoothServiceDisplayName = bluetoothDisplayDisplayName;
            _AutoStart = AutoStart;
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

        // When true, the server is connected with a device (For instance a Windows Phone)
        public bool IsConnected
        {
            get { return GetState() == BluetoothServerState.Connected; }
        }

        // When true, the server is started  
        public bool IsStarted
        {
            get {return GetState() >= BluetoothServerState.Started;}
        }

        // When true, the server is stopped
        public bool IsStopped
        {
            get { return GetState() == BluetoothServerState.Stopped;}
        }

        // When IsConnected is true, the  properties below contain information about the connected device
        public string ConnectedDeviceDisplayName { get { return _connectedDeviceDisplayName; } }
        private string _connectedDeviceDisplayName;
        public string ConnectedDeviceHostName { get { return _connectedDeviceHostName; } }
        private string _connectedDeviceHostName;
        public string ConnectedDeviceServiceName { get { return _connectedDeviceServiceName; } }
        private string _connectedDeviceServiceName;


        // The Id of the Service Name SDP attribute
        private const UInt16 SdpServiceNameAttributeId = 0x100;

        // The SDP Type of the Service Name SDP attribute.
        // The first byte in the SDP Attribute encodes the SDP Attribute Type as follows :
        //    -  the Attribute Type size in the least significant 3 bits,
        //    -  the SDP Attribute Type value in the most significant 5 bits.
        private const byte SdpServiceNameAttributeType = (4 << 3) | 5;


        // RfcommServiceProvider used to start the server and start the advertising
        private RfcommServiceProvider rfcommProvider;
        // StreamSocketListener used to detect connection request from devices
        private StreamSocketListener socketListener;

        // StreamSocket, DataWriter, DataReader used to exchange message over bluetooth 
        private StreamSocket socket;
        private DataWriter writer;
        private DataReader reader;

        // Exception which occurs when Bluetooth is off
        const uint ERR_BLUETOOTH_OFF = 0x80070490;      // The Bluetooth radio is off
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
        public BluetoothServerState GetState()
        {
            return _State;
        }
        // Set the Server state 
        public  void SetState(BluetoothServerState value)
        {
            BluetoothServerState OldState = _State;
            _State = value;
            if (this.StateChanged != null)
            {
                StateChanged(this, _State);
            }
        }
        private BluetoothServerState _State;
        private string[] StateStringArray = { "Created", "Stopped", "Started", "Connected" };
        // Return the StateString
        public string StateString { get { return StateStringArray[(int)GetState()]; } }



        // Bluetooth Server Initialisation method
        /// <summary>
        /// Method used to initialize the Bluetooth Server
        /// This method test if the machine can run the application (bluetooth on)
        /// </summary>
        public async Task<BluetoothServerReturnCode> Initialization()
        {
            try
            {
                rfcommProvider = await RfcommServiceProvider.CreateAsync(
                    RfcommServiceId.FromUuid(_BluetoothServiceUuid));
                if (rfcommProvider != null)
                {
                    SetState(BluetoothServerState.Stopped);
                    return BluetoothServerReturnCode.Success;
                }
                else
                    return BluetoothServerReturnCode.InitError;

            }
            catch (Exception e)
            {
                if ((uint)e.HResult == ERR_BLUETOOTH_OFF)
                    return BluetoothServerReturnCode.InitBluetoothOff;
                else if ((uint)e.HResult == ERR_MISSING_CAPS)
                    return BluetoothServerReturnCode.InitMissingCaps;
                else if ((uint)e.HResult == ERR_NOT_ADVERTISING)
                    return BluetoothServerReturnCode.InitNotAdvertising;

                NotifyLog("Error", "Exception in Initialization - " + e.Message);
                return BluetoothServerReturnCode.InitException;
            }
        }

        // Bluetooth Server Start method
        /// <summary>
        /// Method used to start the Bluetooth Server. 
        /// While the server is started, it's waiting for incoming connection request.
        /// </summary>
        public async Task<BluetoothServerReturnCode> Start()
        {
            try
            {
                NotifyLog("Information", "Starting the server...");
                if (rfcommProvider == null)
                    rfcommProvider = await RfcommServiceProvider.CreateAsync(
                        RfcommServiceId.FromUuid(_BluetoothServiceUuid));

                if (rfcommProvider != null)
                {
                    if (socketListener != null)
                    {
                        socketListener.Dispose();
                        socketListener = null;
                    }


                    // Create a listener for this service and start listening
                    socketListener = new StreamSocketListener();
                    socketListener.ConnectionReceived += OnConnectionReceived;

                    await socketListener.BindServiceNameAsync(rfcommProvider.ServiceId.AsString(),
                        SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);

                    // Set the SDP attributes and start Bluetooth advertising
                    DataWriter sdpWriter = new DataWriter();

                    // Write the Service Name Attribute.
                    sdpWriter.WriteByte(SdpServiceNameAttributeType);

                    // The length of the UTF-8 encoded Service Name SDP Attribute.
                    sdpWriter.WriteByte((byte)BluetoothServiceDisplayName.Length);

                    // The UTF-8 encoded Service Name value.
                    sdpWriter.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                    sdpWriter.WriteString(BluetoothServiceDisplayName);

                    // Set the SDP Attribute on the RFCOMM Service Provider.
                    if(rfcommProvider.SdpRawAttributes.ContainsKey(SdpServiceNameAttributeId))
                        rfcommProvider.SdpRawAttributes.Remove(SdpServiceNameAttributeId);
                    rfcommProvider.SdpRawAttributes.Add(SdpServiceNameAttributeId, sdpWriter.DetachBuffer());
                    // Start Bluetooth advertising
                    SetState(BluetoothServerState.Started); 
                    rfcommProvider.StartAdvertising(socketListener);
                    NotifyLog("Information", "Server Started");
                    return BluetoothServerReturnCode.Success;
                }
            }
            catch(Exception e)
            {
                if ((uint)e.HResult == ERR_BLUETOOTH_OFF)
                    return BluetoothServerReturnCode.StartBluetoothOff;
                else if ((uint)e.HResult == ERR_MISSING_CAPS)
                    return BluetoothServerReturnCode.StartMissingCaps;
                else if ((uint)e.HResult == ERR_NOT_ADVERTISING)
                    return BluetoothServerReturnCode.StartNotAdvertising;
                NotifyLog("Error", "Exception in Start - " + e.Message);
                return BluetoothServerReturnCode.StartException;
            }
            return BluetoothServerReturnCode.StartError;
        }
        // Bluetooth Server Stop method
        /// <summary>
        /// Method used to stop the Bluetooth Server. 
        /// </summary>
        public  BluetoothServerReturnCode Stop()
        {
            try
            {
                NotifyLog("Information", "Stopping the server..." );
                if (GetState() == BluetoothServerState.Connected)
                {
                    ForceDisconnect();
                }
                SetState(BluetoothServerState.Stopped);
                if (rfcommProvider != null)
                {
                    rfcommProvider.StopAdvertising();
                    rfcommProvider = null;
                }

                if (socketListener != null)
                {
                    socketListener.Dispose();
                    socketListener = null;
                }
                NotifyLog("Information", "Server stopped");
            }
            catch (Exception e)
            {
                NotifyLog("Error", "Exception in Stop - " + e.Message);
                return BluetoothServerReturnCode.StopException;
            }
            finally
            {
                rfcommProvider = null;
                socketListener = null;
                writer = null;
                socket = null;
            }
            return BluetoothServerReturnCode.Success;
        }
        /// <summary>
        /// Invoked when the socket listener accepted an incoming Bluetooth connection.
        /// </summary>
        /// <param name="sender">The socket listener that accecpted the connection.</param>
        /// <param name="args">The connection accept parameters, which contain the connected socket.</param>
        private void OnConnectionReceived(StreamSocketListener sender, 
            StreamSocketListenerConnectionReceivedEventArgs args)
        {
            try
            {
                // Don't need the listener anymore
                socketListener.Dispose();
                socketListener = null;

                socket = args.Socket;
                if((socket!=null) && (socket.Information!=null))
                {
                    _connectedDeviceDisplayName = "Remote Address: " + socket.Information.RemoteAddress.DisplayName + " Remote Port: " + socket.Information.RemotePort + " Remote Service: " + socket.Information.RemoteServiceName;
                    _connectedDeviceHostName = socket.Information.RemoteHostName.DisplayName;
                    _connectedDeviceServiceName = socket.Information.RemoteServiceName;
                }

                SetState(BluetoothServerState.Connected);
                NotifyLog("Information", "Client connected" );

                if (ConnectionReceived != null)
                {
                    BluetoothDevice device = new BluetoothDevice(_connectedDeviceHostName,_connectedDeviceServiceName,_connectedDeviceDisplayName);
                    ConnectionReceived(this, device);
                }
            }
            catch (Exception e)
            {
                NotifyLog("Error", "Exception in OnConnectionReceived - " + e.Message);
                return;
            }
            ReceiveIncomingCommands();
        }
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
                        if(bc!=null)
                        {
                           CommandReceived(this, bc);
                        }
                    }
                }
                else
                {
                    // if reader is not null, Disconnect is required
                    if(reader!=null)
                        await Disconnect();
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


        // Bluetooth Server Disconnect method
        /// <summary>
        /// Method used to disconnect the current connection established with the Bluetooth Server. 
        /// </summary>
        public async Task<BluetoothServerReturnCode> Disconnect()
        {
            try
            {
                ForceDisconnect();
                // Create a listener for this service and start listening
                if(socketListener==null)
                {
                    socketListener = new StreamSocketListener();
                    socketListener.ConnectionReceived += OnConnectionReceived;

                    await socketListener.BindServiceNameAsync(rfcommProvider.ServiceId.AsString(),
                        SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);
                }
                NotifyLog("Information", "Client Disconnected");
              
            }
            catch (Exception e)
            {
                NotifyLog("Error", "Exception in Disconnect - " + e.Message);
                return BluetoothServerReturnCode.DisconnectException;
            }
            finally
            {
                reader = null;
                writer = null;
                socket = null;
            }
            return BluetoothServerReturnCode.Success;
        }

        // Bluetooth Server ForceDisconnect method
        /// <summary>
        /// Method used to disconnect the current connection established with the Bluetooth Server. 
        /// </summary>
        public BluetoothServerReturnCode ForceDisconnect()
        {
            try
            {
                NotifyLog("Information", "Disconnecting Client...");
                SetState(BluetoothServerState.Started);

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
                return BluetoothServerReturnCode.DisconnectException;
            }
            finally
            {
                reader = null;
                writer = null;
                socket = null;
            }
            return BluetoothServerReturnCode.Success;
        }


        // Bluetooth Server SendCommand method
        /// <summary>
        /// Method used to send a Command towards the connected device  
        /// </summary>
        /// <param name="Message">Command to be sent towards the device</param>
        public async Task<BluetoothServerReturnCode> SendCommand(BluetoothCommand Command)
        {
            try
            {
                if (Command.CommandLength == 0)
                    return BluetoothServerReturnCode.SendEmptyMessage;
                if (socket != null)
                {
                    if (writer == null)
                        writer = new DataWriter(socket.OutputStream);
                    if (writer != null)
                    {
                        writer.WriteUInt32((uint)Command.CommandLength);
                        writer.WriteBytes(Command.CommandBytes);

                        await writer.StoreAsync();
                        return BluetoothServerReturnCode.Success;
                    }
                }
                else
                    return BluetoothServerReturnCode.SendNoConnection;
            }
            catch (Exception e)
            {
                NotifyLog("Error", "Exception in SendCommand - " + e.Message);
                return BluetoothServerReturnCode.SendException;
            }
            return BluetoothServerReturnCode.SendError;
        }


        // Bluetooth Server ReadCommand method
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

                    readLength = await reader.LoadAsync(currentLength);
                    if (readLength < currentLength)
                    {
                        return null;
                    }
                    byte[] ReceivedBuffer = new byte[currentLength];
                    reader.ReadBytes(ReceivedBuffer);
                    return ReceivedBuffer;
                }
            }
            catch(Exception e)
            {
                NotifyLog("Error", "Exception in ReadCommand - " + e.Message);
            }
            return null;
        }

        // StateChanged event
        // This event occurs each time the application State changes
        public event TypedEventHandler<IBluetoothServer, BluetoothServerState> StateChanged;
        // ConnectionReceived event
        // This event occurs each time a device is connected with the server
        public event TypedEventHandler<IBluetoothServer, BluetoothDevice> ConnectionReceived;
        // CommandReceived event
        // This event occurs each time a command is received 
        public event TypedEventHandler<IBluetoothServer, BluetoothCommand> CommandReceived;
        // LogReceived event
        // This event occurs each time the server wants to log information
        public event TypedEventHandler<IBluetoothServer, string> LogReceived;

    }
}
