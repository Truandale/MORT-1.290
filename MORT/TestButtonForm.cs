using System;
using System.Drawing;
using System.Windows.Forms;

namespace MORT
{
    public partial class TestButtonForm : Form
    {
        private Button btnTest1;
        private Button btnTest2;
        private Button btnTest3;

        public TestButtonForm()
        {
            InitializeComponent();
            CreateTestButtons();
        }

        private void InitializeComponent()
        {
            this.Text = "Тест кнопок";
            this.Size = new Size(400, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;
        }

        private void CreateTestButtons()
        {
            // Тестовая кнопка 1
            btnTest1 = new Button()
            {
                Text = "Тест микрофон",
                Location = new Point(50, 50),
                Size = new Size(150, 40),
                BackColor = Color.LightBlue,
                ForeColor = Color.Black,
                Enabled = true,
                Visible = true
            };
            btnTest1.Click += (s, e) => 
            {
                MessageBox.Show("КНОПКА 1 РАБОТАЕТ!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            // Тестовая кнопка 2
            btnTest2 = new Button()
            {
                Text = "Тест динамики",
                Location = new Point(50, 110),
                Size = new Size(150, 40),
                BackColor = Color.LightGreen,
                ForeColor = Color.Black,
                Enabled = true,
                Visible = true
            };
            btnTest2.Click += (s, e) => 
            {
                MessageBox.Show("КНОПКА 2 РАБОТАЕТ!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            // Тестовая кнопка 3
            btnTest3 = new Button()
            {
                Text = "Тест VB-Cable",
                Location = new Point(50, 170),
                Size = new Size(150, 40),
                BackColor = Color.LightYellow,
                ForeColor = Color.Black,
                Enabled = true,
                Visible = true
            };
            btnTest3.Click += (s, e) => 
            {
                MessageBox.Show("КНОПКА 3 РАБОТАЕТ!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            // Добавляем кнопки на форму
            this.Controls.Add(btnTest1);
            this.Controls.Add(btnTest2);
            this.Controls.Add(btnTest3);

            // Информационная надпись
            Label lblInfo = new Label()
            {
                Text = "Если эти кнопки работают, проблема в основной форме",
                Location = new Point(50, 230),
                Size = new Size(300, 20),
                ForeColor = Color.Red
            };
            this.Controls.Add(lblInfo);
        }
    }
}
