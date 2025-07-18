using System;
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
        /// Поиск VB-Cable устройств с расширенными критериями
        /// </summary>
        private (int inputDevice, int outputDevice, string details) FindVBCableDevices()
        {
            int vbCableInputDevice = -1;
            int vbCableOutputDevice = -1;
            var details = new StringBuilder();
            
            details.AppendLine("Поиск VB-Cable устройств...");
            
            // Расширенный поиск устройств записи
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                var caps = WaveIn.GetCapabilities(i);
                string productName = caps.ProductName.ToLower();
                details.AppendLine($"Проверка записи [{i}]: {caps.ProductName}");
                
                if (productName.Contains("vb-cable") || 
                    productName.Contains("cable input") ||
                    productName.Contains("virtual cable") ||
                    productName.Contains("vac") ||
                    productName.Contains("voicemeeter") ||
                    productName.Contains("vb-audio") ||
                    productName.Contains("cable") ||
                    productName.Contains("vaio"))
                {
                    vbCableInputDevice = i;
                    details.AppendLine($"✓ Найден VB-Cable INPUT: {caps.ProductName}");
                    break;
                }
            }
            
            // Расширенный поиск устройств воспроизведения
            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                var caps = WaveOut.GetCapabilities(i);
                string productName = caps.ProductName.ToLower();
                details.AppendLine($"Проверка вывода [{i}]: {caps.ProductName}");
                
                if (productName.Contains("vb-cable") || 
                    productName.Contains("cable input") ||
                    productName.Contains("virtual cable") ||
                    productName.Contains("vac") ||
                    productName.Contains("voicemeeter") ||
                    productName.Contains("vb-audio") ||
                    productName.Contains("cable") ||
                    productName.Contains("vaio"))
                {
                    vbCableOutputDevice = i;
                    details.AppendLine($"✓ Найден VB-Cable OUTPUT: {caps.ProductName}");
                    break;
                }
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
                    
                    // Ищем VB-Cable устройства с расширенным поиском
                    var (vbCableInputDevice, vbCableOutputDevice, searchDetails) = FindVBCableDevices();
                    
                    if (vbCableInputDevice == -1 || vbCableOutputDevice == -1)
                    {
                        // Показываем детальную информацию о поиске
                        string allDevices = GetAllAudioDevices();
                        string errorMsg = $"VB-Cable не найден!\n\n{searchDetails}\n{allDevices}";
                        
                        MessageBox.Show(errorMsg, "Детали поиска VB-Cable", 
                                      MessageBoxButtons.OK, MessageBoxIcon.Information);
                        
                        progressForm.SetStatus("VB-Cable не найден! Проверьте установку.");
                        await Task.Delay(3000);
                        return false;
                    }
                    
                    progressForm.SetStatus("VB-Cable найден! Тестирование loopback...");
                    
                    // Создаем генератор тестового сигнала
                    sineWaveProvider = new SineWaveProvider(1000.0f); // 1kHz тон
                    
                    // Настройка воспроизведения в VB-Cable
                    waveOut = new WaveOutEvent()
                    {
                        DeviceNumber = vbCableOutputDevice
                    };
                    waveOut.Init(sineWaveProvider);
                    
                    // Настройка записи с VB-Cable
                    waveIn = new WaveInEvent()
                    {
                        DeviceNumber = vbCableInputDevice,
                        WaveFormat = new WaveFormat(44100, 16, 1)
                    };
                    
                    bool signalDetected = false;
                    float maxLevel = 0;
                    
                    waveIn.DataAvailable += (s, e) =>
                    {
                        // Анализируем уровень сигнала
                        for (int i = 0; i < e.BytesRecorded; i += 2)
                        {
                            short sample = (short)((e.Buffer[i + 1] << 8) | e.Buffer[i]);
                            float level = Math.Abs(sample) / 32768.0f;
                            if (level > maxLevel) maxLevel = level;
                            if (level > 0.01f) signalDetected = true; // Порог обнаружения сигнала
                        }
                    };
                    
                    // Начинаем запись
                    waveIn.StartRecording();
                    isRecording = true;
                    
                    await Task.Delay(500); // Небольшая задержка
                    
                    // Начинаем воспроизведение
                    waveOut.Play();
                    isPlaying = true;
                    
                    progressForm.SetStatus("Анализ loopback сигнала...");
                    
                    // Ждем и анализируем
                    await Task.Delay(duration * 1000, cancellationTokenSource.Token);
                    
                    // Останавливаем
                    waveOut.Stop();
                    waveIn.StopRecording();
                    isPlaying = false;
                    isRecording = false;
                    
                    if (signalDetected && maxLevel > 0.01f)
                    {
                        progressForm.SetStatus($"VB-Cable работает! Уровень сигнала: {maxLevel:P1}");
                        await Task.Delay(2000);
                        return true;
                    }
                    else
                    {
                        progressForm.SetStatus("VB-Cable не работает - сигнал не обнаружен!");
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

        public SineWaveProvider(float frequency, float amplitude = 0.25f)
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
