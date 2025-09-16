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
        private Label lblPromptImprove;
        private TextBox txtPromptImprove;
        private Label lblPromptTranslate;
        private TextBox txtPromptTranslate;


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
            this.ClientSize = new Size(560, 680);

            lblApiKey = new Label
            {
                AutoSize = true,
                Text = "API Anahtarı:",
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
                Text = "Göster",
                Location = new Point(452, 16),
                Width = 92
            };
            btnToggle.Click += (s, e) =>
            {
                txtApiKey.UseSystemPasswordChar = !txtApiKey.UseSystemPasswordChar;
                btnToggle.Text = txtApiKey.UseSystemPasswordChar ? "Göster" : "Gizle";
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
                Width = 380
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
                Width = 380
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
                Width = 380
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
                Width = 380
            };

            lblIncludeContext = new Label
            {
                AutoSize = true,
                Text = "Mail bağlamı dahil edilsin mi?",
                Location = new Point(16, 212)
            };

            rdoIncludeYes = new RadioButton
            {
                AutoSize = true,
                Text = "Evet",
                Location = new Point(200, 210),
                Checked = true
            };

            rdoIncludeNo = new RadioButton
            {
                AutoSize = true,
                Text = "Hayır",
                Location = new Point(280, 210)
            };
            
            lblPromptImprove = new Label
            {
                AutoSize = true,
                Text = "İyileştirme Promptu:",
                Location = new Point(16, 250)
            };

            txtPromptImprove = new TextBox
            {
                Location = new Point(16, 270),
                Width = 528,
                Height = 150,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            lblPromptTranslate = new Label
            {
                AutoSize = true,
                Text = "Çeviri Promptu:",
                Location = new Point(16, 430)
            };

            txtPromptTranslate = new TextBox
            {
                Location = new Point(16, 450),
                Width = 528,
                Height = 150,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };

            linkOpenRouter = new LinkLabel
            {
                AutoSize = true,
                Text = "OpenRouter ücretsiz modeller listesi",
                Location = new Point(16, 615),
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
                    MessageBox.Show("Başlatma sırasında hata oluştu:\n" + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnSave = new Button
            {
                Text = "Kaydet",
                Location = new Point(360, 640),
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

                    Properties.Settings.Default.SystemPromptImprove = txtPromptImprove.Text;
                    Properties.Settings.Default.SystemPromptTranslate = txtPromptTranslate.Text;

                    Properties.Settings.Default.Save();
                    MessageBox.Show("Ayarlar kaydedildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                Location = new Point(460, 640),
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
            this.Controls.Add(lblPromptImprove);
            this.Controls.Add(txtPromptImprove);
            this.Controls.Add(lblPromptTranslate);
            this.Controls.Add(txtPromptTranslate);
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

                txtPromptImprove.Text = Properties.Settings.Default.SystemPromptImprove;
                txtPromptTranslate.Text = Properties.Settings.Default.SystemPromptTranslate;
            };
        }
    }
}
  