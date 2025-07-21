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

                // Поиск VB-Cable Input (микрофон)
                var inputDevices = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                foreach (var device in inputDevices)
                {
                    if (device.FriendlyName.ToLower().Contains("cable") || 
                        device.FriendlyName.ToLower().Contains("vb-audio") ||
                        device.FriendlyName.ToLower().Contains("virtual"))
                    {
                        vbInputId = device.ID;
                        vbInputName = device.FriendlyName;
                        break;
                    }
                }

                // Поиск VB-Cable Output (динамики)
                var outputDevices = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                foreach (var device in outputDevices)
                {
                    if (device.FriendlyName.ToLower().Contains("cable") || 
                        device.FriendlyName.ToLower().Contains("vb-audio") ||
                        device.FriendlyName.ToLower().Contains("virtual"))
                    {
                        vbOutputId = device.ID;
                        vbOutputName = device.FriendlyName;
                        break;
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
        /// </summary>
        public (List<(string id, string name)> microphones, List<(string id, string name)> speakers) FindPhysicalDevices()
        {
            var microphones = new List<(string id, string name)>();
            var speakers = new List<(string id, string name)>();

            try
            {
                if (_deviceEnumerator == null) return (microphones, speakers);

                // Поиск физических микрофонов
                var inputDevices = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                foreach (var device in inputDevices)
                {
                    // Исключаем виртуальные устройства
                    if (!device.FriendlyName.ToLower().Contains("cable") && 
                        !device.FriendlyName.ToLower().Contains("vb-audio") &&
                        !device.FriendlyName.ToLower().Contains("virtual") &&
                        !device.FriendlyName.ToLower().Contains("stereomix"))
                    {
                        microphones.Add((device.ID, device.FriendlyName));
                    }
                }

                // Поиск физических динамиков
                var outputDevices = _deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                foreach (var device in outputDevices)
                {
                    // Исключаем виртуальные устройства
                    if (!device.FriendlyName.ToLower().Contains("cable") && 
                        !device.FriendlyName.ToLower().Contains("vb-audio") &&
                        !device.FriendlyName.ToLower().Contains("virtual"))
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
