using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using Coding4Fun.Phone.Controls;
using System.Net.NetworkInformation;
using Windows.Devices.Geolocation;

namespace habhub_chase_car_tracker
{
    public partial class MainPage : PhoneApplicationPage
    {

        private string callsign = "default";
        private uint interval = 120000;
        IsolatedStorageSettings savedSettings;

        bool consent = false;
        bool isRunning = false;

        Apex.Habitat habitat;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            savedSettings = IsolatedStorageSettings.ApplicationSettings;

            if (savedSettings.Contains("callsign"))
            {
                callsign = (string)savedSettings["callsign"];
            }

            tbCallsign.Text = callsign + "_chase";

            if (savedSettings.Contains("interval"))
            {
                interval = Convert.ToUInt32(savedSettings["interval"]);
            }

            if (savedSettings.Contains("consent"))
            {
                consent = (bool)savedSettings["consent"];
            }

            if (!consent)
            {
                MessageBoxResult result = MessageBox.Show("This app accesses your phone's location. Is this okay?", "Location", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    updateSetting("consent", true);
                    consent = true;
                }

            }

        }


        private void start()
        {

            if (!consent)
            {
                stop();
                return;
            }
                  
            if (App.Geolocator == null)
            {
                App.Geolocator = new Geolocator();
                App.Geolocator.DesiredAccuracy = PositionAccuracy.High;
                App.Geolocator.MovementThreshold = 100; // The units are meters.
                App.Geolocator.ReportInterval = interval;
                App.Geolocator.PositionChanged += positionChanged;
                App.Geolocator.StatusChanged += statusChanged;
            }

            habitat = new Apex.Habitat("http://habitat.habhub.org/", "habitat", callsign + "_chase");
            isRunning = true;

        }

        void positionChanged(Geolocator sender, PositionChangedEventArgs e)
        {
            if (!App.RunningInBackground)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    tbLatitude.Text = e.Position.Coordinate.Latitude.ToString("0.00000");
                    tbLongitude.Text = e.Position.Coordinate.Longitude.ToString("0.00000");
                    tbAltitude.Text = e.Position.Coordinate.Altitude.ToString();
                    tbSpeed.Text = e.Position.Coordinate.Speed.ToString();
                    tbLastUpdated.Text = System.DateTime.UtcNow.ToLongTimeString();
                });
            }

            if (NetworkInterface.GetIsNetworkAvailable())
            {
                habitat.uploadListenerTelemetry(e.Position.Coordinate.Latitude.ToString("0.00000"), e.Position.Coordinate.Longitude.ToString("0.00000"), e.Position.Coordinate.Altitude.ToString(), true);
            }

        }

        void statusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case PositionStatus.Disabled:
                    MessageBox.Show("Location Service is disabled.");
                    stop();
                    break;
            }
        }

        private void stop()
        {
            App.Geolocator.PositionChanged -= positionChanged;
            App.Geolocator.StatusChanged -= statusChanged;
            App.Geolocator = null;

            isRunning = false;
        }


        private void ChangeCallsignButton_Click(object sender, EventArgs e)
        {
            changeCallsign();
        }

        private void ChangeIntervalButton_Click(object sender, EventArgs e)
        {
            changeInterval();
        }


        private void StartStopButton_Click(object sender, EventArgs e)
        {

            ApplicationBarIconButton btn = (ApplicationBarIconButton)ApplicationBar.Buttons[0];

            if (isRunning)
            {
                stop();
                btn.Text = "Start";
                btn.IconUri = new Uri("/images/play.png", UriKind.Relative);

            }
            else
            {
                // Start
                
                if (!savedSettings.Contains("callsign"))
                {
                    changeCallsign();
                }

                start();
                btn.Text = "Stop";
                btn.IconUri = new Uri("/images/stop.png", UriKind.Relative);
            }

        }

        private void changeCallsign()
        {
            InputPrompt input = new InputPrompt();
            input.Completed += new EventHandler<PopUpEventArgs<string, PopUpResult>>(changeCallsign_Completed);
            input.Title = "Change Callsign";
            input.InputScope = new InputScope { Names = { new InputScopeName() { NameValue = InputScopeNameValue.LogOnName } } };
            input.Value = callsign;
            input.Show();
        }

        private void changeCallsign_Completed(object sender, PopUpEventArgs<string, PopUpResult> e)
        {
            callsign = e.Result.ToString();
            tbCallsign.Text = callsign + "_chase";
            updateSetting("callsign", callsign);
        }

        private void changeInterval()
        {
            InputPrompt input = new InputPrompt();
            input.Completed += new EventHandler<PopUpEventArgs<string, PopUpResult>>(changeInterval_Completed);
            input.Title = "Change Update Interval";
            input.Message = "(seconds)";
            input.InputScope = new InputScope { Names = { new InputScopeName() { NameValue = InputScopeNameValue.Digits } } };
            input.Value = (interval/1000).ToString();
            input.Show();
        }

        private void changeInterval_Completed(object sender, PopUpEventArgs<string, PopUpResult> e)
        {
            interval = Convert.ToUInt32(e.Result) * 1000;
            if (isRunning)
            {
                stop();
                start();
            }
            updateSetting("interval", interval);
        }

   

        private void updateSetting(string settingName, object settingContent)
        {
            if (savedSettings.Contains(settingName))
            {
                savedSettings[settingName] = settingContent;
            }
            else
            {
                savedSettings.Add(settingName, settingContent);
            }
        }


    }
}