using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace CopyPolish
{
    public class SettingsForm : Form
    {
        private Label lblApiKey;
        private TextBox txtApiKey;
        private Button btnToggle;
        private Button btnSave;
        private Button btnClose;
        private LinkLabel linkOpenRouter;

        public SettingsForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "CopyPolish Ayarlari";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(480, 160);

            lblApiKey = new Label
            {
                AutoSize = true,
                Text = "API Anahtari:",
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
                Text = "Goster",
                Location = new Point(380, 16),
                Width = 80
            };
            btnToggle.Click += (s, e) =>
            {
                txtApiKey.UseSystemPasswordChar = !txtApiKey.UseSystemPasswordChar;
                btnToggle.Text = txtApiKey.UseSystemPasswordChar ? "Goster" : "Gizle";
            };

            linkOpenRouter = new LinkLabel
            {
                AutoSize = true,
                Text = "OpenRouter ucretsiz modeller listesi",
                Location = new Point(16, 60),
                TabStop = true
            };
            linkOpenRouter.LinkClicked += (s, e) =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "https://openrouter.ai/models?fmt=cards&max_price=0&output_modalities=text&input_modalities=text",
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Baslatma sirasinda hata olustu:\n" + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
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
                    MessageBox.Show("API anahtari kaydedildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Kaydetme sirasinda hata olustu:\n" + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            this.Controls.Add(linkOpenRouter);
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

