using System;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace MORT
{
    public partial class TestNAudio : Form
    {
        public TestNAudio()
        {
            InitializeComponent();
            TestAudioDevices();
        }

        private void TestAudioDevices()
        {
            try
            {
                Console.WriteLine("=== Testing NAudio Device Enumeration ===");
                
                // Test WaveIn devices
                int waveInDevices = WaveIn.DeviceCount;
                Console.WriteLine($"WaveIn devices found: {waveInDevices}");
                
                for (int i = 0; i < waveInDevices; i++)
                {
                    var capabilities = WaveIn.GetCapabilities(i);
                    Console.WriteLine($"WaveIn [{i}]: {capabilities.ProductName} - Channels: {capabilities.Channels}");
                }
                
                // Test WaveOut devices
                int waveOutDevices = WaveOut.DeviceCount;
                Console.WriteLine($"WaveOut devices found: {waveOutDevices}");
                
                for (int i = 0; i < waveOutDevices; i++)
                {
                    var capabilities = WaveOut.GetCapabilities(i);
                    Console.WriteLine($"WaveOut [{i}]: {capabilities.ProductName} - Channels: {capabilities.Channels}");
                }
                
                // Test WASAPI devices
                using (var deviceEnumerator = new MMDeviceEnumerator())
                {
                    var playbackDevices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                    Console.WriteLine($"WASAPI Playback devices found: {playbackDevices.Count}");
                    
                    foreach (var device in playbackDevices)
                    {
                        Console.WriteLine($"WASAPI Playback: {device.FriendlyName} - {device.DeviceFriendlyName}");
                    }
                    
                    var captureDevices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                    Console.WriteLine($"WASAPI Capture devices found: {captureDevices.Count}");
                    
                    foreach (var device in captureDevices)
                    {
                        Console.WriteLine($"WASAPI Capture: {device.FriendlyName} - {device.DeviceFriendlyName}");
                    }
                }
                
                Console.WriteLine("=== NAudio Test Complete ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error testing NAudio: {ex.Message}");
                MessageBox.Show($"Error testing NAudio: {ex.Message}", "NAudio Test Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // TestNAudio
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(400, 300);
            this.Name = "TestNAudio";
            this.Text = "NAudio Test";
            this.ResumeLayout(false);
        }
    }
}
