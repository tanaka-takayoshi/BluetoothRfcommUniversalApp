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
using Windows.Foundation;
using System.Collections.Generic;
using System.Threading.Tasks;
using BluetoothRfcommUniversalApp;

namespace BluetoothRfcommUniversalApp
{
    // Predefined Error codes of the IBluetoothServer Interface
    public enum BluetoothClientReturnCode
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
    public enum BluetoothClientState
    {
        Created = 0,
        Initialized,
        Connected,
    }
    // BluetoothServer Interface
    public interface IBluetoothClient
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
        BluetoothClientState GetState();

        // Bluetooth Client Initialisation method
        /// <summary>
        /// Method used to initialize the Bluetooth Client
        /// </summary>
        Task<BluetoothClientReturnCode> Initialization();

        // Bluetooth Client Connect method
        /// <summary>
        /// Invoked to establish a connection with a bluetooth server.
        /// </summary>
        /// <param name="device">BluetoothDevice associated with the bluetooth server.</param>
        Task<BluetoothClientReturnCode> Connect(BluetoothDevice device);

        // Bluetooth Client Disconnect method
        /// <summary>
        /// Method used to disconnect the current connection established with the Bluetooth Client. 
        /// </summary>
        BluetoothClientReturnCode Disconnect();

        // Bluetooth Server SendMessage method
        /// <summary>
        /// Method used to send a message towards the connected device  
        /// </summary>
        /// <param name="Message">Message to be sent towards the device</param>
        Task<BluetoothClientReturnCode> SendCommand(BluetoothCommand Command);

        // Bluetooth Server ReadCommand method
        /// <summary>
        /// Method used to receive command from the connected device  
        /// </summary>
        /// <return>String received, null if error occured</return>
        Task<byte[]> ReadCommand();

        // Bluetooth Client GetListPairedDevices method
        /// <summary>
        /// Method used to receive message from the connected device  
        /// </summary>
        /// <return>String received, null if error occured</return>
        Task<List<BluetoothDevice>> GetListPairedDevices();


        // ConnectionEventReceived event
        // This event occurs each time the application is connected or disconnected with the server
        event TypedEventHandler<IBluetoothClient, BluetoothClientState> StateChanged;
        // CommandReceived event
        // This event occurs each time a command is received 
        event TypedEventHandler<IBluetoothClient, BluetoothCommand> CommandReceived;
        // LogReceived event
        // This event occurs each time the server wants to log information
        event TypedEventHandler<IBluetoothClient, string> LogReceived;


    }
}