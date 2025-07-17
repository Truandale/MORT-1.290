using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MORT
{
    public partial class AdvancedAudioSettings : Form
    {
        #region Controls Declaration
        
        // Main container
        private TabControl? mainTabControl;
        
        // Mode Selection Tab
        private GroupBox? gbWorkMode;
        private RadioButton? rbModeOff;
        private RadioButton? rbModeIncoming;
        private RadioButton? rbModeOutgoing;
        private RadioButton? rbModeBidirectional;
        
        // STT Settings Tab
        private GroupBox? gbSTTSettings;
        private ComboBox? cbSTTEngine;
        private ComboBox? cbWhisperModel;
        private ComboBox? cbVoskModel;
        private TrackBar? tbSTTSensitivity;
        private Label? lblSTTSensitivity;
        
        // TTS Settings Tab
        private GroupBox? gbTTSSettings;
        private ComboBox? cbTTSEngine;
        private ComboBox? cbTTSVoiceRU;
        private ComboBox? cbTTSVoiceEN;
        private TrackBar? tbTTSSpeedRU;
        private TrackBar? tbTTSSpeedEN;
        private TrackBar? tbTTSVolumeRU;
        private TrackBar? tbTTSVolumeEN;
        
        // Audio Devices Tab
        private GroupBox? gbAudioDevices;
        private ComboBox? cbMicrophone;
        private ComboBox? cbSpeakers;
        private ComboBox? cbHeadphones;
        private ComboBox? cbVBCable;
        private Button? btnTestMicrophone;
        private Button? btnTestSpeakers;
        private Button? btnTestVBCable;
        
        // VAD Settings Tab
        private GroupBox? gbVADSettings;
        private TrackBar? tbVADThreshold;
        private TrackBar? tbMinDuration;
        private TrackBar? tbSilenceTimeout;
        private CheckBox? cbEnableVAD;
        private Label? lblVADThreshold;
        private Label? lblMinDuration;
        private Label? lblSilenceTimeout;
        
        // Translation Settings Tab
        private GroupBox? gbTranslationSettings;
        private ComboBox? cbTranslationEngine;
        private TextBox? tbLibreTranslateURL;
        private ComboBox? cbSourceLanguage;
        private ComboBox? cbTargetLanguage;
        private Button? btnTestTranslation;
        
        // Monitoring Tab
        private GroupBox? gbMonitoring;
        private TextBox? tbIncomingText;
        private TextBox? tbTranslatedText;
        private TextBox? tbOutgoingText;
        private ProgressBar? pbMicLevel;
        private ProgressBar? pbSpeakerLevel;
        private Label? lblStatus;
        private Label? lblLatency;
        
        // Control Buttons
        private Button? btnStart;
        private Button? btnStop;
        private Button? btnPause;
        private Button? btnApply;
        private Button? btnCancel;
        private Button? btnOK;
        
        // Settings Manager Reference
        private SettingManager? settingManager;
        
        #endregion

        public AdvancedAudioSettings()
        {
            InitializeComponent();
            InitializeCustomControls();
            LoadSettings();
        }

        public AdvancedAudioSettings(SettingManager settingManager)
        {
            this.settingManager = settingManager;
            InitializeComponent();
            InitializeCustomControls();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Form properties
            this.Text = "AutoVoiceTranslator - Настройки";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.LightGray;
            this.ForeColor = Color.Black;
            
            this.ResumeLayout(false);
        }

        private void InitializeCustomControls()
        {
            // Main TabControl
            mainTabControl = new TabControl()
            {
                Location = new Point(10, 10),
                Size = new Size(760, 500),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            // Create tabs
            CreateModeTab();
            CreateSTTTab();
            CreateTTSTab();
            CreateAudioDevicesTab();
            CreateVADTab();
            CreateTranslationTab();
            CreateMonitoringTab();

            this.Controls.Add(mainTabControl);

            // Control buttons
            CreateControlButtons();
        }

        private void CreateModeTab()
        {
            TabPage modeTab = new TabPage("Режим работы");
            
            gbWorkMode = new GroupBox()
            {
                Text = "Выберите режим работы",
                Location = new Point(10, 10),
                Size = new Size(720, 140),
                ForeColor = Color.Black
            };

            rbModeOff = new RadioButton()
            {
                Text = "🔴 Выключен",
                Location = new Point(20, 30),
                Size = new Size(150, 25),
                ForeColor = Color.Black,
                Checked = true
            };

            rbModeIncoming = new RadioButton()
            {
                Text = "📥 Входящий перевод (EN→RU)",
                Location = new Point(20, 60),
                Size = new Size(250, 25),
                ForeColor = Color.Black
            };

            rbModeOutgoing = new RadioButton()
            {
                Text = "📤 Исходящий перевод (RU→EN)",
                Location = new Point(300, 60),
                Size = new Size(250, 25),
                ForeColor = Color.Black
            };

            rbModeBidirectional = new RadioButton()
            {
                Text = "🔄 Двусторонний перевод",
                Location = new Point(20, 90),
                Size = new Size(250, 25),
                ForeColor = Color.Black
            };

            gbWorkMode.Controls.AddRange(new Control[] 
            { 
                rbModeOff, rbModeIncoming, rbModeOutgoing, rbModeBidirectional 
            });
            
            modeTab.Controls.Add(gbWorkMode);
            mainTabControl?.TabPages.Add(modeTab);
        }

        private void CreateSTTTab()
        {
            TabPage sttTab = new TabPage("Распознавание речи (STT)");
            
            gbSTTSettings = new GroupBox()
            {
                Text = "Настройки Speech-to-Text",
                Location = new Point(10, 10),
                Size = new Size(720, 200),
                ForeColor = Color.Black
            };

            // STT Engine
            Label lblSTTEngine = new Label()
            {
                Text = "Движок STT:",
                Location = new Point(20, 30),
                Size = new Size(100, 20),
                ForeColor = Color.Black
            };

            cbSTTEngine = new ComboBox()
            {
                Location = new Point(130, 28),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbSTTEngine.Items.AddRange(new string[] { "Whisper.NET", "Vosk.NET" });
            cbSTTEngine.SelectedIndex = 0;

            // Whisper Model
            Label lblWhisperModel = new Label()
            {
                Text = "Модель Whisper:",
                Location = new Point(20, 65),
                Size = new Size(100, 20),
                ForeColor = Color.Black
            };

            cbWhisperModel = new ComboBox()
            {
                Location = new Point(130, 63),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbWhisperModel.Items.AddRange(new string[] 
            { 
                "tiny (39MB, быстрая)", 
                "base (74MB, базовая)", 
                "small (244MB, рекомендуется)", 
                "medium (769MB, хорошая)", 
                "large (1550MB, лучшая)" 
            });
            cbWhisperModel.SelectedIndex = 2;

            // Vosk Model
            Label lblVoskModel = new Label()
            {
                Text = "Модель Vosk:",
                Location = new Point(20, 100),
                Size = new Size(100, 20),
                ForeColor = Color.Black
            };

            cbVoskModel = new ComboBox()
            {
                Location = new Point(130, 98),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbVoskModel.Items.AddRange(new string[] 
            { 
                "vosk-model-ru-0.42 (Русский)", 
                "vosk-model-en-us-0.22 (Английский)",
                "vosk-model-small-ru-0.22 (Русский малый)",
                "vosk-model-small-en-us-0.15 (Английский малый)"
            });

            // STT Sensitivity
            lblSTTSensitivity = new Label()
            {
                Text = "Чувствительность: 50%",
                Location = new Point(20, 135),
                Size = new Size(150, 20),
                ForeColor = Color.Black
            };

            tbSTTSensitivity = new TrackBar()
            {
                Location = new Point(130, 130),
                Size = new Size(200, 45),
                Minimum = 0,
                Maximum = 100,
                Value = 50,
                TickFrequency = 10
            };
            tbSTTSensitivity.ValueChanged += (s, e) => 
            {
                lblSTTSensitivity.Text = $"Чувствительность: {tbSTTSensitivity.Value}%";
            };

            gbSTTSettings.Controls.AddRange(new Control[] 
            { 
                lblSTTEngine, cbSTTEngine,
                lblWhisperModel, cbWhisperModel,
                lblVoskModel, cbVoskModel,
                lblSTTSensitivity, tbSTTSensitivity
            });
            
            sttTab.Controls.Add(gbSTTSettings);
            mainTabControl?.TabPages.Add(sttTab);
        }

        private void CreateTTSTab()
        {
            TabPage ttsTab = new TabPage("Синтез речи (TTS)");
            
            gbTTSSettings = new GroupBox()
            {
                Text = "Настройки Text-to-Speech",
                Location = new Point(10, 10),
                Size = new Size(720, 250),
                ForeColor = Color.Black
            };

            // TTS Engine
            Label lblTTSEngine = new Label()
            {
                Text = "Движок TTS:",
                Location = new Point(20, 30),
                Size = new Size(100, 20),
                ForeColor = Color.Black
            };

            cbTTSEngine = new ComboBox()
            {
                Location = new Point(130, 28),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbTTSEngine.Items.AddRange(new string[] { "System.Speech", "Azure TTS" });
            cbTTSEngine.SelectedIndex = 0;

            // Russian Voice
            Label lblTTSVoiceRU = new Label()
            {
                Text = "Голос (RU):",
                Location = new Point(20, 65),
                Size = new Size(100, 20),
                ForeColor = Color.Black
            };

            cbTTSVoiceRU = new ComboBox()
            {
                Location = new Point(130, 63),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // English Voice
            Label lblTTSVoiceEN = new Label()
            {
                Text = "Голос (EN):",
                Location = new Point(350, 65),
                Size = new Size(100, 20),
                ForeColor = Color.Black
            };

            cbTTSVoiceEN = new ComboBox()
            {
                Location = new Point(460, 63),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Speed controls
            Label lblSpeedRU = new Label()
            {
                Text = "Скорость RU: 50%",
                Location = new Point(20, 100),
                Size = new Size(120, 20),
                ForeColor = Color.Black
            };

            tbTTSSpeedRU = new TrackBar()
            {
                Location = new Point(130, 95),
                Size = new Size(150, 45),
                Minimum = 10,
                Maximum = 200,
                Value = 100,
                TickFrequency = 20
            };

            Label lblSpeedEN = new Label()
            {
                Text = "Скорость EN: 50%",
                Location = new Point(350, 100),
                Size = new Size(120, 20),
                ForeColor = Color.Black
            };

            tbTTSSpeedEN = new TrackBar()
            {
                Location = new Point(460, 95),
                Size = new Size(150, 45),
                Minimum = 10,
                Maximum = 200,
                Value = 100,
                TickFrequency = 20
            };

            // Volume controls
            Label lblVolumeRU = new Label()
            {
                Text = "Громкость RU: 100%",
                Location = new Point(20, 150),
                Size = new Size(120, 20),
                ForeColor = Color.Black
            };

            tbTTSVolumeRU = new TrackBar()
            {
                Location = new Point(130, 145),
                Size = new Size(150, 45),
                Minimum = 0,
                Maximum = 100,
                Value = 100,
                TickFrequency = 10
            };

            Label lblVolumeEN = new Label()
            {
                Text = "Громкость EN: 100%",
                Location = new Point(350, 150),
                Size = new Size(120, 20),
                ForeColor = Color.Black
            };

            tbTTSVolumeEN = new TrackBar()
            {
                Location = new Point(460, 145),
                Size = new Size(150, 45),
                Minimum = 0,
                Maximum = 100,
                Value = 100,
                TickFrequency = 10
            };

            gbTTSSettings.Controls.AddRange(new Control[] 
            { 
                lblTTSEngine, cbTTSEngine,
                lblTTSVoiceRU, cbTTSVoiceRU,
                lblTTSVoiceEN, cbTTSVoiceEN,
                lblSpeedRU, tbTTSSpeedRU,
                lblSpeedEN, tbTTSSpeedEN,
                lblVolumeRU, tbTTSVolumeRU,
                lblVolumeEN, tbTTSVolumeEN
            });
            
            ttsTab.Controls.Add(gbTTSSettings);
            mainTabControl?.TabPages.Add(ttsTab);
        }

        private void CreateAudioDevicesTab()
        {
            TabPage devicesTab = new TabPage("Аудио устройства");
            
            gbAudioDevices = new GroupBox()
            {
                Text = "Настройки аудио устройств",
                Location = new Point(10, 10),
                Size = new Size(720, 300),
                ForeColor = Color.Black
            };

            // Microphone
            Label lblMicrophone = new Label()
            {
                Text = "🎤 Микрофон:",
                Location = new Point(20, 30),
                Size = new Size(100, 20),
                ForeColor = Color.Black
            };

            cbMicrophone = new ComboBox()
            {
                Location = new Point(130, 28),
                Size = new Size(300, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            btnTestMicrophone = new Button()
            {
                Text = "Тест",
                Location = new Point(440, 27),
                Size = new Size(60, 25),
                ForeColor = Color.Black
            };

            // Speakers
            Label lblSpeakers = new Label()
            {
                Text = "🔊 Динамики:",
                Location = new Point(20, 70),
                Size = new Size(100, 20),
                ForeColor = Color.Black
            };

            cbSpeakers = new ComboBox()
            {
                Location = new Point(130, 68),
                Size = new Size(300, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            btnTestSpeakers = new Button()
            {
                Text = "Тест",
                Location = new Point(440, 67),
                Size = new Size(60, 25),
                ForeColor = Color.Black
            };

            // Headphones
            Label lblHeadphones = new Label()
            {
                Text = "🎧 Наушники:",
                Location = new Point(20, 110),
                Size = new Size(100, 20),
                ForeColor = Color.Black
            };

            cbHeadphones = new ComboBox()
            {
                Location = new Point(130, 108),
                Size = new Size(300, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // VB-Cable
            Label lblVBCable = new Label()
            {
                Text = "🎛️ VB-Cable:",
                Location = new Point(20, 150),
                Size = new Size(100, 20),
                ForeColor = Color.Black
            };

            cbVBCable = new ComboBox()
            {
                Location = new Point(130, 148),
                Size = new Size(300, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            btnTestVBCable = new Button()
            {
                Text = "Тест",
                Location = new Point(440, 147),
                Size = new Size(60, 25),
                ForeColor = Color.Black
            };

            // Status info
            Label lblDeviceInfo = new Label()
            {
                Text = "ℹ️ VB-Cable должен быть установлен и настроен как микрофон в Discord/игре",
                Location = new Point(20, 190),
                Size = new Size(600, 40),
                ForeColor = Color.DarkBlue
            };

            gbAudioDevices.Controls.AddRange(new Control[] 
            { 
                lblMicrophone, cbMicrophone, btnTestMicrophone,
                lblSpeakers, cbSpeakers, btnTestSpeakers,
                lblHeadphones, cbHeadphones,
                lblVBCable, cbVBCable, btnTestVBCable,
                lblDeviceInfo
            });
            
            devicesTab.Controls.Add(gbAudioDevices);
            mainTabControl?.TabPages.Add(devicesTab);
        }

        private void CreateVADTab()
        {
            TabPage vadTab = new TabPage("VAD (Детекция речи)");
            
            gbVADSettings = new GroupBox()
            {
                Text = "Настройки Voice Activity Detection",
                Location = new Point(10, 10),
                Size = new Size(720, 250),
                ForeColor = Color.Black
            };

            cbEnableVAD = new CheckBox()
            {
                Text = "✅ Включить VAD (Silero Neural Network)",
                Location = new Point(20, 30),
                Size = new Size(300, 20),
                ForeColor = Color.Black,
                Checked = true
            };

            // VAD Threshold
            lblVADThreshold = new Label()
            {
                Text = "Порог детекции речи: 0.5",
                Location = new Point(20, 70),
                Size = new Size(200, 20),
                ForeColor = Color.Black
            };

            tbVADThreshold = new TrackBar()
            {
                Location = new Point(230, 65),
                Size = new Size(200, 45),
                Minimum = 10,
                Maximum = 95,
                Value = 50,
                TickFrequency = 5
            };
            tbVADThreshold.ValueChanged += (s, e) => 
            {
                lblVADThreshold.Text = $"Порог детекции речи: {tbVADThreshold.Value / 100.0:F2}";
            };

            // Min Duration
            lblMinDuration = new Label()
            {
                Text = "Минимальная длительность: 0.5 сек",
                Location = new Point(20, 120),
                Size = new Size(200, 20),
                ForeColor = Color.Black
            };

            tbMinDuration = new TrackBar()
            {
                Location = new Point(230, 115),
                Size = new Size(200, 45),
                Minimum = 1,
                Maximum = 30,
                Value = 5,
                TickFrequency = 2
            };
            tbMinDuration.ValueChanged += (s, e) => 
            {
                lblMinDuration.Text = $"Минимальная длительность: {tbMinDuration.Value / 10.0:F1} сек";
            };

            // Silence Timeout
            lblSilenceTimeout = new Label()
            {
                Text = "Таймаут тишины: 2.0 сек",
                Location = new Point(20, 170),
                Size = new Size(200, 20),
                ForeColor = Color.Black
            };

            tbSilenceTimeout = new TrackBar()
            {
                Location = new Point(230, 165),
                Size = new Size(200, 45),
                Minimum = 5,
                Maximum = 100,
                Value = 20,
                TickFrequency = 5
            };
            tbSilenceTimeout.ValueChanged += (s, e) => 
            {
                lblSilenceTimeout.Text = $"Таймаут тишины: {tbSilenceTimeout.Value / 10.0:F1} сек";
            };

            gbVADSettings.Controls.AddRange(new Control[] 
            { 
                cbEnableVAD,
                lblVADThreshold, tbVADThreshold,
                lblMinDuration, tbMinDuration,
                lblSilenceTimeout, tbSilenceTimeout
            });
            
            vadTab.Controls.Add(gbVADSettings);
            mainTabControl?.TabPages.Add(vadTab);
        }

        private void CreateTranslationTab()
        {
            TabPage translationTab = new TabPage("Перевод");
            
            gbTranslationSettings = new GroupBox()
            {
                Text = "Настройки переводчика",
                Location = new Point(10, 10),
                Size = new Size(720, 300),
                ForeColor = Color.Black
            };

            // Translation Engine
            Label lblTranslationEngine = new Label()
            {
                Text = "Движок перевода:",
                Location = new Point(20, 30),
                Size = new Size(120, 20),
                ForeColor = Color.Black
            };

            cbTranslationEngine = new ComboBox()
            {
                Location = new Point(150, 28),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbTranslationEngine.Items.AddRange(new string[] 
            { 
                "LibreTranslate (локальный)", 
                "Google Translate API", 
                "DeepL API" 
            });
            cbTranslationEngine.SelectedIndex = 0;

            // API Keys info (using existing keys from main app)
            Label lblAPIInfo = new Label()
            {
                Text = "ℹ️ API ключи используются из основных настроек программы",
                Location = new Point(20, 70),
                Size = new Size(450, 20),
                ForeColor = Color.DarkBlue,
                Font = new Font("Segoe UI", 9, FontStyle.Italic)
            };

            Button btnOpenMainSettings = new Button()
            {
                Text = "📝 Открыть настройки API",
                Location = new Point(150, 100),
                Size = new Size(180, 30),
                ForeColor = Color.Black
            };
            btnOpenMainSettings.Click += (s, e) => OpenMainAPISettings();

            // LibreTranslate URL
            Label lblLibreTranslate = new Label()
            {
                Text = "LibreTranslate URL:",
                Location = new Point(20, 110),
                Size = new Size(120, 20),
                ForeColor = Color.Black
            };

            tbLibreTranslateURL = new TextBox()
            {
                Location = new Point(150, 108),
                Size = new Size(300, 25),
                Text = "http://localhost:5000"
            };

            // Source Language
            Label lblSourceLang = new Label()
            {
                Text = "Исходный язык:",
                Location = new Point(20, 150),
                Size = new Size(120, 20),
                ForeColor = Color.Black
            };

            cbSourceLanguage = new ComboBox()
            {
                Location = new Point(150, 148),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbSourceLanguage.Items.AddRange(new string[] { "RU", "EN" });
            cbSourceLanguage.SelectedIndex = 0;

            // Target Language
            Label lblTargetLang = new Label()
            {
                Text = "Целевой язык:",
                Location = new Point(290, 150),
                Size = new Size(120, 20),
                ForeColor = Color.Black
            };

            cbTargetLanguage = new ComboBox()
            {
                Location = new Point(410, 148),
                Size = new Size(120, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbTargetLanguage.Items.AddRange(new string[] { "EN", "RU" });
            cbTargetLanguage.SelectedIndex = 0;

            btnTestTranslation = new Button()
            {
                Text = "Тест перевода",
                Location = new Point(150, 190),
                Size = new Size(120, 30),
                ForeColor = Color.Black
            };

            gbTranslationSettings.Controls.AddRange(new Control[] 
            { 
                lblTranslationEngine, cbTranslationEngine,
                lblAPIInfo, btnOpenMainSettings,
                lblLibreTranslate, tbLibreTranslateURL,
                lblSourceLang, cbSourceLanguage,
                lblTargetLang, cbTargetLanguage,
                btnTestTranslation
            });
            
            translationTab.Controls.Add(gbTranslationSettings);
            mainTabControl?.TabPages.Add(translationTab);
        }

        private void CreateMonitoringTab()
        {
            TabPage monitoringTab = new TabPage("Мониторинг");
            
            gbMonitoring = new GroupBox()
            {
                Text = "Мониторинг в реальном времени",
                Location = new Point(10, 10),
                Size = new Size(720, 400),
                ForeColor = Color.Black
            };

            // Status
            lblStatus = new Label()
            {
                Text = "Статус: Остановлен",
                Location = new Point(20, 30),
                Size = new Size(200, 20),
                ForeColor = Color.Red,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            lblLatency = new Label()
            {
                Text = "Задержка: N/A",
                Location = new Point(240, 30),
                Size = new Size(150, 20),
                ForeColor = Color.Black
            };

            // Microphone Level
            Label lblMicLevel = new Label()
            {
                Text = "🎤 Уровень микрофона:",
                Location = new Point(20, 70),
                Size = new Size(150, 20),
                ForeColor = Color.Black
            };

            pbMicLevel = new ProgressBar()
            {
                Location = new Point(180, 68),
                Size = new Size(200, 20),
                Style = ProgressBarStyle.Continuous
            };

            // Speaker Level
            Label lblSpeakerLevel = new Label()
            {
                Text = "🔊 Уровень динамиков:",
                Location = new Point(20, 100),
                Size = new Size(150, 20),
                ForeColor = Color.Black
            };

            pbSpeakerLevel = new ProgressBar()
            {
                Location = new Point(180, 98),
                Size = new Size(200, 20),
                Style = ProgressBarStyle.Continuous
            };

            // Text monitoring
            Label lblIncoming = new Label()
            {
                Text = "Входящий текст (распознанный):",
                Location = new Point(20, 140),
                Size = new Size(250, 20),
                ForeColor = Color.Black
            };

            tbIncomingText = new TextBox()
            {
                Location = new Point(20, 165),
                Size = new Size(680, 60),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.White,
                ForeColor = Color.DarkGreen
            };

            Label lblTranslated = new Label()
            {
                Text = "Переведенный текст:",
                Location = new Point(20, 240),
                Size = new Size(200, 20),
                ForeColor = Color.Black
            };

            tbTranslatedText = new TextBox()
            {
                Location = new Point(20, 265),
                Size = new Size(680, 60),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.White,
                ForeColor = Color.DarkBlue
            };

            gbMonitoring.Controls.AddRange(new Control[] 
            { 
                lblStatus, lblLatency,
                lblMicLevel, pbMicLevel,
                lblSpeakerLevel, pbSpeakerLevel,
                lblIncoming, tbIncomingText,
                lblTranslated, tbTranslatedText
            });
            
            monitoringTab.Controls.Add(gbMonitoring);
            mainTabControl?.TabPages.Add(monitoringTab);
        }

        private void CreateControlButtons()
        {
            // Start/Stop controls
            btnStart = new Button()
            {
                Text = "▶️ Старт",
                Location = new Point(10, 520),
                Size = new Size(80, 35),
                ForeColor = Color.Black,
                BackColor = Color.LightGreen
            };

            btnStop = new Button()
            {
                Text = "⏹️ Стоп",
                Location = new Point(100, 520),
                Size = new Size(80, 35),
                ForeColor = Color.Black,
                BackColor = Color.LightCoral,
                Enabled = false
            };

            btnPause = new Button()
            {
                Text = "⏸️ Пауза",
                Location = new Point(190, 520),
                Size = new Size(80, 35),
                ForeColor = Color.Black,
                BackColor = Color.LightYellow,
                Enabled = false
            };

            // Dialog buttons
            btnOK = new Button()
            {
                Text = "OK",
                Location = new Point(550, 520),
                Size = new Size(80, 35),
                DialogResult = DialogResult.OK,
                ForeColor = Color.Black
            };

            btnCancel = new Button()
            {
                Text = "Отмена",
                Location = new Point(640, 520),
                Size = new Size(80, 35),
                DialogResult = DialogResult.Cancel,
                ForeColor = Color.Black
            };

            btnApply = new Button()
            {
                Text = "Применить",
                Location = new Point(450, 520),
                Size = new Size(90, 35),
                ForeColor = Color.Black
            };

            // Event handlers
            btnStart.Click += BtnStart_Click;
            btnStop.Click += BtnStop_Click;
            btnPause.Click += BtnPause_Click;
            btnApply.Click += BtnApply_Click;
            btnOK.Click += BtnOK_Click;

            this.Controls.AddRange(new Control[] 
            { 
                btnStart, btnStop, btnPause, 
                btnOK, btnCancel, btnApply 
            });
        }

        #region Event Handlers

        private void OpenMainAPISettings()
        {
            try
            {
                // Открываем основное окно настроек программы
                MessageBox.Show("Настройки API ключей находятся в главном окне программы на вкладке 'Переводчик'.\n\n" +
                              "Доступные переводчики:\n" +
                              "• Google Translate API\n" +
                              "• DeepL API\n" +
                              "• Gemini API\n" +
                              "• Naver API\n" +
                              "• LibreTranslate (локальный)",
                              "API Настройки", 
                              MessageBoxButtons.OK, 
                              MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            // Start AutoVoiceTranslator
            if (btnStart != null) btnStart.Enabled = false;
            if (btnStop != null) btnStop.Enabled = true;
            if (btnPause != null) btnPause.Enabled = true;
            if (lblStatus != null)
            {
                lblStatus.Text = "Статус: Запущен";
                lblStatus.ForeColor = Color.LightGreen;
            }
            
            // TODO: Implement start logic
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            // Stop AutoVoiceTranslator
            if (btnStart != null) btnStart.Enabled = true;
            if (btnStop != null) btnStop.Enabled = false;
            if (btnPause != null) btnPause.Enabled = false;
            if (lblStatus != null)
            {
                lblStatus.Text = "Статус: Остановлен";
                lblStatus.ForeColor = Color.Red;
            }
            
            // TODO: Implement stop logic
        }

        private void BtnPause_Click(object sender, EventArgs e)
        {
            // Toggle pause
            if (btnPause?.Text.Contains("Пауза") == true)
            {
                btnPause.Text = "▶️ Продолжить";
                if (lblStatus != null)
                {
                    lblStatus.Text = "Статус: Пауза";
                    lblStatus.ForeColor = Color.Yellow;
                }
            }
            else
            {
                if (btnPause != null) btnPause.Text = "⏸️ Пауза";
                if (lblStatus != null)
                {
                    lblStatus.Text = "Статус: Запущен";
                    lblStatus.ForeColor = Color.LightGreen;
                }
            }
            
            // TODO: Implement pause logic
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            // Apply settings
            SaveSettings();
            MessageBox.Show("Настройки применены!", "Информация", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            // Save and close
            SaveSettings();
            this.Close();
        }

        #endregion

        #region Settings Management

        private void LoadSettings()
        {
            try
            {
                // Создаем файл настроек, если он не существует
                string settingsPath = "AutoVoiceTranslator_Settings.ini";
                if (!File.Exists(settingsPath))
                {
                    SaveDefaultSettings();
                    return;
                }

                // Загружаем настройки из файла
                string[] lines = File.ReadAllLines(settingsPath);
                foreach (string line in lines)
                {
                    if (line.Contains("="))
                    {
                        string[] parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim();
                            string value = parts[1].Trim();
                            
                            ApplySetting(key, value);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки настроек: {ex.Message}", "Ошибка", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SaveSettings()
        {
            try
            {
                string settingsPath = "AutoVoiceTranslator_Settings.ini";
                using (StreamWriter writer = new StreamWriter(settingsPath))
                {
                    writer.WriteLine("[AutoVoiceTranslator Settings]");
                    writer.WriteLine($"WorkMode={GetSelectedWorkMode()}");
                    
                    // STT Settings
                    writer.WriteLine($"STTEngine={cbSTTEngine?.SelectedIndex ?? 0}");
                    writer.WriteLine($"WhisperModel={cbWhisperModel?.SelectedIndex ?? 2}");
                    writer.WriteLine($"VoskModel={cbVoskModel?.SelectedIndex ?? 0}");
                    writer.WriteLine($"STTSensitivity={tbSTTSensitivity?.Value ?? 50}");
                    
                    // TTS Settings
                    writer.WriteLine($"TTSEngine={cbTTSEngine?.SelectedIndex ?? 0}");
                    writer.WriteLine($"TTSVoiceRU={cbTTSVoiceRU?.SelectedIndex ?? 0}");
                    writer.WriteLine($"TTSVoiceEN={cbTTSVoiceEN?.SelectedIndex ?? 0}");
                    writer.WriteLine($"TTSSpeedRU={tbTTSSpeedRU?.Value ?? 100}");
                    writer.WriteLine($"TTSSpeedEN={tbTTSSpeedEN?.Value ?? 100}");
                    writer.WriteLine($"TTSVolumeRU={tbTTSVolumeRU?.Value ?? 100}");
                    writer.WriteLine($"TTSVolumeEN={tbTTSVolumeEN?.Value ?? 100}");
                    
                    // Audio Devices
                    writer.WriteLine($"Microphone={cbMicrophone?.SelectedIndex ?? 0}");
                    writer.WriteLine($"Speakers={cbSpeakers?.SelectedIndex ?? 0}");
                    writer.WriteLine($"Headphones={cbHeadphones?.SelectedIndex ?? 0}");
                    writer.WriteLine($"VBCable={cbVBCable?.SelectedIndex ?? 0}");
                    
                    // VAD Settings
                    writer.WriteLine($"EnableVAD={cbEnableVAD?.Checked ?? true}");
                    writer.WriteLine($"VADThreshold={tbVADThreshold?.Value ?? 50}");
                    writer.WriteLine($"MinDuration={tbMinDuration?.Value ?? 5}");
                    writer.WriteLine($"SilenceTimeout={tbSilenceTimeout?.Value ?? 20}");
                    
                    // Translation Settings
                    writer.WriteLine($"TranslationEngine={cbTranslationEngine?.SelectedIndex ?? 0}");
                    writer.WriteLine($"LibreTranslateURL={tbLibreTranslateURL?.Text ?? "http://localhost:5000"}");
                    writer.WriteLine($"SourceLanguage={cbSourceLanguage?.SelectedIndex ?? 0}");
                    writer.WriteLine($"TargetLanguage={cbTargetLanguage?.SelectedIndex ?? 0}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения настроек: {ex.Message}", "Ошибка", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveDefaultSettings()
        {
            // Сохраняем настройки по умолчанию
            SaveSettings();
        }

        private string GetSelectedWorkMode()
        {
            if (rbModeOff?.Checked == true) return "Off";
            if (rbModeIncoming?.Checked == true) return "Incoming";
            if (rbModeOutgoing?.Checked == true) return "Outgoing";
            if (rbModeBidirectional?.Checked == true) return "Bidirectional";
            return "Off";
        }

        private void ApplySetting(string key, string value)
        {
            try
            {
                switch (key)
                {
                    case "WorkMode":
                        SetWorkMode(value);
                        break;
                    case "STTEngine":
                        if (cbSTTEngine != null && int.TryParse(value, out int sttEngine))
                            cbSTTEngine.SelectedIndex = Math.Max(0, Math.Min(sttEngine, cbSTTEngine.Items.Count - 1));
                        break;
                    case "WhisperModel":
                        if (cbWhisperModel != null && int.TryParse(value, out int whisperModel))
                            cbWhisperModel.SelectedIndex = Math.Max(0, Math.Min(whisperModel, cbWhisperModel.Items.Count - 1));
                        break;
                    case "VoskModel":
                        if (cbVoskModel != null && int.TryParse(value, out int voskModel))
                            cbVoskModel.SelectedIndex = Math.Max(0, Math.Min(voskModel, cbVoskModel.Items.Count - 1));
                        break;
                    case "STTSensitivity":
                        if (tbSTTSensitivity != null && int.TryParse(value, out int sttSens))
                            tbSTTSensitivity.Value = Math.Max(0, Math.Min(sttSens, 100));
                        break;
                    case "TTSEngine":
                        if (cbTTSEngine != null && int.TryParse(value, out int ttsEngine))
                            cbTTSEngine.SelectedIndex = Math.Max(0, Math.Min(ttsEngine, cbTTSEngine.Items.Count - 1));
                        break;
                    case "TTSVoiceRU":
                        if (cbTTSVoiceRU != null && int.TryParse(value, out int ttsVoiceRU))
                            cbTTSVoiceRU.SelectedIndex = Math.Max(0, Math.Min(ttsVoiceRU, cbTTSVoiceRU.Items.Count - 1));
                        break;
                    case "TTSVoiceEN":
                        if (cbTTSVoiceEN != null && int.TryParse(value, out int ttsVoiceEN))
                            cbTTSVoiceEN.SelectedIndex = Math.Max(0, Math.Min(ttsVoiceEN, cbTTSVoiceEN.Items.Count - 1));
                        break;
                    case "TTSSpeedRU":
                        if (tbTTSSpeedRU != null && int.TryParse(value, out int speedRU))
                            tbTTSSpeedRU.Value = Math.Max(10, Math.Min(speedRU, 200));
                        break;
                    case "TTSSpeedEN":
                        if (tbTTSSpeedEN != null && int.TryParse(value, out int speedEN))
                            tbTTSSpeedEN.Value = Math.Max(10, Math.Min(speedEN, 200));
                        break;
                    case "TTSVolumeRU":
                        if (tbTTSVolumeRU != null && int.TryParse(value, out int volumeRU))
                            tbTTSVolumeRU.Value = Math.Max(0, Math.Min(volumeRU, 100));
                        break;
                    case "TTSVolumeEN":
                        if (tbTTSVolumeEN != null && int.TryParse(value, out int volumeEN))
                            tbTTSVolumeEN.Value = Math.Max(0, Math.Min(volumeEN, 100));
                        break;
                    case "Microphone":
                        if (cbMicrophone != null && int.TryParse(value, out int mic))
                            cbMicrophone.SelectedIndex = Math.Max(0, Math.Min(mic, cbMicrophone.Items.Count - 1));
                        break;
                    case "Speakers":
                        if (cbSpeakers != null && int.TryParse(value, out int speakers))
                            cbSpeakers.SelectedIndex = Math.Max(0, Math.Min(speakers, cbSpeakers.Items.Count - 1));
                        break;
                    case "Headphones":
                        if (cbHeadphones != null && int.TryParse(value, out int headphones))
                            cbHeadphones.SelectedIndex = Math.Max(0, Math.Min(headphones, cbHeadphones.Items.Count - 1));
                        break;
                    case "VBCable":
                        if (cbVBCable != null && int.TryParse(value, out int vbcable))
                            cbVBCable.SelectedIndex = Math.Max(0, Math.Min(vbcable, cbVBCable.Items.Count - 1));
                        break;
                    case "EnableVAD":
                        if (cbEnableVAD != null && bool.TryParse(value, out bool enableVAD))
                            cbEnableVAD.Checked = enableVAD;
                        break;
                    case "VADThreshold":
                        if (tbVADThreshold != null && int.TryParse(value, out int vadThreshold))
                            tbVADThreshold.Value = Math.Max(10, Math.Min(vadThreshold, 95));
                        break;
                    case "MinDuration":
                        if (tbMinDuration != null && int.TryParse(value, out int minDuration))
                            tbMinDuration.Value = Math.Max(1, Math.Min(minDuration, 30));
                        break;
                    case "SilenceTimeout":
                        if (tbSilenceTimeout != null && int.TryParse(value, out int silenceTimeout))
                            tbSilenceTimeout.Value = Math.Max(5, Math.Min(silenceTimeout, 100));
                        break;
                    case "TranslationEngine":
                        if (cbTranslationEngine != null && int.TryParse(value, out int transEngine))
                            cbTranslationEngine.SelectedIndex = Math.Max(0, Math.Min(transEngine, cbTranslationEngine.Items.Count - 1));
                        break;
                    case "LibreTranslateURL":
                        if (tbLibreTranslateURL != null)
                            tbLibreTranslateURL.Text = value;
                        break;
                    case "SourceLanguage":
                        if (cbSourceLanguage != null && int.TryParse(value, out int sourceLang))
                            cbSourceLanguage.SelectedIndex = Math.Max(0, Math.Min(sourceLang, cbSourceLanguage.Items.Count - 1));
                        break;
                    case "TargetLanguage":
                        if (cbTargetLanguage != null && int.TryParse(value, out int targetLang))
                            cbTargetLanguage.SelectedIndex = Math.Max(0, Math.Min(targetLang, cbTargetLanguage.Items.Count - 1));
                        break;
                }
            }
            catch (Exception ex)
            {
                // Игнорируем ошибки применения отдельных настроек
                System.Diagnostics.Debug.WriteLine($"Error applying setting {key}={value}: {ex.Message}");
            }
        }

        private void SetWorkMode(string mode)
        {
            switch (mode)
            {
                case "Off":
                    if (rbModeOff != null) rbModeOff.Checked = true;
                    break;
                case "Incoming":
                    if (rbModeIncoming != null) rbModeIncoming.Checked = true;
                    break;
                case "Outgoing":
                    if (rbModeOutgoing != null) rbModeOutgoing.Checked = true;
                    break;
                case "Bidirectional":
                    if (rbModeBidirectional != null) rbModeBidirectional.Checked = true;
                    break;
                default:
                    if (rbModeOff != null) rbModeOff.Checked = true;
                    break;
            }
        }

        #endregion
    }
}
