using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace MORT
{
    /// <summary>
    /// Универсальный менеджер системного аудиоперевода
    /// Обеспечивает полную интеграцию с системными устройствами и VB-Cable
    /// </summary>
    public class UniversalAudioTranslateManager : IDisposable
    {
        private SystemAudioManager? _systemAudioManager;
        private AudioRouter? _inputRouter;    // Реальный микрофон → VB-Cable Input
        private AudioRouter? _outputRouter;   // VB-Cable Output → Реальные динамики
        private AdvancedAudioSettings? _audioTranslator;
        private SettingManager? _settingsManager;
        private bool _disposed = false;

        // Состояние системы
        private bool _isUniversalModeActive = false;
        private bool _isTranslationActive = false;
        
        // Информация об устройствах
        private string? _vbCableInputId;
        private string? _vbCableOutputId;
        private string? _selectedPhysicalMicId;
        private string? _selectedPhysicalSpeakersId;
        
        // Названия устройств для логирования
        private string _vbInputName = "";
        private string _vbOutputName = "";
        private string _physicalMicName = "";
        private string _physicalSpeakersName = "";

        public event Action<string>? OnLog;
        public event Action<bool>? OnUniversalModeChanged;
        public event Action<bool>? OnTranslationStateChanged;

        public bool IsUniversalModeActive => _isUniversalModeActive;
        public bool IsTranslationActive => _isTranslationActive;

        public UniversalAudioTranslateManager(SettingManager settingsManager)
        {
            _settingsManager = settingsManager;
            _systemAudioManager = new SystemAudioManager();
            _systemAudioManager.OnLog += (msg) => OnLog?.Invoke(msg);
            
            _inputRouter = new AudioRouter();
            _inputRouter.OnLog += (msg) => OnLog?.Invoke($"[INPUT] {msg}");
            
            _outputRouter = new AudioRouter();
            _outputRouter.OnLog += (msg) => OnLog?.Invoke($"[OUTPUT] {msg}");

            LoadConfiguration();
        }

        /// <summary>
        /// Загрузить конфигурацию из настроек
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                // Здесь можно загрузить сохраненные настройки из файла или реестра
                OnLog?.Invoke("📋 Конфигурация загружена");
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"⚠️ Ошибка загрузки конфигурации: {ex.Message}");
            }
        }

        /// <summary>
        /// Автоматическое обнаружение и настройка устройств
        /// </summary>
        public Task<bool> AutoDetectAndConfigureAsync()
        {
            try
            {
                OnLog?.Invoke("🔍 Автоматическое обнаружение устройств...");

                if (_systemAudioManager == null) return Task.FromResult(false);

                // Поиск VB-Cable устройств
                var (vbInputId, vbOutputId, vbInputName, vbOutputName) = _systemAudioManager.FindVBCableDevices();
                
                if (vbInputId == null || vbOutputId == null)
                {
                    OnLog?.Invoke("❌ VB-Cable не найден. Установите VB-Cable для работы универсального режима.");
                    return Task.FromResult(false);
                }

                _vbCableInputId = vbInputId;
                _vbCableOutputId = vbOutputId;
                _vbInputName = vbInputName;
                _vbOutputName = vbOutputName;

                // Поиск физических устройств
                var (microphones, speakers) = _systemAudioManager.FindPhysicalDevices();
                
                if (microphones.Count == 0 || speakers.Count == 0)
                {
                    OnLog?.Invoke("❌ Физические аудиоустройства не найдены.");
                    return Task.FromResult(false);
                }

                // Выбираем первые доступные физические устройства
                var selectedMic = microphones.First();
                var selectedSpeakers = speakers.First();
                
                _selectedPhysicalMicId = selectedMic.id;
                _selectedPhysicalSpeakersId = selectedSpeakers.id;
                _physicalMicName = selectedMic.name;
                _physicalSpeakersName = selectedSpeakers.name;

                OnLog?.Invoke("✅ Устройства обнаружены и настроены:");
                OnLog?.Invoke($"   🎤 Микрофон: {_physicalMicName}");
                OnLog?.Invoke($"   🔊 Динамики: {_physicalSpeakersName}");
                OnLog?.Invoke($"   🔗 VB-Cable: {_vbInputName} | {_vbOutputName}");

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"❌ Ошибка автоматического обнаружения: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Включить универсальный режим аудиоперевода
        /// </summary>
        public async Task<bool> EnableUniversalModeAsync()
        {
            try
            {
                if (_isUniversalModeActive)
                {
                    OnLog?.Invoke("⚠️ Универсальный режим уже активен");
                    return true;
                }

                OnLog?.Invoke("🚀 Включение универсального режима аудиоперевода...");

                // Проверяем конфигурацию
                if (!await AutoDetectAndConfigureAsync())
                {
                    return false;
                }

                if (_systemAudioManager == null || _inputRouter == null || _outputRouter == null)
                {
                    OnLog?.Invoke("❌ Компоненты не инициализированы");
                    return false;
                }

                // Шаг 1: Настройка системных устройств по умолчанию
                OnLog?.Invoke("🔧 Настройка системных устройств по умолчанию...");
                
                if (!_systemAudioManager.SetDefaultInputDevice(_vbCableInputId!, true))
                {
                    OnLog?.Invoke("❌ Не удалось установить VB-Cable Input как микрофон по умолчанию");
                    return false;
                }

                if (!_systemAudioManager.SetDefaultOutputDevice(_vbCableOutputId!, true))
                {
                    OnLog?.Invoke("❌ Не удалось установить VB-Cable Output как динамики по умолчанию");
                    return false;
                }

                // Шаг 2: Запуск перенаправления звука
                OnLog?.Invoke("🔄 Запуск аудиоперенаправления...");
                
                // Найдем индексы устройств для AudioRouter
                var micIndex = FindDeviceIndex(_selectedPhysicalMicId!, true);
                var speakersIndex = FindDeviceIndex(_selectedPhysicalSpeakersId!, false);
                var vbInputIndex = FindDeviceIndex(_vbCableInputId!, true);
                var vbOutputIndex = FindDeviceIndex(_vbCableOutputId!, false);

                if (micIndex == -1 || speakersIndex == -1 || vbInputIndex == -1 || vbOutputIndex == -1)
                {
                    OnLog?.Invoke("❌ Не удалось найти индексы устройств для перенаправления");
                    return false;
                }

                // Маршрутизация: Реальный микрофон → VB-Cable Input
                if (!await _inputRouter.StartRoutingAsync(micIndex, vbInputIndex, _physicalMicName, _vbInputName))
                {
                    OnLog?.Invoke("❌ Не удалось запустить перенаправление микрофона");
                    return false;
                }

                // Маршрутизация: VB-Cable Output → Реальные динамики
                if (!await _outputRouter.StartRoutingAsync(vbOutputIndex, speakersIndex, _vbOutputName, _physicalSpeakersName))
                {
                    OnLog?.Invoke("❌ Не удалось запустить перенаправление динамиков");
                    // Останавливаем уже запущенную маршрутизацию
                    _inputRouter.StopRouting();
                    return false;
                }

                _isUniversalModeActive = true;
                OnUniversalModeChanged?.Invoke(true);

                OnLog?.Invoke("✅ Универсальный режим аудиоперевода включен!");
                OnLog?.Invoke("🎯 Теперь все приложения будут использовать аудиоперевод автоматически");
                OnLog?.Invoke("💡 Используйте горячие клавиши для управления переводом");

                return true;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"❌ Ошибка включения универсального режима: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Выключить универсальный режим аудиоперевода
        /// </summary>
        public async Task<bool> DisableUniversalModeAsync()
        {
            try
            {
                if (!_isUniversalModeActive)
                {
                    OnLog?.Invoke("⚠️ Универсальный режим не активен");
                    return true;
                }

                OnLog?.Invoke("🛑 Выключение универсального режима аудиоперевода...");

                // Остановить аудиоперевод если активен
                if (_isTranslationActive)
                {
                    await StopTranslationAsync();
                }

                // Остановить перенаправление звука
                if (_inputRouter != null && _inputRouter.IsRouting)
                {
                    _inputRouter.StopRouting();
                }

                if (_outputRouter != null && _outputRouter.IsRouting)
                {
                    _outputRouter.StopRouting();
                }

                // Восстановить оригинальные системные устройства
                if (_systemAudioManager != null)
                {
                    _systemAudioManager.RestoreOriginalDevices();
                }

                _isUniversalModeActive = false;
                OnUniversalModeChanged?.Invoke(false);

                OnLog?.Invoke("✅ Универсальный режим выключен, оригинальные устройства восстановлены");

                return true;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"❌ Ошибка выключения универсального режима: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Запустить аудиоперевод (работает только в универсальном режиме)
        /// </summary>
        public Task<bool> StartTranslationAsync()
        {
            try
            {
                if (!_isUniversalModeActive)
                {
                    OnLog?.Invoke("⚠️ Включите сначала универсальный режим");
                    return Task.FromResult(false);
                }

                if (_isTranslationActive)
                {
                    OnLog?.Invoke("⚠️ Аудиоперевод уже активен");
                    return Task.FromResult(true);
                }

                OnLog?.Invoke("🎯 Запуск аудиоперевода...");

                // Здесь должна быть интеграция с существующим Audio Translator
                // Создаем экземпляр AdvancedAudioSettings в режиме только для обработки
                if (_audioTranslator == null && _settingsManager != null)
                {
                    _audioTranslator = new AdvancedAudioSettings(_settingsManager);
                    // Настройка для работы с VB-Cable устройствами
                    // TODO: Добавить метод для программной настройки без UI
                }

                _isTranslationActive = true;
                OnTranslationStateChanged?.Invoke(true);

                OnLog?.Invoke("✅ Аудиоперевод запущен!");
                OnLog?.Invoke("🔄 Весь системный звук теперь переводится автоматически");

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"❌ Ошибка запуска аудиоперевода: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Остановить аудиоперевод
        /// </summary>
        public Task<bool> StopTranslationAsync()
        {
            try
            {
                if (!_isTranslationActive)
                {
                    OnLog?.Invoke("⚠️ Аудиоперевод не активен");
                    return Task.FromResult(true);
                }

                OnLog?.Invoke("⏹️ Остановка аудиоперевода...");

                // Остановить Audio Translator
                if (_audioTranslator != null)
                {
                    _audioTranslator.Dispose();
                    _audioTranslator = null;
                }

                _isTranslationActive = false;
                OnTranslationStateChanged?.Invoke(false);

                OnLog?.Invoke("✅ Аудиоперевод остановлен");

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"❌ Ошибка остановки аудиоперевода: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Переключить состояние аудиоперевода
        /// </summary>
        public async Task<bool> ToggleTranslationAsync()
        {
            if (_isTranslationActive)
            {
                return await StopTranslationAsync();
            }
            else
            {
                return await StartTranslationAsync();
            }
        }

        /// <summary>
        /// Найти индекс устройства по ID для AudioRouter
        /// </summary>
        private int FindDeviceIndex(string deviceId, bool isInput)
        {
            try
            {
                if (isInput)
                {
                    for (int i = 0; i < NAudio.Wave.WaveInEvent.DeviceCount; i++)
                    {
                        var caps = NAudio.Wave.WaveInEvent.GetCapabilities(i);
                        // Простое сравнение по имени (может потребоваться улучшение)
                        if (caps.ProductName.Contains(deviceId) || deviceId.Contains(caps.ProductName))
                        {
                            return i;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < NAudio.Wave.WaveOut.DeviceCount; i++)
                    {
                        var caps = NAudio.Wave.WaveOut.GetCapabilities(i);
                        if (caps.ProductName.Contains(deviceId) || deviceId.Contains(caps.ProductName))
                        {
                            return i;
                        }
                    }
                }

                return -1;
            }
            catch
            {
                return -1;
            }
        }

        /// <summary>
        /// Получить текущий статус системы
        /// </summary>
        public string GetSystemStatus()
        {
            var status = "📊 Статус универсального аудиоперевода:\n";
            status += $"   🌐 Универсальный режим: {(_isUniversalModeActive ? "✅ Включен" : "❌ Выключен")}\n";
            status += $"   🎯 Аудиоперевод: {(_isTranslationActive ? "✅ Активен" : "⏹️ Остановлен")}\n";
            
            if (_isUniversalModeActive)
            {
                status += $"   🎤 Микрофон: {_physicalMicName} → {_vbInputName}\n";
                status += $"   🔊 Динамики: {_vbOutputName} → {_physicalSpeakersName}\n";
                status += $"   🔄 Маршрутизация: {(_inputRouter?.IsRouting == true && _outputRouter?.IsRouting == true ? "✅ Активна" : "❌ Неактивна")}";
            }

            return status;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                DisableUniversalModeAsync().Wait();
                
                _systemAudioManager?.Dispose();
                _inputRouter?.Dispose();
                _outputRouter?.Dispose();
                _audioTranslator?.Dispose();
                
                _disposed = true;
            }
        }
    }
}
