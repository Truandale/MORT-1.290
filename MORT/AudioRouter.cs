using System;
using System.Threading.Tasks;
using NAudio.Wave;

namespace MORT
{
    /// <summary>
    /// Экспериментальный класс для постоянного перенаправления звука между устройствами
    /// </summary>
    public class AudioRouter : IDisposable
    {
        private WaveInEvent? _inputDevice;
        private WaveOutEvent? _outputDevice;
        private BufferedWaveProvider? _waveProvider;
        private bool _isRouting = false;
        private bool _disposed = false;

        public bool IsRouting => _isRouting;
        public string CurrentRoute { get; private set; } = "";

        /// <summary>
        /// Событие для логирования
        /// </summary>
        public event Action<string>? OnLog;

        /// <summary>
        /// Запустить перенаправление звука
        /// </summary>
        /// <param name="inputDeviceIndex">Индекс входного устройства (микрофон/виртуальный вход)</param>
        /// <param name="outputDeviceIndex">Индекс выходного устройства (динамики/наушники)</param>
        /// <param name="inputDeviceName">Название входного устройства для логирования</param>
        /// <param name="outputDeviceName">Название выходного устройства для логирования</param>
        public async Task<bool> StartRoutingAsync(int inputDeviceIndex, int outputDeviceIndex, string inputDeviceName, string outputDeviceName)
        {
            try
            {
                if (_isRouting)
                {
                    OnLog?.Invoke("⚠️ Перенаправление уже активно. Остановите текущее перед запуском нового.");
                    return false;
                }

                OnLog?.Invoke($"🔄 Запуск перенаправления: {inputDeviceName} → {outputDeviceName}");

                // Проверка валидности индексов устройств
                if (inputDeviceIndex < 0 || inputDeviceIndex >= WaveInEvent.DeviceCount)
                {
                    OnLog?.Invoke($"❌ Неверный индекс входного устройства: {inputDeviceIndex}");
                    return false;
                }

                if (outputDeviceIndex < 0 || outputDeviceIndex >= WaveOut.DeviceCount)
                {
                    OnLog?.Invoke($"❌ Неверный индекс выходного устройства: {outputDeviceIndex}");
                    return false;
                }

                // Настройка входного устройства
                _inputDevice = new WaveInEvent();
                _inputDevice.DeviceNumber = inputDeviceIndex;
                _inputDevice.WaveFormat = new WaveFormat(44100, 16, 2); // 44.1kHz, 16-bit, stereo

                // Настройка выходного устройства
                _outputDevice = new WaveOutEvent();
                _outputDevice.DeviceNumber = outputDeviceIndex;

                // Буфер для передачи данных (2 секунды буфера)
                _waveProvider = new BufferedWaveProvider(_inputDevice.WaveFormat)
                {
                    BufferLength = _inputDevice.WaveFormat.AverageBytesPerSecond * 2,
                    DiscardOnBufferOverflow = true
                };

                // Обработчик входящих аудиоданных
                _inputDevice.DataAvailable += OnDataAvailable;
                _inputDevice.RecordingStopped += OnRecordingStopped;

                // Инициализация и запуск
                _outputDevice.Init(_waveProvider);
                
                await Task.Run(() => {
                    _inputDevice.StartRecording();
                    _outputDevice.Play();
                });

                _isRouting = true;
                CurrentRoute = $"{inputDeviceName} → {outputDeviceName}";
                
                OnLog?.Invoke($"✅ Перенаправление активно: {CurrentRoute}");
                OnLog?.Invoke($"📊 Формат: {_inputDevice.WaveFormat}");
                
                return true;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"❌ Ошибка запуска перенаправления: {ex.Message}");
                StopRouting();
                return false;
            }
        }

        /// <summary>
        /// Остановить перенаправление звука
        /// </summary>
        public void StopRouting()
        {
            try
            {
                if (!_isRouting)
                {
                    OnLog?.Invoke("ℹ️ Перенаправление уже остановлено.");
                    return;
                }

                OnLog?.Invoke($"⏹️ Остановка перенаправления: {CurrentRoute}");

                _inputDevice?.StopRecording();
                _outputDevice?.Stop();

                _isRouting = false;
                CurrentRoute = "";

                OnLog?.Invoke("✅ Перенаправление остановлено.");
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"⚠️ Ошибка при остановке: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик входящих аудиоданных
        /// </summary>
        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            try
            {
                if (_waveProvider != null && _isRouting)
                {
                    _waveProvider.AddSamples(e.Buffer, 0, e.BytesRecorded);
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"⚠️ Ошибка обработки аудио: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик остановки записи
        /// </summary>
        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                OnLog?.Invoke($"❌ Запись остановлена с ошибкой: {e.Exception.Message}");
            }
            else
            {
                OnLog?.Invoke("ℹ️ Запись остановлена.");
            }
        }

        /// <summary>
        /// Получить статистику буфера
        /// </summary>
        public string GetBufferStats()
        {
            if (_waveProvider == null || !_isRouting)
                return "Перенаправление неактивно";

            var bufferedMs = _waveProvider.BufferedDuration.TotalMilliseconds;
            var bufferLengthMs = (_waveProvider.BufferLength * 1000.0) / _waveProvider.WaveFormat.AverageBytesPerSecond;
            
            return $"Буфер: {bufferedMs:F0}мс / {bufferLengthMs:F0}мс";
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                StopRouting();

                _inputDevice?.Dispose();
                _outputDevice?.Dispose();
                _waveProvider = null;

                _disposed = true;
            }
        }
    }
}
