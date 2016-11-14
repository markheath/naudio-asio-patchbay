using System;
using System.Windows;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace NAudioAsioPatchBay
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AsioOut asioOut;
        private AsioInputPatcher inputPatcher;
        private bool running;
        
        public MainWindow()
        {
            InitializeComponent();
            foreach (var device in AsioOut.GetDriverNames())
            {
                comboAsioDevices.Items.Add(device);
            }
            if (comboAsioDevices.Items.Count > 0)
                comboAsioDevices.SelectedIndex = 0;
            Closing += (sender, args) => Stop();
            sliderVolume1.ValueChanged += (sender, args) => SetSliders();
            sliderVolume2.ValueChanged += (sender, args) => SetSliders();
            sliderPan1.ValueChanged += (sender, args) => SetSliders();
            sliderPan2.ValueChanged += (sender, args) => SetSliders();
        }

        private void SetSliders()
        {
            if (inputPatcher != null)
            {
                // use a square root pan law
                float pan = (float)sliderPan1.Value; // in range -1 to 1
                float normPan = (-pan + 1) / 2;
                float leftChannel = (float)Math.Sqrt(normPan);
                float rightChannel = (float)Math.Sqrt(1 - normPan);

                // clear out the default routing we don't want
                inputPatcher.RoutingMatrix[1, 1] = 0;

                // I'm panning the first input to the stereo pair output channels 0 and 1
                inputPatcher.RoutingMatrix[0, 0] = (float) (leftChannel * sliderVolume1.Value / sliderVolume1.Maximum);
                inputPatcher.RoutingMatrix[0, 1] = (float) (rightChannel * sliderVolume1.Value / sliderVolume1.Maximum);

                // I'm panning the second input to the stero

                pan = (float)sliderPan2.Value; // in range -1 to 1
                normPan = (-pan + 1) / 2;
                leftChannel = (float)Math.Sqrt(normPan);
                rightChannel = (float)Math.Sqrt(1 - normPan);

                inputPatcher.RoutingMatrix[1, 2] = (float)(leftChannel * sliderVolume2.Value / sliderVolume2.Maximum);
                inputPatcher.RoutingMatrix[1, 3] = (float)(rightChannel * sliderVolume2.Value / sliderVolume2.Maximum);
            }
        }

        private void OnButtonBeginClick(object sender, RoutedEventArgs e)
        {
            if (!running)
            {
                running = true;
                asioOut = new AsioOut((string)comboAsioDevices.SelectedItem);
                int inputChannels = asioOut.DriverInputChannelCount; // that's all my soundcard has :(
                int outputChannels = Math.Min(asioOut.DriverOutputChannelCount, 2); // UI only letting us route to two at the moment
                inputPatcher = new AsioInputPatcher(44100, inputChannels, outputChannels);
                int ignored = 0; // yuck, really need to improve my API
                
                asioOut.InitRecordAndPlayback(new SampleToWaveProvider(inputPatcher), 
                    inputChannels, ignored);
                asioOut.AudioAvailable += OnAsioOutAudioAvailable;
                SetSliders();
                asioOut.Play();
                buttonBegin.Content = "Stop";
            }
            else
            {
                Stop();
            }
        }

        private void Stop()
        {
            if (running)
            {
                asioOut.Stop();
                asioOut.Dispose();
                asioOut = null;
                running = false;
                buttonBegin.Content = "Begin";
            }
        }

        void OnAsioOutAudioAvailable(object sender, AsioAudioAvailableEventArgs e)
        {
            inputPatcher.ProcessBuffer(e.InputBuffers, e.OutputBuffers, 
                e.SamplesPerBuffer, e.AsioSampleType);
            e.WrittenToOutputBuffers = true;
        }
    }
}
