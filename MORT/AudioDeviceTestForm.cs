using System;
using System.IO;
using System.Windows.Forms;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace MORT
{
    public partial class AudioDeviceTestForm : Form
    {
        private ComboBox cbTestMicrophone;
        private ComboBox cbTestSpeakers;
        private Button btnTest;
        private TextBox tbResults;

        public AudioDeviceTestForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.cbTestMicrophone = new ComboBox();
            this.cbTestSpeakers = new ComboBox();
            this.btnTest = new Button();
            this.tbResults = new TextBox();
            this.SuspendLayout();

            // cbTestMicrophone
            this.cbTestMicrophone.Location = new System.Drawing.Point(12, 12);
            this.cbTestMicrophone.Size = new System.Drawing.Size(300, 21);
            this.cbTestMicrophone.DropDownStyle = ComboBoxStyle.DropDownList;

            // cbTestSpeakers
            this.cbTestSpeakers.Location = new System.Drawing.Point(12, 50);
            this.cbTestSpeakers.Size = new System.Drawing.Size(300, 21);
            this.cbTestSpeakers.DropDownStyle = ComboBoxStyle.DropDownList;

            // btnTest
            this.btnTest.Location = new System.Drawing.Point(330, 12);
            this.btnTest.Size = new System.Drawing.Size(150, 60);
            this.btnTest.Text = "Test Audio Devices";
            this.btnTest.UseVisualStyleBackColor = true;
            this.btnTest.Click += new EventHandler(this.btnTest_Click);

            // tbResults
            this.tbResults.Location = new System.Drawing.Point(12, 90);
            this.tbResults.Multiline = true;
            this.tbResults.ScrollBars = ScrollBars.Vertical;
            this.tbResults.Size = new System.Drawing.Size(470, 300);

            // AudioDeviceTestForm
            this.ClientSize = new System.Drawing.Size(500, 410);
            this.Controls.Add(this.cbTestMicrophone);
            this.Controls.Add(this.cbTestSpeakers);
            this.Controls.Add(this.btnTest);
            this.Controls.Add(this.tbResults);
            this.Text = "Audio Device Test";
            this.ResumeLayout(false);
        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            string logPath = Path.Combine(Environment.CurrentDirectory, "audio_device_test.log");
            File.WriteAllText(logPath, ""); // Clear log

            try
            {
                tbResults.Clear();
                
                // Clear ComboBoxes
                cbTestMicrophone.Items.Clear();
                cbTestSpeakers.Items.Clear();

                AppendResult("=== TESTING AUDIO DEVICE ENUMERATION ===");
                AppendResult($"UI Thread: {!this.InvokeRequired}");

                // Test WaveIn devices
                int waveInCount = WaveIn.DeviceCount;
                AppendResult($"WaveIn.DeviceCount = {waveInCount}");
                File.AppendAllText(logPath, $"WaveIn.DeviceCount = {waveInCount}\n");

                for (int i = 0; i < waveInCount; i++)
                {
                    var caps = WaveIn.GetCapabilities(i);
                    string deviceName = $"{caps.ProductName} (WaveIn {i})";
                    cbTestMicrophone.Items.Add(deviceName);
                    AppendResult($"Added WaveIn: {deviceName}");
                    File.AppendAllText(logPath, $"Added WaveIn: {deviceName}\n");
                }

                // Test WaveOut devices
                int waveOutCount = WaveOut.DeviceCount;
                AppendResult($"WaveOut.DeviceCount = {waveOutCount}");
                File.AppendAllText(logPath, $"WaveOut.DeviceCount = {waveOutCount}\n");

                for (int i = 0; i < waveOutCount; i++)
                {
                    var caps = WaveOut.GetCapabilities(i);
                    string deviceName = $"{caps.ProductName} (WaveOut {i})";
                    cbTestSpeakers.Items.Add(deviceName);
                    AppendResult($"Added WaveOut: {deviceName}");
                    File.AppendAllText(logPath, $"Added WaveOut: {deviceName}\n");
                }

                // Test WASAPI devices
                try
                {
                    using (var enumerator = new MMDeviceEnumerator())
                    {
                        var inputDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                        foreach (var device in inputDevices)
                        {
                            string deviceName = $"{device.FriendlyName} (WASAPI Input)";
                            cbTestMicrophone.Items.Add(deviceName);
                            AppendResult($"Added WASAPI Input: {deviceName}");
                            File.AppendAllText(logPath, $"Added WASAPI Input: {deviceName}\n");
                        }

                        var outputDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                        foreach (var device in outputDevices)
                        {
                            string deviceName = $"{device.FriendlyName} (WASAPI Output)";
                            cbTestSpeakers.Items.Add(deviceName);
                            AppendResult($"Added WASAPI Output: {deviceName}");
                            File.AppendAllText(logPath, $"Added WASAPI Output: {deviceName}\n");
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppendResult($"WASAPI Error: {ex.Message}");
                    File.AppendAllText(logPath, $"WASAPI Error: {ex.Message}\n");
                }

                AppendResult($"Final counts - Microphone: {cbTestMicrophone.Items.Count}, Speakers: {cbTestSpeakers.Items.Count}");
                File.AppendAllText(logPath, $"Final counts - Microphone: {cbTestMicrophone.Items.Count}, Speakers: {cbTestSpeakers.Items.Count}\n");

                // Force UI update
                cbTestMicrophone.Refresh();
                cbTestSpeakers.Refresh();
                this.Refresh();

                AppendResult("=== TEST COMPLETED ===");
                File.AppendAllText(logPath, "Test completed\n");

                if (cbTestMicrophone.Items.Count > 0) cbTestMicrophone.SelectedIndex = 0;
                if (cbTestSpeakers.Items.Count > 0) cbTestSpeakers.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                AppendResult($"ERROR: {ex.Message}");
                AppendResult($"Stack Trace: {ex.StackTrace}");
                File.AppendAllText(logPath, $"ERROR: {ex.Message}\n{ex.StackTrace}\n");
            }
        }

        private void AppendResult(string text)
        {
            tbResults.AppendText(text + Environment.NewLine);
            tbResults.ScrollToCaret();
        }
    }
}
