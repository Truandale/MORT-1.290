using System;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace NAudioTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== NAudio Device Enumeration Test ===");
            Console.WriteLine();
            
            try
            {
                // Test WaveIn devices
                Console.WriteLine("Testing WaveIn devices...");
                int waveInDevices = WaveIn.DeviceCount;
                Console.WriteLine($"WaveIn devices found: {waveInDevices}");
                
                for (int i = 0; i < waveInDevices; i++)
                {
                    try
                    {
                        var deviceInfo = WaveIn.GetCapabilities(i);
                        Console.WriteLine($"WaveIn [{i}]: {deviceInfo.ProductName} - Channels: {deviceInfo.Channels}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error getting WaveIn device {i}: {ex.Message}");
                    }
                }
                
                Console.WriteLine();
                
                // Test WaveOut devices
                Console.WriteLine("Testing WaveOut devices...");
                int waveOutDevices = WaveOut.DeviceCount;
                Console.WriteLine($"WaveOut devices found: {waveOutDevices}");
                
                for (int i = 0; i < waveOutDevices; i++)
                {
                    try
                    {
                        var deviceInfo = WaveOut.GetCapabilities(i);
                        Console.WriteLine($"WaveOut [{i}]: {deviceInfo.ProductName} - Channels: {deviceInfo.Channels}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error getting WaveOut device {i}: {ex.Message}");
                    }
                }
                
                Console.WriteLine();
                
                // Test WASAPI devices
                Console.WriteLine("Testing WASAPI devices...");
                try
                {
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
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error enumerating WASAPI devices: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                
                Console.WriteLine();
                Console.WriteLine("NAudio device enumeration test completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error during NAudio device enumeration: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
