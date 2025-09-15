using System;
using System.Drawing;
using System.Windows.Forms;

namespace outlook2
{
    public class SettingsForm : Form
    {
        private Label lblApiKey;
        private TextBox txtApiKey;
        private Button btnToggle;
        private Button btnSave;
        private Button btnClose;

        public SettingsForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "CopyPolish Ayarları";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(480, 160);

            lblApiKey = new Label
            {
                AutoSize = true,
                Text = "API Anahtarı:",
                Location = new Point(16, 22)
            };

            txtApiKey = new TextBox
            {
                Location = new Point(110, 18),
                Width = 260,
                UseSystemPasswordChar = true
            };

            btnToggle = new Button
            {
                Text = "Göster",
                Location = new Point(380, 16),
                Width = 80
            };
            btnToggle.Click += (s, e) =>
            {
                txtApiKey.UseSystemPasswordChar = !txtApiKey.UseSystemPasswordChar;
                btnToggle.Text = txtApiKey.UseSystemPasswordChar ? "Göster" : "Gizle";
            };

            btnSave = new Button
            {
                Text = "Kaydet",
                Location = new Point(290, 100),
                Width = 80
            };
            btnSave.Click += (s, e) =>
            {
                try
                {
                    Properties.Settings.Default.CopyPolishApiKey = txtApiKey.Text ?? string.Empty;
                    Properties.Settings.Default.Save();
                    MessageBox.Show("API anahtarı kaydedildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Kaydetme sırasında hata oluştu:\n" + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnClose = new Button
            {
                Text = "Kapat",
                Location = new Point(380, 100),
                Width = 80
            };
            btnClose.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.Controls.Add(lblApiKey);
            this.Controls.Add(txtApiKey);
            this.Controls.Add(btnToggle);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnClose);

            // Load existing value
            this.Load += (s, e) =>
            {
                txtApiKey.Text = Properties.Settings.Default.CopyPolishApiKey ?? string.Empty;
            };
        }
    }
}

