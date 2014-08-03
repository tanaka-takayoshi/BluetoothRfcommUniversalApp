using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// 空白ページのアイテム テンプレートについては、http://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace BluetoothRfcommUniversalApp
{
    /// <summary>
    /// Frame 内へナビゲートするために利用する空欄ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public string BluetoothServiceName { get { return (App.Current as App).BluetoothRfcommServer.BluetoothServiceName; } }
        public string BluetoothServiceDisplayName { get { return (App.Current as App).BluetoothRfcommServer.BluetoothServiceDisplayName; } }

        public bool AutoStart { get { return (App.Current as App).BluetoothRfcommServer.AutoStart; } set { (App.Current as App).BluetoothRfcommServer.AutoStart = value; NotifyPropertyChanged("AutoStart"); } }
        public bool IsNotConnected { get { return !(App.Current as App).BluetoothRfcommServer.IsConnected; } }
        public bool IsConnected { get { return (App.Current as App).BluetoothRfcommServer.IsConnected; } }
        public bool IsStarted { get { return (App.Current as App).BluetoothRfcommServer.IsStarted; } }
        public bool IsStopped { get { return (App.Current as App).BluetoothRfcommServer.IsStopped; } }
        public string StateString { get { return (App.Current as App).BluetoothRfcommServer.StateString; } }


        public MainPage()
        {
            this.InitializeComponent();
            ImageReceived.DoubleTapped += ImageReceived_DoubleTapped;
        }

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

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // Set DataContext for Databinding
            this.DataContext = this;
            // Subscribe to ConnectionReceived, CommandReceived and LogReceived events
            (App.Current as App).BluetoothRfcommServer.ConnectionReceived += BluetoothRfcommServer_ConnectionReceived;
            (App.Current as App).BluetoothRfcommServer.CommandReceived += BluetoothRfcommServer_CommandReceived;
            (App.Current as App).BluetoothRfcommServer.StateChanged += BluetoothRfcommServer_StateChanged;
            (App.Current as App).BluetoothRfcommServer.LogReceived += BluetoothRfcommServer_LogReceived;



            // Initialize Blutooth Server to check whether the bluetooth is on
            if (((App.Current as App).BluetoothRfcommServer != null) && ((App.Current as App).BluetoothRfcommServer.GetState() == BluetoothServerState.Created))
            {
                BluetoothServerReturnCode r = await (App.Current as App).BluetoothRfcommServer.Initialization();
                if ((r == BluetoothServerReturnCode.Success) && ((App.Current as App).BluetoothRfcommServer.AutoStart == true))
                {
                    r = await (App.Current as App).BluetoothRfcommServer.Start();
                }
                if (r != BluetoothServerReturnCode.Success)
                {
                    if ((r == BluetoothServerReturnCode.InitMissingCaps) || (r == BluetoothServerReturnCode.StartMissingCaps))
                    {
                        Windows.UI.Popups.MessageDialog messageDialog = new Windows.UI.Popups.MessageDialog("bluetooth.rfcomm Capability missing in the application manifest", "Windows Bluetooth Server");
                        await messageDialog.ShowAsync();
                    }
                    else if ((r == BluetoothServerReturnCode.InitBluetoothOff) || (r == BluetoothServerReturnCode.StartBluetoothOff))
                    {
                        Windows.UI.Popups.MessageDialog messageDialog = new Windows.UI.Popups.MessageDialog("Bluetooth is off, bluetooth is required to launch the server", "Windows Bluetooth Server");
                        await messageDialog.ShowAsync();
                    }
                    else if (r == BluetoothServerReturnCode.StartNotAdvertising)
                    {
                        Windows.UI.Popups.MessageDialog messageDialog = new Windows.UI.Popups.MessageDialog("Bluetooth Advertising Error, restart the application", "Windows Bluetooth Server");
                        await messageDialog.ShowAsync();
                    }
                    else
                    {
                        Windows.UI.Popups.MessageDialog messageDialog = new Windows.UI.Popups.MessageDialog("Error occurs while initializing the server", "Windows Bluetooth Server");
                        await messageDialog.ShowAsync();
                    }
                }
            }

        }

        void BluetoothRfcommServer_StateChanged(IBluetoothServer sender, BluetoothServerState args)
        {
            RunOnDispatcherThread(() =>
            {
                NotifyPropertyChanged("IsStopped");
                NotifyPropertyChanged("IsStarted");
                NotifyPropertyChanged("IsConnected");
                NotifyPropertyChanged("IsNotConnected");
                NotifyPropertyChanged("StateString");
            });
        }
        /// <summary>
        /// Invoked before the page is unloaded.
        /// </summary>
        /// <param name="e">Event data that describes how this page will be unloaded.</param>
        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            // Subscribe to ConnectionReceived and LogReceived events
            (App.Current as App).BluetoothRfcommServer.ConnectionReceived -= BluetoothRfcommServer_ConnectionReceived;
            (App.Current as App).BluetoothRfcommServer.LogReceived -= BluetoothRfcommServer_LogReceived;

            base.OnNavigatingFrom(e);
        }
        // RunOnDispatcherThread 
        // Execute asynchronously on the thread the Dispatcher is associated with.
        private async void RunOnDispatcherThread(Action action)
        {
            if (Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess)
            {
                action();
            }
            else
            {
                // Execute asynchronously on the thread the Dispatcher is associated with.
                await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    action();
                }
                );
            }
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
                {
                    ListLogs.SelectedIndex = ListLogs.Items.Count - 1;
                    ListLogs.UpdateLayout();
                    ListLogs.ScrollIntoView(ListLogs.Items[ListLogs.SelectedIndex]);
                }
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
                {
                    ListMessage.SelectedIndex = ListMessage.Items.Count - 1;
                    ListMessage.UpdateLayout();
                    ListMessage.ScrollIntoView(ListMessage.Items[ListMessage.SelectedIndex]);
                }
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
        // Method associated with LogReceived event
        void BluetoothRfcommServer_LogReceived(IBluetoothServer sender, string args)
        {
            AddLog(args);
        }

        // Method associated with ConnectionReceived event
        // Once connected, this method calls ReadMessage method to receive messages in the while(true) loop.
        // If the received message is null, it leaves the while(true) loop.
        // ReadMessage return null when the connection with the device is lost.
        void BluetoothRfcommServer_ConnectionReceived(IBluetoothServer sender, BluetoothDevice args)
        {
            ClearMessage();
            AddLog("Connected with " + (App.Current as App).BluetoothRfcommServer.ConnectedDeviceDisplayName);
        }

        // Method associated with CommandReceived event
        // This method dispatches the received Command 
        void BluetoothRfcommServer_CommandReceived(IBluetoothServer sender, BluetoothCommand args)
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
                RunOnDispatcherThread(() =>
                {
                    BluetoothCommandPicture bp = args as BluetoothCommandPicture;
                    if (bp != null)
                    {
                        DisplayImage(bp.PictureContent);
                        if (bp.PictureContent.Length > 0)
                        {
                            _ImageBuffer = new byte[bp.PictureSize];
                            if (_ImageBuffer != null)
                                Array.Copy(bp.PictureContent, _ImageBuffer, bp.PictureContent.Length);
                        }
                    }
                });

            }
        }

        // Method associated with Click on Start button
        private async void ButtonStart_Click(object sender, RoutedEventArgs e)
        {
            ClearMessage();
            ClearLog();
            await (App.Current as App).BluetoothRfcommServer.Start();
        }
        // Method associated with Click on Stop button
        private void ButtonStop_Click(object sender, RoutedEventArgs e)
        {
            (App.Current as App).BluetoothRfcommServer.Stop();
        }
        // Method associated with Click on Disconnect button
        private async void ButtonDisconnect_Click(object sender, RoutedEventArgs e)
        {
            await (App.Current as App).BluetoothRfcommServer.Disconnect();
        }
        // Method associated with Click on Send button
        private async void ButtonSend_Click(object sender, RoutedEventArgs e)
        {
            BluetoothCommandMessage bm = new BluetoothCommandMessage();
            if (bm != null)
            {
                if (!string.IsNullOrEmpty(Message.Text))
                {
                    bm.CreateMessageCommand(Message.Text);
                    BluetoothServerReturnCode r = await (App.Current as App).BluetoothRfcommServer.SendCommand(bm);
                    if (r == BluetoothServerReturnCode.Success)
                    {
                        AddLog("Command Message Sent: " + Message.Text);
                        AddMessage("Message Sent: " + Message.Text);
                    }
                }
            }
        }

        // Method associated with Click on Save Image button
        private async void ButtonSaveImage_Click(object sender, RoutedEventArgs e)
        {
            // Launch file picker
            try
            {
                FileSavePicker picker = new FileSavePicker();
                picker.FileTypeChoices.Add("JPeg", new List<string>() { ".jpg", ".jpeg" });
                StorageFile file = await picker.PickSaveFileAsync();

                if (file == null)
                    return;
                using (Stream x = await file.OpenStreamForWriteAsync())
                {
                    x.Seek(0, SeekOrigin.Begin);
                    if (_ImageBuffer != null)
                        await x.WriteAsync(_ImageBuffer, 0, _ImageBuffer.Length);
                    await x.FlushAsync();
                    AddLog("Image saved : " + file.Path);
                }
            }
            catch (Exception ex)
            {
                AddLog("Exception while saving image: " + ex.Message);
            }
        }
        // Image buffer
        private byte[] _ImageBuffer;
        // Method which displays the received picture
        async void DisplayImage(byte[] buffer)
        {
            try
            {
                StorageFolder localFolder = KnownFolders.PicturesLibrary;
                DateTime date = DateTime.Now;
                Windows.Storage.StorageFile file = await localFolder.CreateFileAsync(BluetoothRfcommGlobal.BluetoothServiceUuid + "_" + date.Ticks.ToString() + ".jpg", Windows.Storage.CreationCollisionOption.ReplaceExisting);

                Windows.Storage.Streams.IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
                var outStream = fileStream.GetOutputStreamAt(0);

                DataWriter dataWriter = new DataWriter(outStream);
                dataWriter.WriteBytes(buffer);
                await dataWriter.StoreAsync();
                dataWriter.DetachStream();
                await outStream.FlushAsync();

                BitmapImage bi = new BitmapImage();
                bi.SetSource(fileStream);
                ImageReceived.Source = bi;

                await file.DeleteAsync(StorageDeleteOption.PermanentDelete);

            }
            catch (Exception ex)
            {
                AddLog("Exception while displaying received image: " + ex.Message);
            }
        }

        // Method associated with the display in fullscreen of the received picture
        void ImageReceived_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            try
            {

                if (ImageReceived.ActualHeight != MainGrid.ActualHeight)
                {
                    SecondGrid.Children.Remove(ImageReceived);
                    SecondGrid.Children.Remove(ImageReceivedRectangle);
                    MainGrid.Children.Add(ImageReceivedRectangle);
                    MainGrid.Children.Add(ImageReceived);

                    ImageReceivedRectangle.SetValue(Grid.RowProperty, 0);
                    ImageReceivedRectangle.SetValue(Grid.ColumnProperty, 0);
                    ImageReceivedRectangle.SetValue(Grid.ColumnSpanProperty, 2);
                    ImageReceivedRectangle.SetValue(Grid.RowSpanProperty, 2);
                    ImageReceivedRectangle.Height = MainGrid.Height;
                    ImageReceivedRectangle.Width = MainGrid.Width;
                    ImageReceivedRectangle.Margin = new Thickness(0, 0, 0, 0);

                    ImageReceived.SetValue(Grid.RowProperty, 0);
                    ImageReceived.SetValue(Grid.ColumnProperty, 0);
                    ImageReceived.SetValue(Grid.ColumnSpanProperty, 2);
                    ImageReceived.SetValue(Grid.RowSpanProperty, 2);
                    ImageReceived.Height = MainGrid.Height;
                    ImageReceived.Width = MainGrid.Width;
                    ImageReceived.Margin = new Thickness(0, 0, 0, 0);

                }
                else
                {
                    MainGrid.Children.Remove(ImageReceived);
                    MainGrid.Children.Remove(ImageReceivedRectangle);
                    SecondGrid.Children.Add(ImageReceivedRectangle);
                    SecondGrid.Children.Add(ImageReceived);

                    ImageReceivedRectangle.SetValue(Grid.RowProperty, 3);
                    ImageReceivedRectangle.SetValue(Grid.ColumnProperty, 0);
                    ImageReceivedRectangle.SetValue(Grid.ColumnSpanProperty, 1);
                    ImageReceivedRectangle.SetValue(Grid.RowSpanProperty, 1);
                    ImageReceivedRectangle.Margin = new Thickness(10, 10, 10, 10);

                    ImageReceived.SetValue(Grid.RowProperty, 3);
                    ImageReceived.SetValue(Grid.ColumnProperty, 0);
                    ImageReceived.SetValue(Grid.ColumnSpanProperty, 1);
                    ImageReceived.SetValue(Grid.RowSpanProperty, 1);
                    ImageReceived.Margin = new Thickness(10, 10, 10, 10);
                }
            }
            catch (Exception ex)
            {
                AddLog("Exception while changing image display mode : " + ex.Message);
            }
        }

    }
}
