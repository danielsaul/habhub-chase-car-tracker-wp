using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using Coding4Fun.Phone.Controls;
using System.Threading;
using System.Net.NetworkInformation;

namespace habhub_chase_car_tracker
{
    public partial class MainPage : PhoneApplicationPage
    {

        private string callsign = "default";
        IsolatedStorageSettings savedSettings;

        bool isRunning = false;

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

        }


        private void start()
        {
            Thread loopThread = new Thread(new ThreadStart(this.loop));
            loopThread.IsBackground = true;

            isRunning = true;
            loopThread.Start();

        }

        private void stop()
        {
            isRunning = false;
        }

        private void loop()
        {
            var habitat = new Apex.Habitat("http://habitat.habhub.org/", "habitat", callsign + "_chase");

            var location = new Apex.Location();
            location.start();

            while (isRunning)
            {
                bool hasNetworkConnection = NetworkInterface.GetIsNetworkAvailable();

                //Update UI
                Dispatcher.BeginInvoke(() => { 
                    tbLatitude.Text = location.latitude;
                    tbLongitude.Text = location.longitude;
                    tbAltitude.Text = location.altitude;
                    tbSpeed.Text = location.speed;
                    tbLastUpdated.Text = System.DateTime.UtcNow.ToLongTimeString();
                });

                if (hasNetworkConnection)
                {
                    habitat.uploadListenerTelemetry(location.latitude, location.longitude, location.altitude, true);
                }

            }

            location.stop();

        }

        private void ChangeCallsignButton_Click(object sender, EventArgs e)
        {
            changeCallsign();
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
                    return;
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
            if (savedSettings.Contains("callsign"))
            {
                savedSettings["callsign"] = callsign;
            }
            else
            {
                savedSettings.Add("callsign", callsign);
            }
        }



    }
}