using System;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace NAudioTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Тест улучшенного поиска VB-Cable ===");
            Console.WriteLine();
            
            TestVBCableDetection();
            
            Console.WriteLine();
            Console.WriteLine("Нажмите любую клавишу для выхода...");
            Console.ReadKey();
        }
        
        static void TestVBCableDetection()
        {
            Console.WriteLine("1. Поиск VB-Cable устройств воспроизведения:");
            var foundVBCable = false;
            
            for (int deviceId = 0; deviceId < WaveOut.DeviceCount; deviceId++)
            {
                try
                {
                    var capabilities = WaveOut.GetCapabilities(deviceId);
                    var deviceName = capabilities.ProductName?.ToLower() ?? "";
                    
                    Console.WriteLine($"   Устройство {deviceId}: {capabilities.ProductName}");
                    
                    // Улучшенный поиск VB-Cable
                    if (deviceName.Contains("vb-audio") || 
                        deviceName.Contains("cable") || 
                        deviceName.Contains("vaio") ||
                        deviceName.Contains("voicemeeter"))
                    {
                        Console.WriteLine($"   >>> НАЙДЕН VB-Cable: {capabilities.ProductName}");
                        foundVBCable = true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   Ошибка при получении устройства {deviceId}: {ex.Message}");
                }
            }
            
            Console.WriteLine();
            Console.WriteLine("2. Поиск VB-Cable устройств записи:");
            
            for (int deviceId = 0; deviceId < WaveIn.DeviceCount; deviceId++)
            {
                try
                {
                    var capabilities = WaveIn.GetCapabilities(deviceId);
                    var deviceName = capabilities.ProductName?.ToLower() ?? "";
                    
                    Console.WriteLine($"   Устройство {deviceId}: {capabilities.ProductName}");
                    
                    // Улучшенный поиск VB-Cable
                    if (deviceName.Contains("vb-audio") || 
                        deviceName.Contains("cable") || 
                        deviceName.Contains("vaio") ||
                        deviceName.Contains("voicemeeter"))
                    {
                        Console.WriteLine($"   >>> НАЙДЕН VB-Cable: {capabilities.ProductName}");
                        foundVBCable = true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   Ошибка при получении устройства {deviceId}: {ex.Message}");
                }
            }
            
            Console.WriteLine();
            if (foundVBCable)
            {
                Console.WriteLine("✅ VB-Cable устройства НАЙДЕНЫ с улучшенным поиском!");
            }
            else
            {
                Console.WriteLine("❌ VB-Cable устройства НЕ найдены.");
            }
        }
    }
}
