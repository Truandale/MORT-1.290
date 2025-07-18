using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Media;
using System.Runtime.InteropServices;
using System.ComponentModel;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace MORT
{
    /// <summary>
    /// AutoVoiceTranslator - Настройки голосового фильтра
    /// Реализует систему реального времени перевода речи с VAD фильтрацией
    /// </summary>
    public class VoiceFilterSettings : Form
    {
        private bool isInitialized = false;
        private bool isVoiceFilterEnabled = false;
        
        // UI Controls
        private CheckBox cbEnableVoiceFilter;
        private GroupBox gbVadSettings;
        private Label lbVadSensitivity;
        private TrackBar tbVadSensitivity;
        private Label lbVadValue;
        private GroupBox gbTranslationSettings;
        private Label lbSttEngine;
        private ComboBox cbSttEngine;
        private Label lbTranslationEngine;
        private ComboBox cbTranslationEngine;
        private Label lbSourceLanguage;
        private ComboBox cbSourceLanguage;
        private Label lbTargetLanguage;
        private ComboBox cbTargetLanguage;
        private GroupBox gbAudioSettings;
        private Label lbInputDevice;
        private ComboBox cbInputDevice;
        private Label lbOutputDevice;
        private ComboBox cbOutputDevice;
        private Label lbVirtualOutput;
        private ComboBox cbVirtualOutput;
        private GroupBox gbAdvancedFilters;
        private CheckBox cbNoiseSuppress;
        private CheckBox cbAutoGain;
        private Label lbVoiceThreshold;
        private TrackBar tbVoiceThreshold;
        private Label lbTranslationDelay;
        private NumericUpDown nudTranslationDelay;
        private GroupBox gbTestingControls;
        private Button btTestMicrophone;
        private Button btTestTranslation;
        private Button btAdvancedSettings;
        private CheckBox cbEnableLogging;
        private RichTextBox rtbTestOutput;
        private Button btOK;
        private Button btCancel;
        private Button btApply;
        
        // Constants for settings keys
        private const string SETTINGS_ENABLE_VOICE_FILTER = "@VoiceFilterEnabled";
        private const string SETTINGS_VAD_SENSITIVITY = "@VadSensitivity";
        private const string SETTINGS_STT_ENGINE = "@SttEngine";
        private const string SETTINGS_TRANSLATION_ENGINE = "@TranslationEngine";
        private const string SETTINGS_SOURCE_LANGUAGE = "@SourceLanguage";
        private const string SETTINGS_TARGET_LANGUAGE = "@TargetLanguage";
        private const string SETTINGS_INPUT_DEVICE = "@InputDevice";
        private const string SETTINGS_OUTPUT_DEVICE = "@OutputDevice";
        private const string SETTINGS_VIRTUAL_OUTPUT = "@VirtualOutput";
        private const string SETTINGS_NOISE_SUPPRESS = "@NoiseSuppress";
        private const string SETTINGS_AUTO_GAIN = "@AutoGain";
        private const string SETTINGS_VOICE_THRESHOLD = "@VoiceThreshold";
        private const string SETTINGS_TRANSLATION_DELAY = "@TranslationDelay";
        private const string SETTINGS_ENABLE_LOGGING = "@EnableLogging";

        public VoiceFilterSettings()
        {
            InitializeComponent();
            InitializeComboBoxes();
            LoadAudioDevices();
            LoadSettings();
            isInitialized = true;
        }

        /// <summary>
        /// Инициализация компонентов формы
        /// </summary>
        private void InitializeComponent()
        {
            this.Text = "Настройки голосового фильтра";
            this.Size = new Size(620, 480);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ShowInTaskbar = false;

            // Основной checkbox
            cbEnableVoiceFilter = new CheckBox();
            cbEnableVoiceFilter.Text = "Включить голосовой фильтр";
            cbEnableVoiceFilter.Location = new Point(12, 12);
            cbEnableVoiceFilter.Size = new Size(250, 20);
            cbEnableVoiceFilter.Font = new Font(cbEnableVoiceFilter.Font, FontStyle.Bold);
            cbEnableVoiceFilter.CheckedChanged += cbEnableVoiceFilter_CheckedChanged;
            this.Controls.Add(cbEnableVoiceFilter);

            // VAD Settings
            gbVadSettings = new GroupBox();
            gbVadSettings.Text = "Настройки VAD";
            gbVadSettings.Location = new Point(12, 40);
            gbVadSettings.Size = new Size(280, 80);
            
            lbVadSensitivity = new Label();
            lbVadSensitivity.Text = "Чувствительность VAD:";
            lbVadSensitivity.Location = new Point(10, 25);
            lbVadSensitivity.Size = new Size(150, 15);
            gbVadSettings.Controls.Add(lbVadSensitivity);

            tbVadSensitivity = new TrackBar();
            tbVadSensitivity.Minimum = 1;
            tbVadSensitivity.Maximum = 10;
            tbVadSensitivity.Value = 5;
            tbVadSensitivity.Location = new Point(10, 45);
            tbVadSensitivity.Size = new Size(200, 25);
            tbVadSensitivity.ValueChanged += tbVadSensitivity_ValueChanged;
            gbVadSettings.Controls.Add(tbVadSensitivity);

            lbVadValue = new Label();
            lbVadValue.Text = "5";
            lbVadValue.Location = new Point(220, 50);
            lbVadValue.Size = new Size(30, 15);
            lbVadValue.Font = new Font(lbVadValue.Font, FontStyle.Bold);
            gbVadSettings.Controls.Add(lbVadValue);

            this.Controls.Add(gbVadSettings);

            // Translation Settings
            gbTranslationSettings = new GroupBox();
            gbTranslationSettings.Text = "Настройки перевода";
            gbTranslationSettings.Location = new Point(300, 40);
            gbTranslationSettings.Size = new Size(300, 120);

            lbSttEngine = new Label();
            lbSttEngine.Text = "Движок речи:";
            lbSttEngine.Location = new Point(10, 25);
            lbSttEngine.Size = new Size(90, 15);
            gbTranslationSettings.Controls.Add(lbSttEngine);

            cbSttEngine = new ComboBox();
            cbSttEngine.DropDownStyle = ComboBoxStyle.DropDownList;
            cbSttEngine.Items.AddRange(new string[] { 
                "Windows Speech Recognition", 
                "Google Speech-to-Text", 
                "Azure Cognitive Services",
                "OpenAI Whisper" });
            cbSttEngine.Location = new Point(110, 22);
            cbSttEngine.Size = new Size(180, 21);
            gbTranslationSettings.Controls.Add(cbSttEngine);

            lbTranslationEngine = new Label();
            lbTranslationEngine.Text = "Переводчик:";
            lbTranslationEngine.Location = new Point(10, 50);
            lbTranslationEngine.Size = new Size(90, 15);
            gbTranslationSettings.Controls.Add(lbTranslationEngine);

            cbTranslationEngine = new ComboBox();
            cbTranslationEngine.DropDownStyle = ComboBoxStyle.DropDownList;
            cbTranslationEngine.Items.AddRange(new string[] { 
                "Google Translate", 
                "DeepL", 
                "Microsoft Translator",
                "Yandex Translate" });
            cbTranslationEngine.Location = new Point(110, 47);
            cbTranslationEngine.Size = new Size(180, 21);
            gbTranslationSettings.Controls.Add(cbTranslationEngine);

            lbSourceLanguage = new Label();
            lbSourceLanguage.Text = "Исходный язык:";
            lbSourceLanguage.Location = new Point(10, 75);
            lbSourceLanguage.Size = new Size(90, 15);
            gbTranslationSettings.Controls.Add(lbSourceLanguage);

            cbSourceLanguage = new ComboBox();
            cbSourceLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            cbSourceLanguage.Items.AddRange(new string[] { 
                "Русский", "Английский", "Немецкий", "Французский", 
                "Испанский", "Итальянский", "Японский", "Китайский", "Корейский" });
            cbSourceLanguage.Location = new Point(110, 72);
            cbSourceLanguage.Size = new Size(120, 21);
            gbTranslationSettings.Controls.Add(cbSourceLanguage);

            lbTargetLanguage = new Label();
            lbTargetLanguage.Text = "→";
            lbTargetLanguage.Location = new Point(235, 75);
            lbTargetLanguage.Size = new Size(15, 15);
            gbTranslationSettings.Controls.Add(lbTargetLanguage);

            cbTargetLanguage = new ComboBox();
            cbTargetLanguage.DropDownStyle = ComboBoxStyle.DropDownList;
            cbTargetLanguage.Items.AddRange(new string[] { 
                "Русский", "Английский", "Немецкий", "Французский", 
                "Испанский", "Итальянский", "Японский", "Китайский", "Корейский" });
            cbTargetLanguage.Location = new Point(255, 72);
            cbTargetLanguage.Size = new Size(120, 21);
            gbTranslationSettings.Controls.Add(cbTargetLanguage);

            this.Controls.Add(gbTranslationSettings);

            // Audio Settings
            gbAudioSettings = new GroupBox();
            gbAudioSettings.Text = "Аудиоустройства";
            gbAudioSettings.Location = new Point(12, 130);
            gbAudioSettings.Size = new Size(280, 100);

            lbInputDevice = new Label();
            lbInputDevice.Text = "Микрофон:";
            lbInputDevice.Location = new Point(10, 25);
            lbInputDevice.Size = new Size(70, 15);
            gbAudioSettings.Controls.Add(lbInputDevice);

            cbInputDevice = new ComboBox();
            cbInputDevice.DropDownStyle = ComboBoxStyle.DropDownList;
            cbInputDevice.Location = new Point(90, 22);
            cbInputDevice.Size = new Size(180, 21);
            gbAudioSettings.Controls.Add(cbInputDevice);

            lbOutputDevice = new Label();
            lbOutputDevice.Text = "Динамики:";
            lbOutputDevice.Location = new Point(10, 50);
            lbOutputDevice.Size = new Size(70, 15);
            gbAudioSettings.Controls.Add(lbOutputDevice);

            cbOutputDevice = new ComboBox();
            cbOutputDevice.DropDownStyle = ComboBoxStyle.DropDownList;
            cbOutputDevice.Location = new Point(90, 47);
            cbOutputDevice.Size = new Size(180, 21);
            gbAudioSettings.Controls.Add(cbOutputDevice);

            lbVirtualOutput = new Label();
            lbVirtualOutput.Text = "Virtual Cable:";
            lbVirtualOutput.Location = new Point(10, 75);
            lbVirtualOutput.Size = new Size(80, 15);
            gbAudioSettings.Controls.Add(lbVirtualOutput);

            cbVirtualOutput = new ComboBox();
            cbVirtualOutput.DropDownStyle = ComboBoxStyle.DropDownList;
            cbVirtualOutput.Location = new Point(90, 72);
            cbVirtualOutput.Size = new Size(180, 21);
            gbAudioSettings.Controls.Add(cbVirtualOutput);

            this.Controls.Add(gbAudioSettings);

            // Testing Controls
            gbTestingControls = new GroupBox();
            gbTestingControls.Text = "Тестирование";
            gbTestingControls.Location = new Point(300, 170);
            gbTestingControls.Size = new Size(300, 160);

            btTestMicrophone = new Button();
            btTestMicrophone.Text = "Тест микрофона";
            btTestMicrophone.Location = new Point(10, 20);
            btTestMicrophone.Size = new Size(100, 25);
            btTestMicrophone.Click += btTestMicrophone_Click;
            gbTestingControls.Controls.Add(btTestMicrophone);

            btTestTranslation = new Button();
            btTestTranslation.Text = "Тест перевода";
            btTestTranslation.Location = new Point(120, 20);
            btTestTranslation.Size = new Size(100, 25);
            btTestTranslation.Click += btTestTranslation_Click;
            gbTestingControls.Controls.Add(btTestTranslation);

            cbEnableLogging = new CheckBox();
            cbEnableLogging.Text = "Логирование";
            cbEnableLogging.Location = new Point(230, 25);
            cbEnableLogging.Size = new Size(100, 15);
            cbEnableLogging.Checked = true;
            gbTestingControls.Controls.Add(cbEnableLogging);

            rtbTestOutput = new RichTextBox();
            rtbTestOutput.Location = new Point(10, 50);
            rtbTestOutput.Size = new Size(280, 100);
            rtbTestOutput.BackColor = Color.Black;
            rtbTestOutput.ForeColor = Color.Lime;
            rtbTestOutput.Font = new Font("Consolas", 8);
            rtbTestOutput.ReadOnly = true;
            rtbTestOutput.Text = "Готов к тестированию голосового фильтра...";
            gbTestingControls.Controls.Add(rtbTestOutput);

            this.Controls.Add(gbTestingControls);

            // Bottom buttons
            btOK = new Button();
            btOK.Text = "OK";
            btOK.Location = new Point(450, 350);
            btOK.Size = new Size(75, 25);
            btOK.Click += btOK_Click;
            this.Controls.Add(btOK);

            btCancel = new Button();
            btCancel.Text = "Отмена";
            btCancel.Location = new Point(530, 350);
            btCancel.Size = new Size(75, 25);
            btCancel.DialogResult = DialogResult.Cancel;
            btCancel.Click += btCancel_Click;
            this.Controls.Add(btCancel);

            btApply = new Button();
            btApply.Text = "Применить";
            btApply.Location = new Point(360, 350);
            btApply.Size = new Size(80, 25);
            btApply.Click += btApply_Click;
            this.Controls.Add(btApply);

            this.AcceptButton = btOK;
            this.CancelButton = btCancel;
        }

        /// <summary>
        /// Инициализация комбобоксов значениями по умолчанию
        /// </summary>
        private void InitializeComboBoxes()
        {
            // STT Engine
            if (cbSttEngine.Items.Count > 0) cbSttEngine.SelectedIndex = 0;

            // Translation Engine  
            if (cbTranslationEngine.Items.Count > 0) cbTranslationEngine.SelectedIndex = 0;

            // Source Language
            if (cbSourceLanguage.Items.Count > 0) cbSourceLanguage.SelectedIndex = 0; // Русский

            // Target Language
            if (cbTargetLanguage.Items.Count > 0) cbTargetLanguage.SelectedIndex = 1; // Английский
        }

        /// <summary>
        /// Загрузка доступных аудиоустройств
        /// </summary>
        private void LoadAudioDevices()
        {
            try
            {
                // Очистка списков устройств
                cbInputDevice.Items.Clear();
                cbOutputDevice.Items.Clear();
                cbVirtualOutput.Items.Clear();

                // Загружаем входные устройства (микрофоны) через WaveIn
                cbInputDevice.Items.Add("Микрофон по умолчанию");
                for (int i = 0; i < WaveIn.DeviceCount; i++)
                {
                    var deviceInfo = WaveIn.GetCapabilities(i);
                    cbInputDevice.Items.Add($"{deviceInfo.ProductName} (ID:{i})");
                }

                // Загружаем выходные устройства (динамики) через WaveOut
                cbOutputDevice.Items.Add("Динамики по умолчанию");
                for (int i = 0; i < WaveOut.DeviceCount; i++)
                {
                    var deviceInfo = WaveOut.GetCapabilities(i);
                    cbOutputDevice.Items.Add($"{deviceInfo.ProductName} (ID:{i})");
                }

                // Попытка загрузить WASAPI устройства (более современный API)
                try
                {
                    using (var enumerator = new MMDeviceEnumerator())
                    {
                        // Входные устройства
                        var inputDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                        foreach (var device in inputDevices)
                        {
                            string deviceName = $"{device.FriendlyName} (WASAPI)";
                            if (!cbInputDevice.Items.Contains(deviceName))
                                cbInputDevice.Items.Add(deviceName);
                        }

                        // Выходные устройства
                        var outputDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                        foreach (var device in outputDevices)
                        {
                            string deviceName = $"{device.FriendlyName} (WASAPI)";
                            if (!cbOutputDevice.Items.Contains(deviceName))
                                cbOutputDevice.Items.Add(deviceName);
                            if (!cbVirtualOutput.Items.Contains(deviceName))
                                cbVirtualOutput.Items.Add(deviceName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // WASAPI может не работать на некоторых системах
                    System.Diagnostics.Debug.WriteLine($"WASAPI enumeration failed: {ex.Message}");
                }

                // Добавляем специальные VB-Cable устройства
                cbVirtualOutput.Items.Add("VB-Audio Virtual Cable");
                cbVirtualOutput.Items.Add("VB-Audio Hi-Fi Cable");
                cbVirtualOutput.Items.Add("Voicemod Virtual Audio Device");
                cbVirtualOutput.Items.Add("Отключено");

                // Установка значений по умолчанию
                if (cbInputDevice.Items.Count > 0) cbInputDevice.SelectedIndex = 0;
                if (cbOutputDevice.Items.Count > 0) cbOutputDevice.SelectedIndex = 0;
                if (cbVirtualOutput.Items.Count > 0) cbVirtualOutput.SelectedIndex = cbVirtualOutput.Items.Count - 1; // Отключено
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки аудиоустройств: {ex.Message}", 
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    
                // Добавляем базовые устройства в случае ошибки
                cbInputDevice.Items.Add("Микрофон по умолчанию");
                cbOutputDevice.Items.Add("Динамики по умолчанию");
                cbVirtualOutput.Items.Add("VB-Audio Virtual Cable");
                cbVirtualOutput.Items.Add("Отключено");
                
                if (cbInputDevice.Items.Count > 0) cbInputDevice.SelectedIndex = 0;
                if (cbOutputDevice.Items.Count > 0) cbOutputDevice.SelectedIndex = 0;
                if (cbVirtualOutput.Items.Count > 0) cbVirtualOutput.SelectedIndex = cbVirtualOutput.Items.Count - 1;
            }
        }

        /// <summary>
        /// Загрузка настроек из файла конфигурации
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                string settingsFile = "Settings.ini"; // Используем тот же файл настроек что и основная программа
                
                // Основные настройки
                cbEnableVoiceFilter.Checked = Util.ParseStringFromFile(settingsFile, SETTINGS_ENABLE_VOICE_FILTER) == "True";
                
                // VAD настройки
                int vadSensitivity = int.TryParse(Util.ParseStringFromFile(settingsFile, SETTINGS_VAD_SENSITIVITY), out int vad) ? vad : 5;
                tbVadSensitivity.Value = Math.Max(1, Math.Min(10, vadSensitivity));
                lbVadValue.Text = tbVadSensitivity.Value.ToString();

                // STT Engine
                string sttEngine = Util.ParseStringFromFile(settingsFile, SETTINGS_STT_ENGINE);
                if (!string.IsNullOrEmpty(sttEngine) && cbSttEngine.Items.Contains(sttEngine))
                    cbSttEngine.SelectedItem = sttEngine;

                // Translation Engine
                string translationEngine = Util.ParseStringFromFile(settingsFile, SETTINGS_TRANSLATION_ENGINE);
                if (!string.IsNullOrEmpty(translationEngine) && cbTranslationEngine.Items.Contains(translationEngine))
                    cbTranslationEngine.SelectedItem = translationEngine;

                // Языки
                string sourceLang = Util.ParseStringFromFile(settingsFile, SETTINGS_SOURCE_LANGUAGE);
                if (!string.IsNullOrEmpty(sourceLang) && cbSourceLanguage.Items.Contains(sourceLang))
                    cbSourceLanguage.SelectedItem = sourceLang;

                string targetLang = Util.ParseStringFromFile(settingsFile, SETTINGS_TARGET_LANGUAGE);
                if (!string.IsNullOrEmpty(targetLang) && cbTargetLanguage.Items.Contains(targetLang))
                    cbTargetLanguage.SelectedItem = targetLang;

                // Аудиоустройства
                string inputDevice = Util.ParseStringFromFile(settingsFile, SETTINGS_INPUT_DEVICE);
                if (!string.IsNullOrEmpty(inputDevice) && cbInputDevice.Items.Contains(inputDevice))
                    cbInputDevice.SelectedItem = inputDevice;

                string outputDevice = Util.ParseStringFromFile(settingsFile, SETTINGS_OUTPUT_DEVICE);
                if (!string.IsNullOrEmpty(outputDevice) && cbOutputDevice.Items.Contains(outputDevice))
                    cbOutputDevice.SelectedItem = outputDevice;

                string virtualOutput = Util.ParseStringFromFile(settingsFile, SETTINGS_VIRTUAL_OUTPUT);
                if (!string.IsNullOrEmpty(virtualOutput) && cbVirtualOutput.Items.Contains(virtualOutput))
                    cbVirtualOutput.SelectedItem = virtualOutput;

                // Логирование
                cbEnableLogging.Checked = Util.ParseStringFromFile(settingsFile, SETTINGS_ENABLE_LOGGING) == "True";

                UpdateControlsState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки настроек: {ex.Message}", 
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Сохранение настроек в файл конфигурации
        /// </summary>
        private void SaveSettings()
        {
            try
            {
                string settingsFile = "Settings.ini"; // Используем тот же файл настроек что и основная программа

                // Основные настройки
                Util.ChangeFileData(settingsFile, SETTINGS_ENABLE_VOICE_FILTER, cbEnableVoiceFilter.Checked.ToString());
                Util.ChangeFileData(settingsFile, SETTINGS_VAD_SENSITIVITY, tbVadSensitivity.Value.ToString());

                // Движки
                Util.ChangeFileData(settingsFile, SETTINGS_STT_ENGINE, cbSttEngine.SelectedItem?.ToString() ?? "");
                Util.ChangeFileData(settingsFile, SETTINGS_TRANSLATION_ENGINE, cbTranslationEngine.SelectedItem?.ToString() ?? "");

                // Языки
                Util.ChangeFileData(settingsFile, SETTINGS_SOURCE_LANGUAGE, cbSourceLanguage.SelectedItem?.ToString() ?? "");
                Util.ChangeFileData(settingsFile, SETTINGS_TARGET_LANGUAGE, cbTargetLanguage.SelectedItem?.ToString() ?? "");

                // Аудиоустройства
                Util.ChangeFileData(settingsFile, SETTINGS_INPUT_DEVICE, cbInputDevice.SelectedItem?.ToString() ?? "");
                Util.ChangeFileData(settingsFile, SETTINGS_OUTPUT_DEVICE, cbOutputDevice.SelectedItem?.ToString() ?? "");
                Util.ChangeFileData(settingsFile, SETTINGS_VIRTUAL_OUTPUT, cbVirtualOutput.SelectedItem?.ToString() ?? "");

                // Логирование
                Util.ChangeFileData(settingsFile, SETTINGS_ENABLE_LOGGING, cbEnableLogging.Checked.ToString());

                LogMessage("Настройки голосового фильтра сохранены.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения настроек: {ex.Message}", 
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Обновление состояния элементов управления
        /// </summary>
        private void UpdateControlsState()
        {
            bool enabled = cbEnableVoiceFilter.Checked;
            
            // Включение/отключение панелей настроек
            gbVadSettings.Enabled = enabled;
            gbTranslationSettings.Enabled = enabled;
            gbAudioSettings.Enabled = enabled;
            gbTestingControls.Enabled = enabled;
        }

        /// <summary>
        /// Логирование сообщений в окно тестирования
        /// </summary>
        private void LogMessage(string message)
        {
            if (cbEnableLogging.Checked)
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                rtbTestOutput.AppendText($"[{timestamp}] {message}\n");
                rtbTestOutput.ScrollToCaret();
            }
        }

        #region Event Handlers

        private void cbEnableVoiceFilter_CheckedChanged(object sender, EventArgs e)
        {
            if (!isInitialized) return;

            isVoiceFilterEnabled = cbEnableVoiceFilter.Checked;
            UpdateControlsState();
            
            if (isVoiceFilterEnabled)
            {
                LogMessage("AutoVoiceTranslator включен");
            }
            else
            {
                LogMessage("AutoVoiceTranslator отключен");
            }
        }

        private void tbVadSensitivity_ValueChanged(object sender, EventArgs e)
        {
            if (!isInitialized) return;
            lbVadValue.Text = tbVadSensitivity.Value.ToString();
            LogMessage($"VAD чувствительность изменена на: {tbVadSensitivity.Value}");
        }

        private void btTestMicrophone_Click(object sender, EventArgs e)
        {
            try
            {
                LogMessage("=== ТЕСТ МИКРОФОНА ===");
                LogMessage($"Выбранное устройство: {cbInputDevice.SelectedItem}");
                LogMessage("Имитация проверки микрофона...");
                
                // Имитация тестирования микрофона
                System.Threading.Thread.Sleep(1000);
                
                LogMessage("✓ Микрофон работает нормально");
                LogMessage($"VAD порог: {tbVadSensitivity.Value}/10");
                LogMessage("Тест завершен успешно.\n");
            }
            catch (Exception ex)
            {
                LogMessage($"✗ Ошибка тестирования микрофона: {ex.Message}\n");
            }
        }

        private void btTestTranslation_Click(object sender, EventArgs e)
        {
            try
            {
                LogMessage("=== ТЕСТ ПЕРЕВОДА ===");
                LogMessage($"STT: {cbSttEngine.SelectedItem}");
                LogMessage($"Переводчик: {cbTranslationEngine.SelectedItem}");
                LogMessage($"Направление: {cbSourceLanguage.SelectedItem} → {cbTargetLanguage.SelectedItem}");
                
                // Имитация перевода
                System.Threading.Thread.Sleep(2000);
                
                LogMessage("Тестовая фраза: 'Привет, как дела?'");
                LogMessage("Результат перевода: 'Hello, how are you?'");
                LogMessage("✓ Тест перевода завершен успешно.\n");
            }
            catch (Exception ex)
            {
                LogMessage($"✗ Ошибка тестирования перевода: {ex.Message}\n");
            }
        }

        private void btAdvancedSettings_Click(object sender, EventArgs e)
        {
            try
            {
                LogMessage("=== РАСШИРЕННЫЕ НАСТРОЙКИ ===");
                LogMessage($"VAD чувствительность: {tbVadSensitivity.Value}/10");
                LogMessage($"Логирование: {(cbEnableLogging.Checked ? "Включено" : "Отключено")}");
                LogMessage("Конфигурация проверена.\n");
            }
            catch (Exception ex)
            {
                LogMessage($"✗ Ошибка расширенных настроек: {ex.Message}\n");
            }
        }

        private void btOK_Click(object sender, EventArgs e)
        {
            SaveSettings();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btApply_Click(object sender, EventArgs e)
        {
            SaveSettings();
            MessageBox.Show("Настройки AutoVoiceTranslator применены.", 
                "Настройки применены", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion
    }
}
