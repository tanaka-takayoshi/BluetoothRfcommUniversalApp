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

namespace BluetoothRfcommUniversalApp
{
    public class BluetoothRfcommGlobal
    {
       // Service uuid: shared between the bluetooth server (Windows 8 Application) and the bluetoth client (Windows Phone Application)
       public static Guid BluetoothServiceUuid = new Guid("17890000-0068-0069-1532-1992D79BE4D8");
       // Service display name: shared between the bluetooth server (Windows 8 Application) and the bluetoth client (Windows Phone Application)
       public static string BluetoothServiceDisplayName = "My Bluetooth Rfcomm Service";
    }
    
}
