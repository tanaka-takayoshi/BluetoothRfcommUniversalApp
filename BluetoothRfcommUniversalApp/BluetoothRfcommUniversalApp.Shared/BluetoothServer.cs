
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
using System.Threading.Tasks;
using Windows.Foundation;

namespace BluetoothRfcommUniversalApp
{
    // Predefined Error codes of the IBluetoothServer Interface
    public enum BluetoothServerReturnCode
    {
        Success = 0,
        InitError,
        InitBluetoothOff,
        InitMissingCaps,
        InitNotAdvertising,
        InitEmulator,
        InitException,
        StartMissingCaps,
        StartBluetoothOff,
        StartNotAdvertising,
        StartException, 
        StartError,
        StopException,
        ConnectError,
        SendEmptyMessage,
        SendNoConnection,
        SendError,
        SendException,
        DisconnectException,
        ReadNoConnection,
        ReadError,
    }
    // BluetoothServer states
    public enum BluetoothServerState
    {
        Created = 0,
        Stopped,
        Started,
        Connected,
    }
    // BluetoothServer Interface
    public interface IBluetoothServer
    {
        // Service Uuid: this GUID is unique for this application and is shared 
        // between the Windows 8.X application and the Windows Phone 8 application 
        Guid BluetoothServiceUuid { get; }
        // Service Display Name: this name is shared 
        // between the Windows 8.X application and the Windows Phone 8 application 
        string BluetoothServiceDisplayName { get; }

        // When AutoStart = true, the server will start automactically
        bool AutoStart { get; }

        // When true, the server is connected with a device (For instance a Windows Phone)
        bool IsConnected { get; }

        // When IsConnected is true, the  properties below contain information about the connected device
        string ConnectedDeviceDisplayName { get; }
        string ConnectedDeviceHostName { get; }
        string ConnectedDeviceServiceName { get; }

        // Return the state of the server
        BluetoothServerState GetState();

        // Bluetooth Server Initialisation method
        /// <summary>
        /// Method used to initialize the Bluetooth Server
        /// </summary>
        Task<BluetoothServerReturnCode> Initialization();

        // Bluetooth Server Start method
        /// <summary>
        /// Method used to start the Bluetooth Server. 
        /// While the server is started, it's waiting for incoming connection request.
        /// </summary>
        Task<BluetoothServerReturnCode> Start();

        // Bluetooth Server Stop method
        /// <summary>
        /// Method used to stop the Bluetooth Server. 
        /// </summary>
        BluetoothServerReturnCode Stop();

        // Bluetooth Server Disconnect method
        /// <summary>
        /// Method used to disconnect the current connection established with the Bluetooth Server. 
        /// </summary>
        Task<BluetoothServerReturnCode> Disconnect();

        // Bluetooth Server SendMessage method
        /// <summary>
        /// Method used to send a message towards the connected device  
        /// </summary>
        /// <param name="Message">Message to be sent towards the device</param>
        Task<BluetoothServerReturnCode> SendCommand(BluetoothCommand Command);

        // Bluetooth Server ReadCommand method
        /// <summary>
        /// Method used to receive command from the connected device  
        /// </summary>
        /// <return>String received, null if error occured</return>
        Task<byte[]> ReadCommand();

        // StateChanged event
        // This event occurs each time the application State changes
        event TypedEventHandler<IBluetoothServer, BluetoothServerState> StateChanged;
        // ConnectionReceived event
        // This event occurs each time a device is connected with the server
        event TypedEventHandler<IBluetoothServer, BluetoothDevice> ConnectionReceived;
        // This event occurs each time a command is received 
        event TypedEventHandler<IBluetoothServer, BluetoothCommand> CommandReceived;

        // LogReceived event
        // This event occurs each time the server wants to log information
        event TypedEventHandler<IBluetoothServer, string> LogReceived;


    }
}