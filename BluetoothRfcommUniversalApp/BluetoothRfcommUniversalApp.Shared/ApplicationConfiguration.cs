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
using Windows.Storage;

namespace BluetoothRfcommUniversalApp
{
    // ApplicationConfiguration Class
    // 
    // This class is used to manage the persistent parameters of the application
    // AutoStart: When AutoStart=true the application will start automatically the server 
    //            when the application is launching or resuming.
    class ApplicationConfiguration
    {
        private static ApplicationDataContainer SettingsContainer { get; set; }
        static ApplicationConfiguration()
        {
            SettingsContainer = ApplicationData.Current.LocalSettings;
        }
        public static bool AutoStart
        {
            get {

                string s = string.Empty;
                try
                {
                    s = SettingsContainer.Values["AutoStart"] as string;
                    if (!string.IsNullOrEmpty(s))
                        return Convert.ToBoolean(s);
                }
                catch
                {
                    return false;
                }
                return false; 
            }
            set { SettingsContainer.Values["AutoStart"] = value.ToString(); }
        }
    }
}
