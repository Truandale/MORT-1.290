using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using System.Threading;
using System.Text;

namespace MORT
{
    /// <summary>
    /// Класс для тестирования аудио устройств с помощью NAudio
    /// </summary>
    public class AudioDeviceTester : IDisposable
    {
        private WaveInEvent? waveIn;
        private WaveOutEvent? waveOut;
        private BufferedWaveProvider? bufferedWaveProvider;
        private SineWaveProvider? sineWaveProvider;
        private bool isRecording = false;
        private bool isPlaying = false;
        private CancellationTokenSource? cancellationTokenSource;

        /// <summary>
        /// Тестирование микрофона - запись и воспроизведение
        /// </summary>
        /// <param name="deviceNumber">Номер устройства записи</param>
        /// <param name="duration">Длительность теста в секундах</param>
        public async Task<bool> TestMicrophoneAsync(int deviceNumber, int duration = 3)
        {
            try
            {
                cancellationTokenSource = new CancellationTokenSource();
                
                // Показываем прогресс
                using (var progressForm = new AudioTestProgressForm($"Тестирование микрофона #{deviceNumber}", duration))
                {
                    progressForm.Show();
                    progressForm.SetStatus("Инициализация...");
                    
                    // Настройка записи
                    waveIn = new WaveInEvent()
                    {
                        DeviceNumber = deviceNumber,
                        WaveFormat = new WaveFormat(44100, 16, 1) // 44.1kHz, 16-bit, mono
                    };
                    
                    bufferedWaveProvider = new BufferedWaveProvider(waveIn.WaveFormat)
                    {
                        BufferLength = waveIn.WaveFormat.AverageBytesPerSecond * 2,
                        DiscardOnBufferOverflow = true
                    };
                    
                    waveIn.DataAvailable += (s, e) =>
                    {
                        bufferedWaveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);
                    };
                    
                    // Настройка воспроизведения
                    waveOut = new WaveOutEvent();
                    waveOut.Init(bufferedWaveProvider);
                    
                    progressForm.SetStatus("Запись звука... Говорите в микрофон!");
                    
                    // Начинаем запись
                    waveIn.StartRecording();
                    isRecording = true;
                    
                    // Ждем запись
                    await Task.Delay(duration * 1000, cancellationTokenSource.Token);
                    
                    progressForm.SetStatus("Воспроизведение записанного звука...");
                    
                    // Останавливаем запись и воспроизводим
                    waveIn.StopRecording();
                    isRecording = false;
                    
                    if (bufferedWaveProvider.BufferedBytes > 0)
                    {
                        waveOut.Play();
                        isPlaying = true;
                        
                        // Ждем воспроизведение
                        await Task.Delay(duration * 1000, cancellationTokenSource.Token);
                        
                        waveOut.Stop();
                        isPlaying = false;
                        
                        progressForm.SetStatus("Тест завершен успешно!");
                        await Task.Delay(1000);
                        
                        return true;
                    }
                    else
                    {
                        progressForm.SetStatus("Ошибка: не удалось записать звук");
                        await Task.Delay(2000);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при тестировании микрофона: {ex.Message}", 
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
                StopAll();
            }
        }

        /// <summary>
        /// Тестирование динамиков - воспроизведение тестового тона
        /// </summary>
        /// <param name="deviceNumber">Номер устройства воспроизведения</param>
        /// <param name="frequency">Частота тестового тона</param>
        /// <param name="duration">Длительность теста в секундах</param>
        public async Task<bool> TestSpeakersAsync(int deviceNumber, float frequency = 440.0f, int duration = 3)
        {
            try
            {
                cancellationTokenSource = new CancellationTokenSource();
                
                using (var progressForm = new AudioTestProgressForm($"Тестирование динамиков #{deviceNumber}", duration))
                {
                    progressForm.Show();
                    progressForm.SetStatus("Генерация тестового сигнала...");
                    
                    // Создаем генератор синусоиды
                    sineWaveProvider = new SineWaveProvider(frequency);
                    
                    // Настройка воспроизведения
                    waveOut = new WaveOutEvent()
                    {
                        DeviceNumber = deviceNumber
                    };
                    
                    waveOut.Init(sineWaveProvider);
                    
                    progressForm.SetStatus($"Воспроизведение тона {frequency}Hz... Вы должны слышать звук!");
                    
                    // Воспроизводим тон
                    waveOut.Play();
                    isPlaying = true;
                    
                    // Ждем окончания воспроизведения
                    await Task.Delay(duration * 1000, cancellationTokenSource.Token);
                    
                    waveOut.Stop();
                    isPlaying = false;
                    
                    progressForm.SetStatus("Тест завершен успешно!");
                    await Task.Delay(1000);
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при тестировании динамиков: {ex.Message}", 
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
                StopAll();
            }
        }

        /// <summary>
        /// Тестирование микрофона с воспроизведением через отдельное устройство
        /// </summary>
        /// <param name="inputDeviceNumber">Номер устройства записи (микрофон)</param>
        /// <param name="outputDeviceNumber">Номер устройства воспроизведения (динамики), -1 для устройства по умолчанию</param>
        /// <param name="duration">Длительность теста в секундах</param>
        public async Task<bool> TestMicrophoneWithPlaybackAsync(int inputDeviceNumber, int outputDeviceNumber, int duration = 3)
        {
            try
            {
                cancellationTokenSource = new CancellationTokenSource();
                
                // Показываем прогресс
                string inputDeviceName = inputDeviceNumber == -1 ? "По умолчанию" : WaveIn.GetCapabilities(inputDeviceNumber).ProductName;
                string outputDeviceName = outputDeviceNumber == -1 ? "По умолчанию" : WaveOut.GetCapabilities(outputDeviceNumber).ProductName;
                
                using (var progressForm = new AudioTestProgressForm($"Тест: {inputDeviceName} → {outputDeviceName}", duration))
                {
                    progressForm.Show();
                    progressForm.SetStatus("Инициализация...");
                    
                    // Список для сохранения записанных данных
                    var recordedData = new List<byte>();
                    var recordingFormat = new WaveFormat(44100, 16, 1); // 44.1kHz, 16-bit, mono
                    
                    // Настройка записи
                    waveIn = new WaveInEvent()
                    {
                        DeviceNumber = inputDeviceNumber,
                        WaveFormat = recordingFormat
                    };
                    
                    // Обработчик для сохранения записанных данных
                    waveIn.DataAvailable += (s, e) =>
                    {
                        // Сохраняем данные в список для последующего воспроизведения
                        for (int i = 0; i < e.BytesRecorded; i++)
                        {
                            recordedData.Add(e.Buffer[i]);
                        }
                    };
                    
                    progressForm.SetStatus("🎤 Запись звука... Говорите в микрофон!");
                    
                    // Начинаем запись
                    waveIn.StartRecording();
                    isRecording = true;
                    
                    // Ждем запись с обновлением прогресса
                    for (int i = 0; i < duration; i++)
                    {
                        await Task.Delay(1000, cancellationTokenSource.Token);
                        progressForm.SetStatus($"🎤 Запись... ({duration - i - 1} сек осталось)");
                    }
                    
                    // Останавливаем запись
                    waveIn.StopRecording();
                    isRecording = false;
                    
                    progressForm.SetStatus($"🔊 Воспроизведение через {outputDeviceName}...");
                    
                    // Проверяем, есть ли записанные данные
                    if (recordedData.Count > 0)
                    {
                        // Создаем буфер для воспроизведения из записанных данных
                        bufferedWaveProvider = new BufferedWaveProvider(recordingFormat)
                        {
                            BufferLength = recordedData.Count * 2, // Увеличиваем буфер
                            DiscardOnBufferOverflow = false // Не отбрасываем данные
                        };
                        
                        // Добавляем все записанные данные в буфер воспроизведения
                        bufferedWaveProvider.AddSamples(recordedData.ToArray(), 0, recordedData.Count);
                        
                        // Настройка воспроизведения
                        waveOut = new WaveOutEvent()
                        {
                            DeviceNumber = outputDeviceNumber
                        };
                        waveOut.Init(bufferedWaveProvider);
                        
                        // Начинаем воспроизведение
                        waveOut.Play();
                        isPlaying = true;
                        
                        progressForm.SetStatus($"🔊 Воспроизведение записи... Слушайте!");
                        
                        // Ждем воспроизведение с обновлением прогресса
                        for (int i = 0; i < duration; i++)
                        {
                            await Task.Delay(1000, cancellationTokenSource.Token);
                            progressForm.SetStatus($"🔊 Воспроизведение... ({duration - i - 1} сек осталось)");
                        }
                        
                        waveOut.Stop();
                        isPlaying = false;
                        
                        progressForm.SetStatus($"✅ Тест завершен! Записано {recordedData.Count} байт данных");
                        await Task.Delay(1500);
                        
                        return true;
                    }
                    else
                    {
                        progressForm.SetStatus("❌ Ошибка: не удалось записать звук с микрофона");
                        await Task.Delay(2500);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при тестировании микрофона: {ex.Message}", 
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
                StopAll();
            }
        }

        /// <summary>
        /// Тестирование микрофона с мониторингом в реальном времени
        /// Позволяет слышать свой голос в динамиках во время записи
        /// </summary>
        /// <param name="inputDeviceNumber">Номер устройства записи (микрофон)</param>
        /// <param name="outputDeviceNumber">Номер устройства воспроизведения (динамики), -1 для устройства по умолчанию</param>
        /// <param name="duration">Длительность теста в секундах</param>
        public async Task<bool> TestMicrophoneWithRealTimeMonitoringAsync(int inputDeviceNumber, int outputDeviceNumber, int duration = 3)
        {
            try
            {
                cancellationTokenSource = new CancellationTokenSource();
                
                // Показываем прогресс
                string inputDeviceName = inputDeviceNumber == -1 ? "По умолчанию" : WaveIn.GetCapabilities(inputDeviceNumber).ProductName;
                string outputDeviceName = outputDeviceNumber == -1 ? "По умолчанию" : WaveOut.GetCapabilities(outputDeviceNumber).ProductName;
                
                // Детальная диагностика устройств
                System.Diagnostics.Debug.WriteLine($"=== Начало тестирования микрофона с мониторингом ===");
                System.Diagnostics.Debug.WriteLine($"Входное устройство: {inputDeviceNumber} ({inputDeviceName})");
                System.Diagnostics.Debug.WriteLine($"Выходное устройство: {outputDeviceNumber} ({outputDeviceName})");
                System.Diagnostics.Debug.WriteLine($"Доступно WaveIn устройств: {WaveIn.DeviceCount}");
                System.Diagnostics.Debug.WriteLine($"Доступно WaveOut устройств: {WaveOut.DeviceCount}");
                
                using (var progressForm = new AudioTestProgressForm($"Мониторинг: {inputDeviceName} → {outputDeviceName}", duration))
                {
                    progressForm.Show();
                    progressForm.SetStatus("Инициализация...");
                    
                    // Проверка валидности устройств
                    if (inputDeviceNumber >= WaveIn.DeviceCount)
                    {
                        throw new ArgumentException($"Неверный индекс входного устройства: {inputDeviceNumber} (макс: {WaveIn.DeviceCount - 1})");
                    }
                    if (outputDeviceNumber >= WaveOut.DeviceCount)
                    {
                        throw new ArgumentException($"Неверный индекс выходного устройства: {outputDeviceNumber} (макс: {WaveOut.DeviceCount - 1})");
                    }
                    
                    // Настройка записи
                    waveIn = new WaveInEvent()
                    {
                        DeviceNumber = inputDeviceNumber,
                        WaveFormat = new WaveFormat(44100, 16, 1), // 44.1kHz, 16-bit, mono
                        BufferMilliseconds = 20  // Минимальная задержка для real-time
                    };
                    
                    System.Diagnostics.Debug.WriteLine($"Настроена запись: {waveIn.WaveFormat}");
                    
                    bufferedWaveProvider = new BufferedWaveProvider(waveIn.WaveFormat)
                    {
                        BufferLength = waveIn.WaveFormat.AverageBytesPerSecond / 10, // 100ms буфер для низкой задержки
                        DiscardOnBufferOverflow = true
                    };
                    
                    // Создаем усилитель громкости для лучшего мониторинга
                    var volumeProvider = new VolumeWaveProvider16(bufferedWaveProvider)
                    {
                        Volume = 0.5f // Снижаем громкость до 50% для предотвращения переполнения
                    };
                    
                    // Настройка воспроизведения в реальном времени
                    waveOut = new WaveOutEvent()
                    {
                        DeviceNumber = outputDeviceNumber,
                        DesiredLatency = 50  // Низкая задержка для мониторинга
                    };
                    
                    System.Diagnostics.Debug.WriteLine($"Настроено воспроизведение: устройство {outputDeviceNumber}, задержка {waveOut.DesiredLatency}ms");
                    
                    waveOut.Init(volumeProvider); // Используем усилитель громкости
                    
                    // Счетчики для диагностики
                    int samplesReceived = 0;
                    int bytesReceived = 0;
                    int maxAmplitude = 0; // Для проверки уровня сигнала
                    
                    // Обработчик для передачи звука в реальном времени
                    waveIn.DataAvailable += (s, e) =>
                    {
                        samplesReceived++;
                        bytesReceived += e.BytesRecorded;
                        
                        // Проверяем амплитуду сигнала для диагностики
                        for (int i = 0; i < e.BytesRecorded - 1; i += 2)
                        {
                            short sample = (short)(e.Buffer[i] | (e.Buffer[i + 1] << 8));
                            // Полностью безопасное вычисление абсолютного значения с ограничением
                            int amplitude = sample >= 0 ? sample : -(int)sample;
                            // Ограничиваем максимальное значение для предотвращения проблем
                            amplitude = Math.Min(amplitude, 32767);
                            if (amplitude > maxAmplitude)
                                maxAmplitude = amplitude;
                        }
                        
                        // Передаем звук напрямую в буфер для мгновенного воспроизведения
                        bufferedWaveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);
                        
                        // Периодическая диагностика
                        if (samplesReceived % 50 == 0) // Каждые ~1 секунду
                        {
                            System.Diagnostics.Debug.WriteLine($"Получено {samplesReceived} блоков данных, {bytesReceived} байт, макс. амплитуда: {maxAmplitude}");
                        }
                    };
                    
                    progressForm.SetStatus("🎤 Говорите в микрофон - вы должны слышать себя в динамиках!");
                    
                    // Начинаем запись И воспроизведение одновременно
                    System.Diagnostics.Debug.WriteLine("Запускаем запись...");
                    waveIn.StartRecording();
                    isRecording = true;
                    
                    System.Diagnostics.Debug.WriteLine("Запускаем воспроизведение...");
                    waveOut.Play();
                    isPlaying = true;
                    
                    System.Diagnostics.Debug.WriteLine("Мониторинг активен!");
                    
                    // Обновляем прогресс каждую секунду
                    for (int i = 0; i < duration; i++)
                    {
                        await Task.Delay(1000, cancellationTokenSource.Token);
                        progressForm.SetStatus($"🎤 Мониторинг активен ({duration - i - 1} сек осталось)... Говорите!");
                        System.Diagnostics.Debug.WriteLine($"Секунда {i + 1}: получено {samplesReceived} блоков, {bytesReceived} байт");
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Тест завершен. Итого: {samplesReceived} блоков, {bytesReceived} байт, макс. амплитуда: {maxAmplitude}");
                    
                    string resultMessage;
                    if (bytesReceived == 0)
                    {
                        resultMessage = "❌ Микрофон не получает данные! Проверьте подключение и настройки Windows.";
                    }
                    else if (maxAmplitude < 100)
                    {
                        resultMessage = $"⚠️ Микрофон работает, но сигнал очень слабый (макс: {maxAmplitude}). Проверьте громкость микрофона в Windows.";
                    }
                    else
                    {
                        resultMessage = $"✅ Тест завершен! Обработано {bytesReceived} байт звука (макс. амплитуда: {maxAmplitude})";
                    }
                    
                    progressForm.SetStatus(resultMessage);
                    await Task.Delay(2500);
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в TestMicrophoneWithRealTimeMonitoringAsync: {ex}");
                MessageBox.Show($"Ошибка при тестировании микрофона с мониторингом: {ex.Message}", 
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
                StopAll();
            }
        }

        /// <summary>
        /// Получить список всех доступных аудиоустройств
        /// </summary>
        public string GetAllAudioDevices()
        {
            var deviceList = new StringBuilder();
            
            deviceList.AppendLine("=== УСТРОЙСТВА ЗАПИСИ (INPUT) ===");
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                var caps = WaveIn.GetCapabilities(i);
                deviceList.AppendLine($"[{i}] {caps.ProductName} (Channels: {caps.Channels})");
            }
            
            deviceList.AppendLine("\n=== УСТРОЙСТВА ВОСПРОИЗВЕДЕНИЯ (OUTPUT) ===");
            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var caps = WaveOut.GetCapabilities(i);
                deviceList.AppendLine($"[{i}] {caps.ProductName} (Channels: {caps.Channels})");
            }
            
            return deviceList.ToString();
        }

        /// <summary>
        /// Поиск VB-Cable устройств с правильной логикой для loopback теста
        /// </summary>
        private (int inputDevice, int outputDevice, string details) FindVBCableDevices()
        {
            int vbCableInputDevice = -1;  // Для записи с CABLE Output
            int vbCableOutputDevice = -1; // Для воспроизведения в CABLE Input
            var details = new StringBuilder();
            
            details.AppendLine("Поиск VB-Cable устройств для loopback теста...");
            
            // Поиск устройства для ЗАПИСИ - должно быть "CABLE Output"
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                var caps = WaveIn.GetCapabilities(i);
                string productName = caps.ProductName.ToLower();
                details.AppendLine($"Проверка записи [{i}]: {caps.ProductName}");
                
                // Ищем именно CABLE Output для записи
                if (productName.Contains("cable output") ||
                    (productName.Contains("cable") && productName.Contains("output") && productName.Contains("vb-audio")))
                {
                    vbCableInputDevice = i;
                    details.AppendLine($"✓ Найден VB-Cable для записи: {caps.ProductName}");
                    break;
                }
            }
            
            // Поиск устройства для ВОСПРОИЗВЕДЕНИЯ - должно быть "CABLE Input"  
            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var caps = WaveOut.GetCapabilities(i);
                string productName = caps.ProductName.ToLower();
                details.AppendLine($"Проверка вывода [{i}]: {caps.ProductName}");
                
                // Ищем CABLE Input для воспроизведения
                if (productName.Contains("cable input") ||
                    (productName.Contains("cable") && productName.Contains("input") && productName.Contains("vb-audio")))
                {
                    vbCableOutputDevice = i;
                    details.AppendLine($"✓ Найден VB-Cable для воспроизведения: {caps.ProductName}");
                    break;
                }
            }
            
            // Если точные CABLE устройства не найдены, ищем альтернативы
            if (vbCableInputDevice == -1 || vbCableOutputDevice == -1)
            {
                details.AppendLine("\n⚠ Точные CABLE Input/Output устройства не найдены!");
                details.AppendLine("Ищем альтернативные VB-Cable устройства...");
                
                // Поиск любого устройства с "VB-Audio Virtual"
                if (vbCableInputDevice == -1)
                {
                    for (int i = 0; i < WaveIn.DeviceCount; i++)
                    {
                        var caps = WaveIn.GetCapabilities(i);
                        string productName = caps.ProductName.ToLower();
                        
                        if (productName.Contains("vb-audio") && productName.Contains("virtual"))
                        {
                            vbCableInputDevice = i;
                            details.AppendLine($"✓ Найден альтернативный VB-Cable для записи: {caps.ProductName}");
                            break;
                        }
                    }
                }
                
                if (vbCableOutputDevice == -1)
                {
                    for (int i = 0; i < WaveOut.DeviceCount; i++)
                    {
                        var caps = WaveOut.GetCapabilities(i);
                        string productName = caps.ProductName.ToLower();
                        
                        if (productName.Contains("vb-audio") && productName.Contains("virtual"))
                        {
                            vbCableOutputDevice = i;
                            details.AppendLine($"✓ Найден альтернативный VB-Cable для воспроизведения: {caps.ProductName}");
                            break;
                        }
                    }
                }
            }
            
            details.AppendLine($"\nРезультат поиска:");
            details.AppendLine($"- Устройство записи: {(vbCableInputDevice != -1 ? vbCableInputDevice.ToString() : "НЕ НАЙДЕНО")}");
            details.AppendLine($"- Устройство воспроизведения: {(vbCableOutputDevice != -1 ? vbCableOutputDevice.ToString() : "НЕ НАЙДЕНО")}");
            
            if (vbCableInputDevice != -1 && vbCableOutputDevice != -1)
            {
                details.AppendLine("\n✅ VB-Cable устройства найдены для loopback теста!");
            }
            else
            {
                details.AppendLine("\n❌ Не удалось найти подходящие VB-Cable устройства для loopback теста!");
            }
            
            return (vbCableInputDevice, vbCableOutputDevice, details.ToString());
        }

        /// <summary>
        /// Тестирование VB-Cable - проверка loopback соединения
        /// </summary>
        public async Task<bool> TestVBCableAsync(int duration = 5)
        {
            try
            {
                cancellationTokenSource = new CancellationTokenSource();
                
                using (var progressForm = new AudioTestProgressForm("Тестирование VB-Cable", duration))
                {
                    progressForm.Show();
                    progressForm.SetStatus("Поиск VB-Cable устройств...");
                    
                    // Показываем детальную информацию ПЕРЕД поиском
                    string allDevicesInfo = GetAllAudioDevices();
                    MessageBox.Show($"ДОСТУПНЫЕ АУДИОУСТРОЙСТВА:\n\n{allDevicesInfo}", 
                                  "Отладка: Все устройства", 
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    // Ищем VB-Cable устройства с правильной логикой
                    var (vbCableInputDevice, vbCableOutputDevice, searchDetails) = FindVBCableDevices();
                    
                    // Показываем детали поиска
                    MessageBox.Show($"РЕЗУЛЬТАТ ПОИСКА VB-CABLE:\n\n{searchDetails}\n" +
                                  $"Найденные устройства:\n" +
                                  $"- Устройство записи (Input): {vbCableInputDevice}\n" +
                                  $"- Устройство воспроизведения (Output): {vbCableOutputDevice}", 
                                  "Отладка: Поиск VB-Cable", 
                                  MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    if (vbCableInputDevice == -1 || vbCableOutputDevice == -1)
                    {
                        progressForm.SetStatus("VB-Cable не найден! Проверьте установку.");
                        await Task.Delay(3000);
                        return false;
                    }
                    
                    // Показываем что будем тестировать
                    string inputDeviceName = "НЕИЗВЕСТНО";
                    string outputDeviceName = "НЕИЗВЕСТНО";
                    
                    try
                    {
                        if (vbCableInputDevice >= 0 && vbCableInputDevice < WaveIn.DeviceCount)
                        {
                            var inputCaps = WaveIn.GetCapabilities(vbCableInputDevice);
                            inputDeviceName = inputCaps.ProductName;
                        }
                        
                        if (vbCableOutputDevice >= 0 && vbCableOutputDevice < WaveOut.DeviceCount)
                        {
                            var outputCaps = WaveOut.GetCapabilities(vbCableOutputDevice);
                            outputDeviceName = outputCaps.ProductName;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка получения имен устройств: {ex.Message}", "Ошибка", 
                                      MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    
                    var testConfirm = MessageBox.Show($"LOOPBACK ТЕСТ:\n\n" +
                                                    $"Воспроизведение в: [{vbCableOutputDevice}] {outputDeviceName}\n" +
                                                    $"Запись с: [{vbCableInputDevice}] {inputDeviceName}\n\n" +
                                                    $"Продолжить тест?", 
                                                    "Подтверждение теста", 
                                                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    
                    if (testConfirm != DialogResult.Yes)
                    {
                        return false;
                    }
                    
                    progressForm.SetStatus("VB-Cable найден! Тестирование loopback...");
                    
                    // Создаем генератор тестового сигнала (умеренная громкость)
                    sineWaveProvider = new SineWaveProvider(1000.0f, 0.1f); // 1kHz тон, 10% громкости
                    
                    // Настройка воспроизведения в VB-Cable Input (для передачи сигнала)
                    waveOut = new WaveOutEvent()
                    {
                        DeviceNumber = vbCableOutputDevice
                    };
                    waveOut.Init(sineWaveProvider);
                    
                    // Настройка записи с VB-Cable Output (для приема сигнала)
                    waveIn = new WaveInEvent()
                    {
                        DeviceNumber = vbCableInputDevice,
                        WaveFormat = new WaveFormat(44100, 16, 1)
                    };
                    
                    bool signalDetected = false;
                    float maxLevel = 0;
                    int sampleCount = 0;
                    
                    waveIn.DataAvailable += (s, e) =>
                    {
                        // Анализируем уровень сигнала
                        for (int i = 0; i < e.BytesRecorded; i += 2)
                        {
                            short sample = (short)((e.Buffer[i + 1] << 8) | e.Buffer[i]);
                            // Полностью безопасное вычисление абсолютного значения
                            float level = (sample >= 0 ? sample : -(int)sample) / 32768.0f;
                            if (level > maxLevel) maxLevel = level;
                            
                            // Понижаем порог обнаружения и требуем меньше образцов
                            if (level > 0.005f) // Снижен порог с 0.01f до 0.005f
                            {
                                sampleCount++;
                                if (sampleCount > 100) // Требуем только 100 образцов вместо большего количества
                                {
                                    signalDetected = true;
                                }
                            }
                        }
                    };
                    
                    // Начинаем запись
                    waveIn.StartRecording();
                    isRecording = true;
                    
                    progressForm.SetStatus("Запуск записи...");
                    await Task.Delay(500); // Даем время на запуск записи
                    
                    // Начинаем воспроизведение
                    waveOut.Play();
                    isPlaying = true;
                    
                    progressForm.SetStatus("Анализ loopback сигнала... (вы должны слышать тон)");
                    
                    // Ждем и анализируем с промежуточными проверками
                    for (int i = 0; i < duration; i++)
                    {
                        await Task.Delay(1000, cancellationTokenSource.Token);
                        progressForm.SetStatus($"Анализ... {i+1}/{duration}с, max: {maxLevel:P1}, samples: {sampleCount}");
                        
                        // Ранняя проверка успеха
                        if (signalDetected && maxLevel > 0.005f)
                        {
                            break;
                        }
                    }
                    
                    // Останавливаем
                    waveOut.Stop();
                    waveIn.StopRecording();
                    isPlaying = false;
                    isRecording = false;
                    
                    // Более либеральная проверка успеха
                    if (signalDetected && maxLevel > 0.005f)
                    {
                        progressForm.SetStatus($"✓ VB-Cable работает! Уровень: {maxLevel:P1}, образцов: {sampleCount}");
                        await Task.Delay(2000);
                        return true;
                    }
                    else if (maxLevel > 0.001f) // Даже если сигнал очень слабый
                    {
                        progressForm.SetStatus($"⚠ VB-Cable частично работает. Слабый сигнал: {maxLevel:P1}");
                        
                        var result = MessageBox.Show($"Обнаружен слабый сигнал loopback: {maxLevel:P1}\n" +
                                                   $"Образцов сигнала: {sampleCount}\n\n" +
                                                   "VB-Cable может работать, но сигнал слабый.\n" +
                                                   "Считать тест успешным?",
                                                   "Слабый сигнал VB-Cable",
                                                   MessageBoxButtons.YesNo,
                                                   MessageBoxIcon.Question);
                        
                        return result == DialogResult.Yes;
                    }
                    else
                    {
                        progressForm.SetStatus("✗ VB-Cable: loopback сигнал не обнаружен!");
                        
                        string diagnostics = $"Диагностика:\n" +
                                           $"- Максимальный уровень: {maxLevel:P1}\n" +
                                           $"- Образцов сигнала: {sampleCount}\n" +
                                           $"- Устройство воспроизведения: {vbCableOutputDevice} ({outputDeviceName})\n" +
                                           $"- Устройство записи: {vbCableInputDevice} ({inputDeviceName})\n\n" +
                                           "Возможные причины:\n" +
                                           "• VB-Cable не настроен как устройство по умолчанию\n" +
                                           "• Проблемы с драйверами аудио\n" +
                                           "• VB-Cable не установлен правильно\n" +
                                           "• Неправильное сопоставление устройств Input/Output";
                        
                        MessageBox.Show(diagnostics, "Диагностика VB-Cable", 
                                      MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        
                        await Task.Delay(3000);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при тестировании VB-Cable: {ex.Message}", 
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            finally
            {
                StopAll();
            }
        }

        /// <summary>
        /// Остановка всех аудио операций
        /// </summary>
        public void StopAll()
        {
            try
            {
                cancellationTokenSource?.Cancel();
                
                if (isRecording && waveIn != null)
                {
                    waveIn.StopRecording();
                    isRecording = false;
                }
                
                if (isPlaying && waveOut != null)
                {
                    waveOut.Stop();
                    isPlaying = false;
                }
            }
            catch { }
        }

        public void Dispose()
        {
            StopAll();
            
            waveIn?.Dispose();
            waveOut?.Dispose();
            bufferedWaveProvider = null;
            sineWaveProvider = null;
            cancellationTokenSource?.Dispose();
        }
    }

    /// <summary>
    /// Генератор синусоиды для тестирования
    /// </summary>
    public class SineWaveProvider : ISampleProvider
    {
        private float frequency;
        private float amplitude;
        private double phase;

        public WaveFormat WaveFormat => WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);

        public SineWaveProvider(float frequency, float amplitude = 0.1f)
        {
            this.frequency = frequency;
            this.amplitude = amplitude;
            this.phase = 0;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                buffer[offset + i] = (float)(amplitude * Math.Sin(phase));
                phase += 2 * Math.PI * frequency / WaveFormat.SampleRate;
                if (phase > 2 * Math.PI) phase -= 2 * Math.PI;
            }
            return count;
        }
    }

    /// <summary>
    /// Форма прогресса для аудио тестов
    /// </summary>
    public class AudioTestProgressForm : Form
    {
        private Label statusLabel = null!;
        private ProgressBar progressBar = null!;
        private Button cancelButton = null!;
        private System.Windows.Forms.Timer? timer;
        private int totalDuration;
        private int currentTime;

        public AudioTestProgressForm(string title, int duration)
        {
            this.totalDuration = duration;
            this.currentTime = 0;
            
            InitializeForm(title);
            InitializeTimer();
        }

        private void InitializeForm(string title)
        {
            this.Text = title;
            this.Size = new System.Drawing.Size(400, 150);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            statusLabel = new Label()
            {
                Text = "Инициализация...",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(340, 23),
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };

            progressBar = new ProgressBar()
            {
                Location = new System.Drawing.Point(20, 50),
                Size = new System.Drawing.Size(340, 23),
                Maximum = totalDuration,
                Value = 0
            };

            cancelButton = new Button()
            {
                Text = "Отмена",
                Location = new System.Drawing.Point(285, 85),
                Size = new System.Drawing.Size(75, 23)
            };
            cancelButton.Click += (s, e) => this.Close();

            this.Controls.AddRange(new Control[] { statusLabel, progressBar, cancelButton });
        }

        private void InitializeTimer()
        {
            timer = new System.Windows.Forms.Timer()
            {
                Interval = 1000 // 1 секунда
            };
            timer.Tick += (s, e) =>
            {
                currentTime++;
                progressBar.Value = Math.Min(currentTime, totalDuration);
                
                if (currentTime >= totalDuration)
                {
                    timer.Stop();
                }
            };
            timer.Start();
        }

        public void SetStatus(string status)
        {
            if (statusLabel.InvokeRequired)
            {
                statusLabel.Invoke(new Action<string>(SetStatus), status);
            }
            else
            {
                statusLabel.Text = status;
                this.Refresh();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer?.Stop();
                timer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
