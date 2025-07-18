using System;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace MORT.Test
{
    public class AudioDeviceTest
    {
        public static void TestAudioDeviceEnumeration()
        {
            Console.WriteLine("=== Тест перечисления аудио устройств ===");
            
            try
            {
                // Тест WaveIn устройств (микрофоны)
                Console.WriteLine($"\nWaveIn устройства (микрофоны): {WaveIn.DeviceCount}");
                for (int i = 0; i < WaveIn.DeviceCount; i++)
                {
                    var deviceInfo = WaveIn.GetCapabilities(i);
                    Console.WriteLine($"  [{i}] {deviceInfo.ProductName} - {deviceInfo.Channels} каналов");
                }

                // Тест WaveOut устройств (динамики)
                Console.WriteLine($"\nWaveOut устройства (динамики): {WaveOut.DeviceCount}");
                for (int i = 0; i < WaveOut.DeviceCount; i++)
                {
                    var deviceInfo = WaveOut.GetCapabilities(i);
                    Console.WriteLine($"  [{i}] {deviceInfo.ProductName} - {deviceInfo.Channels} каналов");
                }

                // Тест WASAPI устройств (более современный API)
                Console.WriteLine("\nWASAPI устройства:");
                using (var enumerator = new MMDeviceEnumerator())
                {
                    // Входные устройства
                    var inputDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                    Console.WriteLine($"  Входные устройства: {inputDevices.Count}");
                    foreach (var device in inputDevices)
                    {
                        Console.WriteLine($"    - {device.FriendlyName} ({device.State})");
                    }

                    // Выходные устройства
                    var outputDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                    Console.WriteLine($"  Выходные устройства: {outputDevices.Count}");
                    foreach (var device in outputDevices)
                    {
                        Console.WriteLine($"    - {device.FriendlyName} ({device.State})");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при тестировании: {ex.Message}");
            }
        }

        public static void ShowAudioDeviceTestForm()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            var testForm = new Form()
            {
                Text = "Тест аудио устройств",
                Size = new System.Drawing.Size(600, 400),
                StartPosition = FormStartPosition.CenterScreen
            };

            var textBox = new TextBox()
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Dock = DockStyle.Fill
            };

            var button = new Button()
            {
                Text = "Обновить список устройств",
                Dock = DockStyle.Top,
                Height = 30
            };

            button.Click += (s, e) =>
            {
                textBox.Text = "";
                try
                {
                    var output = new System.Text.StringBuilder();
                    
                    // WaveIn устройства
                    output.AppendLine($"WaveIn устройства (микрофоны): {WaveIn.DeviceCount}");
                    for (int i = 0; i < WaveIn.DeviceCount; i++)
                    {
                        var deviceInfo = WaveIn.GetCapabilities(i);
                        output.AppendLine($"  [{i}] {deviceInfo.ProductName} - {deviceInfo.Channels} каналов");
                    }

                    // WaveOut устройства
                    output.AppendLine($"\nWaveOut устройства (динамики): {WaveOut.DeviceCount}");
                    for (int i = 0; i < WaveOut.DeviceCount; i++)
                    {
                        var deviceInfo = WaveOut.GetCapabilities(i);
                        output.AppendLine($"  [{i}] {deviceInfo.ProductName} - {deviceInfo.Channels} каналов");
                    }

                    // WASAPI устройства
                    output.AppendLine("\nWASAPI устройства:");
                    using (var enumerator = new MMDeviceEnumerator())
                    {
                        var inputDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                        output.AppendLine($"  Входные устройства: {inputDevices.Count}");
                        foreach (var device in inputDevices)
                        {
                            output.AppendLine($"    - {device.FriendlyName} ({device.State})");
                        }

                        var outputDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                        output.AppendLine($"  Выходные устройства: {outputDevices.Count}");
                        foreach (var device in outputDevices)
                        {
                            output.AppendLine($"    - {device.FriendlyName} ({device.State})");
                        }
                    }

                    textBox.Text = output.ToString();
                }
                catch (Exception ex)
                {
                    textBox.Text = $"Ошибка: {ex.Message}";
                }
            };

            testForm.Controls.Add(textBox);
            testForm.Controls.Add(button);
            
            // Автоматически загружаем устройства при открытии
            button.PerformClick();
            
            Application.Run(testForm);
        }
    }
}
