using Android.App;
using Android.Widget;
using Android.OS;
using Android.Things.Pio;
using System;
using System.IO;
using Android.Util;
using Android.Content;
using Google.Android.Things.Contrib.Driver.Rainbowhat;
using Google.Android.Things.Contrib.Driver.Ht16k33;

namespace Starter
{
    [Activity(Label = "Starter", MainLauncher = true)]
    [IntentFilter(new[] { Intent.ActionMain }, Categories = new[] { Intent.CategoryLauncher })]
    [IntentFilter(new[] { Intent.ActionMain }, Categories = new[] { "android.intent.category.IOT_LAUNCHER", Intent.CategoryDefault })]
    public class MainActivity : Activity, SeekBar.IOnSeekBarChangeListener, IGpioCallback
    {
        static string TAG = "StarterActivity";
        PeripheralManager _manager;

        int count = 1;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            _manager = PeripheralManager.Instance;

            // Get our button from the layout resource,
            // and attach an event to it
            Button button = FindViewById<Button>(Resource.Id.myButton);

            button.Click += delegate { button.Text = $"{count++} clicks!"; };

            SetupDemoShowPins();
            SetupDemo1();
            SetupDemo2();
            SetupDemo3();
        }

        private void SetupDemoShowPins()
        {
            Log.Debug(TAG, String.Join(", ", _manager.GpioList));
        }

        ToggleButton _ledToggleView;
        IGpio _redLED;

        private void SetupDemo1()
        {
            try
            {
                var LED_PIN_NAME = "GPIO2_IO02";

                // Red LED
                //Raspberry Pi 3 - BCM6
                //i.MX7D - GPIO2_IO02

                _redLED = _manager.OpenGpio(LED_PIN_NAME);
                //redLED = RainbowHat.OpenLedRed();
                // Configure as an output.
                _redLED.SetDirection(Gpio.DirectionOutInitiallyLow);


            }
            catch (IOException ex)
            {
                Log.Error(TAG, "Error during onCreate!", ex);
            }

            _ledToggleView = FindViewById<ToggleButton>(Resource.Id.ledToggle);

            try
            {
                _ledToggleView.Checked = _redLED.Value;
            }
            catch (IOException ex)
            {
                Log.Error(TAG, "Error during setChecked!", ex);
            }


            _ledToggleView.CheckedChange += (sender, e) => {
                try
                {
                    _redLED.Value = e.IsChecked;
                }
                catch (IOException ex)
                {
                    Log.Error(TAG, "Error during onCheckedChanged!", ex);
                }
            }; 

        }

        IGpio _buttonA;
        private void SetupDemo2()
        {
            try
            {
                var pinName = "GPIO6_IO14"; //A button for i.MX7D, BCM21 for Rpi3
                _buttonA = _manager.OpenGpio(pinName);
                // Configure as an input, trigger events on every change.
                _buttonA.SetDirection(Gpio.DirectionIn);
                // Value is true when the pin is HIGH 
                _buttonA.SetActiveType(Gpio.ActiveHigh);
                _buttonA.SetEdgeTriggerType(Gpio.EdgeFalling);
                _buttonA.RegisterGpioCallback(new Handler(), this);


                var buttonB = RainbowHat.OpenButtonB();
                var ledToggleViewB = FindViewById<ToggleButton>(Resource.Id.ledToggleB);
                buttonB.ButtonEvent += (sender, e) => {
                    ledToggleViewB.Checked = !ledToggleViewB.Checked;
                };

            } catch (IOException ex) {
                Log.Error(TAG, "Error during onCreate!", ex);
            }
        }

        public bool OnGpioEdge(IGpio gpio)
        {
            _ledToggleView.Checked = !_ledToggleView.Checked;
            return true;
        }

        AlphanumericDisplay _display;
        SeekBar _ledBrightnessView;

        private void SetupDemo3()
        {
            _ledBrightnessView = FindViewById<SeekBar>(Resource.Id.ledBrightness);

            try
            {

                _display = RainbowHat.OpenDisplay();
                _display.SetEnabled(true);
                _display.Display("HEY");

            }
            catch (IOException ex)
            {
                Log.Error(TAG, "Error during onCreate!", ex);
            }

            _ledBrightnessView.SetOnSeekBarChangeListener(this);
        }

        public void OnProgressChanged(SeekBar seekBar, int progress, bool fromUser)
        {
            try
            { 
                _display.Display(progress);
            }
            catch (IOException ex)
            {
                Log.Error(TAG, "Error display!", ex);
            }
        }

        public void OnStartTrackingTouch(SeekBar seekBar)
        {
        }

        public void OnStopTrackingTouch(SeekBar seekBar)
        {
        }

        protected override void OnDestroy()
        {
            try
            {
                _redLED.Close();
                _buttonA.UnregisterGpioCallback(this);
                _buttonA.Close();

                _display.Close();

                /*pin22.unregisterGpioCallback(pin22Callback);
                pin22.close();

                uart0.close();*/
            }
            catch (IOException ex)
            {
                Log.Error(TAG, "Error during onDestroy!", ex);
            }
            base.OnDestroy();
        }
    }
}

