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
        private Label lblPrimaryModel;
        private TextBox txtPrimaryModel;
        private Label lblFallback1;
        private TextBox txtFallback1;
        private Label lblFallback2;
        private TextBox txtFallback2;
        private Label lblFallback3;
        private TextBox txtFallback3;
        private Label lblIncludeContext;
        private RadioButton rdoIncludeYes;
        private RadioButton rdoIncludeNo;

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
            this.ClientSize = new Size(560, 360);

            lblApiKey = new Label
            {
                AutoSize = true,
                Text = "API Anahtari:",
                Location = new Point(16, 22)
            };

            txtApiKey = new TextBox
            {
                Location = new Point(160, 18),
                Width = 280,
                UseSystemPasswordChar = true
            };

            btnToggle = new Button
            {
                Text = "Goster",
                Location = new Point(452, 16),
                Width = 92
            };
            btnToggle.Click += (s, e) =>
            {
                txtApiKey.UseSystemPasswordChar = !txtApiKey.UseSystemPasswordChar;
                btnToggle.Text = txtApiKey.UseSystemPasswordChar ? "Goster" : "Gizle";
            };

            lblPrimaryModel = new Label
            {
                AutoSize = true,
                Text = "Birincil Model:",
                Location = new Point(16, 70)
            };

            txtPrimaryModel = new TextBox
            {
                Location = new Point(160, 66),
                Width = 360
            };

            lblFallback1 = new Label
            {
                AutoSize = true,
                Text = "1. Yedek Model:",
                Location = new Point(16, 104)
            };

            txtFallback1 = new TextBox
            {
                Location = new Point(160, 100),
                Width = 360
            };

            lblFallback2 = new Label
            {
                AutoSize = true,
                Text = "2. Yedek Model:",
                Location = new Point(16, 138)
            };

            txtFallback2 = new TextBox
            {
                Location = new Point(160, 134),
                Width = 360
            };

            lblFallback3 = new Label
            {
                AutoSize = true,
                Text = "3. Yedek Model:",
                Location = new Point(16, 172)
            };

            txtFallback3 = new TextBox
            {
                Location = new Point(160, 168),
                Width = 360
            };

            lblIncludeContext = new Label
            {
                AutoSize = true,
                Text = "Mail baglami dahil edilsin mi?",
                Location = new Point(16, 212)
            };

            rdoIncludeYes = new RadioButton
            {
                AutoSize = true,
                Text = "Evet",
                Location = new Point(40, 238)
            };

            rdoIncludeNo = new RadioButton
            {
                AutoSize = true,
                Text = "Hayir",
                Location = new Point(120, 238)
            };

            linkOpenRouter = new LinkLabel
            {
                AutoSize = true,
                Text = "OpenRouter ucretsiz modeller listesi",
                Location = new Point(16, 274),
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
                Location = new Point(360, 310),
                Width = 90
            };
            btnSave.Click += (s, e) =>
            {
                try
                {
                    Properties.Settings.Default.CopyPolishApiKey = txtApiKey.Text ?? string.Empty;

                    var primary = (txtPrimaryModel.Text ?? string.Empty).Trim();
                    var fallback1 = (txtFallback1.Text ?? string.Empty).Trim();
                    var fallback2 = (txtFallback2.Text ?? string.Empty).Trim();
                    var fallback3 = (txtFallback3.Text ?? string.Empty).Trim();
                    var includeContext = rdoIncludeYes.Checked;

                    Properties.Settings.Default.PrimaryModelName = string.IsNullOrWhiteSpace(primary) ? ModelConfiguration.DefaultPrimaryModel : primary;
                    Properties.Settings.Default.FallbackModelName1 = string.IsNullOrWhiteSpace(fallback1) ? ModelConfiguration.DefaultFallbackModel1 : fallback1;
                    Properties.Settings.Default.FallbackModelName2 = string.IsNullOrWhiteSpace(fallback2) ? ModelConfiguration.DefaultFallbackModel2 : fallback2;
                    Properties.Settings.Default.FallbackModelName3 = string.IsNullOrWhiteSpace(fallback3) ? ModelConfiguration.DefaultFallbackModel3 : fallback3;
                    Properties.Settings.Default.IncludeEmailContext = includeContext;

                    Properties.Settings.Default.Save();
                    MessageBox.Show("Ayarlar kaydedildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                Location = new Point(460, 310),
                Width = 90
            };
            btnClose.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };

            this.Controls.Add(lblApiKey);
            this.Controls.Add(txtApiKey);
            this.Controls.Add(btnToggle);
            this.Controls.Add(lblPrimaryModel);
            this.Controls.Add(txtPrimaryModel);
            this.Controls.Add(lblFallback1);
            this.Controls.Add(txtFallback1);
            this.Controls.Add(lblFallback2);
            this.Controls.Add(txtFallback2);
            this.Controls.Add(lblFallback3);
            this.Controls.Add(txtFallback3);
            this.Controls.Add(lblIncludeContext);
            this.Controls.Add(rdoIncludeYes);
            this.Controls.Add(rdoIncludeNo);
            this.Controls.Add(linkOpenRouter);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnClose);

            this.Load += (s, e) =>
            {
                txtApiKey.Text = Properties.Settings.Default.CopyPolishApiKey ?? string.Empty;
                txtPrimaryModel.Text = string.IsNullOrWhiteSpace(Properties.Settings.Default.PrimaryModelName)
                    ? ModelConfiguration.DefaultPrimaryModel
                    : Properties.Settings.Default.PrimaryModelName;
                txtFallback1.Text = string.IsNullOrWhiteSpace(Properties.Settings.Default.FallbackModelName1)
                    ? ModelConfiguration.DefaultFallbackModel1
                    : Properties.Settings.Default.FallbackModelName1;
                txtFallback2.Text = string.IsNullOrWhiteSpace(Properties.Settings.Default.FallbackModelName2)
                    ? ModelConfiguration.DefaultFallbackModel2
                    : Properties.Settings.Default.FallbackModelName2;
                txtFallback3.Text = string.IsNullOrWhiteSpace(Properties.Settings.Default.FallbackModelName3)
                    ? ModelConfiguration.DefaultFallbackModel3
                    : Properties.Settings.Default.FallbackModelName3;

                var includeContext = Properties.Settings.Default.IncludeEmailContext;
                rdoIncludeYes.Checked = includeContext;
                rdoIncludeNo.Checked = !includeContext;
            };
        }
    }
}
