using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BluetoothRfcommUniversalApp
{
    /// <summary>
    /// Frame 内へナビゲートするために利用する空欄ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public string BluetoothServiceName { get { return (App.Current as App).BluetoothRfcommClient.BluetoothServiceName; } }
        public string BluetoothServiceDisplayName { get { return (App.Current as App).BluetoothRfcommClient.BluetoothServiceDisplayName; } }

        public bool AutoStart { get { return (App.Current as App).BluetoothRfcommClient.AutoStart; } set { (App.Current as App).BluetoothRfcommClient.AutoStart = value; NotifyPropertyChanged("AutoStart"); } }
        public bool IsNotConnected { get { return !(App.Current as App).BluetoothRfcommClient.IsConnected; } }
        public bool IsConnected { get { return (App.Current as App).BluetoothRfcommClient.IsConnected; } }
        public string StateString { get { return (App.Current as App).BluetoothRfcommClient.StateString; } }
        public bool IsConnectedAndImageLoaded { get { return _IsConnectedAndImageLoaded; } }
        private bool _IsConnectedAndImageLoaded;

        void UpdateIsConnectedAndImageLoaded()
        {
            bool bImageLoaded = false;
            if (_ImageBuffer != null)
            {
                int BufferLen = _ImageBuffer.Length;
                int FileLen = 0;
                if ((int.TryParse(ImageFileSize.Text, out FileLen)) &&
                    (FileLen == BufferLen))
                    bImageLoaded = true;
            }
            bool bConnected = (App.Current as App).BluetoothRfcommClient.IsConnected;
            bool newValue = bConnected && bImageLoaded;

            if (_IsConnectedAndImageLoaded != newValue)
            {
                _IsConnectedAndImageLoaded = newValue;
                NotifyPropertyChanged("IsConnectedAndImageLoaded");
            }
        }
        public string PictureMaxSize { get { return _PictureMaxSize.ToString(); } }
        uint _PictureMaxSize = 3000000;

        // This event method is used for the databinding
        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        // List of Paired devices
        private ObservableCollection<BluetoothDevice> _listPairedDevices;

        // Image Buffer
        byte[] _ImageBuffer;
        // Constructor
        public MainPage()
        {
            InitializeComponent();


            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();

            // Initialize the list of Paired devices
            _listPairedDevices = new ObservableCollection<BluetoothDevice>();
            ListDevices.ItemsSource = _listPairedDevices;

            // Set DataContext for Databinding
            DataContext = this;
            UpdateIsConnectedAndImageLoaded();

        }
        // Load list of Paired devices
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            // Check if BluetoothRfcommClient instance
            if ((App.Current as App).BluetoothRfcommClient == null)
                return;
            // Check BluetoothRfcommClient State
            // If State == BluetoothClientState.Created, the bluetooth client didn't start
            if ((App.Current as App).BluetoothRfcommClient.GetState() == BluetoothClientState.Created)
            {
                BluetoothClientReturnCode r = await (App.Current as App).BluetoothRfcommClient.Initialization();
                if (r != BluetoothClientReturnCode.Success)
                {
                    if ((r == BluetoothClientReturnCode.InitMissingCaps) || (r == BluetoothClientReturnCode.StartMissingCaps))
                    {
                        //   MessageBox.Show("bluetooth.rfcomm Capability missing in the application manifest", "Windows Phone Bluetooth Client", MessageBoxButton.OK);
                    }
                    else if ((r == BluetoothClientReturnCode.InitBluetoothOff) || (r == BluetoothClientReturnCode.StartBluetoothOff))
                    {
                        //   MessageBox.Show("Bluetooth is off, bluetooth is required to launch the client", "Windows Phone Bluetooth Client", MessageBoxButton.OK);
                        {
                            //ConnectionSettingsTask connectionSettingsTask = new ConnectionSettingsTask();
                            //connectionSettingsTask.ConnectionSettingsType = ConnectionSettingsType.Bluetooth;
                            //connectionSettingsTask.Show();
                        }
                    }
                    else if (r == BluetoothClientReturnCode.StartNotAdvertising)
                    {
                        // MessageBox.Show("Bluetooth Advertising Error, restart the application", "Windows Phone Bluetooth Client", MessageBoxButton.OK);
                    }
                    else
                    {
                        //  MessageBox.Show("Error occurs while initializing the client", "Windows Phone Bluetooth Client", MessageBoxButton.OK);
                    }
                    return;
                }
            }

            // Subscribe to CommandReceived, LogReceived and StateChanged
            (App.Current as App).BluetoothRfcommClient.LogReceived += BluetoothRfcommClient_LogReceived;
            (App.Current as App).BluetoothRfcommClient.CommandReceived += BluetoothRfcommClient_CommandReceived;
            (App.Current as App).BluetoothRfcommClient.StateChanged += BluetoothRfcommClient_StateChanged;

            // Update IsConnected state
            UpdateIsConnectedAndImageLoaded();

            // Fill the list of Paired Devices 
            FillListPairedDevices();

        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            // Unsubscribe to CommandReceived, LogReceived and StateChanged
            (App.Current as App).BluetoothRfcommClient.LogReceived -= BluetoothRfcommClient_LogReceived;
            (App.Current as App).BluetoothRfcommClient.CommandReceived -= BluetoothRfcommClient_CommandReceived;
            (App.Current as App).BluetoothRfcommClient.StateChanged -= BluetoothRfcommClient_StateChanged;
        }
        void BluetoothRfcommClient_StateChanged(IBluetoothClient sender, BluetoothClientState NewState)
        {
            RunOnDispatcherThread(() =>
            {
                UpdateIsConnectedAndImageLoaded();
                NotifyPropertyChanged("IsConnected");
                NotifyPropertyChanged("IsNotConnected");
                NotifyPropertyChanged("StateString");
            });

        }
        // RunOnDispatcherThread 
        // Execute asynchronously on the thread the Dispatcher is associated with.
        private void RunOnDispatcherThread(Action action)
        {
            Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
        }

        void BluetoothRfcommClient_LogReceived(IBluetoothClient sender, string args)
        {
            AddLog(args);
        }
        // Add Log in the ListLogs
        void AddLog(string Log)
        {
            DateTime Date = DateTime.Now;
            string DateString = string.Format("{0:d/M/yyyy HH:mm:ss.fff}: ", Date);

            RunOnDispatcherThread(() =>
            {
                ListLogs.Items.Add(DateString + Log);
                if (ListLogs.Items.Count > 0)
                    ListLogs.SelectedIndex = ListLogs.Items.Count - 1;
            });
        }
        // Clear the ListLogs
        void ClearLog()
        {
            RunOnDispatcherThread(() =>
            {
                ListLogs.Items.Clear();
            });
        }

        // Add a Message in the ListMessage
        void AddMessage(string Msg)
        {
            DateTime Date = DateTime.Now;
            string DateString = string.Format("{0:d/M/yyyy HH:mm:ss.fff}: ", Date);

            RunOnDispatcherThread(() =>
            {
                ListMessage.Items.Add(DateString + Msg);
                if (ListMessage.Items.Count > 0)
                    ListMessage.SelectedIndex = ListMessage.Items.Count - 1;
            });
        }
        // Clear the ListMessage
        void ClearMessage()
        {
            RunOnDispatcherThread(() =>
            {
                ListMessage.Items.Clear();
            });
        }

        // Add a Commmand File in the ListFile
        void AddFile(string File)
        {
            DateTime Date = DateTime.Now;
            string DateString = string.Format("{0:d/M/yyyy HH:mm:ss.fff}: ", Date);

            RunOnDispatcherThread(() =>
            {
                ListFile.Items.Add(DateString + File);
                if (ListFile.Items.Count > 0)
                    ListFile.SelectedIndex = ListFile.Items.Count - 1;
            });
        }
        // Clear the ListMessage
        void ClearFile()
        {
            RunOnDispatcherThread(() =>
            {
                ListFile.Items.Clear();
            });
        }

        // Method associated with Click on Send button
        private async void ButtonSend_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Message.Text))
            {
                BluetoothCommandMessage bm = new BluetoothCommandMessage();
                if (bm != null)
                {
                    bm.CreateMessageCommand(Message.Text);
                    BluetoothClientReturnCode r = await (App.Current as App).BluetoothRfcommClient.SendCommand(bm);
                    if (r == BluetoothClientReturnCode.Success)
                        AddMessage("Message Sent: " + Message.Text);
                }
            }
        }


        // Method associated with Click on Send button
        private async void ButtonSendImageFile_Click(object sender, RoutedEventArgs e)
        {
            BluetoothCommandPicture bp = new BluetoothCommandPicture();
            if (bp != null)
            {
                int len = 0;
                if (int.TryParse(ImageFileSize.Text, out len))
                {
                    bp.CreatePictureCommand(ImageFilePath.Text, len, _ImageBuffer);
                    BluetoothClientReturnCode r = await (App.Current as App).BluetoothRfcommClient.SendCommand(bp);
                    if (r == BluetoothClientReturnCode.Success)
                        AddFile("Command Picture Sent: " + ImageFilePath.Text);
                }
            }
        }
        private void ButtonSelectImageFile_Click(object sender, RoutedEventArgs e)
        {
            //PhotoChooserTask chooser = new PhotoChooserTask();
            //chooser.Completed += choosenImage;
            //try
            //{
            //    chooser.Show();
            //}
            //catch (Exception ex)
            //{
            //    AddLog("Exception while selecting the picture" + ex.Message);
            //}
        }
        //private async void choosenImage(object sender, PhotoResult e)
        //{
        //    if (e.TaskResult != TaskResult.OK)
        //    {
        //        return;
        //    }

        //    if (e.ChosenPhoto.Length < _PictureMaxSize)
        //    {
        //        try
        //        {
        //            int Len = (int)e.ChosenPhoto.Length;
        //            _ImageBuffer = new byte[Len];
        //            e.ChosenPhoto.Seek(0, SeekOrigin.Begin);
        //            int LenRead = await e.ChosenPhoto.ReadAsync(_ImageBuffer, 0, Len);
        //            if (LenRead == Len)
        //            {
        //                ImageFilePath.Text = e.OriginalFileName;
        //                ImageFileSize.Text = e.ChosenPhoto.Length.ToString();
        //                UpdateIsConnectedAndImageLoaded();
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            AddLog("Exception while opening the picure file: " + ex.Message);
        //        }
        //    }
        //    else
        //    {
        //    //    MessageBox.Show("Picture file size too large", "Windows Phone Bluetooth Client", MessageBoxButton.OK);
        //        AddLog("Picture file size too large: " + e.ChosenPhoto.Length.ToString());
        //    }
        //}

        async void FillListPairedDevices()
        {
            List<BluetoothDevice> List = await (App.Current as App).BluetoothRfcommClient.GetListPairedDevices();
            if (List != null)
            {
                _listPairedDevices.Clear();

                if (List.Count == 0)
                {
                    PairedDevicesMessage.Text = "No paired devices found ";
                }
                else
                {
                    PairedDevicesMessage.Text = List.Count.ToString() + " paired device(s) found: ";
                    // Add peers to list
                    foreach (var device in List)
                    {
                        _listPairedDevices.Add(device);
                    }
                    string LastDeviceDisplayName = string.Empty;
                    if ((App.Current as App).BluetoothRfcommClient.IsConnected)
                        LastDeviceDisplayName = (App.Current as App).BluetoothRfcommClient.ConnectedDeviceDisplayName;
                    else
                        LastDeviceDisplayName = ""; //TODO ApplicationConfiguration.ConnectedDeviceDisplayName;
                    if (!string.IsNullOrEmpty(LastDeviceDisplayName))
                    {
                        for (int i = 0; i < _listPairedDevices.Count; i++)
                        {
                            if (_listPairedDevices[i].DisplayName == LastDeviceDisplayName)
                            {
                                ListDevices.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                    else
                        ListDevices.SelectedIndex = 0;
                }

            }
        }
        private void ButtonListPairedDevices_Click(object sender, RoutedEventArgs e)
        {
            FillListPairedDevices();
        }
        // Method associated with CommandReceived event
        // This method dispatches the received Command 
        void BluetoothRfcommClient_CommandReceived(IBluetoothClient sender, BluetoothCommand args)
        {
            AddLog("Command received " + args.ToString());
            if (args.GetType() == typeof(BluetoothCommandMessage))
            {
                BluetoothCommandMessage bm = args as BluetoothCommandMessage;
                if (bm != null)
                    AddMessage("Message Received: " + bm.MessageContent);
            }
            else if (args.GetType() == typeof(BluetoothCommandPicture))
            {
                BluetoothCommandPicture bp = args as BluetoothCommandPicture;
                if (bp != null)
                    AddFile("File Received: " + bp.PicturePath);
            }
        }

        private async void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            if ((ListDevices.SelectedIndex >= 0) && (ListDevices.SelectedIndex < ListDevices.Items.Count))
            {
                BluetoothDevice d = ListDevices.Items[ListDevices.SelectedIndex] as BluetoothDevice;
                if (d != null)
                {
                    BluetoothClientReturnCode r = await (App.Current as App).BluetoothRfcommClient.Connect(d);
                    if (r == BluetoothClientReturnCode.Success)
                    {
                        // Start Message loop
                        ClearMessage();
                        ClearFile();
                    }
                }
            };
        }

        private void ButtonDisconnect_Click(object sender, RoutedEventArgs e)
        {
            (App.Current as App).BluetoothRfcommClient.Disconnect();

        }

        // Sample code for building a localized ApplicationBar
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Set the page's ApplicationBar to a new instance of ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Create a new button and set the text value to the localized string from AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Create a new menu item with the localized string from AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}

    }
}
