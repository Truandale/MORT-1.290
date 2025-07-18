using System;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace MORT
{
    public static class TestAudioDevices
    {
        public static void TestDeviceEnumeration()
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
                
                // Show detailed device list in MessageBox for user
                var tester = new AudioDeviceTester();
                string allDevices = tester.GetAllAudioDevices();
                MessageBox.Show(allDevices, "Все доступные аудиоустройства", 
                              MessageBoxButtons.OK, MessageBoxIcon.Information);
                tester.Dispose();
                
                // Test WASAPI devices
                using (var deviceEnumerator = new MMDeviceEnumerator())
                {
                    var playbackDevices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                    Console.WriteLine($"WASAPI Playback devices found: {playbackDevices.Count}");
                    
                    foreach (var device in playbackDevices)
                    {
                        Console.WriteLine($"WASAPI Playback: {device.FriendlyName} - {device.DeviceFriendlyName}");
                    }
                    
                    var recordingDevices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                    Console.WriteLine($"WASAPI Recording devices found: {recordingDevices.Count}");
                    
                    foreach (var device in recordingDevices)
                    {
                        Console.WriteLine($"WASAPI Recording: {device.FriendlyName} - {device.DeviceFriendlyName}");
                    }
                }
                
                MessageBox.Show("NAudio device enumeration test completed successfully! Check console for output.", "NAudio Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during NAudio device enumeration: {ex.Message}", "NAudio Test Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Console.WriteLine($"Error: {ex}");
            }
        }

        public static string GetDeviceEnumerationResults()
        {
            try
            {
                string results = "=== NAudio Device Enumeration Results ===\n\n";
                
                // Test WaveIn devices
                int waveInDevices = WaveIn.DeviceCount;
                results += $"WaveIn devices found: {waveInDevices}\n";
                
                for (int i = 0; i < waveInDevices && i < 10; i++)
                {
                    var capabilities = WaveIn.GetCapabilities(i);
                    results += $"WaveIn [{i}]: {capabilities.ProductName} - Channels: {capabilities.Channels}\n";
                }
                if (waveInDevices > 10) results += "... and more\n";
                
                results += "\n";
                
                // Test WaveOut devices
                int waveOutDevices = WaveOut.DeviceCount;
                results += $"WaveOut devices found: {waveOutDevices}\n";
                
                for (int i = 0; i < waveOutDevices && i < 10; i++)
                {
                    var capabilities = WaveOut.GetCapabilities(i);
                    results += $"WaveOut [{i}]: {capabilities.ProductName} - Channels: {capabilities.Channels}\n";
                }
                if (waveOutDevices > 10) results += "... and more\n";
                
                results += "\n";
                
                // Test WASAPI devices
                using (var deviceEnumerator = new MMDeviceEnumerator())
                {
                    var playbackDevices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                    results += $"WASAPI Playback devices found: {playbackDevices.Count}\n";
                    
                    int count = 0;
                    foreach (var device in playbackDevices)
                    {
                        if (count >= 10) break;
                        results += $"WASAPI Playback: {device.FriendlyName}\n";
                        count++;
                    }
                    if (playbackDevices.Count > 10) results += "... and more\n";
                    
                    results += "\n";
                    
                    var recordingDevices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                    results += $"WASAPI Recording devices found: {recordingDevices.Count}\n";
                    
                    count = 0;
                    foreach (var device in recordingDevices)
                    {
                        if (count >= 10) break;
                        results += $"WASAPI Recording: {device.FriendlyName}\n";
                        count++;
                    }
                    if (recordingDevices.Count > 10) results += "... and more\n";
                }
                
                results += "\nNAudio device enumeration completed successfully!";
                return results;
            }
            catch (Exception ex)
            {
                return $"Error testing NAudio device enumeration: {ex.Message}";
            }
        }
    }
}
