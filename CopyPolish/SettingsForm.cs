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
        private Button btnResetImprove;
        private Label lblHelpImprove;
        private Button btnResetTranslate;
        private Label lblHelpTranslate;

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
            this.ClientSize = new Size(560, 720);

            lblApiKey = new Label { AutoSize = true, Text = "API Anahtarı:", Location = new Point(16, 22) };
            txtApiKey = new TextBox { Location = new Point(160, 18), Width = 280, UseSystemPasswordChar = true };
            btnToggle = new Button { Text = "Göster", Location = new Point(452, 16), Width = 92 };
            btnToggle.Click += (s, e) => {
                txtApiKey.UseSystemPasswordChar = !txtApiKey.UseSystemPasswordChar;
                btnToggle.Text = txtApiKey.UseSystemPasswordChar ? "Göster" : "Gizle";
            };

            lblPrimaryModel = new Label { AutoSize = true, Text = "Birincil Model:", Location = new Point(16, 70) };
            txtPrimaryModel = new TextBox { Location = new Point(160, 66), Width = 380 };
            lblFallback1 = new Label { AutoSize = true, Text = "1. Yedek Model:", Location = new Point(16, 104) };
            txtFallback1 = new TextBox { Location = new Point(160, 100), Width = 380 };
            lblFallback2 = new Label { AutoSize = true, Text = "2. Yedek Model:", Location = new Point(16, 138) };
            txtFallback2 = new TextBox { Location = new Point(160, 134), Width = 380 };
            lblFallback3 = new Label { AutoSize = true, Text = "3. Yedek Model:", Location = new Point(16, 172) };
            txtFallback3 = new TextBox { Location = new Point(160, 168), Width = 380 };

            lblIncludeContext = new Label { AutoSize = true, Text = "Mail bağlamı dahil edilsin mi?", Location = new Point(16, 212) };
            rdoIncludeYes = new RadioButton { AutoSize = true, Text = "Evet", Location = new Point(200, 210), Checked = true };
            rdoIncludeNo = new RadioButton { AutoSize = true, Text = "Hayır", Location = new Point(280, 210) };

            lblPromptImprove = new Label { AutoSize = true, Text = "İyileştirme Promptu:", Location = new Point(16, 250) };
            lblHelpImprove = new Label { AutoSize = true, Text = "(?)", Location = new Point(150, 250), Font = new Font(this.Font, FontStyle.Bold), Cursor = Cursors.Help };
            txtPromptImprove = new TextBox { Location = new Point(16, 270), Width = 528, Height = 150, Multiline = true, ScrollBars = ScrollBars.Vertical };
            btnResetImprove = new Button { Text = "Varsayılana Sıfırla", Location = new Point(418, 425), Width = 126 };

            lblPromptTranslate = new Label { AutoSize = true, Text = "Çeviri Promptu:", Location = new Point(16, 460) };
            lblHelpTranslate = new Label { AutoSize = true, Text = "(?)", Location = new Point(120, 460), Font = new Font(this.Font, FontStyle.Bold), Cursor = Cursors.Help };
            txtPromptTranslate = new TextBox { Location = new Point(16, 480), Width = 528, Height = 150, Multiline = true, ScrollBars = ScrollBars.Vertical };
            btnResetTranslate = new Button { Text = "Varsayılana Sıfırla", Location = new Point(418, 635), Width = 126 };

            linkOpenRouter = new LinkLabel { AutoSize = true, Text = "OpenRouter ücretsiz modeller listesi", Location = new Point(16, 660), TabStop = true };
            linkOpenRouter.LinkClicked += (s, e) => {
                try { Process.Start(new ProcessStartInfo { FileName = "https://openrouter.ai/models?fmt=cards&max_price=0&output_modalities=text&input_modalities=text", UseShellExecute = true }); }
                catch (Exception ex) { MessageBox.Show("Başlatma sırasında hata oluştu:\n" + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };

            btnSave = new Button { Text = "Kaydet", Location = new Point(360, 680), Width = 90 };
            btnClose = new Button { Text = "Kapat", Location = new Point(460, 680), Width = 90 };
            
            btnResetImprove.Click += (s, e) => { txtPromptImprove.Text = ModelConfiguration.DefaultSystemPromptImprove; };
            btnResetTranslate.Click += (s, e) => { txtPromptTranslate.Text = ModelConfiguration.DefaultSystemPromptTranslate; };

            lblHelpImprove.Click += (s, e) => {
                MessageBox.Show("Etkili bir iyileştirme prompt'u, yapay zekaya net talimatlar vermelidir.\n\n- Tonu belirtin (örn: 'Daha resmi yap', 'Daha samimi bir dil kullan').\n- Amacı belirtin (örn: 'Metni kısalt', 'Ana fikri vurgula').\n- Kurallarınızı numaralandırarak veya maddeler halinde listeleyin.", "İyileştirme Promptu İpuçları", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            lblHelpTranslate.Click += (s, e) => {
                MessageBox.Show("Etkili bir çeviri prompt'u, bağlamı ve formatı korumaya odaklanmalıdır.\n\n- Kaynak ve hedef dilleri netleştirin (örn: 'Türkçe'den İngilizce'ye çevir').\n- Anlamı ve tonu korumasını isteyin.\n- Formatlama kurallarını belirtin (örn: 'Satır sonlarını ve paragrafları koru').", "Çeviri Promptu İpuçları", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            btnSave.Click += (s, e) => {
                try
                {
                    Properties.Settings.Default.CopyPolishApiKey = txtApiKey.Text;
                    Properties.Settings.Default.PrimaryModelName = string.IsNullOrWhiteSpace(txtPrimaryModel.Text) ? ModelConfiguration.DefaultPrimaryModel : txtPrimaryModel.Text.Trim();
                    Properties.Settings.Default.FallbackModelName1 = string.IsNullOrWhiteSpace(txtFallback1.Text) ? ModelConfiguration.DefaultFallbackModel1 : txtFallback1.Text.Trim();
                    Properties.Settings.Default.FallbackModelName2 = string.IsNullOrWhiteSpace(txtFallback2.Text) ? ModelConfiguration.DefaultFallbackModel2 : txtFallback2.Text.Trim();
                    Properties.Settings.Default.FallbackModelName3 = string.IsNullOrWhiteSpace(txtFallback3.Text) ? ModelConfiguration.DefaultFallbackModel3 : txtFallback3.Text.Trim();
                    Properties.Settings.Default.IncludeEmailContext = rdoIncludeYes.Checked;
                    Properties.Settings.Default.SystemPromptImprove = txtPromptImprove.Text;
                    Properties.Settings.Default.SystemPromptTranslate = txtPromptTranslate.Text;
                    Properties.Settings.Default.Save();
                    MessageBox.Show("Ayarlar kaydedildi.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex) { MessageBox.Show("Kaydetme sırasında hata oluştu:\n" + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };
            
            btnClose.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.AddRange(new Control[] { lblApiKey, txtApiKey, btnToggle, lblPrimaryModel, txtPrimaryModel, lblFallback1, txtFallback1, lblFallback2, txtFallback2, lblFallback3, txtFallback3, lblIncludeContext, rdoIncludeYes, rdoIncludeNo, lblPromptImprove, lblHelpImprove, txtPromptImprove, btnResetImprove, lblPromptTranslate, lblHelpTranslate, txtPromptTranslate, btnResetTranslate, linkOpenRouter, btnSave, btnClose });

            this.Load += (s, e) => {
                txtApiKey.Text = Properties.Settings.Default.CopyPolishApiKey;
                txtPrimaryModel.Text = string.IsNullOrWhiteSpace(Properties.Settings.Default.PrimaryModelName) ? ModelConfiguration.DefaultPrimaryModel : Properties.Settings.Default.PrimaryModelName;
                txtFallback1.Text = string.IsNullOrWhiteSpace(Properties.Settings.Default.FallbackModelName1) ? ModelConfiguration.DefaultFallbackModel1 : Properties.Settings.Default.FallbackModelName1;
                txtFallback2.Text = string.IsNullOrWhiteSpace(Properties.Settings.Default.FallbackModelName2) ? ModelConfiguration.DefaultFallbackModel2 : Properties.Settings.Default.FallbackModelName2;
                txtFallback3.Text = string.IsNullOrWhiteSpace(Properties.Settings.Default.FallbackModelName3) ? ModelConfiguration.DefaultFallbackModel3 : Properties.Settings.Default.FallbackModelName3;
                rdoIncludeYes.Checked = Properties.Settings.Default.IncludeEmailContext;
                rdoIncludeNo.Checked = !Properties.Settings.Default.IncludeEmailContext;
                txtPromptImprove.Text = string.IsNullOrWhiteSpace(Properties.Settings.Default.SystemPromptImprove) ? ModelConfiguration.DefaultSystemPromptImprove : Properties.Settings.Default.SystemPromptImprove;
                txtPromptTranslate.Text = string.IsNullOrWhiteSpace(Properties.Settings.Default.SystemPromptTranslate) ? ModelConfiguration.DefaultSystemPromptTranslate : Properties.Settings.Default.SystemPromptTranslate;
            };
        }
    }
}
  