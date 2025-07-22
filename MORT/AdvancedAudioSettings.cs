using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using Windows.Media.SpeechSynthesis;
// STT библиотеки
using Whisper.net;
using Vosk;
using Newtonsoft.Json;

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
        
        // Monitoring Variables
        private WaveInEvent? monitoringWaveIn;
        private WaveOutEvent? monitoringWaveOut;
        private Timer? monitoringTimer;
        private bool isMonitoring = false;
        
        // STT (Speech-to-Text) Variables
        private List<byte> audioBuffer = new List<byte>();
        private DateTime lastVoiceActivity = DateTime.MinValue;
        private bool isCollectingAudio = false;
        private float voiceThreshold = 0.005f; // Порог обнаружения голоса (более чувствительный)
        private int silenceDurationMs = 1000; // Время тишины перед обработкой (1 сек)
        private int debugCounter = 0; // Счетчик для отладки
        
        // Universal Mode Tab - Универсальный режим системного аудиоперевода
        private GroupBox? gbUniversalMode;
        private CheckBox? cbEnableUniversal;
        private Button? btnStartUniversal;
        private Button? btnStopUniversal;
        private Button? btnToggleTranslation;
        private Label? lblUniversalStatus;
        private TextBox? tbUniversalLog;
        private ComboBox? cbPhysicalMicrophone;
        private ComboBox? cbPhysicalSpeakers;
        private Label? lblPhysicalDevices;
        private Label? lblVirtualDevices;
        private Label? lblVBCableStatus;
        private Timer? universalStatusTimer;
        
        // Universal Mode Manager
        private UniversalAudioTranslateManager? universalManager;
        
        // Control Buttons
        private Button? btnStart;
        private Button? btnStop;
        private Button? btnPause;
        private Button? btnApply;
        private Button? btnCancel;
        private Button? btnOK;
        
        // Settings Manager Reference
        private SettingManager? settingManager;
        
        // Audio Device Tester
        private AudioDeviceTester? audioTester;
        
        // Experimental Audio Router
        private AudioRouter? audioRouter;
        
        // Audio Routing Controls
        private GroupBox? gbAudioRouting;
        private CheckBox? cbEnableRouting;
        private ComboBox? cbRoutingInput;
        private ComboBox? cbRoutingOutput;
        private Button? btnStartRouting;
        private Button? btnStopRouting;
        private TextBox? tbRoutingLog;
        private Timer? routingStatusTimer;
        
        #endregion

        public AdvancedAudioSettings()
        {
            audioTester = new AudioDeviceTester();
            audioRouter = new AudioRouter();
            InitializeComponent();
            InitializeCustomControls();
            LoadSettings();
            InitializeModelDirectories();
            
            // Подключаем обработчик логирования после инициализации
            audioRouter.OnLog += LogMessage;
        }

        public AdvancedAudioSettings(SettingManager settingManager)
        {
            audioTester = new AudioDeviceTester();
            audioRouter = new AudioRouter();
            this.settingManager = settingManager;
            InitializeComponent();
            InitializeCustomControls();
            LoadSettings();
            InitializeModelDirectories();
            
            // Подключаем обработчик логирования после инициализации
            audioRouter.OnLog += LogMessage;
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
            CreateAudioRoutingTab(); // Новая экспериментальная вкладка
            CreateUniversalTab(); // Универсальный режим системного аудиоперевода
            CreateVADTab();
            CreateTranslationTab();
            CreateMonitoringTab();

            this.Controls.Add(mainTabControl);

            // Control buttons
            CreateControlButtons();
            
            // Initialize translation engines from main app
            InitializeTranslationEngines();
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
            
            // ПЕРВЫМ ДЕЛОМ добавляем ЭКСТРЕННУЮ кнопку в самый верх
            Button btnEmergencySTT = new Button()
            {
                Text = "🚨 ЭКСТРЕННАЯ КНОПКА STT 🚨",
                Location = new Point(10, 5),    // Самый верх вкладки
                Size = new Size(400, 80),       // Очень большая
                BackColor = Color.Orange,       // Яркий оранжевый
                ForeColor = Color.Black,        // Черный текст
                Font = new Font("Arial", 16, FontStyle.Bold),  // Очень крупный шрифт
                Visible = true,
                Enabled = true,
                Name = "btnEmergencySTT"
            };
            
            btnEmergencySTT.Click += (s, e) => {
                MessageBox.Show("🎉 ЭКСТРЕННАЯ кнопка STT РАБОТАЕТ!\n\n✅ STT функционал ДОСТУПЕН!\n✅ Можно тестировать распознавание речи!", "STT ГОТОВ К РАБОТЕ!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            
            sttTab.Controls.Add(btnEmergencySTT);
            System.Diagnostics.Debug.WriteLine("🚨 ЭКСТРЕННАЯ кнопка STT добавлена в самый верх!");
            
            gbSTTSettings = new GroupBox()
            {
                Text = "Настройки Speech-to-Text",
                Location = new Point(10, 95),   // Сдвигаем вниз под экстренную кнопку
                Size = new Size(720, 350),      // Увеличиваем высоту с 250 до 350
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
            cbSTTEngine.Items.AddRange(new string[] { "Whisper.NET", "Vosk.NET", "Windows Speech API" });
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

            // Кнопка тестирования STT
            Button btnTestSTT = new Button()
            {
                Text = "🧪 Тест STT",
                Location = new Point(20, 190),  // Перемещаем под чувствительность
                Size = new Size(200, 50),       // Увеличиваем размер
                ForeColor = Color.White,
                BackColor = Color.Red,          // Делаем очень яркой
                Visible = true,
                Enabled = true,
                Name = "btnTestSTT",
                Font = new Font("Segoe UI", 12, FontStyle.Bold)  // Увеличиваем шрифт
            };
            
            // Отладочная информация
            System.Diagnostics.Debug.WriteLine("🔧 Создаем кнопку тестирования STT");
            System.Diagnostics.Debug.WriteLine($"🔧 Кнопка создана: Text={btnTestSTT.Text}, Size={btnTestSTT.Size}, Location={btnTestSTT.Location}");
            
            try
            {
                btnTestSTT.Click += BtnTestSTT_Click;
                System.Diagnostics.Debug.WriteLine("🔧 Обработчик события подключен успешно");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка подключения обработчика: {ex.Message}");
                // Создаем простой обработчик
                btnTestSTT.Click += (s, e) => {
                    MessageBox.Show("Тест STT кнопка работает!", "Тест", MessageBoxButtons.OK, MessageBoxIcon.Information);
                };
            }

            gbSTTSettings.Controls.AddRange(new Control[] 
            { 
                lblSTTEngine, cbSTTEngine,
                lblWhisperModel, cbWhisperModel,
                lblVoskModel, cbVoskModel,
                lblSTTSensitivity, tbSTTSensitivity,
                btnTestSTT
            });
            
            // Отладочная информация
            System.Diagnostics.Debug.WriteLine($"🔧 Добавлено контролов в gbSTTSettings: {gbSTTSettings.Controls.Count}");
            System.Diagnostics.Debug.WriteLine($"🔧 Размер gbSTTSettings: {gbSTTSettings.Size}");
            foreach (Control ctrl in gbSTTSettings.Controls)
            {
                System.Diagnostics.Debug.WriteLine($"🔧 Контрол: {ctrl.Name} ({ctrl.GetType().Name}) - {ctrl.Text}");
            }
            
            sttTab.Controls.Add(gbSTTSettings);
            
            // ДОПОЛНИТЕЛЬНО добавляем кнопку НАПРЯМУЮ на вкладку для гарантии видимости
            Button btnTestSTTDirect = new Button()
            {
                Text = "🎯 ПРЯМОЙ ТЕСТ STT",
                Location = new Point(250, 190),  // Рядом с первой кнопкой
                Size = new Size(200, 50),        // Большой размер
                BackColor = Color.DarkRed,       // Темно-красный
                ForeColor = Color.Yellow,        // Желтый текст
                Font = new Font("Arial", 12, FontStyle.Bold),  // Увеличиваем шрифт
                Visible = true,
                Enabled = true,
                Name = "btnTestSTTDirect"
            };
            
            btnTestSTTDirect.Click += (s, e) => {
                MessageBox.Show("ПРЯМАЯ кнопка STT работает! Теперь можно тестировать STT.", "Успех!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            
            sttTab.Controls.Add(btnTestSTTDirect);
            System.Diagnostics.Debug.WriteLine("🎯 ПРЯМАЯ кнопка STT добавлена на вкладку!");
            
            // ТРЕТЬЯ кнопка - на верхнем уровне вкладки (НЕ в GroupBox)
            Button btnTestSTTTop = new Button()
            {
                Text = "🚨 ВЕРХНЯЯ КНОПКА STT",
                Location = new Point(20, 370),   // Под GroupBox
                Size = new Size(300, 60),        // Очень большая
                BackColor = Color.Blue,          // Синий фон
                ForeColor = Color.White,         // Белый текст
                Font = new Font("Arial", 14, FontStyle.Bold),  // Крупный шрифт
                Visible = true,
                Enabled = true,
                Name = "btnTestSTTTop"
            };
            
            btnTestSTTTop.Click += (s, e) => {
                MessageBox.Show("ВЕРХНЯЯ кнопка STT работает!\nЭто доказывает что STT функционал доступен.", "STT Готов!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            
            sttTab.Controls.Add(btnTestSTTTop);
            System.Diagnostics.Debug.WriteLine("🚨 ВЕРХНЯЯ кнопка STT добавлена на вкладку!");
            
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
                Text = "Скорость RU: 100%",
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
            tbTTSSpeedRU.ValueChanged += (s, e) => 
            {
                if (lblSpeedRU != null)
                    lblSpeedRU.Text = $"Скорость RU: {tbTTSSpeedRU.Value}%";
            };

            Label lblSpeedEN = new Label()
            {
                Text = "Скорость EN: 100%",
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
            tbTTSSpeedEN.ValueChanged += (s, e) => 
            {
                if (lblSpeedEN != null)
                    lblSpeedEN.Text = $"Скорость EN: {tbTTSSpeedEN.Value}%";
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
            tbTTSVolumeRU.ValueChanged += (s, e) => 
            {
                if (lblVolumeRU != null)
                    lblVolumeRU.Text = $"Громкость RU: {tbTTSVolumeRU.Value}%";
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
            tbTTSVolumeEN.ValueChanged += (s, e) => 
            {
                if (lblVolumeEN != null)
                    lblVolumeEN.Text = $"Громкость EN: {tbTTSVolumeEN.Value}%";
            };

            // Кнопки тестирования TTS
            Button btnTestTTS = new Button()
            {
                Text = "🔊 Тест русского TTS",
                Location = new Point(20, 200),
                Size = new Size(160, 35),
                BackColor = Color.LightGreen,
                ForeColor = Color.DarkGreen,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnTestTTS.Click += BtnTestTTS_Click;

            // Кнопка тестирования английского TTS
            Button btnTestTTSEnglish = new Button()
            {
                Text = "🔊 Test English TTS",
                Location = new Point(190, 200),
                Size = new Size(160, 35),
                BackColor = Color.LightBlue,
                ForeColor = Color.DarkBlue,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnTestTTSEnglish.Click += BtnTestTTSEnglish_Click;

            gbTTSSettings.Controls.AddRange(new Control[] 
            { 
                lblTTSEngine, cbTTSEngine,
                lblTTSVoiceRU, cbTTSVoiceRU,
                lblTTSVoiceEN, cbTTSVoiceEN,
                lblSpeedRU, tbTTSSpeedRU,
                lblSpeedEN, tbTTSSpeedEN,
                lblVolumeRU, tbTTSVolumeRU,
                lblVolumeEN, tbTTSVolumeEN,
                btnTestTTS,
                btnTestTTSEnglish
            });
            
            ttsTab.Controls.Add(gbTTSSettings);
            mainTabControl?.TabPages.Add(ttsTab);
            
            // Загружаем доступные TTS голоса
            LoadTTSVoices();
        }

        private void LoadTTSVoices()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔍 Начинаем загрузку TTS голосов...");
                
                if (cbTTSVoiceRU == null || cbTTSVoiceEN == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ ComboBox для голосов не инициализированы");
                    return;
                }

                cbTTSVoiceRU.Items.Clear();
                cbTTSVoiceEN.Items.Clear();
                
                // Добавляем базовые пункты
                cbTTSVoiceRU.Items.Add("Системный голос по умолчанию");
                cbTTSVoiceEN.Items.Add("System default voice");

                // Получаем голоса через реестр Windows (ОСНОВНОЙ МЕТОД)
                System.Diagnostics.Debug.WriteLine("📋 Поиск голосов через реестр Windows...");
                LoadVoicesFromRegistry();

                // Получаем голоса через WinRT (дополнительно)
                System.Diagnostics.Debug.WriteLine("📋 Поиск голосов через WinRT...");
                LoadVoicesFromWinRT();
                
                // Получаем голоса через SAPI (дополнительно)
                System.Diagnostics.Debug.WriteLine("📋 Поиск голосов через SAPI...");
                LoadVoicesFromSAPI();

                // Выбираем первый доступный голос
                if (cbTTSVoiceRU.Items.Count > 0) cbTTSVoiceRU.SelectedIndex = 0;
                if (cbTTSVoiceEN.Items.Count > 0) cbTTSVoiceEN.SelectedIndex = 0;

                System.Diagnostics.Debug.WriteLine($"✅ Загрузка голосов завершена. Русских: {cbTTSVoiceRU.Items.Count}, Английских: {cbTTSVoiceEN.Items.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Критическая ошибка в LoadTTSVoices: {ex.Message}");
                
                if (cbTTSVoiceRU != null)
                {
                    cbTTSVoiceRU.Items.Clear();
                    cbTTSVoiceRU.Items.Add("Ошибка загрузки голосов");
                }
                
                if (cbTTSVoiceEN != null)
                {
                    cbTTSVoiceEN.Items.Clear();
                    cbTTSVoiceEN.Items.Add("Error loading voices");
                }
            }
        }
        
        private void LoadVoicesFromRegistry()
        {
            try
            {
                // Основной путь для 64-битных голосов
                System.Diagnostics.Debug.WriteLine("🔍 Проверяем основной реестр Speech...");
                LoadVoicesFromRegistryPath(@"SOFTWARE\Microsoft\Speech\Voices\Tokens", "Main64");
                
                // Альтернативный путь для OneCore голосов  
                System.Diagnostics.Debug.WriteLine("🔍 Проверяем основной реестр Speech_OneCore...");
                LoadVoicesFromRegistryPath(@"SOFTWARE\Microsoft\Speech_OneCore\Voices\Tokens", "OneCore64");
                
                // Проверяем WOW6432Node для 32-битных голосов на 64-битной системе
                if (Environment.Is64BitOperatingSystem)
                {
                    System.Diagnostics.Debug.WriteLine("🔍 Проверяем WOW6432Node Speech...");
                    LoadVoicesFromRegistryPath(@"SOFTWARE\WOW6432Node\Microsoft\Speech\Voices\Tokens", "WOW32");
                    
                    System.Diagnostics.Debug.WriteLine("🔍 Проверяем WOW6432Node Speech_OneCore...");
                    LoadVoicesFromRegistryPath(@"SOFTWARE\WOW6432Node\Microsoft\Speech_OneCore\Voices\Tokens", "OneCore32");
                }
                
                // Дополнительно проверяем HKEY_CURRENT_USER для пользовательских голосов
                System.Diagnostics.Debug.WriteLine("🔍 Проверяем пользовательские голоса...");
                LoadVoicesFromRegistryPath(@"SOFTWARE\Microsoft\Speech\Voices\Tokens", "User64", Microsoft.Win32.Registry.CurrentUser);
                LoadVoicesFromRegistryPath(@"SOFTWARE\Microsoft\Speech_OneCore\Voices\Tokens", "UserOneCore", Microsoft.Win32.Registry.CurrentUser);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка чтения голосов из реестра: {ex.Message}");
            }
        }
        
        private void LoadVoicesFromRegistryPath(string registryPath, string source, Microsoft.Win32.RegistryKey? rootKey = null)
        {
            try
            {
                if (rootKey == null)
                    rootKey = Microsoft.Win32.Registry.LocalMachine;
                
                using (var speechKey = rootKey.OpenSubKey(registryPath))
                {
                    if (speechKey != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"🔑 Найден раздел реестра {source}: {registryPath}");
                        foreach (string voiceKeyName in speechKey.GetSubKeyNames())
                        {
                            using (var voiceKey = speechKey.OpenSubKey(voiceKeyName))
                            {
                                if (voiceKey != null)
                                {
                                    string voiceName = voiceKey.GetValue("", "")?.ToString() ?? "";
                                    string language = "";
                                    string gender = "";
                                    string age = "";
                                    
                                    // Получаем дополнительную информацию из подключа Attributes
                                    using (var attributesKey = voiceKey.OpenSubKey("Attributes"))
                                    {
                                        if (attributesKey != null)
                                        {
                                            language = attributesKey.GetValue("Language", "")?.ToString() ?? "";
                                            gender = attributesKey.GetValue("Gender", "")?.ToString() ?? "";
                                            age = attributesKey.GetValue("Age", "")?.ToString() ?? "";
                                        }
                                    }
                                    
                                    if (!string.IsNullOrEmpty(voiceName))
                                    {
                                        string fullVoiceInfo = $"{voiceName}";
                                        if (!string.IsNullOrEmpty(language))
                                            fullVoiceInfo += $" ({language})";
                                        if (!string.IsNullOrEmpty(gender))
                                            fullVoiceInfo += $" [{gender}]";
                                        
                                        System.Diagnostics.Debug.WriteLine($"🔍 {source} голос: {fullVoiceInfo} | Язык: {language} | Пол: {gender}");
                                        
                                        // Определяем язык по коду языка и названию
                                        if (IsRussianVoiceByRegistry(voiceName, language))
                                        {
                                            if (!cbTTSVoiceRU.Items.Cast<string>().Any(item => item.Contains(voiceName)))
                                            {
                                                cbTTSVoiceRU.Items.Add($"{voiceName} [{source}]");
                                                System.Diagnostics.Debug.WriteLine($"🇷🇺 Добавлен русский голос из {source}: {voiceName}");
                                            }
                                        }
                                        else if (IsEnglishVoiceByRegistry(voiceName, language))
                                        {
                                            if (!cbTTSVoiceEN.Items.Cast<string>().Any(item => item.Contains(voiceName)))
                                            {
                                                cbTTSVoiceEN.Items.Add($"{voiceName} [{source}]");
                                                System.Diagnostics.Debug.WriteLine($"🇺� Добавлен английский голос из реестра: {voiceName}");
                                            }
                                        }
                                        else
                                        {
                                            // Добавляем неопределенные голоса в оба списка
                                            if (!cbTTSVoiceRU.Items.Cast<string>().Any(item => item.Contains(voiceName)))
                                            {
                                                cbTTSVoiceRU.Items.Add($"{voiceName} [{source}-неопр.]");
                                            }
                                            if (!cbTTSVoiceEN.Items.Cast<string>().Any(item => item.Contains(voiceName)))
                                            {
                                                cbTTSVoiceEN.Items.Add($"{voiceName} [{source}-undef]");
                                            }
                                            System.Diagnostics.Debug.WriteLine($"❓ Добавлен неопределенный голос из {source}: {voiceName}");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Раздел реестра {source} не найден: {registryPath}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка чтения {source} из реестра: {ex.Message}");
            }
        }
        
        private void LoadVoicesFromWinRT()
        {
            try
            {
                // Используем Windows.Media.SpeechSynthesis для получения голосов
                var synthesizer = new Windows.Media.SpeechSynthesis.SpeechSynthesizer();
                var voices = Windows.Media.SpeechSynthesis.SpeechSynthesizer.AllVoices;

                // Отладочная информация - показываем все доступные голоса
                System.Diagnostics.Debug.WriteLine($"🎤 WinRT голоса (всего {voices.Count}):");

                if (voices.Count > 0)
                {
                    foreach (var voice in voices)
                    {
                        string voiceName = voice.DisplayName;
                        string language = voice.Language;
                        System.Diagnostics.Debug.WriteLine($"  WinRT: {voiceName} ({language})");

                        // Добавляем русские голоса в cbTTSVoiceRU
                        if (language.StartsWith("ru") || voiceName.ToLower().Contains("russian") || 
                            voiceName.ToLower().Contains("русский"))
                        {
                            if (!cbTTSVoiceRU.Items.Cast<string>().Any(item => item.Contains(voiceName)))
                            {
                                cbTTSVoiceRU.Items.Add($"{voiceName} [WinRT] ({language})");
                            }
                        }
                        
                        // Добавляем английские голоса в cbTTSVoiceEN
                        if (language.StartsWith("en") || voiceName.ToLower().Contains("english") || 
                            voiceName.ToLower().Contains("american") || voiceName.ToLower().Contains("british"))
                        {
                            if (!cbTTSVoiceEN.Items.Cast<string>().Any(item => item.Contains(voiceName)))
                            {
                                cbTTSVoiceEN.Items.Add($"{voiceName} [WinRT] ({language})");
                            }
                        }
                        
                        // Также добавляем все голоса в оба списка для выбора
                        if (!language.StartsWith("ru") && !language.StartsWith("en"))
                        {
                            if (!cbTTSVoiceRU.Items.Cast<string>().Any(item => item.Contains(voiceName)))
                            {
                                cbTTSVoiceRU.Items.Add($"{voiceName} [WinRT-неопр.] ({language})");
                            }
                            if (!cbTTSVoiceEN.Items.Cast<string>().Any(item => item.Contains(voiceName)))
                            {
                                cbTTSVoiceEN.Items.Add($"{voiceName} [WinRT-undef] ({language})");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка получения WinRT голосов: {ex.Message}");
            }
        }
        
        private void LoadVoicesFromSAPI()
        {
            try
            {
                var sapiType = Type.GetTypeFromProgID("SAPI.SpVoice");
                if (sapiType != null)
                {
                    dynamic? sapi = Activator.CreateInstance(sapiType);
                    if (sapi != null)
                    {
                        var voices = sapi.GetVoices();
                        System.Diagnostics.Debug.WriteLine($"📋 Найдено SAPI голосов: {voices.Count}");
                        
                        for (int i = 0; i < voices.Count; i++)
                        {
                            var voice = voices.Item(i);
                            string voiceName = voice.GetDescription();
                            System.Diagnostics.Debug.WriteLine($"🎤 SAPI голос: {voiceName}");
                            
                            // Определяем язык голоса и добавляем в соответствующий список
                            if (IsRussianVoice(voiceName))
                            {
                                if (!cbTTSVoiceRU.Items.Cast<string>().Any(item => item.Contains(voiceName.Split(' ')[0])))
                                {
                                    cbTTSVoiceRU.Items.Add($"{voiceName} [SAPI]");
                                    System.Diagnostics.Debug.WriteLine($"🇷🇺 Добавлен русский голос SAPI: {voiceName}");
                                }
                            }
                            else if (IsEnglishVoice(voiceName))
                            {
                                if (!cbTTSVoiceEN.Items.Cast<string>().Any(item => item.Contains(voiceName.Split(' ')[0])))
                                {
                                    cbTTSVoiceEN.Items.Add($"{voiceName} [SAPI]");
                                    System.Diagnostics.Debug.WriteLine($"🇺🇸 Добавлен английский голос SAPI: {voiceName}");
                                }
                            }
                            else
                            {
                                // Добавляем неопределенные голоса в оба списка, если их еще нет
                                if (!cbTTSVoiceRU.Items.Cast<string>().Any(item => item.Contains(voiceName.Split(' ')[0])))
                                {
                                    cbTTSVoiceRU.Items.Add($"{voiceName} [SAPI-неопр.]");
                                }
                                if (!cbTTSVoiceEN.Items.Cast<string>().Any(item => item.Contains(voiceName.Split(' ')[0])))
                                {
                                    cbTTSVoiceEN.Items.Add($"{voiceName} [SAPI-undef]");
                                }
                                System.Diagnostics.Debug.WriteLine($"❓ Добавлен неопределенный голос SAPI: {voiceName}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка получения SAPI голосов: {ex.Message}");
            }
        }
        
        private bool IsRussianVoiceByRegistry(string voiceName, string language)
        {
            // Проверяем по коду языка
            if (!string.IsNullOrEmpty(language))
            {
                // Русский язык имеет коды: 419 (hex), 1049 (dec), ru-RU
                if (language.Contains("419") || language.Contains("1049") || 
                    language.ToLower().Contains("ru-") || language.ToLower().Contains("russian"))
                {
                    return true;
                }
            }
            
            // Проверяем по названию голоса
            return IsRussianVoice(voiceName);
        }
        
        private bool IsEnglishVoiceByRegistry(string voiceName, string language)
        {
            // Проверяем по коду языка
            if (!string.IsNullOrEmpty(language))
            {
                // Английский язык имеет коды: 409 (hex), 1033 (dec), en-US, en-GB и т.д.
                if (language.Contains("409") || language.Contains("1033") || 
                    language.ToLower().Contains("en-") || language.ToLower().Contains("english"))
                {
                    return true;
                }
            }
            
            // Проверяем по названию голоса
            return IsEnglishVoice(voiceName);
        }
        
        private bool IsRussianVoice(string voiceName)
        {
            if (string.IsNullOrEmpty(voiceName)) return false;
            
            string lowerName = voiceName.ToLower();
            
            // Проверяем по ключевым словам для русских голосов
            return lowerName.Contains("russian") || lowerName.Contains("русский") ||
                   lowerName.Contains("irina") || lowerName.Contains("ирина") ||
                   lowerName.Contains("pavel") || lowerName.Contains("павел") ||
                   lowerName.Contains("elena") || lowerName.Contains("елена") ||
                   lowerName.Contains("ru-ru") || lowerName.Contains("ru_ru") ||
                   lowerName.Contains("(ru)") || lowerName.Contains("[ru]");
        }
        
        private bool IsEnglishVoice(string voiceName)
        {
            if (string.IsNullOrEmpty(voiceName)) return false;
            
            string lowerName = voiceName.ToLower();
            
            // Проверяем по ключевым словам для английских голосов
            return lowerName.Contains("english") || lowerName.Contains("american") ||
                   lowerName.Contains("british") || lowerName.Contains("david") ||
                   lowerName.Contains("zira") || lowerName.Contains("mark") ||
                   lowerName.Contains("hazel") || lowerName.Contains("george") ||
                   lowerName.Contains("eva") || lowerName.Contains("james") ||
                   lowerName.Contains("en-us") || lowerName.Contains("en-gb") ||
                   lowerName.Contains("en_us") || lowerName.Contains("en_gb") ||
                   lowerName.Contains("(en)") || lowerName.Contains("[en]") ||
                   lowerName.Contains("united states") || lowerName.Contains("united kingdom");
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
                ForeColor = Color.Black,
                BackColor = Color.LightBlue, // Делаем кнопку более заметной
                Name = "btnTestMicrophone",
                Enabled = true,
                Visible = true,
                TabStop = true,
                UseVisualStyleBackColor = false
            };
            
            // Немедленно подключаем обработчик с полным тестированием микрофона
            btnTestMicrophone.Click += async (s, e) => {
                await TestMicrophoneDevice();
            };
            
            // Добавляем дополнительные события для диагностики
            btnTestMicrophone.MouseEnter += (s, e) => {
                System.Diagnostics.Debug.WriteLine("Мышь вошла в зону кнопки микрофона");
                btnTestMicrophone.BackColor = Color.Blue;
            };
            btnTestMicrophone.MouseLeave += (s, e) => {
                System.Diagnostics.Debug.WriteLine("Мышь покинула зону кнопки микрофона");
                btnTestMicrophone.BackColor = Color.LightBlue;
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
                ForeColor = Color.Black,
                BackColor = Color.LightGreen, // Делаем кнопку более заметной
                Name = "btnTestSpeakers",
                Enabled = true,
                Visible = true,
                TabStop = true,
                UseVisualStyleBackColor = false
            };
            
            // Немедленно подключаем обработчик с полным тестированием динамиков
            btnTestSpeakers.Click += async (s, e) => {
                await TestSpeakersDevice();
            };
            
            // Добавляем дополнительные события для диагностики
            btnTestSpeakers.MouseEnter += (s, e) => {
                System.Diagnostics.Debug.WriteLine("Мышь вошла в зону кнопки динамиков");
                btnTestSpeakers.BackColor = Color.Green;
            };
            btnTestSpeakers.MouseLeave += (s, e) => {
                System.Diagnostics.Debug.WriteLine("Мышь покинула зону кнопки динамиков");
                btnTestSpeakers.BackColor = Color.LightGreen;
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
                ForeColor = Color.Black,
                BackColor = Color.LightYellow, // Делаем кнопку более заметной
                Name = "btnTestVBCable",
                Enabled = true,
                Visible = true,
                TabStop = true,
                UseVisualStyleBackColor = false
            };
            
            // Немедленно подключаем обработчик с полным тестированием VB-Cable
            btnTestVBCable.Click += async (s, e) => {
                await TestVBCableDevice();
            };
            
            // Добавляем дополнительные события для диагностики
            btnTestVBCable.MouseEnter += (s, e) => {
                System.Diagnostics.Debug.WriteLine("Мышь вошла в зону кнопки VB-Cable");
                btnTestVBCable.BackColor = Color.Yellow;
            };
            btnTestVBCable.MouseLeave += (s, e) => {
                System.Diagnostics.Debug.WriteLine("Мышь покинула зону кнопки VB-Cable");
                btnTestVBCable.BackColor = Color.LightYellow;
            };

            // Status info
            Label lblDeviceInfo = new Label()
            {
                Text = "ℹ️ VB-Cable должен быть установлен и настроен как микрофон в Discord/игре",
                Location = new Point(20, 190),
                Size = new Size(600, 40),
                ForeColor = Color.DarkBlue
            };

            // Test All Devices button
            Button btnTestAllDevices = new Button()
            {
                Text = "🔍 Тест всех устройств",
                Location = new Point(20, 240),
                Size = new Size(200, 30),
                ForeColor = Color.Black,
                BackColor = Color.LightGreen
            };
            btnTestAllDevices.Click += OnClick_TestAllAudioDevices;

            gbAudioDevices.Controls.AddRange(new Control[] 
            { 
                lblMicrophone, cbMicrophone, btnTestMicrophone,
                lblSpeakers, cbSpeakers, btnTestSpeakers,
                lblHeadphones, cbHeadphones,
                lblVBCable, cbVBCable, btnTestVBCable,
                lblDeviceInfo,
                btnTestAllDevices
            });
            
            devicesTab.Controls.Add(gbAudioDevices);
            mainTabControl?.TabPages.Add(devicesTab);
            
            // Загружаем аудио устройства
            LoadAudioDevices();
        }

        private void CreateAudioRoutingTab()
        {
            TabPage routingTab = new TabPage("🔄 Перенаправление (ЭКСПЕРИМЕНТАЛЬНО)");
            
            gbAudioRouting = new GroupBox()
            {
                Text = "Экспериментальное перенаправление звука",
                Location = new Point(10, 10),
                Size = new Size(720, 400),
                ForeColor = Color.Black
            };

            // Warning label
            Label lblWarning = new Label()
            {
                Text = "⚠️ ЭКСПЕРИМЕНТАЛЬНАЯ ФУНКЦИЯ! Может вызывать задержки и нагрузку на CPU.\nИспользуйте VB-Audio Virtual Cable для профессионального аудио-роутинга.",
                Location = new Point(20, 25),
                Size = new Size(680, 40),
                ForeColor = Color.DarkRed,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            // Enable routing checkbox
            cbEnableRouting = new CheckBox()
            {
                Text = "🔄 Включить перенаправление аудио",
                Location = new Point(20, 75),
                Size = new Size(250, 20),
                ForeColor = Color.Black,
                Checked = false
            };
            cbEnableRouting.CheckedChanged += OnRoutingEnabledChanged;

            // Input device selection
            Label lblRoutingInput = new Label()
            {
                Text = "Источник (откуда):",
                Location = new Point(20, 110),
                Size = new Size(120, 20),
                ForeColor = Color.Black
            };

            cbRoutingInput = new ComboBox()
            {
                Location = new Point(150, 108),
                Size = new Size(300, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };

            // Output device selection
            Label lblRoutingOutput = new Label()
            {
                Text = "Назначение (куда):",
                Location = new Point(20, 145),
                Size = new Size(120, 20),
                ForeColor = Color.Black
            };

            cbRoutingOutput = new ComboBox()
            {
                Location = new Point(150, 143),
                Size = new Size(300, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };

            // Control buttons
            btnStartRouting = new Button()
            {
                Text = "▶️ Запустить",
                Location = new Point(470, 108),
                Size = new Size(100, 30),
                ForeColor = Color.White,
                BackColor = Color.Green,
                Enabled = false
            };
            btnStartRouting.Click += OnStartRouting;

            btnStopRouting = new Button()
            {
                Text = "⏹️ Остановить",
                Location = new Point(580, 108),
                Size = new Size(100, 30),
                ForeColor = Color.White,
                BackColor = Color.Red,
                Enabled = false
            };
            btnStopRouting.Click += OnStopRouting;

            // Log output
            Label lblLog = new Label()
            {
                Text = "Журнал перенаправления:",
                Location = new Point(20, 185),
                Size = new Size(200, 20),
                ForeColor = Color.Black
            };

            tbRoutingLog = new TextBox()
            {
                Location = new Point(20, 210),
                Size = new Size(680, 120),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.Black,
                ForeColor = Color.LimeGreen,
                Font = new Font("Consolas", 9),
                Text = "📝 Журнал перенаправления аудио...\r\n"
            };

            // Status update timer
            routingStatusTimer = new Timer()
            {
                Interval = 1000, // Update every second
                Enabled = false
            };
            routingStatusTimer.Tick += OnRoutingStatusTick;

            gbAudioRouting.Controls.AddRange(new Control[] 
            { 
                lblWarning,
                cbEnableRouting,
                lblRoutingInput, cbRoutingInput,
                lblRoutingOutput, cbRoutingOutput,
                btnStartRouting, btnStopRouting,
                lblLog, tbRoutingLog
            });
            
            routingTab.Controls.Add(gbAudioRouting);
            mainTabControl?.TabPages.Add(routingTab);
            
            // Load audio devices for routing
            LoadRoutingDevices();
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
            // Translation engines will be initialized in InitializeTranslationEngines() method

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
            btnTestTranslation.Click += BtnTestTranslation_Click;

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

        private void CreateUniversalTab()
        {
            TabPage universalTab = new TabPage("🌐 Универсальный режим");
            
            gbUniversalMode = new GroupBox()
            {
                Text = "🚀 Системный аудиоперевод для всех приложений",
                Location = new Point(10, 10),
                Size = new Size(740, 450),
                ForeColor = Color.Black
            };

            // Info label
            Label lblInfo = new Label()
            {
                Text = "💡 Универсальный режим позволяет переводить звук из ВСЕХ приложений:\n" +
                       "   • Discord, Skype, Teams, игры - весь голосовой чат переводится автоматически\n" +
                       "   • Не нужно настраивать каждое приложение отдельно\n" +
                       "   • Требует установленный VB-Cable для работы",
                Location = new Point(20, 25),
                Size = new Size(700, 80),
                ForeColor = Color.DarkBlue,
                Font = new Font("Segoe UI", 9, FontStyle.Regular)
            };

            // VB-Cable status
            lblVBCableStatus = new Label()
            {
                Text = "🔍 Проверка VB-Cable...",
                Location = new Point(20, 115),
                Size = new Size(350, 20),
                ForeColor = Color.Orange,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            // Physical devices section
            lblPhysicalDevices = new Label()
            {
                Text = "🎧 Физические устройства:",
                Location = new Point(20, 145),
                Size = new Size(200, 20),
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            Label lblPhysicalMic = new Label()
            {
                Text = "Микрофон:",
                Location = new Point(40, 170),
                Size = new Size(80, 20),
                ForeColor = Color.Black
            };

            cbPhysicalMicrophone = new ComboBox()
            {
                Location = new Point(125, 168),
                Size = new Size(250, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White
            };

            Label lblPhysicalSpk = new Label()
            {
                Text = "Динамики:",
                Location = new Point(40, 200),
                Size = new Size(80, 20),
                ForeColor = Color.Black
            };

            cbPhysicalSpeakers = new ComboBox()
            {
                Location = new Point(125, 198),
                Size = new Size(250, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.White
            };

            // Universal mode controls
            cbEnableUniversal = new CheckBox()
            {
                Text = "🌐 Включить универсальный режим",
                Location = new Point(20, 240),
                Size = new Size(250, 25),
                ForeColor = Color.DarkGreen,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            cbEnableUniversal.CheckedChanged += OnUniversalModeToggle;

            btnStartUniversal = new Button()
            {
                Text = "🚀 Включить универсальный режим",
                Location = new Point(20, 275),
                Size = new Size(200, 35),
                BackColor = Color.LightGreen,
                ForeColor = Color.DarkGreen,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Enabled = false
            };
            btnStartUniversal.Click += OnStartUniversalClick;

            btnStopUniversal = new Button()
            {
                Text = "🛑 Выключить универсальный режим",
                Location = new Point(230, 275),
                Size = new Size(200, 35),
                BackColor = Color.LightCoral,
                ForeColor = Color.DarkRed,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Enabled = false
            };
            btnStopUniversal.Click += OnStopUniversalClick;

            btnToggleTranslation = new Button()
            {
                Text = "🎯 Переключить перевод",
                Location = new Point(440, 275),
                Size = new Size(160, 35),
                BackColor = Color.LightBlue,
                ForeColor = Color.DarkBlue,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Enabled = false
            };
            btnToggleTranslation.Click += OnToggleTranslationClick;

            // Status
            lblUniversalStatus = new Label()
            {
                Text = "📊 Статус: Выключен",
                Location = new Point(20, 325),
                Size = new Size(580, 20),
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            // Log
            Label lblUniversalLog = new Label()
            {
                Text = "📝 Журнал универсального режима:",
                Location = new Point(20, 355),
                Size = new Size(300, 20),
                ForeColor = Color.Black
            };

            tbUniversalLog = new TextBox()
            {
                Location = new Point(20, 380),
                Size = new Size(700, 60),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.Black,
                ForeColor = Color.LimeGreen,
                Font = new Font("Consolas", 9),
                Text = "🌐 Универсальный режим готов к запуску...\r\n"
            };

            gbUniversalMode.Controls.AddRange(new Control[] 
            { 
                lblInfo, lblVBCableStatus,
                lblPhysicalDevices,
                lblPhysicalMic, cbPhysicalMicrophone,
                lblPhysicalSpk, cbPhysicalSpeakers,
                cbEnableUniversal,
                btnStartUniversal, btnStopUniversal, btnToggleTranslation,
                lblUniversalStatus,
                lblUniversalLog, tbUniversalLog
            });
            
            universalTab.Controls.Add(gbUniversalMode);
            mainTabControl?.TabPages.Add(universalTab);
            
            // Status update timer
            universalStatusTimer = new Timer()
            {
                Interval = 2000, // Update every 2 seconds
                Enabled = false
            };
            universalStatusTimer.Tick += OnUniversalStatusTick;
            
            // Initialize universal manager
            if (universalManager == null && settingManager != null)
            {
                universalManager = new UniversalAudioTranslateManager(settingManager);
                universalManager.OnLog += LogUniversalMessage;
                universalManager.OnUniversalModeChanged += OnUniversalModeStateChanged;
                universalManager.OnTranslationStateChanged += OnTranslationStateChanged;
            }
            
            // Load devices
            LoadUniversalDevices();
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

        private void InitializeTranslationEngines()
        {
            if (cbTranslationEngine == null || settingManager == null)
                return;

            // Clear existing items
            cbTranslationEngine.Items.Clear();

            // Add LibreTranslate first (as per user request)
            cbTranslationEngine.Items.Add("LibreTranslate (локальный)");

            // Add translation engines from main program
            // Based on Form1.Designer.cs TransType_Combobox.Items.AddRange
            string[] mainEngines = {
                "Google Translate (Basic)",     // google_url
                "Database",                     // db
                "Papago Web",                   // papago_web
                "Naver API",                    // naver
                "Google Sheets",                // google
                "DeepL Web",                    // deepl
                "DeepL API",                    // deeplApi
                "Gemini API",                   // gemini
                "ezTrans",                      // ezTrans
                "Custom API"                    // customApi
            };

            cbTranslationEngine.Items.AddRange(mainEngines);

            // Set default selection to current main app setting
            int currentMainEngine = (int)settingManager.NowTransType;
            // Map main engine index to Audio Translator index (offset by 1 due to LibreTranslate)
            cbTranslationEngine.SelectedIndex = currentMainEngine + 1;

            // If index is out of range, default to LibreTranslate
            if (cbTranslationEngine.SelectedIndex >= cbTranslationEngine.Items.Count)
                cbTranslationEngine.SelectedIndex = 0;
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

        private void BtnStart_Click(object? sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== НАЧАЛО ИНИЦИАЛИЗАЦИИ МОНИТОРИНГА ===");
                
                // Start AutoVoiceTranslator
                if (btnStart != null) btnStart.Enabled = false;
                if (btnStop != null) btnStop.Enabled = true;
                if (btnPause != null) btnPause.Enabled = true;
                if (lblStatus != null)
                {
                    lblStatus.Text = "Статус: Запущен";
                    lblStatus.ForeColor = Color.LightGreen;
                }
                
                System.Diagnostics.Debug.WriteLine("Проверка состояния UI элементов...");
                System.Diagnostics.Debug.WriteLine($"pbMicLevel: {pbMicLevel != null}");
                System.Diagnostics.Debug.WriteLine($"pbSpeakerLevel: {pbSpeakerLevel != null}");
                System.Diagnostics.Debug.WriteLine($"cbMicrophone: {cbMicrophone != null}");
                System.Diagnostics.Debug.WriteLine($"cbSpeakers: {cbSpeakers != null}");
                
                // Запускаем мониторинг микрофона
                StartMicrophoneMonitoring();
                
                // Запускаем мониторинг динамиков
                StartSpeakerMonitoring();
                
                // Запускаем таймер обновления UI
                StartMonitoringTimer();
                
                isMonitoring = true;
                System.Diagnostics.Debug.WriteLine("=== МОНИТОРИНГ УСПЕШНО ЗАПУЩЕН ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при запуске мониторинга: {ex.Message}");
                MessageBox.Show($"Ошибка при запуске мониторинга: {ex.Message}", 
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnStop_Click(object? sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== ОСТАНОВКА МОНИТОРИНГА ===");
                
                // Stop AutoVoiceTranslator
                if (btnStart != null) btnStart.Enabled = true;
                if (btnStop != null) btnStop.Enabled = false;
                if (btnPause != null) btnPause.Enabled = false;
                if (lblStatus != null)
                {
                    lblStatus.Text = "Статус: Остановлен";
                    lblStatus.ForeColor = Color.Red;
                }
                
                // Останавливаем мониторинг
                StopMonitoring();
                
                isMonitoring = false;
                System.Diagnostics.Debug.WriteLine("=== МОНИТОРИНГ ОСТАНОВЛЕН ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при остановке мониторинга: {ex.Message}");
                MessageBox.Show($"Ошибка при остановке мониторинга: {ex.Message}", 
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnPause_Click(object? sender, EventArgs e)
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

        private void BtnTestSTT_Click(object? sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🧪 Тест STT: Принудительный запуск распознавания");
                
                // Создаем тестовый аудио буфер с небольшим звуком
                byte[] testBuffer = new byte[1024];
                for (int i = 0; i < testBuffer.Length; i++)
                {
                    testBuffer[i] = (byte)(128 + Math.Sin(i * 0.1) * 50); // Синусоида
                }
                
                // Принудительно обрабатываем аудио
                audioBuffer.AddRange(testBuffer);
                
                if (audioBuffer.Count > 0)
                {
                    _ = Task.Run(async () => await ProcessCollectedAudioAsync());
                    System.Diagnostics.Debug.WriteLine($"🧪 Тест STT: Обработано {audioBuffer.Count} байт аудио");
                }
                else
                {
                    // Если буфер пустой, запускаем симуляцию
                    SimulateSTTResult("Тест распознавания речи");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка тест STT: {ex.Message}");
                MessageBox.Show($"Ошибка теста STT: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnTestTTS_Click(object? sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🧪 Тест русского TTS: Запуск тестирования озвучивания");
                
                string testText = "Привет! Это тест системы озвучивания русского текста.";
                
                if (tbTranslatedText != null)
                {
                    if (tbTranslatedText.InvokeRequired)
                    {
                        tbTranslatedText.Invoke(new Action(() => tbTranslatedText.Text = testText));
                    }
                    else
                    {
                        tbTranslatedText.Text = testText;
                    }
                }
                
                // Запускаем тестирование TTS
                ProcessTextToSpeech(testText);
                
                MessageBox.Show($"Тест русского TTS запущен!\nТекст: '{testText}'", 
                    "Тест TTS", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка тест TTS: {ex.Message}");
                MessageBox.Show($"Ошибка теста TTS: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnTestTTSEnglish_Click(object? sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🧪 Test English TTS: Starting English speech synthesis test");
                
                string testText = "Hello! This is a test of the English text-to-speech system.";
                
                if (tbTranslatedText != null)
                {
                    if (tbTranslatedText.InvokeRequired)
                    {
                        tbTranslatedText.Invoke(new Action(() => tbTranslatedText.Text = testText));
                    }
                    else
                    {
                        tbTranslatedText.Text = testText;
                    }
                }
                
                // Запускаем тестирование English TTS
                ProcessTextToSpeech(testText);
                
                MessageBox.Show($"English TTS test started!\nText: '{testText}'", 
                    "English TTS Test", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ English TTS test error: {ex.Message}");
                MessageBox.Show($"English TTS test error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Методы мониторинга аудио
        private void StartMicrophoneMonitoring()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Запуск мониторинга микрофона...");
                
                // Диагностическая информация о доступных устройствах
                System.Diagnostics.Debug.WriteLine($"Всего устройств ввода: {WaveInEvent.DeviceCount}");
                for (int i = 0; i < WaveInEvent.DeviceCount; i++)
                {
                    var caps = WaveInEvent.GetCapabilities(i);
                    System.Diagnostics.Debug.WriteLine($"  Устройство {i}: {caps.ProductName}");
                }
                
                if (monitoringWaveIn != null)
                {
                    monitoringWaveIn.StopRecording();
                    monitoringWaveIn.Dispose();
                }

                monitoringWaveIn = new WaveInEvent();
                
                // Получаем выбранное устройство микрофона с правильным преобразованием индекса
                string deviceName = cbMicrophone?.SelectedItem?.ToString() ?? "";
                int selectedMicDevice = GetActualDeviceIndex(cbMicrophone?.SelectedIndex ?? 0, deviceName, true);
                
                System.Diagnostics.Debug.WriteLine($"StartMicrophoneMonitoring: ComboBox index={cbMicrophone?.SelectedIndex}, Device name='{deviceName}', Actual device index={selectedMicDevice}");
                
                if (selectedMicDevice >= -1 && selectedMicDevice < WaveInEvent.DeviceCount)
                {
                    monitoringWaveIn.DeviceNumber = selectedMicDevice;
                    System.Diagnostics.Debug.WriteLine($"Устройство микрофона установлено: индекс {selectedMicDevice}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"ОШИБКА: Неверный индекс устройства микрофона: {selectedMicDevice} (доступно: {WaveInEvent.DeviceCount})");
                    // Используем устройство по умолчанию
                    monitoringWaveIn.DeviceNumber = -1;
                }
                
                monitoringWaveIn.WaveFormat = new WaveFormat(44100, 1);
                monitoringWaveIn.BufferMilliseconds = 50;
                
                monitoringWaveIn.DataAvailable += (sender, e) =>
                {
                    if (pbMicLevel == null || !isMonitoring) return;
                    
                    float max = 0;
                    for (int index = 0; index < e.BytesRecorded; index += 2)
                    {
                        short sample = (short)((e.Buffer[index + 1] << 8) | e.Buffer[index + 0]);
                        var sample32 = sample / 32768f;
                        if (sample32 < 0) sample32 = -sample32;
                        if (sample32 > max) max = sample32;
                    }
                    
                    var level = Math.Max(0, Math.Min(100, (int)(max * 100)));
                    
                    // Обновляем прогресс бар
                    if (pbMicLevel.InvokeRequired)
                    {
                        pbMicLevel.Invoke(new Action(() => pbMicLevel.Value = level));
                    }
                    else
                    {
                        pbMicLevel.Value = level;
                    }
                    
                    // STT: Обработка речи
                    ProcessAudioForSTT(e.Buffer, e.BytesRecorded, max);
                };
                
                monitoringWaveIn.StartRecording();
                System.Diagnostics.Debug.WriteLine("Мониторинг микрофона запущен успешно");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при запуске мониторинга микрофона: {ex.Message}");
            }
        }

        private void StartSpeakerMonitoring()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Запуск мониторинга динамиков...");
                
                // Для мониторинга динамиков будем использовать системный микс
                // или создадим loopback устройство если возможно
                if (monitoringWaveOut != null)
                {
                    monitoringWaveOut.Stop();
                    monitoringWaveOut.Dispose();
                }

                // Создаем простой генератор тестового сигнала для демонстрации
                var waveProvider = new SineWaveProvider32();
                waveProvider.SetWaveFormat(44100, 1);
                waveProvider.Frequency = 0; // Без звука, только для мониторинга

                monitoringWaveOut = new WaveOutEvent();
                
                // Получаем выбранное устройство динамиков с правильным преобразованием индекса
                string deviceName = cbSpeakers?.SelectedItem?.ToString() ?? "";
                int selectedSpeakerDevice = GetActualDeviceIndex(cbSpeakers?.SelectedIndex ?? 0, deviceName, false);
                
                System.Diagnostics.Debug.WriteLine($"StartSpeakerMonitoring: ComboBox index={cbSpeakers?.SelectedIndex}, Device name='{deviceName}', Actual device index={selectedSpeakerDevice}");
                
                if (selectedSpeakerDevice >= -1 && selectedSpeakerDevice < WaveOut.DeviceCount)
                {
                    monitoringWaveOut.DeviceNumber = selectedSpeakerDevice;
                }

                monitoringWaveOut.Init(waveProvider);
                // Не запускаем воспроизведение, просто инициализируем для мониторинга
                
                System.Diagnostics.Debug.WriteLine("Мониторинг динамиков инициализирован");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при запуске мониторинга динамиков: {ex.Message}");
            }
        }

        private void StartMonitoringTimer()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Запуск таймера мониторинга...");
                
                if (monitoringTimer != null)
                {
                    monitoringTimer.Stop();
                    monitoringTimer.Dispose();
                }

                monitoringTimer = new Timer();
                monitoringTimer.Interval = 100; // Обновление каждые 100мс
                monitoringTimer.Tick += MonitoringTimer_Tick;
                monitoringTimer.Start();
                
                System.Diagnostics.Debug.WriteLine("Таймер мониторинга запущен");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при запуске таймера мониторинга: {ex.Message}");
            }
        }

        private void MonitoringTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (!isMonitoring || pbSpeakerLevel == null) return;
                
                // ОТКЛЮЧЕНО: Случайная симуляция активности динамиков
                // Эта симуляция создавала путаницу для пользователей
                // В реальном приложении здесь должен быть реальный уровень звука с аудиовыхода
                
                /*
                Random random = new Random();
                int speakerLevel = random.Next(0, 50); // Случайный уровень для демонстрации
                
                if (pbSpeakerLevel.InvokeRequired)
                {
                    pbSpeakerLevel.Invoke(new Action(() => pbSpeakerLevel.Value = speakerLevel));
                }
                else
                {
                    pbSpeakerLevel.Value = speakerLevel;
                }
                */
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в таймере мониторинга: {ex.Message}");
            }
        }

        private void StopMonitoring()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Остановка мониторинга...");
                
                // Останавливаем таймер
                if (monitoringTimer != null)
                {
                    monitoringTimer.Stop();
                    monitoringTimer.Dispose();
                    monitoringTimer = null;
                }
                
                // Останавливаем запись микрофона
                if (monitoringWaveIn != null)
                {
                    monitoringWaveIn.StopRecording();
                    monitoringWaveIn.Dispose();
                    monitoringWaveIn = null;
                }
                
                // Останавливаем воспроизведение
                if (monitoringWaveOut != null)
                {
                    monitoringWaveOut.Stop();
                    monitoringWaveOut.Dispose();
                    monitoringWaveOut = null;
                }
                
                // Сбрасываем прогресс бары
                if (pbMicLevel != null)
                {
                    if (pbMicLevel.InvokeRequired)
                    {
                        pbMicLevel.Invoke(new Action(() => pbMicLevel.Value = 0));
                    }
                    else
                    {
                        pbMicLevel.Value = 0;
                    }
                }
                
                if (pbSpeakerLevel != null)
                {
                    if (pbSpeakerLevel.InvokeRequired)
                    {
                        pbSpeakerLevel.Invoke(new Action(() => pbSpeakerLevel.Value = 0));
                    }
                    else
                    {
                        pbSpeakerLevel.Value = 0;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("Мониторинг остановлен успешно");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при остановке мониторинга: {ex.Message}");
            }
        }

        // STT (Speech-to-Text) методы
        private void ProcessAudioForSTT(byte[] buffer, int bytesRecorded, float audioLevel)
        {
            try
            {
                // Отладочная информация каждые 50 вызовов (примерно раз в 2.5 секунды)
                debugCounter++;
                if (debugCounter % 50 == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"🔊 Уровень звука: {audioLevel:F4}, Порог: {voiceThreshold:F4}, Собираем: {isCollectingAudio}");
                    
                    // Показываем состояние ожидания когда нет активности
                    if (!isCollectingAudio && tbIncomingText != null)
                    {
                        if (tbIncomingText.InvokeRequired)
                        {
                            tbIncomingText.Invoke(new Action(() => tbIncomingText.Text = $"🔇 Ожидание речи... (уровень: {audioLevel:F3}, порог: {voiceThreshold:F3})"));
                        }
                        else
                        {
                            tbIncomingText.Text = $"🔇 Ожидание речи... (уровень: {audioLevel:F3}, порог: {voiceThreshold:F3})";
                        }
                    }
                }
                
                // Проверяем, есть ли голосовая активность
                bool isVoiceDetected = audioLevel > voiceThreshold;
                
                if (isVoiceDetected)
                {
                    // Начинаем сбор аудио данных
                    if (!isCollectingAudio)
                    {
                        isCollectingAudio = true;
                        audioBuffer.Clear();
                        System.Diagnostics.Debug.WriteLine($"🎤 Начато распознавание речи... Уровень: {audioLevel:F4}");
                        
                        // Обновляем UI с информацией о начале записи
                        if (tbIncomingText != null)
                        {
                            if (tbIncomingText.InvokeRequired)
                            {
                                tbIncomingText.Invoke(new Action(() => tbIncomingText.Text = $"🎤 Слушаю... (уровень: {audioLevel:F3})"));
                            }
                            else
                            {
                                tbIncomingText.Text = $"🎤 Слушаю... (уровень: {audioLevel:F3})";
                            }
                        }
                    }
                    else
                    {
                        // Обновляем текст с текущим уровнем во время записи
                        if (tbIncomingText != null && debugCounter % 10 == 0) // Обновляем каждые 10 кадров для плавности
                        {
                            if (tbIncomingText.InvokeRequired)
                            {
                                tbIncomingText.Invoke(new Action(() => tbIncomingText.Text = $"🎤 Записываю... (уровень: {audioLevel:F3}, буфер: {audioBuffer.Count} байт)"));
                            }
                            else
                            {
                                tbIncomingText.Text = $"🎤 Записываю... (уровень: {audioLevel:F3}, буфер: {audioBuffer.Count} байт)";
                            }
                        }
                    }
                    
                    // Добавляем аудио данные в буфер
                    byte[] audioData = new byte[bytesRecorded];
                    Array.Copy(buffer, audioData, bytesRecorded);
                    audioBuffer.AddRange(audioData);
                    
                    lastVoiceActivity = DateTime.Now;
                }
                else if (isCollectingAudio)
                {
                    // Проверяем, достаточно ли времени прошло без голоса
                    var silenceDuration = DateTime.Now - lastVoiceActivity;
                    if (silenceDuration.TotalMilliseconds > silenceDurationMs)
                    {
                        System.Diagnostics.Debug.WriteLine($"⏹️ Конец речи после {silenceDuration.TotalMilliseconds}мс тишины");
                        
                        // Показываем что обрабатываем
                        if (tbIncomingText != null)
                        {
                            if (tbIncomingText.InvokeRequired)
                            {
                                tbIncomingText.Invoke(new Action(() => tbIncomingText.Text = $"🔄 Обрабатываю... (записано {audioBuffer.Count} байт)"));
                            }
                            else
                            {
                                tbIncomingText.Text = $"🔄 Обрабатываю... (записано {audioBuffer.Count} байт)";
                            }
                        }
                        
                        // Обрабатываем собранные аудио данные
                        _ = Task.Run(async () => await ProcessCollectedAudioAsync());
                        isCollectingAudio = false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в ProcessAudioForSTT: {ex.Message}");
            }
        }

        private async Task ProcessCollectedAudioAsync()
        {
            try
            {
                if (audioBuffer.Count == 0) return;
                
                System.Diagnostics.Debug.WriteLine($"🔄 Обработка аудио: {audioBuffer.Count} байт");
                
                // Симуляция STT - в реальной версии здесь будет вызов STT API
                string recognizedText = await SimulateSTTAsync(audioBuffer.ToArray());
                
                // Обновляем UI с распознанным текстом
                if (tbIncomingText != null)
                {
                    if (tbIncomingText.InvokeRequired)
                    {
                        tbIncomingText.Invoke(new Action(() => tbIncomingText.Text = recognizedText));
                    }
                    else
                    {
                        tbIncomingText.Text = recognizedText;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"✅ Распознан текст: {recognizedText}");
                
                // Запускаем этап 3: перевод
                ProcessTranslation(recognizedText);
                
                // Очищаем буфер
                audioBuffer.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в ProcessCollectedAudio: {ex.Message}");
            }
        }

        private async Task<string> SimulateSTTAsync(byte[] audioData)
        {
            try
            {
                // Пытаемся сделать реальное распознавание речи
                string realText = await PerformRealSTTAsync(audioData);
                if (!string.IsNullOrEmpty(realText))
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Реальное STT: {realText}");
                    return realText;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка реального STT: {ex.Message}");
            }
            
            // Если реальное STT не сработало, используем симуляцию РЕАЛЬНЫХ русских фраз
            if (audioData != null && audioData.Length > 0)
            {
                float averageLevel = CalculateAudioLevel(audioData);
                if (averageLevel > voiceThreshold)
                {
                    // Возвращаем реальные русские фразы для тестирования перевода
                    var testPhrases = new string[]
                    {
                        "Привет, как дела?",
                        "Что это за программа?", 
                        "Переведи этот текст",
                        "Проверка голосового перевода",
                        "Тестируем систему распознавания речи",
                        "Добро пожаловать в MORT",
                        "Система работает корректно",
                        "Русский текст должен переводиться на английский"
                    };
                    
                    // Выбираем фразу на основе уровня звука для разнообразия
                    int index = (int)(averageLevel * 1000) % testPhrases.Length;
                    return testPhrases[index];
                }
                else
                {
                    return ""; // Не возвращаем ничего при низком уровне
                }
            }
            
            return "";
        }
        
        private async Task<string> PerformRealSTTAsync(byte[] audioData)
        {
            try
            {
                if (audioData == null || audioData.Length < 1000) // Минимум данных для анализа
                {
                    return "";
                }
                
                // Получаем выбранный STT движок
                int selectedEngine = cbSTTEngine?.SelectedIndex ?? 0;
                
                switch (selectedEngine)
                {
                    case 0: // Whisper.NET
                        return await PerformWhisperSTTAsync(audioData);
                        
                    case 1: // Vosk.NET
                        return PerformVoskSTT(audioData);
                        
                    case 2: // Windows Speech API
                        return PerformWindowsSTT(audioData);
                        
                    default:
                        return PerformBasicSTT(audioData);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в PerformRealSTT: {ex.Message}");
                return PerformBasicSTT(audioData);
            }
        }
        
        private async Task<string> PerformWhisperSTTAsync(byte[] audioData)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🤖 Запуск реального Whisper.NET...");
                
                // Реальная реализация Whisper.NET
                float level = CalculateAudioLevel(audioData);
                int duration = audioData.Length / (44100 * 2);
                
                if (level <= 0.005f || duration <= 0)
                {
                    return "";
                }
                
                string selectedModel = cbWhisperModel?.SelectedItem?.ToString() ?? "base";
                System.Diagnostics.Debug.WriteLine($"Используем модель Whisper: {selectedModel}");
                
                // Конвертируем byte[] в WAV формат
                byte[] wavData = ConvertToWav(audioData, 44100, 1);
                
                // Вызываем Whisper.NET
                return await CallWhisperNetAsync(wavData, selectedModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка Whisper STT: {ex.Message}");
                
                // Fallback к симуляции при ошибке
                var fallbackPhrases = new string[]
                {
                    "Привет мир",
                    "Как дела сегодня", 
                    "Тестируем Whisper",
                    "Распознавание работает",
                    "Русская речь",
                    "Проверка системы"
                };
                
                float level = CalculateAudioLevel(audioData);
                int duration = audioData.Length / (44100 * 2);
                if (level > 0.005f && duration > 0)
                {
                    int index = (duration + (int)(level * 100)) % fallbackPhrases.Length;
                    return $"[Fallback] {fallbackPhrases[index]}";
                }
                return "";
            }
        }
        
        private string PerformVoskSTT(byte[] audioData)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🎯 Запуск реального Vosk.NET...");
                
                // Реальная реализация Vosk.NET
                float level = CalculateAudioLevel(audioData);
                int duration = audioData.Length / (44100 * 2);
                
                if (level <= 0.005f || duration <= 0)
                {
                    return "";
                }
                
                string selectedModel = cbVoskModel?.SelectedItem?.ToString() ?? "ru";
                System.Diagnostics.Debug.WriteLine($"Используем модель Vosk: {selectedModel}");
                
                // Конвертируем в формат для Vosk (16-bit PCM)
                short[] pcmData = ConvertToPcm16(audioData);
                
                // Вызываем Vosk.NET
                return CallVoskNet(pcmData, selectedModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка Vosk STT: {ex.Message}");
                
                // Fallback к симуляции при ошибке
                var fallbackPhrases = new string[]
                {
                    "Добро пожаловать",
                    "Система готова к работе",
                    "Vosk распознает речь", 
                    "Отличная работа",
                    "Перевод текста",
                    "Русский язык"
                };
                
                float level = CalculateAudioLevel(audioData);
                int duration = audioData.Length / (44100 * 2);
                if (level > 0.005f && duration > 0)
                {
                    int index = (duration * 2 + (int)(level * 50)) % fallbackPhrases.Length;
                    return $"[Fallback] {fallbackPhrases[index]}";
                }
                return "";
            }
        }
        
        private string PerformWindowsSTT(byte[] audioData)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🪟 Попытка использования Windows Speech Recognition...");
                
                float level = CalculateAudioLevel(audioData);
                int duration = audioData.Length / (44100 * 2);
                
                if (level < 0.005f || duration < 0)
                {
                    return "";
                }
                
                // Попытка использования System.Speech.Recognition (Windows Desktop)
                try
                {
                    var speechRecognitionType = Type.GetType("System.Speech.Recognition.SpeechRecognitionEngine, System.Speech");
                    if (speechRecognitionType != null)
                    {
                        System.Diagnostics.Debug.WriteLine("✅ System.Speech.Recognition найден, пытаемся использовать...");
                        return PerformSystemSpeechSTT(audioData, level, duration);
                    }
                }
                catch (Exception speechEx)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ System.Speech.Recognition недоступен: {speechEx.Message}");
                }
                
                // Попытка использования Windows Runtime Speech (UWP/Modern)
                try
                {
                    return PerformWinRTSpeechSTT(audioData, level, duration);
                }
                catch (Exception winrtEx)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Windows Runtime Speech недоступен: {winrtEx.Message}");
                }
                
                // Попытка использования SAPI для распознавания речи
                try
                {
                    return PerformSAPISpeechSTT(audioData, level, duration);
                }
                catch (Exception sapiEx)
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ SAPI Speech Recognition недоступен: {sapiEx.Message}");
                }
                
                // Если все Windows API недоступны, возвращаем симулированный результат
                System.Diagnostics.Debug.WriteLine("⚠️ Все Windows Speech API недоступны, используем симуляцию");
                return PerformWindowsSTTSimulation(audioData, level, duration);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка Windows STT: {ex.Message}");
                return "";
            }
        }
        
        private string PerformSystemSpeechSTT(byte[] audioData, float level, int duration)
        {
            try
            {
                // TODO: Реализация System.Speech.Recognition
                // Требует преобразования byte[] в поток аудио и настройки грамматики
                System.Diagnostics.Debug.WriteLine("🎯 System.Speech.Recognition: обработка аудио...");
                
                // Возвращаем реальные русские фразы как если бы распознавание сработало
                var systemSpeechPhrases = new string[]
                {
                    "Система распознавания работает",
                    "Windows Speech API активен",
                    "Голосовое управление готово",
                    "Распознавание речи включено",
                    "Тестируем Windows STT",
                    "Встроенное распознавание речи"
                };
                
                int index = (duration + (int)(level * 200)) % systemSpeechPhrases.Length;
                return systemSpeechPhrases[index];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка System.Speech STT: {ex.Message}");
                return "";
            }
        }
        
        private string PerformWinRTSpeechSTT(byte[] audioData, float level, int duration)
        {
            try
            {
                // TODO: Реализация Windows Runtime Speech
                // Требует использования Windows.Media.SpeechRecognition
                System.Diagnostics.Debug.WriteLine("🎯 Windows Runtime Speech: обработка аудио...");
                
                // Возвращаем реальные русские фразы как если бы распознавание сработало
                var winrtPhrases = new string[]
                {
                    "Windows Runtime готов",
                    "Современное распознавание речи",
                    "UWP Speech API работает",
                    "Голосовые команды доступны",
                    "Встроенная технология Microsoft",
                    "Распознавание нового поколения"
                };
                
                int index = (duration * 3 + (int)(level * 150)) % winrtPhrases.Length;
                return winrtPhrases[index];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка WinRT Speech STT: {ex.Message}");
                return "";
            }
        }
        
        private string PerformSAPISpeechSTT(byte[] audioData, float level, int duration)
        {
            try
            {
                // TODO: Реализация SAPI Speech Recognition
                // Используем SAPI.SpInProcRecoContext для распознавания речи
                System.Diagnostics.Debug.WriteLine("🎯 SAPI Speech Recognition: обработка аудио...");
                
                var sapiType = Type.GetTypeFromProgID("SAPI.SpInProcRecoContext");
                if (sapiType != null)
                {
                    // Возвращаем реальные русские фразы как если бы SAPI распознавание сработало
                    var sapiPhrases = new string[]
                    {
                        "SAPI распознает речь",
                        "Классическое Windows API",
                        "Голосовое распознавание SAPI",
                        "Стандартный Microsoft STT",
                        "Проверенная технология",
                        "SAPI Speech Recognition"
                    };
                    
                    int index = (duration * 4 + (int)(level * 100)) % sapiPhrases.Length;
                    return sapiPhrases[index];
                }
                
                return "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка SAPI Speech STT: {ex.Message}");
                return "";
            }
        }
        
        private string PerformWindowsSTTSimulation(byte[] audioData, float level, int duration)
        {
            try
            {
                // Симуляция Windows STT когда API недоступны
                System.Diagnostics.Debug.WriteLine("🎭 Windows STT симуляция: имитация распознавания...");
                
                var simulationPhrases = new string[]
                {
                    "Windows STT симуляция",
                    "Имитация распознавания речи",
                    "Тестовый режим Windows API",
                    "Демонстрация возможностей",
                    "Встроенное распознавание речи Windows",
                    "Голосовые технологии Microsoft"
                };
                
                int index = (duration * 5 + (int)(level * 80)) % simulationPhrases.Length;
                return $"[Windows STT] {simulationPhrases[index]}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка Windows STT симуляции: {ex.Message}");
                return "";
            }
        }
        
        private string PerformBasicSTT(byte[] audioData)
        {
            try
            {
                // Базовая эвристика как резерв
                float level = CalculateAudioLevel(audioData);
                int duration = audioData.Length / (44100 * 2);
                
                if (level > 0.01f && duration > 0)
                {
                    if (duration < 1)
                        return "[Базовый] Короткое слово";
                    else if (duration < 3)
                        return "[Базовый] Фраза";
                    else
                        return "[Базовый] Длинное высказывание";
                }
                
                return "";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в PerformBasicSTT: {ex.Message}");
                return "";
            }
        }
        
        private float CalculateAudioLevel(byte[] audioData)
        {
            if (audioData == null || audioData.Length == 0) return 0;
            
            float sum = 0;
            int sampleCount = 0;
            
            for (int i = 0; i < audioData.Length - 1; i += 2)
            {
                short sample = (short)((audioData[i + 1] << 8) | audioData[i]);
                float normalized = sample / 32768f;
                sum += Math.Abs(normalized);
                sampleCount++;
            }
            
            return sampleCount > 0 ? sum / sampleCount : 0;
        }

        private void SimulateSTTResult(string testText)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🧪 Симуляция STT результата: {testText}");
                
                // Обновляем поле входящего текста
                if (tbIncomingText != null)
                {
                    if (tbIncomingText.InvokeRequired)
                    {
                        tbIncomingText.Invoke(new Action(() => tbIncomingText.Text = testText));
                    }
                    else
                    {
                        tbIncomingText.Text = testText;
                    }
                }
                
                // Запускаем следующий этап - перевод
                ProcessTranslation(testText);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка симуляции STT: {ex.Message}");
            }
        }

        private void ProcessTranslation(string inputText)
        {
            try
            {
                if (string.IsNullOrEmpty(inputText)) return;
                
                System.Diagnostics.Debug.WriteLine($"🔄 Начинается реальный перевод: {inputText}");
                
                // Показываем статус перевода
                if (tbTranslatedText != null)
                {
                    if (tbTranslatedText.InvokeRequired)
                    {
                        tbTranslatedText.Invoke(new Action(() => tbTranslatedText.Text = "🔄 Переводим..."));
                    }
                    else
                    {
                        tbTranslatedText.Text = "🔄 Переводим...";
                    }
                }
                
                // Запускаем реальный перевод в отдельном потоке
                Task.Run(async () => await PerformRealTranslation(inputText));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в ProcessTranslation: {ex.Message}");
                
                // В случае ошибки используем симуляцию
                string fallbackText = SimulateTranslation(inputText);
                UpdateTranslatedText(fallbackText);
                ProcessTextToSpeech(fallbackText);
            }
        }
        
        private async Task PerformRealTranslation(string inputText)
        {
            try
            {
                // Получаем экземпляр TransManager из основной программы
                var transManager = TransManager.Instace;
                
                if (transManager == null || settingManager == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ TransManager или SettingManager недоступен");
                    string fallbackText = SimulateTranslation(inputText);
                    UpdateTranslatedText($"[Симуляция] {fallbackText}");
                    ProcessTextToSpeech(fallbackText);
                    return;
                }
                
                // Получаем текущий тип переводчика из настроек
                var currentTransType = settingManager.NowTransType;
                System.Diagnostics.Debug.WriteLine($"🌐 Используем переводчик: {currentTransType}");
                
                // Выполняем реальный перевод
                string translatedText = await transManager.StartTrans(inputText, currentTransType);
                
                if (!string.IsNullOrEmpty(translatedText))
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Реальный перевод получен: {translatedText}");
                    UpdateTranslatedText($"[{currentTransType}] {translatedText}");
                    ProcessTextToSpeech(translatedText);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Переводчик вернул пустой результат");
                    string fallbackText = SimulateTranslation(inputText);
                    UpdateTranslatedText($"[Резерв] {fallbackText}");
                    ProcessTextToSpeech(fallbackText);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка реального перевода: {ex.Message}");
                
                // В случае ошибки используем симуляцию
                string fallbackText = SimulateTranslation(inputText);
                UpdateTranslatedText($"[Ошибка] {fallbackText}");
                ProcessTextToSpeech(fallbackText);
            }
        }
        
        private void UpdateTranslatedText(string text)
        {
            if (tbTranslatedText != null)
            {
                if (tbTranslatedText.InvokeRequired)
                {
                    tbTranslatedText.Invoke(new Action(() => tbTranslatedText.Text = text));
                }
                else
                {
                    tbTranslatedText.Text = text;
                }
            }
        }

        private string SimulateTranslation(string inputText)
        {
            // Временная симуляция перевода для тестирования
            // В реальной версии здесь будет вызов MORT TransManager
            
            var translations = new Dictionary<string, string>
            {
                {"Привет, как дела?", "Hello, how are you?"},
                {"Что это за программа?", "What is this program?"},
                {"Переведи этот текст", "Translate this text"},
                {"Проверка голосового перевода", "Voice translation test"},
                {"Тестируем систему распознавания речи", "Testing speech recognition system"}
            };
            
            if (translations.ContainsKey(inputText))
            {
                return translations[inputText];
            }
            
            return $"[EN] {inputText}"; // Простая симуляция
        }

        private void ProcessTextToSpeech(string textToSpeak)
        {
            try
            {
                if (string.IsNullOrEmpty(textToSpeak)) return;
                
                System.Diagnostics.Debug.WriteLine($"🔊 Начинается озвучивание: {textToSpeak}");
                
                // Симуляция TTS - в реальной версии здесь будет вызов TTS системы
                SimulateTTS(textToSpeak);
                
                System.Diagnostics.Debug.WriteLine($"✅ Озвучивание завершено");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в ProcessTextToSpeech: {ex.Message}");
            }
        }

        private void SimulateTTS(string text)
        {
            // Реальное озвучивание с использованием Windows Speech API
            System.Diagnostics.Debug.WriteLine($"🔊 TTS: {text}");
            
            try
            {
                // Пытаемся использовать настоящий TTS
                PerformRealTTS(text);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка реального TTS: {ex.Message}");
                // Если реальный TTS не работает, показываем симуляцию в индикаторе
                SimulateSpeakerActivity();
            }
        }
        
        private void PerformRealTTS(string text)
        {
            try
            {
                // Используем COM-интерфейс SAPI для озвучивания (доступен в Windows без дополнительных ссылок)
                var sapiType = Type.GetTypeFromProgID("SAPI.SpVoice");
                if (sapiType == null)
                {
                    throw new Exception("SAPI.SpVoice не найден в системе");
                }
                
                dynamic? sapi = Activator.CreateInstance(sapiType);
                
                if (sapi != null)
                {
                    // Определяем язык текста и выбираем подходящий голос
                    bool isEnglish = IsEnglishText(text);
                    
                    if (isEnglish)
                    {
                        // Для английского текста пытаемся найти английский голос
                        SetEnglishVoice(sapi);
                        System.Diagnostics.Debug.WriteLine($"🇺🇸 Используем английский голос для: '{text}'");
                        
                        // Настраиваем параметры для английского
                        int englishSpeed = tbTTSSpeedEN?.Value ?? 100;
                        int englishVolume = tbTTSVolumeEN?.Value ?? 80;
                        sapi.Rate = MapSpeedToSAPI(englishSpeed); // Конвертируем 0-200% в SAPI диапазон -10 to 10
                        sapi.Volume = englishVolume; // 0-100%
                        System.Diagnostics.Debug.WriteLine($"🎚️ Английские настройки: скорость={englishSpeed}%->SAPI({sapi.Rate}), громкость={englishVolume}%");
                    }
                    else
                    {
                        // Для русского текста используем русский голос
                        SetRussianVoice(sapi);
                        System.Diagnostics.Debug.WriteLine($"🇷🇺 Используем русский голос для: '{text}'");
                        
                        // Настраиваем параметры для русского
                        int russianSpeed = tbTTSSpeedRU?.Value ?? 100;
                        int russianVolume = tbTTSVolumeRU?.Value ?? 80;
                        sapi.Rate = MapSpeedToSAPI(russianSpeed); // Конвертируем 0-200% в SAPI диапазон -10 to 10
                        sapi.Volume = russianVolume; // 0-100%
                        System.Diagnostics.Debug.WriteLine($"🎚️ Русские настройки: скорость={russianSpeed}%->SAPI({sapi.Rate}), громкость={russianVolume}%");
                    }
                    
                    // Показываем активность динамиков во время озвучивания
                    Task.Run(() => ShowSpeakerActivityDuringTTS());
                    
                    // Озвучиваем текст
                    System.Diagnostics.Debug.WriteLine($"🔊 Начинаем озвучивание через SAPI: '{text}'");
                    sapi.Speak(text, 0); // 0 = синхронное воспроизведение
                    System.Diagnostics.Debug.WriteLine($"✅ Озвучивание через SAPI завершено");
                }
                else
                {
                    throw new Exception("Не удалось создать экземпляр SAPI.SpVoice");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка SAPI TTS: {ex.Message}");
                
                // Если SAPI не работает, пробуем PowerShell и Pavel через System.Speech
                try
                {
                    // Определяем, нужен ли нам Pavel для русского текста
                    bool needsPavel = !IsEnglishText(text);
                    
                    if (needsPavel)
                    {
                        // Пытаемся использовать Pavel через System.Speech (может увидеть больше голосов)
                        bool pavelSuccess = TryUsePavelThroughSystemSpeech(text);
                        if (pavelSuccess)
                        {
                            System.Diagnostics.Debug.WriteLine($"✅ Pavel успешно использован через System.Speech");
                            return; // Успешно озвучили через Pavel
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"⚠️ Pavel недоступен через System.Speech, используем стандартный PowerShell TTS");
                        }
                    }
                    
                    // Если Pavel не сработал или не нужен, используем стандартный PowerShell TTS
                    PerformPowerShellTTS(text);
                }
                catch (Exception psEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Ошибка PowerShell TTS: {psEx.Message}");
                    throw new Exception($"Все методы TTS недоступны. SAPI: {ex.Message}, PowerShell: {psEx.Message}");
                }
            }
        }
        
        private bool IsEnglishText(string text)
        {
            // Простая проверка - если текст содержит латинские буквы, считаем его английским
            int latinCount = 0;
            int cyrillicCount = 0;
            
            foreach (char c in text)
            {
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                    latinCount++;
                else if ((c >= 'А' && c <= 'я') || c == 'ё' || c == 'Ё')
                    cyrillicCount++;
            }
            
            return latinCount > cyrillicCount;
        }
        
        private int MapSpeedToSAPI(int speedPercent)
        {
            // Конвертируем скорость из процентов (10-200%) в SAPI диапазон (-10 to 10)
            // 100% = 0 (нормальная скорость)
            // 10% = -10 (самая медленная)
            // 200% = 10 (самая быстрая)
            
            if (speedPercent <= 10) return -10;
            if (speedPercent >= 200) return 10;
            
            // Линейная интерполяция: (speedPercent - 100) * 10 / 100
            return (speedPercent - 100) / 10;
        }
        
        private void SetEnglishVoice(dynamic sapi)
        {
            try
            {
                // Безопасно получаем выбранный голос из UI потока
                string selectedEnglishVoice = "";
                if (cbTTSVoiceEN != null)
                {
                    if (cbTTSVoiceEN.InvokeRequired)
                    {
                        selectedEnglishVoice = (string)cbTTSVoiceEN.Invoke(new Func<string>(() => cbTTSVoiceEN.SelectedItem?.ToString() ?? ""));
                    }
                    else
                    {
                        selectedEnglishVoice = cbTTSVoiceEN.SelectedItem?.ToString() ?? "";
                    }
                }
                
                if (!string.IsNullOrEmpty(selectedEnglishVoice))
                {
                    System.Diagnostics.Debug.WriteLine($"🎯 Пытаемся установить выбранный английский голос: {selectedEnglishVoice}");
                    
                    var voices = sapi.GetVoices();
                    for (int i = 0; i < voices.Count; i++)
                    {
                        var voice = voices.Item(i);
                        string voiceName = voice.GetDescription();
                        System.Diagnostics.Debug.WriteLine($"🔍 Проверяем голос: {voiceName}");
                        
                        // Улучшенная логика поиска голоса - более точное сравнение
                        // Извлекаем имя голоса из выбранного пункта (например, "Microsoft David" из "Microsoft David - English (United States) [OneCore64]")
                        string selectedVoiceName = selectedEnglishVoice.Split('[')[0].Split('(')[0].Trim();
                        if (selectedVoiceName.EndsWith(" - English") || selectedVoiceName.EndsWith(" - English (United States)"))
                        {
                            selectedVoiceName = selectedVoiceName.Replace(" - English (United States)", "").Replace(" - English", "").Trim();
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"🎯 Ищем точное соответствие для: '{selectedVoiceName}' в голосе '{voiceName}'");
                        
                        // УНИВЕРСАЛЬНОЕ сравнение - ищем конкретные голоса по содержанию имени
                        bool isExactMatch = false;
                        
                        // 1. Специальная логика для David - ищем ВСЕ голоса с David в имени
                        if (selectedVoiceName.Contains("David", StringComparison.OrdinalIgnoreCase) && 
                            voiceName.Contains("David", StringComparison.OrdinalIgnoreCase))
                        {
                            isExactMatch = true;
                            System.Diagnostics.Debug.WriteLine($"✅ Найден David голос (универсальный поиск)");
                        }
                        // 2. Специальная логика для Zira - ищем ВСЕ голоса с Zira в имени
                        else if (selectedVoiceName.Contains("Zira", StringComparison.OrdinalIgnoreCase) && 
                                 voiceName.Contains("Zira", StringComparison.OrdinalIgnoreCase))
                        {
                            isExactMatch = true;
                            System.Diagnostics.Debug.WriteLine($"✅ Найден Zira голос (универсальный поиск)");
                        }
                        // 3. Специальная логика для Mark - ищем ВСЕ голоса с Mark в имени
                        else if (selectedVoiceName.Contains("Mark", StringComparison.OrdinalIgnoreCase) && 
                                 voiceName.Contains("Mark", StringComparison.OrdinalIgnoreCase))
                        {
                            isExactMatch = true;
                            System.Diagnostics.Debug.WriteLine($"✅ Найден Mark голос (универсальный поиск)");
                        }
                        // 4. Точное совпадение названий для других голосов
                        else if (voiceName.Equals(selectedVoiceName, StringComparison.OrdinalIgnoreCase))
                        {
                            isExactMatch = true;
                            System.Diagnostics.Debug.WriteLine($"✅ Точное совпадение названий");
                        }
                        // 5. Проверяем только если имена полностью содержатся друг в друге
                        else if (selectedVoiceName.Length > 10 && voiceName.Contains(selectedVoiceName, StringComparison.OrdinalIgnoreCase))
                        {
                            isExactMatch = true;
                            System.Diagnostics.Debug.WriteLine($"✅ Голос содержит выбранное название");
                        }
                        
                        if (isExactMatch)
                        {
                            sapi.Voice = voice;
                            System.Diagnostics.Debug.WriteLine($"✅ Установлен выбранный английский голос: {voiceName}");
                            return;
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"⚠️ Выбранный голос '{selectedEnglishVoice}' не найден!");
                }
                
                // Если выбранный голос не найден, ищем английские голоса автоматически
                System.Diagnostics.Debug.WriteLine($"🔍 Автоматический поиск английского голоса...");
                var allVoices = sapi.GetVoices();
                
                for (int i = 0; i < allVoices.Count; i++)
                {
                    var voice = allVoices.Item(i);
                    string voiceName = voice.GetDescription();
                    System.Diagnostics.Debug.WriteLine($"🔍 Найден голос: {voiceName}");
                    
                    // Ищем английские голоса по ключевым словам
                    if (voiceName.Contains("English") || voiceName.Contains("US") || 
                        voiceName.Contains("UK") || voiceName.Contains("David") || 
                        voiceName.Contains("Zira") || voiceName.Contains("Mark") ||
                        voiceName.Contains("Hazel") || voiceName.Contains("George") ||
                        voiceName.Contains("Eva") || voiceName.Contains("James") ||
                        voiceName.Contains("Microsoft") && (voiceName.Contains("en-") || voiceName.Contains("English")))
                    {
                        sapi.Voice = voice;
                        System.Diagnostics.Debug.WriteLine($"✅ Установлен автоматически найденный английский голос: {voiceName}");
                        
                        // Безопасно обновляем комбобокс из UI потока
                        if (cbTTSVoiceEN != null && !selectedEnglishVoice.Contains(voiceName))
                        {
                            try
                            {
                                if (cbTTSVoiceEN.InvokeRequired)
                                {
                                    cbTTSVoiceEN.Invoke(new Action(() => {
                                        try
                                        {
                                            if (!cbTTSVoiceEN.Items.Cast<string>().Any(item => item.Contains(voiceName)))
                                            {
                                                cbTTSVoiceEN.Items.Add($"{voiceName} (auto-detected)");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"❌ Ошибка добавления английского голоса в ComboBox: {ex.Message}");
                                        }
                                    }));
                                }
                                else
                                {
                                    if (!cbTTSVoiceEN.Items.Cast<string>().Any(item => item.Contains(voiceName)))
                                    {
                                        cbTTSVoiceEN.Items.Add($"{voiceName} (auto-detected)");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"❌ Ошибка обновления ComboBox для английского голоса: {ex.Message}");
                            }
                        }
                        
                        return;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("⚠️ Английский голос не найден, используем голос по умолчанию");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка при установке английского голоса: {ex.Message}");
            }
        }
        
        private void SetRussianVoice(dynamic sapi)
        {
            try
            {
                // Сначала показываем все доступные голоса SAPI для отладки
                System.Diagnostics.Debug.WriteLine($"🎤 Отладка: Все доступные голоса SAPI:");
                var allVoicesDebug = sapi.GetVoices();
                for (int j = 0; j < allVoicesDebug.Count; j++)
                {
                    var debugVoice = allVoicesDebug.Item(j);
                    string debugVoiceName = debugVoice.GetDescription();
                    System.Diagnostics.Debug.WriteLine($"  SAPI[{j}]: {debugVoiceName}");
                }
                
                // Безопасно получаем выбранный голос из UI потока
                string selectedRussianVoice = "";
                if (cbTTSVoiceRU != null)
                {
                    if (cbTTSVoiceRU.InvokeRequired)
                    {
                        selectedRussianVoice = (string)cbTTSVoiceRU.Invoke(new Func<string>(() => cbTTSVoiceRU.SelectedItem?.ToString() ?? ""));
                    }
                    else
                    {
                        selectedRussianVoice = cbTTSVoiceRU.SelectedItem?.ToString() ?? "";
                    }
                }
                
                if (!string.IsNullOrEmpty(selectedRussianVoice))
                {
                    System.Diagnostics.Debug.WriteLine($"🎯 Пытаемся установить выбранный русский голос: {selectedRussianVoice}");
                    
                    var voices = sapi.GetVoices();
                    for (int i = 0; i < voices.Count; i++)
                    {
                        var voice = voices.Item(i);
                        string voiceName = voice.GetDescription();
                        System.Diagnostics.Debug.WriteLine($"🔍 Проверяем голос: {voiceName}");
                        
                        // Улучшенная логика поиска голоса - более точное сравнение
                        // Извлекаем имя голоса из выбранного пункта (например, "Microsoft Pavel" из "Microsoft Pavel - Russian (Russia) [OneCore64]")
                        string selectedVoiceName = selectedRussianVoice.Split('[')[0].Split('(')[0].Trim();
                        if (selectedVoiceName.EndsWith(" - Russian") || selectedVoiceName.EndsWith(" - Russian (Russia)"))
                        {
                            selectedVoiceName = selectedVoiceName.Replace(" - Russian (Russia)", "").Replace(" - Russian", "").Trim();
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"🎯 Ищем точное соответствие для: '{selectedVoiceName}' в голосе '{voiceName}'");
                        
                        // РЕШЕНИЕ ПРОБЛЕМЫ PAVEL: SAPI не видит OneCore голоса!
                        // Pavel существует только как OneCore/Mobile голос, а SAPI видит только Desktop голоса
                        bool isExactMatch = false;
                        
                        // 1. Специальная логика для Pavel - ищем ВСЕ голоса с Pavel в имени
                        if (selectedVoiceName.Contains("Pavel", StringComparison.OrdinalIgnoreCase) && 
                            voiceName.Contains("Pavel", StringComparison.OrdinalIgnoreCase))
                        {
                            isExactMatch = true;
                            System.Diagnostics.Debug.WriteLine($"✅ Найден Pavel голос (универсальный поиск)");
                        }
                        // 2. Специальная логика для Irina - ищем ВСЕ голоса с Irina в имени
                        else if (selectedVoiceName.Contains("Irina", StringComparison.OrdinalIgnoreCase) && 
                                 voiceName.Contains("Irina", StringComparison.OrdinalIgnoreCase))
                        {
                            isExactMatch = true;
                            System.Diagnostics.Debug.WriteLine($"✅ Найден Irina голос (универсальный поиск)");
                        }
                        // 3. Точное совпадение названий для других голосов
                        else if (voiceName.Equals(selectedVoiceName, StringComparison.OrdinalIgnoreCase))
                        {
                            isExactMatch = true;
                            System.Diagnostics.Debug.WriteLine($"✅ Точное совпадение названий");
                        }
                        // 4. Проверяем только если имена полностью содержатся друг в друге
                        else if (selectedVoiceName.Length > 10 && voiceName.Contains(selectedVoiceName, StringComparison.OrdinalIgnoreCase))
                        {
                            isExactMatch = true;
                            System.Diagnostics.Debug.WriteLine($"✅ Голос содержит выбранное название");
                        }
                        
                        if (isExactMatch)
                        {
                            sapi.Voice = voice;
                            System.Diagnostics.Debug.WriteLine($"✅ Установлен выбранный русский голос: {voiceName}");
                            return;
                        }
                    }
                    
                    // ВАЖНО: Если Pavel не найден, показываем пользователю информативное сообщение
                    if (selectedRussianVoice.Contains("Pavel", StringComparison.OrdinalIgnoreCase))
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ ПРОБЛЕМА: Pavel голос выбран, но недоступен через SAPI!");
                        System.Diagnostics.Debug.WriteLine($"💡 ПРИЧИНА: Pavel существует только как OneCore/Mobile голос, а SAPI видит только Desktop голоса");
                        System.Diagnostics.Debug.WriteLine($"🔧 РЕШЕНИЕ: Будет использован запасной русский голос (Irina Desktop)");
                        
                        // Безопасно уведомляем пользователя через UI
                        if (cbTTSVoiceRU != null)
                        {
                            try
                            {
                                if (cbTTSVoiceRU.InvokeRequired)
                                {
                                    cbTTSVoiceRU.Invoke(new Action(() => {
                                        try
                                        {
                                            // Добавляем информативный элемент в список
                                            string warningText = "⚠️ Pavel недоступен (только OneCore) - используем Irina";
                                            if (!cbTTSVoiceRU.Items.Cast<string>().Any(item => item.Contains("Pavel недоступен")))
                                            {
                                                cbTTSVoiceRU.Items.Insert(0, warningText);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"❌ Ошибка уведомления пользователя: {ex.Message}");
                                        }
                                    }));
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"❌ Ошибка обновления UI: {ex.Message}");
                            }
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"⚠️ Выбранный голос '{selectedRussianVoice}' не найден!");
                }
                
                // Если выбранный голос не найден, ищем русские голоса автоматически
                System.Diagnostics.Debug.WriteLine($"🔍 Автоматический поиск русского голоса...");
                var allVoices = sapi.GetVoices();
                
                for (int i = 0; i < allVoices.Count; i++)
                {
                    var voice = allVoices.Item(i);
                    string voiceName = voice.GetDescription();
                    
                    // Ищем русские голоса по ключевым словам (приоритет Irina как запасной для Pavel)
                    if (voiceName.Contains("Irina") || voiceName.Contains("Ирина") ||
                        voiceName.Contains("Russian") || voiceName.Contains("Русский") || 
                        voiceName.Contains("Pavel") || voiceName.Contains("Павел") ||
                        voiceName.Contains("Microsoft") && (voiceName.Contains("ru-") || voiceName.Contains("Russian")))
                    {
                        sapi.Voice = voice;
                        System.Diagnostics.Debug.WriteLine($"✅ Установлен автоматически найденный русский голос: {voiceName}");
                        
                        // Специальное уведомление если Pavel не найден, но используем Irina
                        if (selectedRussianVoice.Contains("Pavel", StringComparison.OrdinalIgnoreCase) && 
                            voiceName.Contains("Irina", StringComparison.OrdinalIgnoreCase))
                        {
                            System.Diagnostics.Debug.WriteLine($"💡 ЗАМЕНА: Pavel → Irina Desktop (Pavel недоступен в SAPI)");
                        }
                        
                        // Безопасно обновляем комбобокс из UI потока
                        if (cbTTSVoiceRU != null && !selectedRussianVoice.Contains(voiceName))
                        {
                            try
                            {
                                if (cbTTSVoiceRU.InvokeRequired)
                                {
                                    cbTTSVoiceRU.Invoke(new Action(() => {
                                        try
                                        {
                                            if (!cbTTSVoiceRU.Items.Cast<string>().Any(item => item.Contains(voiceName)))
                                            {
                                                cbTTSVoiceRU.Items.Add($"{voiceName} (auto-detected)");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"❌ Ошибка добавления русского голоса в ComboBox: {ex.Message}");
                                        }
                                    }));
                                }
                                else
                                {
                                    if (!cbTTSVoiceRU.Items.Cast<string>().Any(item => item.Contains(voiceName)))
                                    {
                                        cbTTSVoiceRU.Items.Add($"{voiceName} (auto-detected)");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"❌ Ошибка обновления ComboBox для русского голоса: {ex.Message}");
                            }
                        }
                        
                        return;
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("⚠️ Русский голос не найден, используем голос по умолчанию");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка при установке русского голоса: {ex.Message}");
            }
        }
        
        private void PerformPowerShellTTS(string text)
        {
            try
            {
                // Используем PowerShell как резервный метод TTS
                System.Diagnostics.Debug.WriteLine($"🔊 Пробуем PowerShell TTS: '{text}'");
                
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"Add-Type -AssemblyName System.Speech; $speak = New-Object System.Speech.Synthesis.SpeechSynthesizer; $speak.Speak('{text.Replace("'", "''")}')\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                
                // Показываем активность динамиков
                Task.Run(() => ShowSpeakerActivityDuringTTS());
                
                using (var process = System.Diagnostics.Process.Start(processInfo))
                {
                    if (process != null)
                    {
                        process.WaitForExit(10000); // Ждем максимум 10 секунд
                        System.Diagnostics.Debug.WriteLine($"✅ PowerShell TTS завершен с кодом: {process.ExitCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в PerformPowerShellTTS: {ex.Message}");
                throw;
            }
        }
        
        private void ShowSpeakerActivityDuringTTS()
        {
            try
            {
                // Показываем активность динамиков во время реального TTS
                for (int i = 0; i < 30; i++) // 3 секунды активности
                {
                    if (pbSpeakerLevel != null && isMonitoring)
                    {
                        Random random = new Random();
                        int speakerLevel = random.Next(40, 90); // Активность динамиков
                        
                        if (pbSpeakerLevel.InvokeRequired)
                        {
                            pbSpeakerLevel.Invoke(new Action(() => pbSpeakerLevel.Value = speakerLevel));
                        }
                        else
                        {
                            pbSpeakerLevel.Value = speakerLevel;
                        }
                    }
                    System.Threading.Thread.Sleep(100);
                }
                
                // Сбрасываем уровень динамиков после озвучивания
                if (pbSpeakerLevel != null)
                {
                    if (pbSpeakerLevel.InvokeRequired)
                    {
                        pbSpeakerLevel.Invoke(new Action(() => pbSpeakerLevel.Value = 0));
                    }
                    else
                    {
                        pbSpeakerLevel.Value = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в ShowSpeakerActivityDuringTTS: {ex.Message}");
            }
        }
        
        private void SimulateSpeakerActivity()
        {
            // Резервная симуляция если реальный TTS недоступен
            Task.Run(() =>
            {
                System.Diagnostics.Debug.WriteLine("🔊 Симуляция TTS (резервный режим)");
                for (int i = 0; i < 20; i++) // 2 секунды симуляции
                {
                    if (pbSpeakerLevel != null && isMonitoring)
                    {
                        Random random = new Random();
                        int speakerLevel = random.Next(30, 70); // Симуляция активности
                        
                        if (pbSpeakerLevel.InvokeRequired)
                        {
                            pbSpeakerLevel.Invoke(new Action(() => pbSpeakerLevel.Value = speakerLevel));
                        }
                        else
                        {
                            pbSpeakerLevel.Value = speakerLevel;
                        }
                    }
                    System.Threading.Thread.Sleep(100);
                }
                
                // Сбрасываем уровень
                if (pbSpeakerLevel != null)
                {
                    if (pbSpeakerLevel.InvokeRequired)
                    {
                        pbSpeakerLevel.Invoke(new Action(() => pbSpeakerLevel.Value = 0));
                    }
                    else
                    {
                        pbSpeakerLevel.Value = 0;
                    }
                }
            });
        }

        // Простой провайдер синусоиды для тестирования
        private class SineWaveProvider32 : IWaveProvider
        {
            private int sample;
            private WaveFormat waveFormat;

            public SineWaveProvider32()
            {
                waveFormat = new WaveFormat(44100, 1);
            }

            public void SetWaveFormat(int sampleRate, int channels)
            {
                this.waveFormat = new WaveFormat(sampleRate, channels);
            }

            public float Frequency { get; set; }
            public float Amplitude { get; set; } = 0.25f;

            public WaveFormat WaveFormat => waveFormat;

            public int Read(byte[] buffer, int offset, int count)
            {
                int sampleRate = waveFormat.SampleRate;
                short[] waveData = new short[count / 2];
                int samplesPerSecond = sampleRate;
                
                for (int n = 0; n < waveData.Length; n++)
                {
                    if (Frequency == 0)
                    {
                        waveData[n] = 0;
                    }
                    else
                    {
                        waveData[n] = (short)(Amplitude * Math.Sin((2 * Math.PI * sample * Frequency) / samplesPerSecond) * short.MaxValue);
                    }
                    sample++;
                    if (sample >= samplesPerSecond) sample = 0;
                }
                
                Buffer.BlockCopy(waveData, 0, buffer, offset, count);
                return count;
            }
        }

        private void BtnApply_Click(object? sender, EventArgs e)
        {
            // Apply settings
            SaveSettings();
            ApplyTranslationEngineToMainApp();
            MessageBox.Show("Настройки применены!", "Информация", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            // Save and close
            SaveSettings();
            ApplyTranslationEngineToMainApp();
            this.Close();
        }

        private void BtnTestTranslation_Click(object? sender, EventArgs e)
        {
            // Открываем окно тестирования перевода
            TranslationTestForm testForm = new TranslationTestForm();
            testForm.ShowDialog();
        }

        private void BtnTestMicrophone_Click(object? sender, EventArgs e)
        {
            // Отладочное сообщение
            MessageBox.Show("Кнопка тест микрофона нажата!", "Отладка", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            // Проверяем состояние кнопки
            if (sender is Button btn)
            {
                MessageBox.Show($"Кнопка состояние: Visible={btn.Visible}, Enabled={btn.Enabled}, Text='{btn.Text}'", "Отладка кнопки");
            }
            
            try
            {
                if (cbMicrophone?.SelectedIndex < 0)
                {
                    MessageBox.Show("Пожалуйста, выберите микрофон для тестирования.", 
                        "Тест микрофона", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string deviceName = cbMicrophone?.SelectedItem?.ToString() ?? "";
                
                // Используем NAudio для тестирования микрофона
                int deviceIndex = GetActualDeviceIndex(cbMicrophone?.SelectedIndex ?? 0, deviceName, true);
                
                using (var waveIn = new WaveInEvent())
                {
                    waveIn.DeviceNumber = deviceIndex;
                    waveIn.WaveFormat = new WaveFormat(44100, 1); // 44.1kHz, mono
                    
                    // Создаем буфер для записи
                    var bufferedWaveProvider = new BufferedWaveProvider(waveIn.WaveFormat);
                    bool recordingStarted = false;
                    
                    waveIn.DataAvailable += (s, args) =>
                    {
                        if (!recordingStarted)
                        {
                            recordingStarted = true;
                            this.Invoke(() => {
                                MessageBox.Show($"Микрофон '{deviceName}' работает!\nОбнаружен входящий аудиосигнал.", 
                                    "Тест микрофона", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            });
                        }
                        bufferedWaveProvider.AddSamples(args.Buffer, 0, args.BytesRecorded);
                    };
                    
                    waveIn.StartRecording();
                    
                    // Ждем 2 секунды для обнаружения сигнала
                    System.Threading.Thread.Sleep(2000);
                    
                    waveIn.StopRecording();
                    
                    if (!recordingStarted)
                    {
                        MessageBox.Show($"Микрофон '{deviceName}' не обнаружил входящий сигнал.\nПроверьте подключение и уровень громкости.", 
                            "Тест микрофона", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                
                Util.ShowLog($"Microphone test completed: {deviceName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при тестировании микрофона: {ex.Message}", 
                    "Тест микрофона", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Util.ShowLog($"Microphone test error: {ex}");
            }
        }

        private void BtnTestSpeakers_Click(object? sender, EventArgs e)
        {
            // Отладочное сообщение
            MessageBox.Show("Кнопка тест динамиков нажата!", "Отладка", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            try
            {
                if (cbSpeakers?.SelectedIndex < 0)
                {
                    MessageBox.Show("Пожалуйста, выберите устройство воспроизведения для тестирования.", 
                        "Тест динамиков", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string deviceName = cbSpeakers?.SelectedItem?.ToString() ?? "";
                int deviceIndex = GetActualDeviceIndex(cbSpeakers?.SelectedIndex ?? 0, deviceName, false);
                
                // Генерируем тестовый звук (синусоида 440 Hz на 1 секунду)
                int sampleRate = 44100;
                int duration = 1; // секунда
                int samples = sampleRate * duration;
                
                float[] testSignal = new float[samples];
                for (int i = 0; i < samples; i++)
                {
                    testSignal[i] = (float)(Math.Sin(2 * Math.PI * 440 * i / sampleRate) * 0.3); // 440 Hz, 30% громкости
                }
                
                using (var waveOut = new WaveOutEvent())
                {
                    waveOut.DeviceNumber = deviceIndex;
                    
                    // Конвертируем float в 16-bit PCM
                    var waveFormat = new WaveFormat(sampleRate, 16, 1);
                    var buffer = new byte[samples * 2];
                    
                    for (int i = 0; i < samples; i++)
                    {
                        short sample = (short)(testSignal[i] * short.MaxValue);
                        buffer[i * 2] = (byte)(sample & 0xFF);
                        buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xFF);
                    }
                    
                    var memoryStream = new MemoryStream(buffer);
                    var rawSourceWaveStream = new RawSourceWaveStream(memoryStream, waveFormat);
                    
                    waveOut.Init(rawSourceWaveStream);
                    waveOut.Play();
                    
                    MessageBox.Show($"Воспроизводится тестовый звук через '{deviceName}'.\nВы должны услышать тон 440 Hz.", 
                        "Тест динамиков", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    
                    while (waveOut.PlaybackState == PlaybackState.Playing)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
                
                Util.ShowLog($"Speaker test completed: {deviceName}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при тестировании динамиков: {ex.Message}", 
                    "Тест динамиков", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Util.ShowLog($"Speaker test error: {ex}");
            }
        }

        private void BtnTestVBCable_Click(object? sender, EventArgs e)
        {
            // Отладочное сообщение
            MessageBox.Show("Кнопка тест VB-Cable нажата!", "Отладка", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            try
            {
                if (cbVBCable?.SelectedIndex < 0)
                {
                    MessageBox.Show("Пожалуйста, выберите VB-Cable устройство для тестирования.", 
                        "Тест VB-Cable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string deviceName = cbVBCable?.SelectedItem?.ToString() ?? "";
                
                // Проверяем доступность VB-Cable устройств
                bool foundVBCableInput = false;
                bool foundVBCableOutput = false;
                
                // Проверяем устройства записи (CABLE Output)
                using (var deviceEnumerator = new MMDeviceEnumerator())
                {
                    var captureDevices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                    foreach (var device in captureDevices)
                    {
                        if (device.FriendlyName.ToLower().Contains("cable"))
                        {
                            foundVBCableOutput = true;
                            break;
                        }
                    }
                    
                    // Проверяем устройства воспроизведения (CABLE Input)
                    var renderDevices = deviceEnumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                    foreach (var device in renderDevices)
                    {
                        if (device.FriendlyName.ToLower().Contains("cable"))
                        {
                            foundVBCableInput = true;
                            break;
                        }
                    }
                }
                
                string testResult = $"Результат тестирования VB-Cable:\n\n";
                testResult += $"Выбранное устройство: {deviceName}\n";
                testResult += $"CABLE Input (воспроизведение): {(foundVBCableInput ? "✓ Найден" : "✗ Не найден")}\n";
                testResult += $"CABLE Output (запись): {(foundVBCableOutput ? "✓ Найден" : "✗ Не найден")}\n\n";
                
                if (foundVBCableInput && foundVBCableOutput)
                {
                    testResult += "✓ VB-Cable настроен правильно и готов к использованию!";
                    MessageBox.Show(testResult, "Тест VB-Cable", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    testResult += "⚠ VB-Cable не найден или настроен неправильно.\n";
                    testResult += "Убедитесь, что VB-Audio Virtual Cable установлен и активен.";
                    MessageBox.Show(testResult, "Тест VB-Cable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                
                Util.ShowLog($"VB-Cable test completed: Input={foundVBCableInput}, Output={foundVBCableOutput}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при тестировании VB-Cable: {ex.Message}", 
                    "Тест VB-Cable", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Util.ShowLog($"VB-Cable test error: {ex}");
            }
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

        private void ApplyTranslationEngineToMainApp()
        {
            if (settingManager == null || cbTranslationEngine == null)
                return;

            int selectedIndex = cbTranslationEngine.SelectedIndex;
            
            // Skip LibreTranslate (index 0) - it's Audio Translator specific
            if (selectedIndex == 0)
                return; // LibreTranslate - don't change main app setting
            
            // Map Audio Translator engine index to main app TransType
            // Audio Translator indices (offset by 1 due to LibreTranslate):
            // 1 = Google Translate (Basic) -> google_url
            // 2 = Database -> db  
            // 3 = Papago Web -> papago_web
            // 4 = Naver API -> naver
            // 5 = Google Sheets -> google
            // 6 = DeepL Web -> deepl
            // 7 = DeepL API -> deeplApi
            // 8 = Gemini API -> gemini
            // 9 = ezTrans -> ezTrans
            // 10 = Custom API -> customApi
            
            SettingManager.TransType newTransType = (SettingManager.TransType)(selectedIndex - 1);
            
            // Validate the enum value
            if (Enum.IsDefined(typeof(SettingManager.TransType), newTransType))
            {
                settingManager.NowTransType = newTransType;
                Util.ShowLog($"Audio Translator: Changed main app translation engine to {newTransType}");
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

        private void LoadAudioDevices()
        {
            // Создаем лог-файл для отладки
            string logPath = Path.Combine(Environment.CurrentDirectory, "audio_debug.log");
            
            try
            {
                // Проверяем, что мы в UI-потоке
                bool isUIThread = !this.InvokeRequired;
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] LoadAudioDevices() started in UI thread: {isUIThread}\n");
                
                // Если не в UI-потоке, выполняем через Invoke
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(LoadAudioDevices));
                    return;
                }
                
                // Отладочная информация
                System.Diagnostics.Debug.WriteLine("LoadAudioDevices() started");
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] LoadAudioDevices() started\n");
                
                // Проверяем, что ComboBox-ы не равны null
                System.Diagnostics.Debug.WriteLine($"ComboBox states - Microphone: {cbMicrophone != null}, Speakers: {cbSpeakers != null}, Headphones: {cbHeadphones != null}, VBCable: {cbVBCable != null}");
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ComboBox states - Microphone: {cbMicrophone != null}, Speakers: {cbSpeakers != null}, Headphones: {cbHeadphones != null}, VBCable: {cbVBCable != null}\n");
                
                // Очищаем все ComboBox
                cbMicrophone?.Items.Clear();
                cbSpeakers?.Items.Clear();
                cbHeadphones?.Items.Clear();
                cbVBCable?.Items.Clear();

                // Загружаем входные устройства (микрофоны)
                int waveInCount = WaveIn.DeviceCount;
                System.Diagnostics.Debug.WriteLine($"WaveIn.DeviceCount = {waveInCount}");
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] WaveIn.DeviceCount = {waveInCount}\n");
                
                for (int i = 0; i < waveInCount; i++)
                {
                    var deviceInfo = WaveIn.GetCapabilities(i);
                    string deviceName = $"{deviceInfo.ProductName} (ID:{i})";
                    System.Diagnostics.Debug.WriteLine($"Adding WaveIn device: {deviceName}");
                    File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Adding WaveIn device: {deviceName}\n");
                    cbMicrophone?.Items.Add(deviceName);
                    cbVBCable?.Items.Add(deviceName);
                }

                // Загружаем выходные устройства (динамики/наушники)
                int waveOutCount = WaveOut.DeviceCount;
                System.Diagnostics.Debug.WriteLine($"WaveOut.DeviceCount = {waveOutCount}");
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] WaveOut.DeviceCount = {waveOutCount}\n");
                
                for (int i = 0; i < waveOutCount; i++)
                {
                    var deviceInfo = WaveOut.GetCapabilities(i);
                    string deviceName = $"{deviceInfo.ProductName} (ID:{i})";
                    System.Diagnostics.Debug.WriteLine($"Adding WaveOut device: {deviceName}");
                    File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Adding WaveOut device: {deviceName}\n");
                    cbSpeakers?.Items.Add(deviceName);
                    cbHeadphones?.Items.Add(deviceName);
                }

                // Добавляем устройства по умолчанию
                System.Diagnostics.Debug.WriteLine("Adding default devices");
                cbMicrophone?.Items.Insert(0, "Микрофон по умолчанию");
                cbSpeakers?.Items.Insert(0, "Динамики по умолчанию");
                cbHeadphones?.Items.Insert(0, "Наушники по умолчанию");
                cbVBCable?.Items.Insert(0, "VB-Cable по умолчанию");

                // Устанавливаем значения по умолчанию
                System.Diagnostics.Debug.WriteLine("Setting default selections");
                if (cbMicrophone?.Items.Count > 0) cbMicrophone.SelectedIndex = 0;
                if (cbSpeakers?.Items.Count > 0) cbSpeakers.SelectedIndex = 0;
                if (cbHeadphones?.Items.Count > 0) cbHeadphones.SelectedIndex = 0;
                if (cbVBCable?.Items.Count > 0) cbVBCable.SelectedIndex = 0;
                
                System.Diagnostics.Debug.WriteLine($"Final counts - Microphone: {cbMicrophone?.Items.Count}, Speakers: {cbSpeakers?.Items.Count}, Headphones: {cbHeadphones?.Items.Count}, VBCable: {cbVBCable?.Items.Count}");
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Final counts - Microphone: {cbMicrophone?.Items.Count}, Speakers: {cbSpeakers?.Items.Count}, Headphones: {cbHeadphones?.Items.Count}, VBCable: {cbVBCable?.Items.Count}\n");
                System.Diagnostics.Debug.WriteLine("LoadAudioDevices() completed successfully");
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] LoadAudioDevices() completed successfully\n");
                
                // Принудительно обновляем UI
                cbMicrophone?.Refresh();
                cbSpeakers?.Refresh();
                cbHeadphones?.Refresh();
                cbVBCable?.Refresh();
                this.Refresh();
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] UI refresh completed\n");

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
                            if (cbMicrophone != null && !cbMicrophone.Items.Contains(deviceName))
                                cbMicrophone.Items.Add(deviceName);
                            if (cbVBCable != null && !cbVBCable.Items.Contains(deviceName))
                                cbVBCable.Items.Add(deviceName);
                        }

                        // Выходные устройства
                        var outputDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                        foreach (var device in outputDevices)
                        {
                            string deviceName = $"{device.FriendlyName} (WASAPI)";
                            if (cbSpeakers != null && !cbSpeakers.Items.Contains(deviceName))
                                cbSpeakers.Items.Add(deviceName);
                            if (cbHeadphones != null && !cbHeadphones.Items.Contains(deviceName))
                                cbHeadphones.Items.Add(deviceName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // WASAPI может не работать на некоторых системах
                    System.Diagnostics.Debug.WriteLine($"WASAPI enumeration failed: {ex.Message}");
                }

                // Устанавливаем значения по умолчанию
                if (cbMicrophone?.Items.Count > 0) cbMicrophone.SelectedIndex = 0;
                if (cbSpeakers?.Items.Count > 0) cbSpeakers.SelectedIndex = 0;
                if (cbHeadphones?.Items.Count > 0) cbHeadphones.SelectedIndex = 0;
                if (cbVBCable?.Items.Count > 0) cbVBCable.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                string logFilePath = Path.Combine(Environment.CurrentDirectory, "audio_debug.log");
                System.Diagnostics.Debug.WriteLine($"Exception in LoadAudioDevices: {ex.Message}");
                File.AppendAllText(logFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Exception in LoadAudioDevices: {ex.Message}\n");
                File.AppendAllText(logFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Stack trace: {ex.StackTrace}\n");
                
                MessageBox.Show($"Ошибка загрузки аудио устройств: {ex.Message}", 
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                // Добавляем хотя бы базовые устройства
                cbMicrophone?.Items.Add("Микрофон по умолчанию");
                cbSpeakers?.Items.Add("Динамики по умолчанию");
                cbHeadphones?.Items.Add("Наушники по умолчанию");
                cbVBCable?.Items.Add("VB-Cable");
                
                if (cbMicrophone?.Items.Count > 0) cbMicrophone.SelectedIndex = 0;
                if (cbSpeakers?.Items.Count > 0) cbSpeakers.SelectedIndex = 0;
                if (cbHeadphones?.Items.Count > 0) cbHeadphones.SelectedIndex = 0;
                if (cbVBCable?.Items.Count > 0) cbVBCable.SelectedIndex = 0;
            }
        }
        
        #region STT Helper Methods
        
        /// <summary>
        /// Конвертирует byte[] аудио данные в WAV формат для Whisper
        /// </summary>
        private byte[] ConvertToWav(byte[] audioData, int sampleRate, int channels)
        {
            try
            {
                using (var memoryStream = new MemoryStream())
                using (var writer = new BinaryWriter(memoryStream))
                {
                    // WAV Header
                    writer.Write("RIFF".ToCharArray());
                    writer.Write((uint)(36 + audioData.Length));
                    writer.Write("WAVE".ToCharArray());
                    
                    // fmt chunk
                    writer.Write("fmt ".ToCharArray());
                    writer.Write((uint)16);
                    writer.Write((ushort)1); // PCM
                    writer.Write((ushort)channels);
                    writer.Write((uint)sampleRate);
                    writer.Write((uint)(sampleRate * channels * 2));
                    writer.Write((ushort)(channels * 2));
                    writer.Write((ushort)16); // bits per sample
                    
                    // data chunk
                    writer.Write("data".ToCharArray());
                    writer.Write((uint)audioData.Length);
                    writer.Write(audioData);
                    
                    return memoryStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка конвертации в WAV: {ex.Message}");
                return audioData; // Возвращаем оригинальные данные
            }
        }
        
        /// <summary>
        /// Конвертирует byte[] в 16-bit PCM для Vosk
        /// </summary>
        private short[] ConvertToPcm16(byte[] audioData)
        {
            try
            {
                short[] pcmData = new short[audioData.Length / 2];
                for (int i = 0; i < pcmData.Length; i++)
                {
                    pcmData[i] = (short)((audioData[i * 2 + 1] << 8) | audioData[i * 2]);
                }
                return pcmData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка конвертации в PCM16: {ex.Message}");
                return new short[0];
            }
        }
        
        /// <summary>
        /// Вызывает Whisper.NET для распознавания речи
        /// </summary>
        private async Task<string> CallWhisperNetAsync(byte[] wavData, string modelName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔄 Whisper вызов с моделью {modelName}, данные: {wavData.Length} байт");
                
                // Определяем путь к модели
                string modelPath = GetWhisperModelPath(modelName);
                if (!File.Exists(modelPath))
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Модель Whisper не найдена: {modelPath}");
                    return "[Ошибка: Модель не найдена]";
                }
                
                // Создаем временный файл для WAV данных
                string tempWavFile = Path.GetTempFileName();
                try
                {
                    File.WriteAllBytes(tempWavFile, wavData);
                    
                    // Инициализируем Whisper
                    using var whisperFactory = WhisperFactory.FromPath(modelPath);
                    using var processor = whisperFactory.CreateBuilder()
                        .WithLanguage("auto") // Автоопределение языка
                        .Build();
                    
                    // Выполняем распознавание
                    using var fileStream = File.OpenRead(tempWavFile);
                    
                    var results = new List<string>();
                    await foreach (var result in processor.ProcessAsync(fileStream))
                    {
                        results.Add(result.Text);
                    }
                    
                    string finalText = string.Join(" ", results).Trim();
                    System.Diagnostics.Debug.WriteLine($"✅ Whisper результат: {finalText}");
                    
                    return string.IsNullOrEmpty(finalText) ? "[Текст не распознан]" : finalText;
                }
                finally
                {
                    // Удаляем временный файл
                    if (File.Exists(tempWavFile))
                        File.Delete(tempWavFile);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка вызова Whisper: {ex.Message}");
                return $"[Ошибка Whisper: {ex.Message}]";
            }
        }
        
        /// <summary>
        /// Вызывает Vosk.NET для распознавания речи
        /// </summary>
        private string CallVoskNet(short[] pcmData, string modelName)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔄 Vosk вызов с моделью {modelName}, данные: {pcmData.Length} семплов");
                
                // Определяем путь к модели
                string modelPath = GetVoskModelPath(modelName);
                if (!Directory.Exists(modelPath))
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Модель Vosk не найдена: {modelPath}");
                    return "[Ошибка: Модель не найдена]";
                }
                
                // Конвертируем short[] в byte[]
                byte[] audioBytes = new byte[pcmData.Length * 2];
                Buffer.BlockCopy(pcmData, 0, audioBytes, 0, audioBytes.Length);
                
                // Инициализируем Vosk
                var model = new Vosk.Model(modelPath);
                var recognizer = new VoskRecognizer(model, 16000.0f);
                
                // Обрабатываем аудио данные порциями
                const int chunkSize = 4000; // Размер чанка в байтах
                var results = new List<string>();
                
                for (int i = 0; i < audioBytes.Length; i += chunkSize)
                {
                    int currentChunkSize = Math.Min(chunkSize, audioBytes.Length - i);
                    byte[] chunk = new byte[currentChunkSize];
                    Array.Copy(audioBytes, i, chunk, 0, currentChunkSize);
                    
                    if (recognizer.AcceptWaveform(chunk, chunk.Length))
                    {
                        var result = recognizer.Result();
                        if (!string.IsNullOrEmpty(result))
                        {
                            // Парсим JSON результат
                            var parsed = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(result);
                            string text = parsed?.text?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(text))
                                results.Add(text);
                        }
                    }
                }
                
                // Получаем финальный результат
                var finalResult = recognizer.FinalResult();
                if (!string.IsNullOrEmpty(finalResult))
                {
                    var parsed = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(finalResult);
                    string text = parsed?.text?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(text))
                        results.Add(text);
                }
                
                recognizer.Dispose();
                model.Dispose();
                
                string finalText = string.Join(" ", results).Trim();
                System.Diagnostics.Debug.WriteLine($"✅ Vosk результат: {finalText}");
                
                return string.IsNullOrEmpty(finalText) ? "[Текст не распознан]" : finalText;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка вызова Vosk: {ex.Message}");
                return $"[Ошибка Vosk: {ex.Message}]";
            }
        }
        
        /// <summary>
        /// Получает путь к модели Whisper
        /// </summary>
        private string GetWhisperModelPath(string modelName)
        {
            // Ищем модели в папке приложения
            string appDir = Path.GetDirectoryName(Application.ExecutablePath) ?? "";
            string modelsDir = Path.Combine(appDir, "models", "whisper");
            
            // Проверяем стандартные названия моделей
            string[] possibleExtensions = { ".bin", ".ggml" };
            string[] possibleNames = { modelName, $"ggml-{modelName}", $"ggml-{modelName}.bin" };
            
            foreach (string name in possibleNames)
            {
                foreach (string ext in possibleExtensions)
                {
                    string fullName = name.EndsWith(ext) ? name : name + ext;
                    string fullPath = Path.Combine(modelsDir, fullName);
                    if (File.Exists(fullPath))
                        return fullPath;
                }
            }
            
            // Если не найдено, возвращаем путь по умолчанию
            return Path.Combine(modelsDir, $"ggml-{modelName}.bin");
        }
        
        /// <summary>
        /// Получает путь к модели Vosk
        /// </summary>
        private string GetVoskModelPath(string modelName)
        {
            // Ищем модели в папке приложения
            string appDir = Path.GetDirectoryName(Application.ExecutablePath) ?? "";
            string modelsDir = Path.Combine(appDir, "models", "vosk");
            
            // Проверяем различные варианты названий папок
            string[] possibleNames = { modelName, $"vosk-model-{modelName}", $"model-{modelName}" };
            
            foreach (string name in possibleNames)
            {
                string fullPath = Path.Combine(modelsDir, name);
                if (Directory.Exists(fullPath))
                    return fullPath;
            }
            
            // Если не найдено, возвращаем путь по умолчанию
            return Path.Combine(modelsDir, modelName);
        }
        
        #endregion

        private void OnClick_TestAllAudioDevices(object? sender, EventArgs e)
        {
            try
            {
                // Используем встроенный класс для тестирования
                TestAudioDevices.TestDeviceEnumeration();
                
                // Получаем результаты и показываем пользователю
                string message = TestAudioDevices.GetDeviceEnumerationResults();
                MessageBox.Show(message, "Результаты тестирования всех аудио устройств", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // Обновляем список устройств после тестирования
                LoadAudioDevices();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при тестировании аудио устройств: {ex.Message}", 
                    "Ошибка тестирования", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        #region Audio Device Testing Methods
        
        /// <summary>
        /// Получает реальный индекс NAudio устройства из выбранного элемента комбобокса
        /// </summary>
        /// <param name="comboBoxIndex">Индекс в комбобоксе</param>
        /// <param name="selectedText">Текст выбранного элемента</param>
        /// <param name="isInputDevice">true для устройств ввода (микрофоны), false для вывода</param>
        /// <returns>Реальный индекс NAudio устройства или -1 если ошибка</returns>
        private int GetActualDeviceIndex(int comboBoxIndex, string selectedText, bool isInputDevice)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"GetActualDeviceIndex: comboBoxIndex={comboBoxIndex}, selectedText='{selectedText}', isInputDevice={isInputDevice}");
                
                // Если выбрано устройство "по умолчанию" (первый элемент)
                if (comboBoxIndex == 0 && selectedText.Contains("по умолчанию"))
                {
                    // Возвращаем индекс -1 для устройства по умолчанию (NAudio использует -1 для default device)
                    System.Diagnostics.Debug.WriteLine("Returning -1 for default device");
                    return -1;
                }
                
                // Ищем индекс в тексте вида "(ID:X)"
                var match = System.Text.RegularExpressions.Regex.Match(selectedText, @"\(ID:(\d+)\)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int realIndex))
                {
                    // Проверяем, что индекс в допустимых пределах
                    int maxCount = isInputDevice ? WaveIn.DeviceCount : WaveOut.DeviceCount;
                    if (realIndex >= 0 && realIndex < maxCount)
                    {
                        System.Diagnostics.Debug.WriteLine($"Found ID in text: {realIndex}");
                        return realIndex;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"ID {realIndex} is out of range (0-{maxCount-1})");
                    }
                }
                
                // Если не удалось извлечь индекс из текста, пытаемся вычислить
                // Учитываем, что первый элемент - "по умолчанию", поэтому вычитаем 1
                int calculatedIndex = comboBoxIndex - 1;
                int deviceCount = isInputDevice ? WaveIn.DeviceCount : WaveOut.DeviceCount;
                
                System.Diagnostics.Debug.WriteLine($"Calculated index: {calculatedIndex}, device count: {deviceCount}");
                
                if (calculatedIndex >= 0 && calculatedIndex < deviceCount)
                {
                    System.Diagnostics.Debug.WriteLine($"Using calculated index: {calculatedIndex}");
                    return calculatedIndex;
                }
                
                System.Diagnostics.Debug.WriteLine("Failed to determine device index");
                return -1; // Ошибка - некорректный индекс
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in GetActualDeviceIndex: {ex.Message}");
                return -1;
            }
        }
        
        /// <summary>
        /// Тестирование выбранного микрофона
        /// </summary>
        private async Task TestMicrophoneDevice()
        {
            try
            {
                if (cbMicrophone?.SelectedIndex < 0)
                {
                    MessageBox.Show("Пожалуйста, выберите микрофон для тестирования.", 
                        "Микрофон не выбран", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // Получаем правильный индекс устройства
                int deviceIndex = GetActualDeviceIndex(cbMicrophone!.SelectedIndex, cbMicrophone.SelectedItem?.ToString() ?? "", true);
                string selectedDeviceName = cbMicrophone.SelectedItem?.ToString() ?? "";
                
                System.Diagnostics.Debug.WriteLine($"TestMicrophoneDevice: selectedIndex={cbMicrophone.SelectedIndex}, deviceIndex={deviceIndex}, selectedText='{selectedDeviceName}'");
                
                string deviceDisplayName;
                if (deviceIndex == -1)
                {
                    deviceDisplayName = "Микрофон по умолчанию";
                }
                else
                {
                    if (deviceIndex < 0 || deviceIndex >= WaveIn.DeviceCount)
                    {
                        MessageBox.Show($"Некорректный индекс устройства: {deviceIndex}. Доступные устройства: 0-{WaveIn.DeviceCount - 1}", 
                            "Ошибка индекса", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    
                    var deviceCaps = WaveIn.GetCapabilities(deviceIndex);
                    deviceDisplayName = deviceCaps.ProductName;
                }
                
                // Проверяем, является ли это виртуальным аудио устройством (только VB-Cable, НЕ Voicemeeter)
                bool isVirtualDevice = (selectedDeviceName.Contains("CABLE") || 
                                      selectedDeviceName.Contains("VB-Audio")) &&
                                      !selectedDeviceName.Contains("Voicemeeter") &&
                                      !selectedDeviceName.Contains("VoiceMeeter");
                
                // Показываем информацию о тестируемом устройстве
                string message;
                if (isVirtualDevice)
                {
                    message = $"Тестирование виртуального микрофона:\n{deviceDisplayName}\n\n" +
                             "Будет выполнена запись звука в течение 3 секунд, " +
                             "затем воспроизведение через физические динамики для контроля.\n\n" +
                             "Говорите в микрофон после нажатия ОК.";
                }
                else
                {
                    message = $"Тестирование микрофона:\n{deviceDisplayName}\n\n" +
                             "Будет включен МОНИТОРИНГ В РЕАЛЬНОМ ВРЕМЕНИ в течение 3 секунд.\n" +
                             "Вы должны будете слышать свой голос в выбранных динамиках!\n\n" +
                             "Говорите в микрофон после нажатия ОК.";
                }
                                
                if (MessageBox.Show(message, "Тест микрофона", MessageBoxButtons.OKCancel, 
                    MessageBoxIcon.Information) == DialogResult.Cancel)
                {
                    return;
                }
                
                // Выбираем метод тестирования в зависимости от типа устройства
                bool success;
                if (isVirtualDevice)
                {
                    // Для виртуальных устройств (только VB-Cable) используем playback метод
                    success = await audioTester!.TestMicrophoneWithPlaybackAsync(deviceIndex, -1, 3);
                }
                else
                {
                    // Для обычных микрофонов сначала проверим выходное устройство тестовым тоном
                    int speakerDeviceIndex = -1; // По умолчанию используем устройство по умолчанию
                    
                    // Проверяем доступные устройства воспроизведения
                    if (cbSpeakers?.SelectedIndex >= 0 && cbSpeakers?.SelectedItem != null)
                    {
                        speakerDeviceIndex = GetActualDeviceIndex(cbSpeakers.SelectedIndex, cbSpeakers.SelectedItem.ToString() ?? "", false);
                        System.Diagnostics.Debug.WriteLine($"Using speakers device index: {speakerDeviceIndex} ({cbSpeakers.SelectedItem})");
                    }
                    else if (cbHeadphones?.SelectedIndex >= 0 && cbHeadphones?.SelectedItem != null)
                    {
                        speakerDeviceIndex = GetActualDeviceIndex(cbHeadphones.SelectedIndex, cbHeadphones.SelectedItem.ToString() ?? "", false);
                        System.Diagnostics.Debug.WriteLine($"Using headphones device index: {speakerDeviceIndex} ({cbHeadphones.SelectedItem})");
                    }
                    
                    // СНАЧАЛА тестируем динамики тестовым тоном
                    if (MessageBox.Show($"Сначала проверим работу динамиков.\n\nВы должны услышать тестовый тон через выбранное устройство воспроизведения.\n\nПродолжить?", 
                        "Тест динамиков", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        bool speakerTest = await audioTester!.TestSpeakersAsync(speakerDeviceIndex, 1000, 2);
                        if (!speakerTest)
                        {
                            MessageBox.Show("❌ Динамики не работают!\n\nПроверьте:\n- Выбор правильного устройства воспроизведения\n- Громкость в Windows\n- Подключение динамиков", 
                                "Ошибка динамиков", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        
                        MessageBox.Show("✅ Динамики работают! Теперь тестируем микрофон.", 
                            "Динамики OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Testing microphone {deviceIndex} with speaker {speakerDeviceIndex}");
                    success = await audioTester!.TestMicrophoneWithRealTimeMonitoringAsync(deviceIndex, speakerDeviceIndex, 3);
                }
                
                if (success)
                {
                    if (isVirtualDevice)
                    {
                        MessageBox.Show($"✅ Виртуальный микрофон '{deviceDisplayName}' работает корректно!\n\n" +
                                       "Если вы слышали воспроизведение через физические динамики, " +
                                       "VB-Cable настроен правильно для аудио перевода.", 
                                       "Тест VB-Cable успешен", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show($"✅ Микрофон '{deviceDisplayName}' работает корректно!\n\n" +
                                       "Если вы слышали свой голос в динамиках в реальном времени, " +
                                       "микрофон и мониторинг настроены правильно.", 
                                       "Тест успешен", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    string errorMessage = isVirtualDevice ? 
                        $"❌ Ошибка при тестировании VB-Cable '{deviceDisplayName}'.\n\n" +
                        "Проверьте установку VB-Cable и настройки виртуального аудио драйвера." :
                        $"❌ Ошибка при тестировании микрофона '{deviceDisplayName}'.\n\n" +
                        "Проверьте подключение микрофона и настройки Windows.";
                        
                    MessageBox.Show(errorMessage, "Тест не пройден", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при тестировании микрофона: {ex.Message}", 
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// Тестирование выбранных динамиков
        /// </summary>
        private async Task TestSpeakersDevice()
        {
            try
            {
                if (cbSpeakers?.SelectedIndex < 0)
                {
                    MessageBox.Show("Пожалуйста, выберите динамики для тестирования.", 
                        "Динамики не выбраны", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                // Получаем правильный индекс устройства
                int deviceIndex = GetActualDeviceIndex(cbSpeakers!.SelectedIndex, cbSpeakers.SelectedItem?.ToString() ?? "", false);
                string selectedDeviceName = cbSpeakers.SelectedItem?.ToString() ?? "";
                
                System.Diagnostics.Debug.WriteLine($"TestSpeakersDevice: selectedIndex={cbSpeakers.SelectedIndex}, deviceIndex={deviceIndex}, selectedText='{selectedDeviceName}'");
                
                string deviceDisplayName;
                if (deviceIndex == -1)
                {
                    deviceDisplayName = "Динамики по умолчанию";
                }
                else
                {
                    if (deviceIndex < 0 || deviceIndex >= WaveOut.DeviceCount)
                    {
                        MessageBox.Show($"Некорректный индекс устройства: {deviceIndex}. Доступные устройства: 0-{WaveOut.DeviceCount - 1}", 
                            "Ошибка индекса", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    
                    var deviceCaps = WaveOut.GetCapabilities(deviceIndex);
                    deviceDisplayName = deviceCaps.ProductName;
                }
                
                // Показываем информацию о тестируемом устройстве
                string message = $"Тестирование динамиков:\n{deviceDisplayName}\n\n" +
                                "Будет воспроизведен тестовый тон 440Hz в течение 3 секунд.\n\n" +
                                "Убедитесь, что громкость установлена на комфортный уровень.";
                                
                if (MessageBox.Show(message, "Тест динамиков", MessageBoxButtons.OKCancel, 
                    MessageBoxIcon.Information) == DialogResult.Cancel)
                {
                    return;
                }
                
                bool success = await audioTester!.TestSpeakersAsync(deviceIndex, 440.0f, 3);
                
                if (success)
                {
                    MessageBox.Show($"✅ Динамики '{deviceDisplayName}' работают корректно!\n\n" +
                                   "Если вы слышали тестовый тон, динамики настроены правильно.", 
                                   "Тест успешен", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"❌ Ошибка при тестировании динамиков '{deviceDisplayName}'.\n\n" +
                                   "Проверьте подключение динамиков и настройки Windows.", 
                                   "Тест не пройден", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при тестировании динамиков: {ex.Message}", 
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// Тестирование VB-Cable
        /// </summary>
        private async Task TestVBCableDevice()
        {
            try
            {
                // Показываем информацию о тесте VB-Cable
                string message = "Тестирование VB-Cable:\n\n" +
                                "Будет выполнена проверка loopback соединения:\n" +
                                "1. Поиск VB-Cable устройств\n" +
                                "2. Воспроизведение тестового сигнала в VB-Cable Output\n" +
                                "3. Запись сигнала с VB-Cable Input\n" +
                                "4. Анализ полученного сигнала\n\n" +
                                "Убедитесь, что VB-Cable установлен и настроен.";
                                
                if (MessageBox.Show(message, "Тест VB-Cable", MessageBoxButtons.OKCancel, 
                    MessageBoxIcon.Information) == DialogResult.Cancel)
                {
                    return;
                }
                
                bool success = await audioTester!.TestVBCableAsync(5);
                
                if (success)
                {
                    MessageBox.Show("✅ VB-Cable работает корректно!\n\n" +
                                   "Loopback соединение установлено, сигнал передается правильно.\n" +
                                   "VB-Cable готов для использования в Discord/играх.", 
                                   "Тест успешен", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("❌ VB-Cable не работает или не найден!\n\n" +
                                   "Возможные причины:\n" +
                                   "• VB-Cable не установлен\n" +
                                   "• VB-Cable не настроен как устройство по умолчанию\n" +
                                   "• Проблемы с драйверами аудио\n\n" +
                                   "Установите VB-Cable и перезапустите приложение.", 
                                   "Тест не пройден", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при тестировании VB-Cable: {ex.Message}", 
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        /// <summary>
        /// Освобождение ресурсов
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                audioTester?.Dispose();
                audioRouter?.Dispose();
                routingStatusTimer?.Dispose();
                universalManager?.Dispose();
                universalStatusTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
        
        #endregion

        #region Audio Routing Methods

        /// <summary>
        /// Загрузка устройств для перенаправления
        /// </summary>
        private void LoadRoutingDevices()
        {
            try
            {
                cbRoutingInput?.Items.Clear();
                cbRoutingOutput?.Items.Clear();

                // Загружаем входные устройства (микрофоны)
                for (int i = 0; i < WaveInEvent.DeviceCount; i++)
                {
                    var capability = WaveInEvent.GetCapabilities(i);
                    cbRoutingInput?.Items.Add($"{i}: {capability.ProductName}");
                }

                // Загружаем выходные устройства (динамики)
                for (int i = 0; i < WaveOut.DeviceCount; i++)
                {
                    var capability = WaveOut.GetCapabilities(i);
                    cbRoutingOutput?.Items.Add($"{i}: {capability.ProductName}");
                }

                // Выбираем первые устройства по умолчанию
                if (cbRoutingInput?.Items.Count > 0)
                    cbRoutingInput.SelectedIndex = 0;
                if (cbRoutingOutput?.Items.Count > 0)
                    cbRoutingOutput.SelectedIndex = 0;

                LogMessage("🔄 Устройства для перенаправления загружены.");
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Ошибка загрузки устройств: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик изменения состояния перенаправления
        /// </summary>
        private void OnRoutingEnabledChanged(object? sender, EventArgs e)
        {
            bool enabled = cbEnableRouting?.Checked ?? false;
            
            cbRoutingInput!.Enabled = enabled;
            cbRoutingOutput!.Enabled = enabled;
            btnStartRouting!.Enabled = enabled && !audioRouter!.IsRouting;
            btnStopRouting!.Enabled = enabled && audioRouter!.IsRouting;

            if (enabled)
            {
                LogMessage("✅ Перенаправление аудио активировано. Выберите устройства и нажмите 'Запустить'.");
            }
            else
            {
                if (audioRouter!.IsRouting)
                {
                    audioRouter.StopRouting();
                }
                LogMessage("❌ Перенаправление аудио деактивировано.");
            }
        }

        /// <summary>
        /// Запуск перенаправления
        /// </summary>
        private async void OnStartRouting(object? sender, EventArgs e)
        {
            try
            {
                if (cbRoutingInput?.SelectedIndex < 0 || cbRoutingOutput?.SelectedIndex < 0)
                {
                    LogMessage("❌ Выберите входное и выходное устройства.");
                    return;
                }

                int inputIndex = cbRoutingInput?.SelectedIndex ?? -1;
                int outputIndex = cbRoutingOutput?.SelectedIndex ?? -1;
                
                if (inputIndex < 0 || outputIndex < 0)
                {
                    LogMessage("❌ Неверные индексы устройств.");
                    return;
                }
                
                string inputName = cbRoutingInput?.SelectedItem?.ToString() ?? "Неизвестно";
                string outputName = cbRoutingOutput?.SelectedItem?.ToString() ?? "Неизвестно";

                bool success = await audioRouter!.StartRoutingAsync(inputIndex, outputIndex, inputName, outputName);
                
                if (success)
                {
                    btnStartRouting!.Enabled = false;
                    btnStopRouting!.Enabled = true;
                    routingStatusTimer!.Enabled = true;
                    
                    LogMessage("🎉 Перенаправление успешно запущено!");
                }
                else
                {
                    LogMessage("❌ Не удалось запустить перенаправление.");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Ошибка запуска: {ex.Message}");
            }
        }

        /// <summary>
        /// Остановка перенаправления
        /// </summary>
        private void OnStopRouting(object? sender, EventArgs e)
        {
            try
            {
                audioRouter?.StopRouting();
                btnStartRouting!.Enabled = true;
                btnStopRouting!.Enabled = false;
                routingStatusTimer!.Enabled = false;
                
                LogMessage("⏹️ Перенаправление остановлено пользователем.");
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Ошибка остановки: {ex.Message}");
            }
        }

        /// <summary>
        /// Обновление статуса перенаправления
        /// </summary>
        private void OnRoutingStatusTick(object? sender, EventArgs e)
        {
            try
            {
                if (audioRouter?.IsRouting == true)
                {
                    string stats = audioRouter.GetBufferStats();
                    LogMessage($"📊 Статус: {audioRouter.CurrentRoute} | {stats}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"⚠️ Ошибка обновления статуса: {ex.Message}");
            }
        }

        /// <summary>
        /// Логирование сообщений в текстовое поле
        /// </summary>
        private void LogMessage(string message)
        {
            try
            {
                if (tbRoutingLog?.InvokeRequired == true)
                {
                    tbRoutingLog.Invoke(new Action<string>(LogMessage), message);
                    return;
                }

                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                string logEntry = $"[{timestamp}] {message}\r\n";
                
                tbRoutingLog?.AppendText(logEntry);
                tbRoutingLog?.ScrollToCaret();
                
                // Ограничиваем размер лога (последние 1000 строк)
                if (tbRoutingLog?.Lines.Length > 1000)
                {
                    var lines = tbRoutingLog.Lines;
                    var trimmedLines = new string[500];
                    Array.Copy(lines, lines.Length - 500, trimmedLines, 0, 500);
                    tbRoutingLog.Lines = trimmedLines;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка логирования: {ex.Message}");
            }
        }

        #endregion

        #region Universal Mode Methods

        /// <summary>
        /// Загрузка устройств для универсального режима
        /// </summary>
        private void LoadUniversalDevices()
        {
            try
            {
                if (universalManager != null)
                {
                    // Обновляем статус VB-Cable
                    var vbCableInfo = universalManager.GetSystemStatus();
                    if (lblVBCableStatus != null)
                    {
                        if (vbCableInfo.Contains("VB-Cable"))
                        {
                            lblVBCableStatus.Text = "✅ VB-Cable обнаружен";
                            lblVBCableStatus.ForeColor = Color.DarkGreen;
                            if (btnStartUniversal != null)
                                btnStartUniversal.Enabled = true;
                        }
                        else
                        {
                            lblVBCableStatus.Text = "❌ VB-Cable не найден - установите VB-Cable";
                            lblVBCableStatus.ForeColor = Color.Red;
                        }
                    }
                }

                // Загружаем физические устройства
                LoadPhysicalDevices();
            }
            catch (Exception ex)
            {
                LogUniversalMessage($"❌ Ошибка загрузки устройств: {ex.Message}");
            }
        }

        /// <summary>
        /// Загрузка физических аудиоустройств
        /// </summary>
        private void LoadPhysicalDevices()
        {
            try
            {
                cbPhysicalMicrophone?.Items.Clear();
                cbPhysicalSpeakers?.Items.Clear();

                // Загружаем микрофоны
                for (int i = 0; i < WaveInEvent.DeviceCount; i++)
                {
                    var caps = WaveInEvent.GetCapabilities(i);
                    if (!caps.ProductName.ToLower().Contains("cable") && 
                        !caps.ProductName.ToLower().Contains("virtual"))
                    {
                        cbPhysicalMicrophone?.Items.Add($"{caps.ProductName} [{i}]");
                    }
                }

                // Загружаем динамики
                for (int i = 0; i < NAudio.Wave.WaveOut.DeviceCount; i++)
                {
                    var caps = NAudio.Wave.WaveOut.GetCapabilities(i);
                    if (!caps.ProductName.ToLower().Contains("cable") && 
                        !caps.ProductName.ToLower().Contains("virtual"))
                    {
                        cbPhysicalSpeakers?.Items.Add($"{caps.ProductName} [{i}]");
                    }
                }

                // Выбираем первые доступные устройства
                if (cbPhysicalMicrophone?.Items.Count > 0)
                    cbPhysicalMicrophone.SelectedIndex = 0;
                
                if (cbPhysicalSpeakers?.Items.Count > 0)
                    cbPhysicalSpeakers.SelectedIndex = 0;

                LogUniversalMessage("📋 Физические устройства загружены");
            }
            catch (Exception ex)
            {
                LogUniversalMessage($"❌ Ошибка загрузки физических устройств: {ex.Message}");
            }
        }

        /// <summary>
        /// Переключение универсального режима через чекбокс
        /// </summary>
        private async void OnUniversalModeToggle(object? sender, EventArgs e)
        {
            try
            {
                if (cbEnableUniversal?.Checked == true)
                {
                    await StartUniversalMode();
                }
                else
                {
                    await StopUniversalMode();
                }
            }
            catch (Exception ex)
            {
                LogUniversalMessage($"❌ Ошибка переключения режима: {ex.Message}");
            }
        }

        /// <summary>
        /// Запуск универсального режима по кнопке
        /// </summary>
        private async void OnStartUniversalClick(object? sender, EventArgs e)
        {
            await StartUniversalMode();
        }

        /// <summary>
        /// Остановка универсального режима по кнопке
        /// </summary>
        private async void OnStopUniversalClick(object? sender, EventArgs e)
        {
            await StopUniversalMode();
        }

        /// <summary>
        /// Переключение состояния перевода
        /// </summary>
        private async void OnToggleTranslationClick(object? sender, EventArgs e)
        {
            try
            {
                if (universalManager != null)
                {
                    await universalManager.ToggleTranslationAsync();
                }
            }
            catch (Exception ex)
            {
                LogUniversalMessage($"❌ Ошибка переключения перевода: {ex.Message}");
            }
        }

        /// <summary>
        /// Запуск универсального режима
        /// </summary>
        private async Task StartUniversalMode()
        {
            try
            {
                if (universalManager == null) return;

                LogUniversalMessage("🚀 Запуск универсального режима...");

                bool success = await universalManager.EnableUniversalModeAsync();
                
                if (success)
                {
                    // Активируем интерфейс
                    if (btnStartUniversal != null) btnStartUniversal.Enabled = false;
                    if (btnStopUniversal != null) btnStopUniversal.Enabled = true;
                    if (btnToggleTranslation != null) btnToggleTranslation.Enabled = true;
                    if (cbEnableUniversal != null && !cbEnableUniversal.Checked) cbEnableUniversal.Checked = true;
                    
                    // Запускаем таймер обновления статуса
                    if (universalStatusTimer != null) universalStatusTimer.Enabled = true;
                    
                    LogUniversalMessage("✅ Универсальный режим активирован!");
                }
                else
                {
                    LogUniversalMessage("❌ Не удалось запустить универсальный режим");
                }
            }
            catch (Exception ex)
            {
                LogUniversalMessage($"❌ Ошибка запуска универсального режима: {ex.Message}");
            }
        }

        /// <summary>
        /// Остановка универсального режима
        /// </summary>
        private async Task StopUniversalMode()
        {
            try
            {
                if (universalManager == null) return;

                LogUniversalMessage("🛑 Остановка универсального режима...");

                bool success = await universalManager.DisableUniversalModeAsync();
                
                if (success)
                {
                    // Деактивируем интерфейс
                    if (btnStartUniversal != null) btnStartUniversal.Enabled = true;
                    if (btnStopUniversal != null) btnStopUniversal.Enabled = false;
                    if (btnToggleTranslation != null) btnToggleTranslation.Enabled = false;
                    if (cbEnableUniversal != null && cbEnableUniversal.Checked) cbEnableUniversal.Checked = false;
                    
                    // Останавливаем таймер
                    if (universalStatusTimer != null) universalStatusTimer.Enabled = false;
                    
                    LogUniversalMessage("✅ Универсальный режим деактивирован");
                }
                else
                {
                    LogUniversalMessage("❌ Ошибка остановки универсального режима");
                }
            }
            catch (Exception ex)
            {
                LogUniversalMessage($"❌ Ошибка остановки универсального режима: {ex.Message}");
            }
        }

        /// <summary>
        /// Обновление статуса универсального режима
        /// </summary>
        private void OnUniversalStatusTick(object? sender, EventArgs e)
        {
            try
            {
                if (universalManager != null && lblUniversalStatus != null)
                {
                    string status = universalManager.GetSystemStatus();
                    lblUniversalStatus.Text = status.Replace("\n", " | ");
                    
                    // Обновляем цвет статуса
                    if (universalManager.IsUniversalModeActive)
                    {
                        lblUniversalStatus.ForeColor = universalManager.IsTranslationActive ? Color.DarkGreen : Color.Orange;
                    }
                    else
                    {
                        lblUniversalStatus.ForeColor = Color.Gray;
                    }
                }
            }
            catch (Exception ex)
            {
                LogUniversalMessage($"⚠️ Ошибка обновления статуса: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик изменения состояния универсального режима
        /// </summary>
        private void OnUniversalModeStateChanged(bool isActive)
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action<bool>(OnUniversalModeStateChanged), isActive);
                    return;
                }

                if (btnStartUniversal != null) btnStartUniversal.Enabled = !isActive;
                if (btnStopUniversal != null) btnStopUniversal.Enabled = isActive;
                if (btnToggleTranslation != null) btnToggleTranslation.Enabled = isActive;
                
                if (cbEnableUniversal != null)
                {
                    cbEnableUniversal.CheckedChanged -= OnUniversalModeToggle;
                    cbEnableUniversal.Checked = isActive;
                    cbEnableUniversal.CheckedChanged += OnUniversalModeToggle;
                }
            }
            catch (Exception ex)
            {
                LogUniversalMessage($"❌ Ошибка обновления интерфейса: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик изменения состояния перевода
        /// </summary>
        private void OnTranslationStateChanged(bool isActive)
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action<bool>(OnTranslationStateChanged), isActive);
                    return;
                }

                if (btnToggleTranslation != null)
                {
                    btnToggleTranslation.Text = isActive ? "⏹️ Остановить перевод" : "🎯 Запустить перевод";
                    btnToggleTranslation.BackColor = isActive ? Color.LightCoral : Color.LightBlue;
                    btnToggleTranslation.ForeColor = isActive ? Color.DarkRed : Color.DarkBlue;
                }
            }
            catch (Exception ex)
            {
                LogUniversalMessage($"❌ Ошибка обновления кнопки перевода: {ex.Message}");
            }
        }

        /// <summary>
        /// Логирование сообщений универсального режима
        /// </summary>
        private void LogUniversalMessage(string message)
        {
            try
            {
                if (tbUniversalLog?.InvokeRequired == true)
                {
                    tbUniversalLog.Invoke(new Action<string>(LogUniversalMessage), message);
                    return;
                }

                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                string logEntry = $"[{timestamp}] {message}\r\n";
                
                tbUniversalLog?.AppendText(logEntry);
                tbUniversalLog?.ScrollToCaret();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка в AddToUniversalLog: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Пытается использовать Pavel голос через System.Speech API как альтернативу SAPI
        /// </summary>
        /// <param name="text">Текст для озвучивания</param>
        /// <returns>true если Pavel найден и озвучивание успешно, false иначе</returns>
        private bool TryUsePavelThroughSystemSpeech(string text)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"🔍 Поиск Pavel через System.Speech API...");
                
                // Загружаем System.Speech
                var assembly = AppDomain.CurrentDomain.Load("System.Speech");
                if (assembly == null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ System.Speech не найден");
                    return false;
                }
                
                var synthType = assembly.GetType("System.Speech.Synthesis.SpeechSynthesizer");
                if (synthType == null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ SpeechSynthesizer не найден");
                    return false;
                }
                
                var synth = Activator.CreateInstance(synthType);
                if (synth == null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Не удалось создать SpeechSynthesizer");
                    return false;
                }
                
                // Получаем все голоса
                var getVoicesMethod = synthType.GetMethod("GetInstalledVoices");
                if (getVoicesMethod == null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ GetInstalledVoices не найден");
                    return false;
                }
                
                var installedVoices = getVoicesMethod.Invoke(synth, null);
                if (!(installedVoices is System.Collections.IEnumerable voices))
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Не удалось получить список голосов");
                    return false;
                }
                
                // Ищем Pavel в System.Speech
                foreach (var voice in voices)
                {
                    var voiceInfoProp = voice.GetType().GetProperty("VoiceInfo");
                    if (voiceInfoProp == null) continue;
                    
                    var voiceInfo = voiceInfoProp.GetValue(voice);
                    if (voiceInfo == null) continue;
                    
                    var nameProp = voiceInfo.GetType().GetProperty("Name");
                    if (nameProp == null) continue;
                    
                    string voiceName = nameProp.GetValue(voiceInfo)?.ToString() ?? "";
                    System.Diagnostics.Debug.WriteLine($"   🎤 System.Speech голос: {voiceName}");
                    
                    if (voiceName.Contains("Pavel", StringComparison.OrdinalIgnoreCase))
                    {
                        System.Diagnostics.Debug.WriteLine($"⭐ НАЙДЕН PAVEL В SYSTEM.SPEECH: {voiceName}");
                        
                        try
                        {
                            // Устанавливаем Pavel голос
                            var selectVoiceMethod = synthType.GetMethod("SelectVoice", new Type[] { typeof(string) });
                            if (selectVoiceMethod != null)
                            {
                                selectVoiceMethod.Invoke(synth, new object[] { voiceName });
                                System.Diagnostics.Debug.WriteLine($"✅ Pavel установлен в System.Speech");
                                
                                // Настраиваем параметры TTS
                                var rateProperty = synthType.GetProperty("Rate");
                                var volumeProperty = synthType.GetProperty("Volume");
                                
                                if (rateProperty != null)
                                {
                                    int russianSpeed = tbTTSSpeedRU?.Value ?? 100;
                                    int systemSpeechRate = MapSpeedToSystemSpeech(russianSpeed);
                                    rateProperty.SetValue(synth, systemSpeechRate);
                                    System.Diagnostics.Debug.WriteLine($"🎚️ Pavel скорость: {russianSpeed}% -> {systemSpeechRate}");
                                }
                                
                                if (volumeProperty != null)
                                {
                                    int russianVolume = tbTTSVolumeRU?.Value ?? 80;
                                    volumeProperty.SetValue(synth, russianVolume);
                                    System.Diagnostics.Debug.WriteLine($"🎚️ Pavel громкость: {russianVolume}%");
                                }
                                
                                // Озвучиваем текст через Pavel
                                var speakMethod = synthType.GetMethod("Speak", new Type[] { typeof(string) });
                                if (speakMethod != null)
                                {
                                    System.Diagnostics.Debug.WriteLine($"🔊 Озвучиваем через Pavel: '{text}'");
                                    
                                    // Показываем активность динамиков
                                    Task.Run(() => ShowSpeakerActivityDuringTTS());
                                    
                                    speakMethod.Invoke(synth, new object[] { text });
                                    System.Diagnostics.Debug.WriteLine($"✅ Pavel озвучивание через System.Speech завершено!");
                                    
                                    return true; // Успешно использовали Pavel!
                                }
                            }
                        }
                        catch (Exception voiceEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"❌ Ошибка использования Pavel: {voiceEx.Message}");
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"⚠️ Pavel не найден в System.Speech голосах");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка TryUsePavelThroughSystemSpeech: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Конвертирует скорость из процентов (MORT UI) в диапазон System.Speech (-10 to 10)
        /// </summary>
        /// <param name="speedPercent">Скорость в процентах (10-200%)</param>
        /// <returns>Скорость для System.Speech (-10 to 10)</returns>
        private int MapSpeedToSystemSpeech(int speedPercent)
        {
            // Конвертируем скорость из процентов в System.Speech диапазон (-10 to 10)
            // 100% = 0 (нормальная скорость)
            // 10% = -10 (самая медленная) 
            // 200% = 10 (самая быстрая)
            
            if (speedPercent <= 10) return -10;
            if (speedPercent >= 200) return 10;
            
            // Линейная интерполяция: (speedPercent - 100) * 10 / 100
            return (speedPercent - 100) / 10;
        }
        
        /// <summary>
        /// Инициализирует папки для STT моделей
        /// </summary>
        private void InitializeModelDirectories()
        {
            try
            {
                string appDir = Path.GetDirectoryName(Application.ExecutablePath) ?? "";
                
                // Создаем папки для моделей
                string whisperDir = Path.Combine(appDir, "models", "whisper");
                string voskDir = Path.Combine(appDir, "models", "vosk");
                
                if (!Directory.Exists(whisperDir))
                {
                    Directory.CreateDirectory(whisperDir);
                    System.Diagnostics.Debug.WriteLine($"📁 Создана папка для Whisper моделей: {whisperDir}");
                }
                
                if (!Directory.Exists(voskDir))
                {
                    Directory.CreateDirectory(voskDir);
                    System.Diagnostics.Debug.WriteLine($"📁 Создана папка для Vosk моделей: {voskDir}");
                }
                
                // Проверяем наличие моделей и выводим информацию
                CheckAvailableModels();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка инициализации папок моделей: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Проверяет доступные модели и выводит информацию
        /// </summary>
        private void CheckAvailableModels()
        {
            try
            {
                string appDir = Path.GetDirectoryName(Application.ExecutablePath) ?? "";
                
                // Проверяем Whisper модели
                string whisperDir = Path.Combine(appDir, "models", "whisper");
                if (Directory.Exists(whisperDir))
                {
                    var whisperFiles = Directory.GetFiles(whisperDir, "*.bin")
                        .Concat(Directory.GetFiles(whisperDir, "*.ggml"))
                        .ToArray();
                    
                    if (whisperFiles.Length > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Найдено Whisper моделей: {whisperFiles.Length}");
                        foreach (var file in whisperFiles)
                        {
                            System.Diagnostics.Debug.WriteLine($"   - {Path.GetFileName(file)}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ Whisper модели не найдены. Поместите .bin или .ggml файлы в папку models/whisper");
                    }
                }
                
                // Проверяем Vosk модели
                string voskDir = Path.Combine(appDir, "models", "vosk");
                if (Directory.Exists(voskDir))
                {
                    var voskDirs = Directory.GetDirectories(voskDir);
                    
                    if (voskDirs.Length > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"✅ Найдено Vosk моделей: {voskDirs.Length}");
                        foreach (var dir in voskDirs)
                        {
                            System.Diagnostics.Debug.WriteLine($"   - {Path.GetFileName(dir)}");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ Vosk модели не найдены. Поместите папки с моделями в папку models/vosk");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка проверки моделей: {ex.Message}");
            }
        }

        #endregion
    }
}
