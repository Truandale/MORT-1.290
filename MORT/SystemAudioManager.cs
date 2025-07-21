using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;

namespace MORT
{
    /// <summary>
    /// Менеджер системных аудиоустройств для программного переключения устройств по умолчанию
    /// </summary>
    public class SystemAudioManager : IDisposable
    {
        private MMDeviceEnumerator? _deviceEnumerator;
        private bool _disposed = false;

        // Сохраненные оригинальные устройства для восстановления
        private MMDevice? _originalDefaultMicrophone;
        private MMDevice? _originalDefaultSpeakers;
        private MMDevice? _originalDefaultCommunicationMicrophone;
        private MMDevice? _originalDefaultCommunicationSpeakers;

        public event Action<string>? OnLog;

        public SystemAudioManager()
        {
            _deviceEnumerator = new MMDeviceEnumerator();
            SaveOriginalDevices();
        }

        /// <summary>
        /// Сохранить оригинальные устройства для последующего восстановления
        /// </summary>
        private void SaveOriginalDevices()
        {
            try
            {
                if (_deviceEnumerator == null) return;

                _originalDefaultMicrophone = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
                _originalDefaultSpeakers = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                _originalDefaultCommunicationMicrophone = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
                _originalDefaultCommunicationSpeakers = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Communications);

                OnLog?.Invoke("💾 Оригинальные устройства сохранены для восстановления");
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"⚠️ Ошибка сохранения оригинальных устройств: {ex.Message}");
            }
        }

        /// <summary>
        /// Установить устройство как умолчание для записи (микрофон)
        /// </summary>
        /// <param name="deviceId">ID устройства</param>
        /// <param name="setAsCommunicationDevice">Установить также как устройство связи</param>
        public bool SetDefaultInputDevice(string deviceId, bool setAsCommunicationDevice = true)
        {
            try
            {
                if (_deviceEnumerator == null) return false;

                var device = _deviceEnumerator.GetDevice(deviceId);
                if (device == null)
                {
                    OnLog?.Invoke($"❌ Устройство записи не найдено: {deviceId}");
                    return false;
                }

                // Используем PolicyConfig API для программного переключения
                var policyConfig = new PolicyConfigClient();
                
                // Установить как мультимедийное устройство по умолчанию
                int result = policyConfig.SetDefaultEndpoint(deviceId, Role.Multimedia);
                if (result != 0)
                {
                    OnLog?.Invoke($"❌ Ошибка установки устройства записи (мультимедиа): {result}");
                    return false;
                }

                // Установить как устройство связи
                if (setAsCommunicationDevice)
                {
                    result = policyConfig.SetDefaultEndpoint(deviceId, Role.Communications);
                    if (result != 0)
                    {
                        OnLog?.Invoke($"⚠️ Ошибка установки устройства записи (связь): {result}");
                    }
                }

                OnLog?.Invoke($"✅ Установлен микрофон по умолчанию: {device.FriendlyName}");
                return true;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"❌ Ошибка установки устройства записи: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Установить устройство как умолчание для воспроизведения (динамики)
        /// </summary>
        /// <param name="deviceId">ID устройства</param>
        /// <param name="setAsCommunicationDevice">Установить также как устройство связи</param>
        public bool SetDefaultOutputDevice(string deviceId, bool setAsCommunicationDevice = true)
        {
            try
            {
                if (_deviceEnumerator == null) return false;

                var device = _deviceEnumerator.GetDevice(deviceId);
                if (device == null)
                {
                    OnLog?.Invoke($"❌ Устройство воспроизведения не найдено: {deviceId}");
                    return false;
                }

                var policyConfig = new PolicyConfigClient();
                
                // Установить как мультимедийное устройство по умолчанию
                int result = policyConfig.SetDefaultEndpoint(deviceId, Role.Multimedia);
                if (result != 0)
                {
                    OnLog?.Invoke($"❌ Ошибка установки устройства воспроизведения (мультимедиа): {result}");
                    return false;
                }

                // Установить как устройство связи
                if (setAsCommunicationDevice)
                {
                    result = policyConfig.SetDefaultEndpoint(deviceId, Role.Communications);
                    if (result != 0)
                    {
                        OnLog?.Invoke($"⚠️ Ошибка установки устройства воспроизведения (связь): {result}");
                    }
                }

                OnLog?.Invoke($"✅ Установлены динамики по умолчанию: {device.FriendlyName}");
                return true;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"❌ Ошибка установки устройства воспроизведения: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Найти VB-Cable устройства
        /// Ищет сначала по FriendlyName, затем по техническому имени для отказоустойчивости
        /// 
        /// ВАЖНО: 
        /// - InputId (микрофон/запись) = "CABLE Output" в DataFlow.Capture
        /// - OutputId (динамики/воспроизведение) = "CABLE Input" в DataFlow.Render
        /// </summary>
        public (string? inputId, string? outputId, string inputName, string outputName) FindVBCableDevices()
        {
            try
            {
                if (_deviceEnumerator == null) return (null, null, "", "");

                string? vbInputId = null;
                string? vbOutputId = null;
                string vbInputName = "";
                string vbOutputName = "";

                // Поиск VB-Cable Input (устройство записи/микрофон) с приоритетом
                var inputDevices = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                
                // Приоритет 1: CABLE Output (правильное устройство записи) по FriendlyName
                foreach (var device in inputDevices)
                {
                    string deviceName = device.FriendlyName.ToLower();
                    if (deviceName.Contains("cable output"))
                    {
                        vbInputId = device.ID;
                        vbInputName = device.FriendlyName;
                        break;
                    }
                }

                // Приоритет 2: Отказоустойчивый поиск по техническому имени "VB-Audio Virtual Cable"
                if (vbInputId == null)
                {
                    foreach (var device in inputDevices)
                    {
                        try
                        {
                            // Ищем точно по техническому имени как на скриншотах
                            var deviceDesc = device.DeviceFriendlyName?.ToLower() ?? "";
                            var deviceId = device.ID?.ToLower() ?? "";
                            
                            // Поиск по точному техническому имени "VB-Audio Virtual Cable"
                            if (deviceDesc.Contains("vb-audio virtual cable") || 
                                deviceId.Contains("vb-audio virtual cable"))
                            {
                                vbInputId = device.ID;
                                vbInputName = device.FriendlyName + " (найден по техническому имени)";
                                OnLog?.Invoke($"🔧 VB-Cable Input найден по техническому имени: VB-Audio Virtual Cable");
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            // Игнорируем ошибки доступа к свойствам устройства
                            OnLog?.Invoke($"⚠️ Ошибка чтения технического имени устройства: {ex.Message}");
                        }
                    }
                }

                // Поиск VB-Cable Output (устройство воспроизведения/динамики)
                var outputDevices = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                
                // Приоритет 1: CABLE Input (правильное устройство воспроизведения) по FriendlyName
                foreach (var device in outputDevices)
                {
                    string deviceName = device.FriendlyName.ToLower();
                    if (deviceName.Contains("cable input") && !deviceName.Contains("16ch"))
                    {
                        vbOutputId = device.ID;
                        vbOutputName = device.FriendlyName;
                        break;
                    }
                }
                
                // Приоритет 2: CABLE In 16ch по FriendlyName (если обычного нет)
                if (vbOutputId == null)
                {
                    foreach (var device in outputDevices)
                    {
                        string deviceName = device.FriendlyName.ToLower();
                        if (deviceName.Contains("cable") && deviceName.Contains("16ch"))
                        {
                            vbOutputId = device.ID;
                            vbOutputName = device.FriendlyName;
                            break;
                        }
                    }
                }

                // Приоритет 3: Отказоустойчивый поиск Output по техническому имени "VB-Audio Virtual Cable"
                if (vbOutputId == null)
                {
                    foreach (var device in outputDevices)
                    {
                        try
                        {
                            // Ищем точно по техническому имени как на скриншотах
                            var deviceDesc = device.DeviceFriendlyName?.ToLower() ?? "";
                            var deviceId = device.ID?.ToLower() ?? "";
                            
                            // Поиск по точному техническому имени "VB-Audio Virtual Cable"
                            if (deviceDesc.Contains("vb-audio virtual cable") || 
                                deviceId.Contains("vb-audio virtual cable"))
                            {
                                vbOutputId = device.ID;
                                vbOutputName = device.FriendlyName + " (найден по техническому имени)";
                                OnLog?.Invoke($"🔧 VB-Cable Output найден по техническому имени: VB-Audio Virtual Cable");
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            // Игнорируем ошибки доступа к свойствам устройства
                            OnLog?.Invoke($"⚠️ Ошибка чтения технического имени устройства: {ex.Message}");
                        }
                    }
                }

                if (vbInputId != null && vbOutputId != null)
                {
                    OnLog?.Invoke($"🔍 Найдены VB-Cable устройства: {vbInputName} | {vbOutputName}");
                }
                else
                {
                    OnLog?.Invoke("⚠️ VB-Cable устройства не найдены. Убедитесь, что VB-Cable установлен.");
                }

                return (vbInputId, vbOutputId, vbInputName, vbOutputName);
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"❌ Ошибка поиска VB-Cable устройств: {ex.Message}");
                return (null, null, "", "");
            }
        }

        /// <summary>
        /// Найти физические (реальные) аудиоустройства
        /// Исключает ВСЕ виртуальные кабели VB-Audio
        /// </summary>
        public (List<(string id, string name)> microphones, List<(string id, string name)> speakers) FindPhysicalDevices()
        {
            var microphones = new List<(string id, string name)>();
            var speakers = new List<(string id, string name)>();

            try
            {
                if (_deviceEnumerator == null) return (microphones, speakers);

                // Поиск физических микрофонов (исключая ВСЕ VB-Cable устройства)
                var inputDevices = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                foreach (var device in inputDevices)
                {
                    string deviceName = device.FriendlyName.ToLower();
                    
                    // Исключаем ВСЕ VB-Cable устройства: CABLE Input, CABLE In 16ch, CABLE Output
                    bool isVBCable = deviceName.Contains("cable input") || 
                                    deviceName.Contains("cable output") ||
                                    (deviceName.Contains("cable") && deviceName.Contains("16ch"));
                    
                    if (!isVBCable)
                    {
                        microphones.Add((device.ID, device.FriendlyName));
                    }
                }

                // Поиск физических динамиков (исключая ВСЕ VB-Cable устройства)
                var outputDevices = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                foreach (var device in outputDevices)
                {
                    string deviceName = device.FriendlyName.ToLower();
                    
                    // Исключаем ВСЕ VB-Cable устройства
                    bool isVBCable = deviceName.Contains("cable input") || 
                                    deviceName.Contains("cable output") ||
                                    (deviceName.Contains("cable") && deviceName.Contains("16ch"));
                    
                    if (!isVBCable)
                    {
                        speakers.Add((device.ID, device.FriendlyName));
                    }
                }

                OnLog?.Invoke($"🔍 Найдено физических устройств: {microphones.Count} микрофонов, {speakers.Count} динамиков");
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"❌ Ошибка поиска физических устройств: {ex.Message}");
            }

            return (microphones, speakers);
        }

        /// <summary>
        /// Восстановить оригинальные устройства по умолчанию
        /// </summary>
        public bool RestoreOriginalDevices()
        {
            try
            {
                bool success = true;

                if (_originalDefaultMicrophone != null)
                {
                    if (!SetDefaultInputDevice(_originalDefaultMicrophone.ID, false))
                        success = false;
                }

                if (_originalDefaultSpeakers != null)
                {
                    if (!SetDefaultOutputDevice(_originalDefaultSpeakers.ID, false))
                        success = false;
                }

                if (_originalDefaultCommunicationMicrophone != null)
                {
                    var policyConfig = new PolicyConfigClient();
                    policyConfig.SetDefaultEndpoint(_originalDefaultCommunicationMicrophone.ID, Role.Communications);
                }

                if (_originalDefaultCommunicationSpeakers != null)
                {
                    var policyConfig = new PolicyConfigClient();
                    policyConfig.SetDefaultEndpoint(_originalDefaultCommunicationSpeakers.ID, Role.Communications);
                }

                if (success)
                {
                    OnLog?.Invoke("✅ Оригинальные устройства восстановлены");
                }
                else
                {
                    OnLog?.Invoke("⚠️ Частичное восстановление оригинальных устройств");
                }

                return success;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"❌ Ошибка восстановления оригинальных устройств: {ex.Message}");
                return false;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _deviceEnumerator?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// PolicyConfig API для программного изменения устройств по умолчанию
    /// </summary>
    [ComImport]
    [Guid("f8679f50-850a-41cf-9c72-430f290290c8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IPolicyConfig
    {
        [PreserveSig]
        int GetMixFormat(string deviceId, IntPtr format);
        
        [PreserveSig]
        int GetDeviceFormat(string deviceId, bool defaultDevice, IntPtr format);
        
        [PreserveSig]
        int ResetDeviceFormat(string deviceId);
        
        [PreserveSig]
        int SetDeviceFormat(string deviceId, IntPtr endpointFormat, IntPtr mixFormat);
        
        [PreserveSig]
        int GetProcessingPeriod(string deviceId, bool defaultDevice, out long defaultPeriod, out long minimumPeriod);
        
        [PreserveSig]
        int SetProcessingPeriod(string deviceId, ref long period);
        
        [PreserveSig]
        int GetShareMode(string deviceId, IntPtr mode);
        
        [PreserveSig]
        int SetShareMode(string deviceId, IntPtr mode);
        
        [PreserveSig]
        int GetPropertyValue(string deviceId, ref PropertyKey key, IntPtr value);
        
        [PreserveSig]
        int SetPropertyValue(string deviceId, ref PropertyKey key, IntPtr value);
        
        [PreserveSig]
        int SetDefaultEndpoint(string deviceId, Role role);
        
        [PreserveSig]
        int SetEndpointVisibility(string deviceId, bool visible);
    }

    [ComImport]
    [Guid("568b9108-44bf-40b4-9006-86afe5b5a620")]
    class PolicyConfigClient : IPolicyConfig
    {
        public extern int GetMixFormat(string deviceId, IntPtr format);
        public extern int GetDeviceFormat(string deviceId, bool defaultDevice, IntPtr format);
        public extern int ResetDeviceFormat(string deviceId);
        public extern int SetDeviceFormat(string deviceId, IntPtr endpointFormat, IntPtr mixFormat);
        public extern int GetProcessingPeriod(string deviceId, bool defaultDevice, out long defaultPeriod, out long minimumPeriod);
        public extern int SetProcessingPeriod(string deviceId, ref long period);
        public extern int GetShareMode(string deviceId, IntPtr mode);
        public extern int SetShareMode(string deviceId, IntPtr mode);
        public extern int GetPropertyValue(string deviceId, ref PropertyKey key, IntPtr value);
        public extern int SetPropertyValue(string deviceId, ref PropertyKey key, IntPtr value);
        
        [PreserveSig]
        public extern int SetDefaultEndpoint(string deviceId, Role role);
        
        public extern int SetEndpointVisibility(string deviceId, bool visible);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PropertyKey
    {
        public Guid fmtid;
        public uint pid;
    }
}
