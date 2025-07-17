using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;
using MORT.Manager;

namespace MORT
{
    public partial class TranslationTestForm : Form
    {
        private TextBox? tbSourceText;
        private TextBox? tbTranslatedText;
        private ComboBox? cbTranslationEngine;
        private ComboBox? cbSourceLanguage;
        private ComboBox? cbTargetLanguage;
        private Button? btnTranslate;
        private Button? btnClear;
        private Button? btnClose;
        private Label? lblStatus;
        private ProgressBar? progressBar;
        
        public TranslationTestForm()
        {
            InitializeComponent();
            InitializeControls();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Form properties
            this.Text = "Тест перевода в реальном времени";
            this.Size = new Size(700, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.ForeColor = Color.Black;
            
            this.ResumeLayout(false);
        }

        private void InitializeControls()
        {
            // Header
            Label lblHeader = new Label()
            {
                Text = "🌐 Тестирование переводчиков",
                Location = new Point(20, 20),
                Size = new Size(400, 30),
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };

            // Translation Engine
            Label lblEngine = new Label()
            {
                Text = "Переводчик:",
                Location = new Point(20, 70),
                Size = new Size(100, 20),
                ForeColor = Color.Black
            };

            cbTranslationEngine = new ComboBox()
            {
                Location = new Point(130, 68),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            
            // Используем те же переводчики, что и в основном приложении
            cbTranslationEngine.Items.AddRange(new string[] 
            { 
                "TRANSLATE GOOGLE", 
                "TRANSLATE DB", 
                "TRANSLATE PAPAGO WEB", 
                "TRANSLATE NAVER", 
                "TRANSLATE GOOGLE SHEET", 
                "TRANSLATE DEEPL", 
                "TRANSLATE DEEPLAPI", 
                "TRANSLATE GEMINI API", 
                "TRANSLATE EZTRANS", 
                "TRANSLATE CUSTOM API" 
            });

            // Устанавливаем текущий выбранный переводчик из основного приложения
            try
            {
                // Получаем текущий тип переводчика из настроек
                // Используем текущий выбранный переводчик из основного приложения
                var form1 = Application.OpenForms.OfType<Form1>().FirstOrDefault();
                if (form1 != null)
                {
                    var currentTransType = form1.MySettingManager.NowTransType;
                    cbTranslationEngine.SelectedIndex = (int)currentTransType;
                }
                else
                {
                    cbTranslationEngine.SelectedIndex = 0;
                }
            }
            catch
            {
                // Если не удалось получить настройки, используем Google по умолчанию
                cbTranslationEngine.SelectedIndex = 0;
            }
            cbTranslationEngine.SelectedIndex = 0;

            // Source Language
            Label lblSourceLang = new Label()
            {
                Text = "Из:",
                Location = new Point(350, 70),
                Size = new Size(30, 20),
                ForeColor = Color.Black
            };

            cbSourceLanguage = new ComboBox()
            {
                Location = new Point(385, 68),
                Size = new Size(80, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbSourceLanguage.Items.AddRange(new string[] { "RU", "EN", "KO", "JA", "ZH" });
            cbSourceLanguage.SelectedIndex = 0;

            // Target Language
            Label lblTargetLang = new Label()
            {
                Text = "В:",
                Location = new Point(480, 70),
                Size = new Size(25, 20),
                ForeColor = Color.Black
            };

            cbTargetLanguage = new ComboBox()
            {
                Location = new Point(510, 68),
                Size = new Size(80, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbTargetLanguage.Items.AddRange(new string[] { "EN", "RU", "KO", "JA", "ZH" });
            cbTargetLanguage.SelectedIndex = 0;

            // Source Text
            Label lblSourceText = new Label()
            {
                Text = "Исходный текст:",
                Location = new Point(20, 120),
                Size = new Size(150, 20),
                ForeColor = Color.Black
            };

            tbSourceText = new TextBox()
            {
                Location = new Point(20, 145),
                Size = new Size(640, 120),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "Введите текст для перевода..."
            };

            // Translate Button
            btnTranslate = new Button()
            {
                Text = "🔄 Перевести",
                Location = new Point(20, 280),
                Size = new Size(120, 35),
                BackColor = Color.LightGreen,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnTranslate.Click += BtnTranslate_Click;

            // Clear Button
            btnClear = new Button()
            {
                Text = "🗑️ Очистить",
                Location = new Point(150, 280),
                Size = new Size(100, 35),
                BackColor = Color.LightYellow,
                ForeColor = Color.Black
            };
            btnClear.Click += BtnClear_Click;

            // Progress Bar
            progressBar = new ProgressBar()
            {
                Location = new Point(270, 285),
                Size = new Size(200, 25),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };

            // Status Label
            lblStatus = new Label()
            {
                Text = "Готов к переводу",
                Location = new Point(480, 290),
                Size = new Size(180, 20),
                ForeColor = Color.Green
            };

            // Translated Text
            Label lblTranslatedText = new Label()
            {
                Text = "Переведенный текст:",
                Location = new Point(20, 330),
                Size = new Size(150, 20),
                ForeColor = Color.Black
            };

            tbTranslatedText = new TextBox()
            {
                Location = new Point(20, 355),
                Size = new Size(640, 120),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.LightGray
            };

            // Close Button
            btnClose = new Button()
            {
                Text = "Закрыть",
                Location = new Point(580, 500),
                Size = new Size(80, 35),
                DialogResult = DialogResult.OK,
                ForeColor = Color.Black
            };

            // Add all controls
            this.Controls.AddRange(new Control[] 
            { 
                lblHeader,
                lblEngine, cbTranslationEngine,
                lblSourceLang, cbSourceLanguage,
                lblTargetLang, cbTargetLanguage,
                lblSourceText, tbSourceText,
                btnTranslate, btnClear,
                progressBar, lblStatus,
                lblTranslatedText, tbTranslatedText,
                btnClose
            });
        }

        private async void BtnTranslate_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbSourceText?.Text))
            {
                MessageBox.Show("Пожалуйста, введите текст для перевода.", "Предупреждение", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Show progress
            if (progressBar != null) progressBar.Visible = true;
            if (btnTranslate != null) btnTranslate.Enabled = false;
            if (lblStatus != null)
            {
                lblStatus.Text = "Переводим...";
                lblStatus.ForeColor = Color.Orange;
            }
            tbTranslatedText?.Clear();

            try
            {
                // Get translation using real API
                string sourceText = tbSourceText?.Text ?? "";
                string translatedText = await GetTranslationAsync(sourceText);
                
                // Display result
                if (tbTranslatedText != null)
                    tbTranslatedText.Text = translatedText;
                
                if (lblStatus != null)
                {
                    lblStatus.Text = "Перевод выполнен успешно!";
                    lblStatus.ForeColor = Color.Green;
                }
            }
            catch (Exception ex)
            {
                if (tbTranslatedText != null)
                    tbTranslatedText.Text = $"Ошибка перевода: {ex.Message}";
                
                if (lblStatus != null)
                {
                    lblStatus.Text = "Ошибка при переводе";
                    lblStatus.ForeColor = Color.Red;
                }
            }
            finally
            {
                // Hide progress
                if (progressBar != null) progressBar.Visible = false;
                if (btnTranslate != null) btnTranslate.Enabled = true;
            }
        }

        private async Task<string> GetTranslationAsync(string sourceText)
        {
            await Task.Delay(100); // Small delay to show progress
            
            try
            {
                // Get TransManager instance
                TransManager transManager = TransManager.Instace;
                
                // Determine translation type based on selected engine
                SettingManager.TransType transType = GetTranslationType();
                
                // For Google Sheets, check if it's initialized
                if (transType == SettingManager.TransType.google)
                {
                    if (transManager?.sheets == null)
                    {
                        return "Ошибка: Google Sheets API не настроен. Сначала настройте Google Sheets в основных настройках.";
                    }
                }
                
                // Perform translation using real API
                string result = await transManager.StartTrans(sourceText, transType);
                
                return string.IsNullOrWhiteSpace(result) ? "Перевод не удался" : result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка API перевода: {ex.Message}");
            }
        }

        private SettingManager.TransType GetTranslationType()
        {
            string? selectedEngine = cbTranslationEngine?.SelectedItem?.ToString();
            
            return selectedEngine switch
            {
                "TRANSLATE GOOGLE" => SettingManager.TransType.google_url,
                "TRANSLATE DB" => SettingManager.TransType.db,
                "TRANSLATE PAPAGO WEB" => SettingManager.TransType.papago_web,
                "TRANSLATE NAVER" => SettingManager.TransType.naver,
                "TRANSLATE GOOGLE SHEET" => SettingManager.TransType.google,
                "TRANSLATE DEEPL" => SettingManager.TransType.deepl,
                "TRANSLATE DEEPLAPI" => SettingManager.TransType.deeplApi,
                "TRANSLATE GEMINI API" => SettingManager.TransType.gemini,
                "TRANSLATE EZTRANS" => SettingManager.TransType.ezTrans,
                "TRANSLATE CUSTOM API" => SettingManager.TransType.customApi,
                _ => SettingManager.TransType.google_url // Default
            };
        }

        private void BtnClear_Click(object? sender, EventArgs e)
        {
            tbSourceText?.Clear();
            tbTranslatedText?.Clear();
            if (lblStatus != null)
            {
                lblStatus.Text = "Готов к переводу";
                lblStatus.ForeColor = Color.Green;
            }
        }
    }
}
