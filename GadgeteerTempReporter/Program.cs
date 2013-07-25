using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Touch;
using System.Net;
using System.Net.Sockets;
using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Net.NetworkInformation;
using System.IO;
using System.Text;

namespace GadgeteerApp1
{
    public partial class Program
    {


        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            // Use Debug.Print to show messages in Visual Studio's "Output" window during debugging.
            Debug.Print("Program Started");
            //Set up network
            try
            {
               

                NetworkInterface ni = NetworkInterface.GetAllNetworkInterfaces()[0];
                ni.EnableStaticDns(new string[]{"10.10.10.1"});
                ni.EnableStaticIP("10.10.10.215","255.255.255.0","10.10.10.1");
                Debug.Print("Physical addr: " + ni.PhysicalAddress);
                if (ni.IPAddress != "0.0.0.0")
                    Debug.Print("IP Address:" + ni.IPAddress);
                Program.Mainboard.SetDebugLED(true);
            }
            catch (Exception exception)
            {
                throw new ArgumentException("Could not resolve host via DNS.", exception);
            }
            temperatureHumidity.MeasurementComplete += temperatureHumidity_MeasurementComplete;


            //Set up timer to update lights and send temp readings
            GT.Timer timer = new GT.Timer(60*1000);
            timer.Tick += timer_Tick;
            timer.Start();
            Debug.Print("Initating first read");
            temperatureHumidity.RequestMeasurement();
        }

        int lightCounter = 1;
        int maxLigths = 7;
        bool lightsOnOrOff = true;
        int tempReadingCounter = 0;
        int maxTempRead = 30;

        void temperatureHumidity_MeasurementComplete(GTM.Seeed.TemperatureHumidity sender, double temperature, double relativeHumidity)
        {

            SendRequest(temperature, relativeHumidity);
        }
        public static void SendRequest(double temperature, double relativeHumidity)
        {
            Debug.Print("Temp: " + temperature.ToString("f2") + " Humid: " + relativeHumidity.ToString("f2"));
            string url = "http://erthtemp.azurewebsites.net/Temp/temp/" + temperature.ToString("f2") + "/humi/" + relativeHumidity.ToString("f2");
            Debug.Print("Url: " + url);
  

            var request = (HttpWebRequest)WebRequest.Create(url);

            // request line
            request.Method = "GET";
            request.Timeout = 5000;     // 5 seconds
            // send request and receive response
            using (var response =
                (HttpWebResponse)request.GetResponse())
            {
                HandleResponse(response);
            }
        }


        public static void HandleResponse(HttpWebResponse response)
        {
            Debug.Print("Response: " + new StreamReader(response.GetResponseStream()).ReadToEnd().ToString());
            response.Close();
        }

        void UpdateLights()
        {
            Debug.Print("LightsOnOrOff: " + lightsOnOrOff.ToString());
            Debug.Print("Lightcounter: " + lightCounter.ToString());
            if(lightsOnOrOff)
            {
                led7r.TurnLightOn(lightCounter, false);
                lightCounter++;
            }
            else
            {
                led7r.TurnLightOff(lightCounter);
                lightCounter--;
            }
            Debug.Print("Lightcounter før if: " + lightCounter.ToString());
            if (lightCounter == maxLigths || lightCounter < 1)
            {
                lightsOnOrOff = !lightsOnOrOff;
            }
        }

        void timer_Tick(GT.Timer timer)
        {
            UpdateLights();
            tempReadingCounter++;
            Debug.Print("Tempreadcounter = " + tempReadingCounter.ToString());
            NetworkInterface ni = NetworkInterface.GetAllNetworkInterfaces()[0];
            Debug.Print("IP Address:" + ni.IPAddress);
            if (tempReadingCounter == maxTempRead)
            {
                temperatureHumidity.RequestMeasurement();
                tempReadingCounter = 0;
            }
        }
    }
}
