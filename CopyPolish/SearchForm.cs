using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Outlook = Microsoft.Office.Interop.Outlook;
using System.Runtime.InteropServices;

namespace CopyPolish
{
    public class SearchForm : Form
    {
        // Debug log dosyası - TEMP klasörüne yaz
        private static readonly string DebugLogPath = Path.Combine(
            Path.GetTempPath(), 
            "CopyPolish_SearchDebug.log");
        
        // PERFORMANS: Debug log'ları varsayılan olarak kapalı (production için)
        // Sorun giderme için true yapılabilir
        private static readonly bool EnableDebugLog = true;
        
        private static void DebugLog(string message)
        {
            if (!EnableDebugLog) return; // Hızlı çıkış
            
            try
            {
                string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                File.AppendAllText(DebugLogPath, logLine + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // Log yazılamıyorsa sessizce devam et
                System.Diagnostics.Debug.WriteLine($"Log yazma hatası: {ex.Message}");
            }
        }
        
        // Modern Renk Paleti
        private static readonly Color PrimaryColor = Color.FromArgb(0, 120, 215);      // Ana mavi
        private static readonly Color PrimaryDarkColor = Color.FromArgb(0, 99, 177);   // Koyu mavi (hover)
        private static readonly Color AccentColor = Color.FromArgb(0, 153, 255);       // Açık mavi accent
        private static readonly Color BackgroundColor = Color.FromArgb(243, 243, 243); // Açık gri arka plan
        private static readonly Color CardColor = Color.White;                          // Kart arka planı
        private static readonly Color BorderColor = Color.FromArgb(218, 218, 218);     // Kenarlık rengi
        private static readonly Color TextColor = Color.FromArgb(51, 51, 51);          // Ana metin rengi
        private static readonly Color TextSecondaryColor = Color.FromArgb(102, 102, 102); // İkincil metin
        private static readonly Color SuccessColor = Color.FromArgb(16, 124, 16);      // Başarı yeşili
        private static readonly Color WarningColor = Color.FromArgb(255, 185, 0);      // Uyarı sarısı
        private static readonly Color GridAlternateColor = Color.FromArgb(250, 250, 252); // Grid alternatif satır
        private static readonly Color HighlightColor = Color.FromArgb(255, 248, 220);  // Vurgulama sarısı

        private TreeView treeFolders;
        private Button btnSearch;
        private Button btnClear; // Temizle butonu
        private DataGridView gridResults;
        private Label lblStatus;
        private SplitContainer splitContainer;
        private SplitContainer resultsSplitContainer; // Sonuç ve önizleme için
        private Panel topPanel;
        private BackgroundWorker searchWorker;
        private bool isSearching = false;
        private bool suppressSelectionChanged = false; // Grid seçim olayını programatik değişikliklerde bastır
        private string currentSearchQuery = "";
        private HashSet<string> addedEntryIds = new HashSet<string>(); // Mükerrer sonuçları önle
        private int lastAddedCount = 0; // Son eklenen sonuç sayısı
        private ComboBox cmbAttachmentFilter; // Ek filtresi
        private ComboBox cmbDateFilter; // Tarih filtresi
        private DateTimePicker dtpFrom; // Özel tarih başlangıç
        private DateTimePicker dtpTo; // Özel tarih bitiş
        private ComboBox cmbSearchHistory; // Arama geçmişi
        private ComboBox cmbReadFilter; // Okundu/Okunmadı filtresi
        private ComboBox cmbImportanceFilter; // Önem filtresi
        private TextBox txtFromFilter; // Gönderen filtresi
        private TextBox txtToFilter; // Alıcı filtresi
        private TextBox txtCcFilter; // Bilgi filtresi
        private TextBox txtSubjectFilter; // Konu filtresi
        private ComboBox cmbFromLogic; // Gönderen mantığı (VE/VEYA)
        private ComboBox cmbToLogic; // Alıcı mantığı
        private ComboBox cmbCcLogic; // Bilgi mantığı
        private ComboBox cmbSubjectLogic; // Konu mantığı
        private RichTextBox txtPreview; // Mail önizleme
        private Label lblPreviewSubject; // Önizleme konu
        private Label lblPreviewFrom; // Önizleme gönderen
        private Label lblPreviewDate; // Önizleme tarih
        private Label lblPreviewPlaceholder; // Önizleme placeholder
        private static List<string> searchHistory = new List<string>(); // Arama geçmişi (statik - oturumlar arası)
        private static readonly CultureInfo TurkishCulture = new CultureInfo("tr-TR"); // Türkçe karakter desteği
        
        // Türkçe büyük/küçük harf dönüşümü için yardımcı metod
        private static string ToTurkishLower(string text)
        {
            return text.ToLower(TurkishCulture);
        }
        
        private static string ToTurkishUpper(string text)
        {
            return text.ToUpper(TurkishCulture);
        }
        
        // Türkçe title case (ilk harf büyük)
        private static string ToTurkishTitleCase(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return char.ToUpper(text[0], TurkishCulture) + (text.Length > 1 ? text.Substring(1).ToLower(TurkishCulture) : "");
        }
        
        // SQL için özel karakterleri escape et
        private static string EscapeSqlString(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Replace("'", "''").Replace("%", "[%]").Replace("_", "[_]");
        }
        
        // Türkçe duyarlı içerik arama (i/İ, ı/I sorununu çözer)
        private static int TurkishIndexOf(string source, string value)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(value)) return -1;
            return TurkishCulture.CompareInfo.IndexOf(source, value, CompareOptions.IgnoreCase);
        }
        
        // Türkçe karakter varyasyonları oluştur (SQL aramasi için)
        // PERFORMANS OPTİMİZASYONU: Sadece en gerekli varyasyonlar (küçük harf + ı/i değişimi)
        // SQL LIKE case-insensitive olduğu için büyük harf varyasyonlarına gerek yok
        private static List<string> GetTurkishVariations(string text)
        {
            if (string.IsNullOrEmpty(text)) return new List<string> { text };
            
            var variations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // Sadece küçük harf varyasyonları yeterli (SQL case-insensitive)
            string lower = ToTurkishLower(text);
            variations.Add(lower);
            
            // Türkçe karakterlerin İngilizce karşılıkları ve tersi
            // ı->i, i->ı, ş->s, s->ş, ğ->g, g->ğ, ü->u, u->ü, ö->o, o->ö, ç->c, c->ç
            
            // Basit varyasyonlar (tek karakter değişimi)
            // Karmaşık kombinasyonlar (örn: hem ş hem ü içeren kelimeler) için recursive gerekebilir
            // ama performans için şimdilik sadece temel varyasyonları ekliyoruz.
            
            // 1. İngilizce karakterlere dönüştürülmüş hali (en yaygın senaryo)
            string english = lower.Replace('ı', 'i')
                                  .Replace('ş', 's')
                                  .Replace('ğ', 'g')
                                  .Replace('ü', 'u')
                                  .Replace('ö', 'o')
                                  .Replace('ç', 'c');
            variations.Add(english);
            
            // 2. Kritik ı/i değişimi (tek başına)
            if (lower.Contains('ı')) variations.Add(lower.Replace('ı', 'i'));
            if (lower.Contains('i')) variations.Add(lower.Replace('i', 'ı'));
            
            // 3. Diğer karakterler (tek başına)
            if (lower.Contains('ş')) variations.Add(lower.Replace('ş', 's'));
            if (lower.Contains('s')) variations.Add(lower.Replace('s', 'ş'));
            
            if (lower.Contains('ğ')) variations.Add(lower.Replace('ğ', 'g'));
            if (lower.Contains('g')) variations.Add(lower.Replace('g', 'ğ'));
            
            if (lower.Contains('ü')) variations.Add(lower.Replace('ü', 'u'));
            if (lower.Contains('u')) variations.Add(lower.Replace('u', 'ü'));
            
            if (lower.Contains('ö')) variations.Add(lower.Replace('ö', 'o'));
            if (lower.Contains('o')) variations.Add(lower.Replace('o', 'ö'));
            
            if (lower.Contains('ç')) variations.Add(lower.Replace('ç', 'c'));
            if (lower.Contains('c')) variations.Add(lower.Replace('c', 'ç'));
            
            return variations.ToList();
        }
        
        // Modern buton için yardımcı metod
        private Button CreateModernButton(string text, Color bgColor, Color foreColor, int width = 100, int height = 32)
        {
            Button btn = new Button
            {
                Text = text,
                Width = width,
                Height = height,
                FlatStyle = FlatStyle.Flat,
                BackColor = bgColor,
                ForeColor = foreColor,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(bgColor, 0.1f);
            btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(bgColor, 0.2f);
            return btn;
        }
        
        // Modern ComboBox stili
        private void StyleComboBox(ComboBox cmb)
        {
            cmb.FlatStyle = FlatStyle.Flat;
            cmb.BackColor = CardColor;
            cmb.ForeColor = TextColor;
            cmb.Font = new Font("Segoe UI", 9);
        }
        
        // Modern Label stili
        private Label CreateStyledLabel(string text, bool isBold = false, int fontSize = 9)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                ForeColor = TextSecondaryColor,
                Font = new Font("Segoe UI", fontSize, isBold ? FontStyle.Bold : FontStyle.Regular)
            };
        }

        public SearchForm()
        {
            InitializeComponent();
            InitializeBackgroundWorker();
            LoadFolders();
            
            // Form kapanırken worker'ı iptal et
            this.FormClosing += (s, e) =>
            {
                if (searchWorker != null && searchWorker.IsBusy)
                {
                    searchWorker.CancelAsync();
                }
            };
        }

        private void InitializeBackgroundWorker()
        {
            searchWorker = new BackgroundWorker();
            searchWorker.WorkerReportsProgress = true;
            searchWorker.WorkerSupportsCancellation = true;
            searchWorker.DoWork += SearchWorker_DoWork;
            searchWorker.ProgressChanged += SearchWorker_ProgressChanged;
            searchWorker.RunWorkerCompleted += SearchWorker_RunWorkerCompleted;
        }

        private void InitializeComponent()
        {
            this.Text = "Gelişmiş Arama - CopyPolish";
            this.Size = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = BackgroundColor;
            this.Font = new Font("Segoe UI", 9);

            // ═══════════════════════════════════════════════════════════════
            // TOP PANEL - Modern Arama Kartı
            // ═══════════════════════════════════════════════════════════════
            topPanel = new Panel 
            { 
                Dock = DockStyle.Top, 
                Height = 190, 
                Padding = new Padding(12),
                BackColor = CardColor
            };
            
            // Üst kenarlık çizgisi efekti
            Panel topBorder = new Panel
            {
                Dock = DockStyle.Top,
                Height = 3,
                BackColor = PrimaryColor
            };
            
            // === SATIR 1 - Arama Kutusu ===
            Panel searchRow = new Panel { Location = new Point(12, 8), Size = new Size(1160, 34) };
            
            // Arama geçmişi ComboBox - Modern stil
            cmbSearchHistory = new ComboBox 
            { 
                Location = new Point(15, 4), 
                Width = 535, 
                Height = 30,
                Font = new Font("Segoe UI", 11),
                DropDownStyle = ComboBoxStyle.DropDown,
                FlatStyle = FlatStyle.Standard,
                BackColor = Color.White,
                ForeColor = TextColor
            };
            // Kenarlık efekti için parent panel
            Panel searchBoxContainer = new Panel
            {
                Location = new Point(12, 2),
                Size = new Size(541, 32),
                BackColor = BorderColor,
                Padding = new Padding(1)
            };
            cmbSearchHistory.Location = new Point(1, 1);
            cmbSearchHistory.Width = 537;
            searchBoxContainer.Controls.Add(cmbSearchHistory);
            cmbSearchHistory.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) PerformSearch(); };
            foreach (var item in searchHistory)
            {
                cmbSearchHistory.Items.Add(item);
            }

            // Modern Ara butonu
            btnSearch = CreateModernButton("Ara", PrimaryColor, Color.White, 90, 30);
            btnSearch.Location = new Point(560, 3);
            btnSearch.Click += (s, e) => PerformSearch();
            
            // Temizle butonu - X
            btnClear = CreateModernButton("X", Color.FromArgb(220, 53, 69), Color.White, 36, 30);
            btnClear.Location = new Point(655, 3);
            btnClear.Visible = false;
            btnClear.Click += (s, e) => ClearAll();
            
            // Bilgi ikonu (?) - Tooltip ile kullanım bilgisi
            Label lblInfo = new Label
            {
                Text = "?",
                Location = new Point(700, 3),
                Size = new Size(28, 28),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = PrimaryColor,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            // Yuvarlak görünüm için
            lblInfo.Paint += (s, pe) =>
            {
                pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(PrimaryColor))
                {
                    pe.Graphics.FillEllipse(brush, 0, 0, lblInfo.Width - 1, lblInfo.Height - 1);
                }
                TextRenderer.DrawText(pe.Graphics, "?", lblInfo.Font, new Rectangle(0, 0, lblInfo.Width, lblInfo.Height), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            
            // ToolTip oluştur
            ToolTip infoToolTip = new ToolTip
            {
                AutoPopDelay = 15000, // 15 saniye görünsün
                InitialDelay = 200,
                ReshowDelay = 100,
                ShowAlways = true,
                IsBalloon = false
            };
            
            string tooltipText = 
                "ARAMA İPUÇLARI\n" +
                "─────────────────────────────\n\n" +
                "• Normal arama: Yazdığınız kelimeler konu,\n" +
                "  gönderen ve içerikte aranır.\n\n" +
                "• Tam ifade araması: Tırnak içinde yazın.\n" +
                "  Örnek: \"Toplantı daveti\"\n\n" +
                "• Kişi adı araması: Gönderen adını yazın.\n" +
                "  Örnek: Ahmet veya ahmet.yilmaz\n\n" +
                "• Çoklu kelime: Boşlukla ayırın.\n" +
                "  Örnek: proje rapor (her ikisi de aranır)\n\n" +
                "• Filtreler: Tarih, ek, okunma durumu ve\n" +
                "  önem derecesine göre filtreleyebilirsiniz.";
            
            infoToolTip.SetToolTip(lblInfo, tooltipText);
            
            cmbSearchHistory.TextChanged += (s, e) => UpdateClearButtonVisibility();
            
            searchRow.Controls.AddRange(new Control[] { searchBoxContainer, btnSearch, btnClear, lblInfo });

            // === SATIR 2 - Filtreler (Tarih ve Durum) ===
            Panel filterRow1 = new Panel { Location = new Point(12, 44), Size = new Size(1160, 34) };
            
            // Tarih Grubu
            Label lblDateIcon = CreateStyledLabel("Tarih:", true);
            lblDateIcon.Location = new Point(0, 8);
            
            cmbDateFilter = new ComboBox 
            { 
                Location = new Point(70, 4), 
                Width = 140, 
                Height = 28,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            StyleComboBox(cmbDateFilter);
            cmbDateFilter.Items.AddRange(new object[] { "Tümü", "Bugün", "Son 7 Gün", "Son 30 Gün", "Son 3 Ay", "Özel Aralık" });
            cmbDateFilter.SelectedIndex = 0;
            cmbDateFilter.SelectedIndexChanged += CmbDateFilter_SelectedIndexChanged;
            
            dtpFrom = new DateTimePicker 
            { 
                Location = new Point(220, 4), 
                Width = 120, 
                Height = 28,
                Format = DateTimePickerFormat.Short, 
                Visible = false,
                Font = new Font("Segoe UI", 9)
            };
            Label lblDateDash = CreateStyledLabel("—");
            lblDateDash.Location = new Point(345, 8);
            lblDateDash.Visible = false;
            
            dtpTo = new DateTimePicker 
            { 
                Location = new Point(365, 4), 
                Width = 120, 
                Height = 28,
                Format = DateTimePickerFormat.Short, 
                Visible = false,
                Font = new Font("Segoe UI", 9)
            };
            dtpFrom.Value = DateTime.Now.AddMonths(-1);
            dtpTo.Value = DateTime.Now;
            
            // Durum Grubu (sağ taraf)
            Label lblReadIcon = CreateStyledLabel("Durum:", true);
            lblReadIcon.Location = new Point(520, 8);
            
            cmbReadFilter = new ComboBox 
            { 
                Location = new Point(600, 4), 
                Width = 120, 
                Height = 28,
                DropDownStyle = ComboBoxStyle.DropDownList 
            };
            StyleComboBox(cmbReadFilter);
            cmbReadFilter.Items.AddRange(new object[] { "Tümü", "Okunmamış", "Okunmuş" });
            cmbReadFilter.SelectedIndex = 0;
            
            filterRow1.Controls.AddRange(new Control[] { 
                lblDateIcon, cmbDateFilter, dtpFrom, lblDateDash, dtpTo,
                lblReadIcon, cmbReadFilter
            });

            // === SATIR 3 - Ek Filtreler (Ek ve Önem) ===
            Panel filterRow2 = new Panel { Location = new Point(12, 80), Size = new Size(1160, 34) };
            
            // Ek Grubu
            Label lblAttachIcon = CreateStyledLabel("Ek:", true);
            lblAttachIcon.Location = new Point(0, 8);
            
            cmbAttachmentFilter = new ComboBox 
            { 
                Location = new Point(70, 4), 
                Width = 140, 
                Height = 28,
                DropDownStyle = ComboBoxStyle.DropDownList 
            };
            StyleComboBox(cmbAttachmentFilter);
            cmbAttachmentFilter.Items.AddRange(new object[] { "Tümü", "Eki Olan", "Eki Olmayan" });
            cmbAttachmentFilter.SelectedIndex = 0;
            
            // Önem Grubu
            Label lblImportIcon = CreateStyledLabel("Önem:", true);
            lblImportIcon.Location = new Point(250, 8);
            
            cmbImportanceFilter = new ComboBox 
            { 
                Location = new Point(320, 4), 
                Width = 130, 
                Height = 28,
                DropDownStyle = ComboBoxStyle.DropDownList 
            };
            StyleComboBox(cmbImportanceFilter);
            cmbImportanceFilter.Items.AddRange(new object[] { "Tümü", "Yüksek", "Normal", "Düşük" });
            cmbImportanceFilter.SelectedIndex = 0;

            filterRow2.Controls.AddRange(new Control[] { 
                lblAttachIcon, cmbAttachmentFilter, 
                lblImportIcon, cmbImportanceFilter
            });

            // === SATIR 3 - Gönderen/Alıcı/Bilgi/Konu ===
            Panel filterRow3 = new Panel { Location = new Point(12, 116), Size = new Size(1160, 40) };

            // Mantık combobox oluşturucu
            ComboBox CreateLogicCombo()
            {
                var cmb = new ComboBox
                {
                    Width = 60,
                    Height = 26,
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Font = new Font("Segoe UI", 9)
                };
                cmb.Items.AddRange(new object[] { "VE", "VEYA" });
                cmb.SelectedIndex = 0;
                return cmb;
            }

            Label lblFrom = CreateStyledLabel("Gönderen:", true);
            lblFrom.Location = new Point(0, 10);
            txtFromFilter = new TextBox
            {
                Location = new Point(80, 6),
                Width = 150,
                Height = 26,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9)
            };
            cmbFromLogic = CreateLogicCombo();
            cmbFromLogic.Location = new Point(240, 6);

            Label lblTo = CreateStyledLabel("Alıcı:", true);
            lblTo.Location = new Point(300, 10);
            txtToFilter = new TextBox
            {
                Location = new Point(350, 6),
                Width = 150,
                Height = 26,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9)
            };
            cmbToLogic = CreateLogicCombo();
            cmbToLogic.Location = new Point(510, 6);

            Label lblCc = CreateStyledLabel("Bilgi:", true);
            lblCc.Location = new Point(570, 10);
            txtCcFilter = new TextBox
            {
                Location = new Point(620, 6),
                Width = 150,
                Height = 26,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9)
            };
            cmbCcLogic = CreateLogicCombo();
            cmbCcLogic.Location = new Point(780, 6);

            Label lblSubject = CreateStyledLabel("Konu:", true);
            lblSubject.Location = new Point(840, 10);
            txtSubjectFilter = new TextBox
            {
                Location = new Point(890, 6),
                Width = 150,
                Height = 26,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9)
            };
            cmbSubjectLogic = CreateLogicCombo();
            cmbSubjectLogic.Location = new Point(1050, 6);
            cmbSubjectLogic.Visible = false; // Konu son kutu olduğu için gizli (yer tutucu)

            filterRow3.Controls.AddRange(new Control[] {
                lblFrom, txtFromFilter, cmbFromLogic,
                lblTo, txtToFilter, cmbToLogic,
                lblCc, txtCcFilter, cmbCcLogic,
                lblSubject, txtSubjectFilter
            });

            topPanel.Controls.AddRange(new Control[] { searchRow, filterRow1, filterRow2, filterRow3 });
            topPanel.Controls.Add(topBorder);
            topBorder.SendToBack();

            // ═══════════════════════════════════════════════════════════════
            // STATUS BAR - Modern Alt Bilgi Çubuğu
            // ═══════════════════════════════════════════════════════════════
            Panel statusPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                BackColor = Color.FromArgb(248, 249, 250)
            };
            
            Panel statusTopLine = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = BorderColor
            };
            statusPanel.Controls.Add(statusTopLine);
            
            lblStatus = new Label 
            { 
                Dock = DockStyle.Fill, 
                Text = "Hazır - Aramaya başlamak için metin girin", 
                TextAlign = ContentAlignment.MiddleLeft, 
                Padding = new Padding(10, 0, 10, 0),
                ForeColor = TextSecondaryColor,
                Font = new Font("Segoe UI", 9)
            };
            statusPanel.Controls.Add(lblStatus);

            // ═══════════════════════════════════════════════════════════════
            // ANA İÇERİK - Split Container
            // ═══════════════════════════════════════════════════════════════
            splitContainer = new SplitContainer 
            { 
                Dock = DockStyle.Fill, 
                Orientation = Orientation.Vertical,
                SplitterWidth = 6,
                BackColor = BackgroundColor
            };
            // MinSize değerlerini ayrı ayarla (object initializer'da sorun çıkıyor)
            splitContainer.Panel1MinSize = 50;
            splitContainer.Panel2MinSize = 50;
            
            this.Load += (s, e) => {
                try
                {
                    // Form yüklendikten sonra SplitterDistance ayarla
                    int availableWidth = splitContainer.Width - splitContainer.SplitterWidth;
                    if (availableWidth > 100)
                    {
                        int newDistance = (int)(availableWidth * 0.18);
                        newDistance = Math.Max(50, Math.Min(newDistance, availableWidth - 50));
                        splitContainer.SplitterDistance = newDistance;
                    }
                    
                    // resultsSplitContainer için de yükseklik ayarla
                    int availableHeight = resultsSplitContainer.Height - resultsSplitContainer.SplitterWidth;
                    if (availableHeight > 100)
                    {
                        int resultDistance = (int)(availableHeight * 0.55); // %55 üstte (grid)
                        resultDistance = Math.Max(50, Math.Min(resultDistance, availableHeight - 50));
                        resultsSplitContainer.SplitterDistance = resultDistance;
                    }
                }
                catch { }
            };

            // ═══════════════════════════════════════════════════════════════
            // SOL PANEL - Klasör Ağacı
            // ═══════════════════════════════════════════════════════════════
            Panel folderPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardColor,
                Padding = new Padding(8)
            };
            
            Label lblFolders = new Label 
            { 
                Text = "Klasörler", 
                Dock = DockStyle.Top, 
                Height = 32, 
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = TextColor,
                Padding = new Padding(5, 8, 5, 0),
                BackColor = CardColor
            };
            
            Panel folderDivider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = BorderColor
            };
            
            treeFolders = new TreeView 
            { 
                Dock = DockStyle.Fill, 
                CheckBoxes = true,
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.None,
                BackColor = CardColor,
                ForeColor = TextColor,
                ItemHeight = 24,
                FullRowSelect = true,
                ShowLines = true,
                ShowPlusMinus = true
            };
            
            // Parent seçilince alt klasörler de seçilsin
            treeFolders.AfterCheck += TreeFolders_AfterCheck;
            
            folderPanel.Controls.Add(treeFolders);
            folderPanel.Controls.Add(folderDivider);
            folderPanel.Controls.Add(lblFolders);
            
            splitContainer.Panel1.Controls.Add(folderPanel);
            splitContainer.Panel1.Padding = new Padding(0, 0, 3, 0);

            // ═══════════════════════════════════════════════════════════════
            // SAĞ PANEL - Sonuçlar + Önizleme
            // ═══════════════════════════════════════════════════════════════
            resultsSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterWidth = 6,
                BackColor = BackgroundColor
            };
            // MinSize değerlerini düşük tut, form yüklendikten sonra ayarlanacak
            resultsSplitContainer.Panel1MinSize = 25;
            resultsSplitContainer.Panel2MinSize = 25;

            // --- Üst: Sonuç Grid ---
            Panel resultsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardColor,
                Padding = new Padding(0)
            };
            
            Label lblResults = new Label 
            { 
                Text = "Sonuçlar", 
                Dock = DockStyle.Top, 
                Height = 32, 
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = TextColor,
                Padding = new Padding(10, 8, 10, 0),
                BackColor = CardColor
            };
            
            Panel resultsDivider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = BorderColor
            };
            
            // Modern DataGridView
            gridResults = new DataGridView();
            gridResults.Dock = DockStyle.Fill;
            gridResults.AllowUserToAddRows = false;
            gridResults.AllowUserToDeleteRows = false;
            gridResults.ReadOnly = true;
            gridResults.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridResults.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridResults.BackgroundColor = CardColor;
            gridResults.BorderStyle = BorderStyle.None;
            gridResults.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            gridResults.GridColor = BorderColor;
            gridResults.RowHeadersVisible = false;
            gridResults.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            gridResults.ColumnHeadersHeight = 36;
            gridResults.RowTemplate.Height = 32;
            gridResults.EnableHeadersVisualStyles = false;
            gridResults.MultiSelect = false;
            
            // Header stili
            gridResults.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 249, 250),
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                SelectionBackColor = Color.FromArgb(248, 249, 250),
                SelectionForeColor = TextColor
            };
            
            // Hücre stili
            gridResults.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = CardColor,
                ForeColor = TextColor,
                Font = new Font("Segoe UI", 9),
                SelectionBackColor = Color.FromArgb(204, 229, 255),
                SelectionForeColor = TextColor,
                Padding = new Padding(8, 0, 0, 0)
            };
            
            // Alternatif satır rengi
            gridResults.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = GridAlternateColor,
                ForeColor = TextColor,
                SelectionBackColor = Color.FromArgb(204, 229, 255),
                SelectionForeColor = TextColor
            };
            
            gridResults.Columns.Add("Subject", "Konu");
            gridResults.Columns.Add("MatchText", "Eşleşme");
            gridResults.Columns.Add("Sender", "Gönderen");
            gridResults.Columns.Add("Time", "Tarih");
            gridResults.Columns.Add("Folder", "Klasör");
            
            gridResults.Columns["Subject"].FillWeight = 28;
            gridResults.Columns["MatchText"].FillWeight = 27;
            gridResults.Columns["Sender"].FillWeight = 17;
            gridResults.Columns["Time"].FillWeight = 13;
            gridResults.Columns["Folder"].FillWeight = 15;

            gridResults.CellDoubleClick += GridResults_CellDoubleClick;
            gridResults.SelectionChanged += GridResults_SelectionChanged;

            resultsPanel.Controls.Add(gridResults);
            resultsPanel.Controls.Add(resultsDivider);
            resultsPanel.Controls.Add(lblResults);
            
            resultsSplitContainer.Panel1.Controls.Add(resultsPanel);
            resultsSplitContainer.Panel1.Padding = new Padding(3, 0, 0, 3);

            // --- Alt: Önizleme Paneli ---
            Panel previewContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardColor,
                Padding = new Padding(0)
            };
            
            Label lblPreviewTitle = new Label 
            { 
                Text = "Önizleme", 
                Dock = DockStyle.Top, 
                Height = 32, 
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = TextColor,
                Padding = new Padding(10, 8, 10, 0),
                BackColor = Color.FromArgb(248, 249, 250)
            };
            
            Panel previewDivider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = BorderColor
            };
            
            Panel previewPanel = new Panel 
            { 
                Dock = DockStyle.Fill, 
                BackColor = CardColor, 
                Padding = new Padding(12) 
            };
            
            Panel previewHeader = new Panel 
            { 
                Dock = DockStyle.Top, 
                Height = 75, 
                Padding = new Padding(0, 5, 0, 5), 
                Visible = false,
                BackColor = CardColor
            };
            
            lblPreviewSubject = new Label 
            { 
                Text = "", 
                Font = new Font("Segoe UI", 11, FontStyle.Bold), 
                Location = new Point(0, 2), 
                AutoSize = true, 
                MaximumSize = new Size(700, 0),
                ForeColor = TextColor
            };
            lblPreviewFrom = new Label 
            { 
                Text = "", 
                Font = new Font("Segoe UI", 9), 
                Location = new Point(0, 28), 
                AutoSize = true, 
                ForeColor = PrimaryColor
            };
            lblPreviewDate = new Label 
            { 
                Text = "", 
                Font = new Font("Segoe UI", 9), 
                Location = new Point(0, 50), 
                AutoSize = true, 
                ForeColor = TextSecondaryColor
            };
            previewHeader.Controls.AddRange(new Control[] { lblPreviewSubject, lblPreviewFrom, lblPreviewDate });
            
            txtPreview = new RichTextBox 
            { 
                Dock = DockStyle.Fill, 
                ReadOnly = true, 
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 10),
                BackColor = CardColor,
                ForeColor = TextColor,
                Visible = false
            };
            
            lblPreviewPlaceholder = new Label 
            { 
                Text = "Önizlemek için bir sonuç seçin", 
                Dock = DockStyle.Fill, 
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = TextSecondaryColor,
                Font = new Font("Segoe UI", 11),
                BackColor = CardColor
            };

            previewPanel.Controls.Add(txtPreview);
            previewPanel.Controls.Add(lblPreviewPlaceholder);
            previewPanel.Controls.Add(previewHeader);
            
            previewContainer.Controls.Add(previewPanel);
            previewContainer.Controls.Add(previewDivider);
            previewContainer.Controls.Add(lblPreviewTitle);
            
            resultsSplitContainer.Panel2.Controls.Add(previewContainer);
            resultsSplitContainer.Panel2.Padding = new Padding(3, 3, 0, 0);

            splitContainer.Panel2.Controls.Add(resultsSplitContainer);
            splitContainer.Panel2.Padding = new Padding(0);

            // Form'a kontrolleri ekle (doğru sırada)
            this.Controls.Add(splitContainer);
            this.Controls.Add(topPanel);
            this.Controls.Add(statusPanel);
        }

        private void CmbDateFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool showCustom = cmbDateFilter.SelectedItem?.ToString() == "Özel Aralık";
            dtpFrom.Visible = showCustom;
            dtpTo.Visible = showCustom;
            // lblTo da görünür olmalı
            foreach (Control c in dtpFrom.Parent.Controls)
            {
                if (c is Label lbl && lbl.Text == "—")
                {
                    lbl.Visible = showCustom;
                }
            }
        }

        private void GridResults_SelectionChanged(object sender, EventArgs e)
        {
            if (suppressSelectionChanged) return;
            
            if (gridResults.SelectedRows.Count > 0)
            {
                var tag = gridResults.SelectedRows[0].Tag as ItemLocation;
                if (tag != null)
                {
                    ShowPreview(tag.EntryID, tag.StoreID);
                }
            }
        }

        private void ShowPreview(string entryId, string storeId)
        {
            try
            {
                Outlook.NameSpace ns = Globals.ThisAddIn.Application.GetNamespace("MAPI");
                object item = ns.GetItemFromID(entryId, storeId);
                if (item is Outlook.MailItem mail)
                {
                    lblPreviewSubject.Text = mail.Subject ?? "(Konu yok)";
                    lblPreviewFrom.Text = $"Kimden: {mail.SenderName} <{mail.SenderEmailAddress}>";
                    lblPreviewDate.Text = $"Tarih: {mail.ReceivedTime:dd.MM.yyyy HH:mm}";
                    txtPreview.Text = mail.Body ?? "";
                    
                    // Placeholder'ı gizle, içeriği göster
                    lblPreviewPlaceholder.Visible = false;
                    lblPreviewSubject.Parent.Visible = true; // previewHeader
                    txtPreview.Visible = true;
                    
                    Marshal.ReleaseComObject(mail);
                }
                else
                {
                    ShowPreviewPlaceholder("Önizleme yüklenemedi.");
                    if (item != null) Marshal.ReleaseComObject(item);
                }
            }
            catch (Exception ex)
            {
                ShowPreviewPlaceholder("Önizleme yüklenirken hata: " + ex.Message);
            }
        }
        
        private bool isUpdatingTreeCheck = false; // Recursive kontrolünü önle
        
        private void TreeFolders_AfterCheck(object sender, TreeViewEventArgs e)
        {
            // Programatik değişikliklerden kaynaklanan sonsuz döngüyü önle
            if (isUpdatingTreeCheck) return;
            
            isUpdatingTreeCheck = true;
            try
            {
                // Alt klasörleri de seç/kaldır
                SetChildNodesChecked(e.Node, e.Node.Checked);
            }
            finally
            {
                isUpdatingTreeCheck = false;
            }
        }
        
        private void SetChildNodesChecked(TreeNode node, bool isChecked)
        {
            foreach (TreeNode child in node.Nodes)
            {
                child.Checked = isChecked;
                SetChildNodesChecked(child, isChecked);
            }
        }
        
        private void ShowPreviewPlaceholder(string message = "Önizlemek için bir sonuç seçin")
        {
            lblPreviewPlaceholder.Text = message;
            lblPreviewPlaceholder.Visible = true;
            lblPreviewSubject.Parent.Visible = false; // previewHeader
            txtPreview.Visible = false;
        }
        
        private void UpdateClearButtonVisibility()
        {
            // X butonu: arama kutusu doluysa veya sonuç varsa göster
            bool hasText = !string.IsNullOrEmpty(cmbSearchHistory.Text);
            bool hasResults = gridResults.Rows.Count > 0;
            btnClear.Visible = hasText || hasResults;
        }
        
        private void ClearAll()
        {
            searchHistory.Clear(); 
            cmbSearchHistory.Items.Clear(); 
            cmbSearchHistory.Text = ""; 
            gridResults.Rows.Clear();
            addedEntryIds.Clear();
            ShowPreviewPlaceholder();
            lblStatus.Text = "Hazır - Aramaya başlamak için metin girin";
            UpdateClearButtonVisibility();
        }

        private void LoadFolders()
        {
            try
            {
                Outlook.NameSpace ns = Globals.ThisAddIn.Application.GetNamespace("MAPI");
                foreach (Outlook.Folder folder in ns.Folders)
                {
                    AddFolderNode(folder, treeFolders.Nodes);
                }
                if (treeFolders.Nodes.Count > 0) treeFolders.Nodes[0].Expand();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Klasörler yüklenirken hata oluştu: " + ex.Message);
            }
        }

        private void AddFolderNode(Outlook.Folder folder, TreeNodeCollection parentCollection)
        {
            try
            {
                TreeNode node = parentCollection.Add(folder.EntryID, folder.Name);
                node.Tag = folder;

                // Default check Inbox
                if (folder.Name == "Inbox" || folder.Name == "Gelen Kutusu")
                {
                    node.Checked = true;
                }

                if (folder.Folders.Count > 0)
                {
                    foreach (Outlook.Folder subFolder in folder.Folders)
                    {
                        AddFolderNode(subFolder, node.Nodes);
                    }
                }
            }
            catch { /* Skip folders we can't access */ }
        }

        private void PerformSearch()
        {
            string query = cmbSearchHistory.Text.Trim();
            string fromText = txtFromFilter.Text.Trim();
            string toText = txtToFilter.Text.Trim();
            string ccText = txtCcFilter.Text.Trim();
            string subjectText = txtSubjectFilter.Text.Trim();

            List<Outlook.Folder> selectedFolders = GetSelectedFolders(treeFolders.Nodes);
            if (selectedFolders.Count == 0)
            {
                MessageBox.Show("Lütfen en az bir klasör seçin.");
                return;
            }

            // Eğer arama devam ediyorsa iptal et
            if (isSearching && searchWorker.IsBusy)
            {
                searchWorker.CancelAsync();
                return;
            }

            // Arama geçmişine ekle (en üste, mükerrer varsa çıkar)
            if (!string.IsNullOrEmpty(query))
            {
                searchHistory.Remove(query); // Varsa çıkar
                searchHistory.Insert(0, query); // En başa ekle
                if (searchHistory.Count > 20) searchHistory.RemoveAt(20); // Max 20 kayıt
                
                // ComboBox'ı güncelle
                cmbSearchHistory.Items.Clear();
                foreach (var item in searchHistory)
                {
                    cmbSearchHistory.Items.Add(item);
                }
            }

            gridResults.Rows.Clear();
            addedEntryIds.Clear(); // Mükerrer kontrolünü sıfırla
            lastAddedCount = 0;
            currentSearchQuery = query;
            lblStatus.Text = "Aranıyor...";
            btnSearch.Text = "Durdur";
            isSearching = true;

            // Önizlemeyi temizle
            ShowPreviewPlaceholder("Aranıyor...");
            
            // X butonunu göster (arama başladı)
            UpdateClearButtonVisibility();

            // Filtreleri al
            string attachmentFilter = cmbAttachmentFilter.SelectedItem?.ToString() ?? "Tümü";
            string readFilter = cmbReadFilter.SelectedItem?.ToString() ?? "Tümü";
            string importanceFilter = cmbImportanceFilter.SelectedItem?.ToString() ?? "Tümü";
            DateTime? dateFrom = null;
            DateTime? dateTo = null;
            string dateFilterValue = cmbDateFilter.SelectedItem?.ToString() ?? "Tümü";

            bool hasAnyInput = !string.IsNullOrWhiteSpace(query) ||
                               !string.IsNullOrWhiteSpace(fromText) ||
                               !string.IsNullOrWhiteSpace(toText) ||
                               !string.IsNullOrWhiteSpace(ccText) ||
                               !string.IsNullOrWhiteSpace(subjectText);

            bool hasNonDefaultFilter =
                attachmentFilter != "Tümü" ||
                readFilter != "Tümü" ||
                importanceFilter != "Tümü" ||
                dateFilterValue != "Tümü";

            if (!hasAnyInput && !hasNonDefaultFilter)
            {
                MessageBox.Show("Metin girişi yapın veya filtrelerden en az birini değiştirin.");
                return;
            }
            
            switch (dateFilterValue)
            {
                case "Bugün":
                    dateFrom = DateTime.Today;
                    dateTo = DateTime.Now;
                    break;
                case "Son 7 Gün":
                    dateFrom = DateTime.Today.AddDays(-7);
                    dateTo = DateTime.Now;
                    break;
                case "Son 30 Gün":
                    dateFrom = DateTime.Today.AddDays(-30);
                    dateTo = DateTime.Now;
                    break;
                case "Son 3 Ay":
                    dateFrom = DateTime.Today.AddMonths(-3);
                    dateTo = DateTime.Now;
                    break;
                case "Özel Aralık":
                    dateFrom = dtpFrom.Value.Date;
                    dateTo = dtpTo.Value.Date.AddDays(1).AddSeconds(-1); // Günün sonuna kadar
                    break;
            }

            // Arama kelimelerini analiz et (sadece genel kutu için)
            List<string> exactPhrases = new List<string>(); // Tırnak içi tam ifadeler
            List<string> additionalKeywords = new List<string>(); // Tırnak dışı ek kelimeler
            if (!string.IsNullOrEmpty(query))
            {
                string remainingQuery = query;
                var doubleQuoteMatches = System.Text.RegularExpressions.Regex.Matches(query, "\"([^\"]+)\"");
                foreach (System.Text.RegularExpressions.Match match in doubleQuoteMatches)
                {
                    if (match.Groups.Count > 1)
                    {
                        string phrase = match.Groups[1].Value.Trim();
                        if (!string.IsNullOrEmpty(phrase))
                        {
                            exactPhrases.Add(phrase);
                        }
                        remainingQuery = remainingQuery.Replace(match.Value, " ");
                    }
                }
                
                var singleQuoteMatches = System.Text.RegularExpressions.Regex.Matches(query, "'([^']+)'");
                foreach (System.Text.RegularExpressions.Match match in singleQuoteMatches)
                {
                    if (match.Groups.Count > 1)
                    {
                        string phrase = match.Groups[1].Value.Trim();
                        if (!string.IsNullOrEmpty(phrase))
                        {
                            exactPhrases.Add(phrase);
                        }
                        remainingQuery = remainingQuery.Replace(match.Value, " ");
                    }
                }
                
                string[] remainingWords = remainingQuery.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string word in remainingWords)
                {
                    string trimmed = word.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        additionalKeywords.Add(trimmed);
                    }
                }
            }

            // Eski değişkenleri uyumluluk için oluştur
            List<string> searchKeywords = new List<string>();
            bool isExactPhrase = exactPhrases.Count > 0;
            
            searchKeywords.AddRange(exactPhrases);
            searchKeywords.AddRange(additionalKeywords);

            if (searchKeywords.Count == 0 && !string.IsNullOrEmpty(query))
            {
                searchKeywords.Add(query);
            }

            // Status'ta filtreleri göster
            if (exactPhrases.Count > 0 && additionalKeywords.Count > 0)
            {
                lblStatus.Text = $"Aranıyor... (Tam ifade: \"{string.Join("\", \"", exactPhrases)}\" + {additionalKeywords.Count} ek kelime)";
            }
            else if (exactPhrases.Count > 0)
            {
                lblStatus.Text = $"Aranıyor... (Tam ifade: \"{string.Join("\", \"", exactPhrases)}\")";
            }
            else
            {
                lblStatus.Text = "Aranıyor...";
            }

            DebugLog($"UI arama parametreleri -> genel:'{query}', konu:'{subjectText}', gönderen:'{fromText}', alıcı:'{toText}', cc:'{ccText}', date:{dateFilterValue}, ek:{attachmentFilter}, okunma:{readFilter}, önem:{importanceFilter}");

            // BackgroundWorker başlat
            searchWorker.RunWorkerAsync(new SearchParameters 
            { 
                Query = query, 
                Folders = selectedFolders,
                AttachmentFilter = attachmentFilter,
                SearchKeywords = searchKeywords,
                ExactPhrases = exactPhrases,
                AdditionalKeywords = additionalKeywords,
                IsExactPhrase = isExactPhrase,
                ReadFilter = readFilter,
                ImportanceFilter = importanceFilter,
                DateFrom = dateFrom,
                DateTo = dateTo,
                FromText = fromText,
                ToText = toText,
                CcText = ccText,
                SubjectText = subjectText,
                FromLogic = cmbFromLogic.SelectedItem?.ToString() ?? "VE",
                ToLogic = cmbToLogic.SelectedItem?.ToString() ?? "VE",
                CcLogic = cmbCcLogic.SelectedItem?.ToString() ?? "VE",
                SubjectLogic = cmbSubjectLogic.SelectedItem?.ToString() ?? "VE"
            });
        }

        private void SearchWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            SearchParameters parameters = e.Argument as SearchParameters;
            string query = parameters.Query;
            List<Outlook.Folder> folders = parameters.Folders;
            string attachmentFilter = parameters.AttachmentFilter;
            List<string> searchKeywords = parameters.SearchKeywords ?? new List<string>();
            List<string> exactPhrases = parameters.ExactPhrases ?? new List<string>();
            List<string> additionalKeywords = parameters.AdditionalKeywords ?? new List<string>();
            bool isExactPhrase = parameters.IsExactPhrase;
            string readFilter = parameters.ReadFilter ?? "Tümü";
            string importanceFilter = parameters.ImportanceFilter ?? "Tümü";
            DateTime? dateFrom = parameters.DateFrom;
            DateTime? dateTo = parameters.DateTo;
            string fromText = parameters.FromText ?? "";
            string toText = parameters.ToText ?? "";
            string ccText = parameters.CcText ?? "";
            string subjectTextFilter = parameters.SubjectText ?? "";
            string fromLogic = parameters.FromLogic ?? "VE";
            string toLogic = parameters.ToLogic ?? "VE";
            string ccLogic = parameters.CcLogic ?? "VE";
            string subjectLogic = parameters.SubjectLogic ?? "VE";

            int processedCount = 0;
            List<SearchResult> batchResults = new List<SearchResult>();
            const int BATCH_SIZE = 100; // Her 100 sonuçta bir UI güncelle (performans için artırıldı)
            
            // Namespace'i bir kere al ve yeniden kullan
            Outlook.NameSpace ns = Globals.ThisAddIn.Application.GetNamespace("MAPI");
            
            DebugLog("========== YENİ ARAMA BAŞLADI ==========");
            DebugLog($"Arama sorgusu: '{query}'");
            DebugLog($"Tam ifade mi: {isExactPhrase}");
            DebugLog($"Arama kelimeleri: {string.Join(", ", searchKeywords.Select(k => $"'{k}'"))}");
            DebugLog($"Klasör sayısı: {folders.Count}");

            foreach (var folder in folders)
            {
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                try
                {
                    DebugLog($"--- Klasör: {folder.Name} ---");
                    
                    // PERFORMANS: SQL filtrelerini kullanıcı kutularına göre kur (subject zorunlu + kişi grubu ayrı)
                    string NormalizeLogic(string logic) => logic == null ? "AND" : (logic.Trim().ToUpperInvariant() == "VEYA" ? "OR" : "AND");

                    Func<string, string[], string> buildFilter = (text, cols) =>
                    {
                        var parts = new List<string>();
                        foreach (var variant in GetTurkishVariations(text))
                        {
                            string escaped = EscapeSqlString(variant);
                            foreach (var col in cols)
                            {
                                parts.Add($"\"{col}\" LIKE '%{escaped}%'");
                            }
                        }
                        return parts.Count > 0 ? "(" + string.Join(" OR ", parts) + ")" : string.Empty;
                    };

                    List<string> mandatoryAnd = new List<string>();

                    // Genel arama (konu + text + html) zorunlu AND blok — kişi alanlarını genel aramaya katma
                    if (!string.IsNullOrWhiteSpace(query))
                    {
                        string primarySearchTerm = searchKeywords.Count > 0 ? searchKeywords[0] : query;
                        string generalExpr = buildFilter(primarySearchTerm, new[]
                        {
                            "urn:schemas:httpmail:subject",
                            "urn:schemas:httpmail:textdescription",
                            "urn:schemas:httpmail:htmldescription"
                        });
                        if (!string.IsNullOrEmpty(generalExpr)) mandatoryAnd.Add(generalExpr);
                    }

                    // Konu zorunlu AND blok
                    if (!string.IsNullOrWhiteSpace(subjectTextFilter))
                    {
                        string subjectExpr = buildFilter(subjectTextFilter, new[] { "urn:schemas:httpmail:subject" });
                        if (!string.IsNullOrEmpty(subjectExpr)) mandatoryAnd.Add(subjectExpr);
                    }

                    // Kişi grubu: Gönderen/Alıcı/CC alanlarını sırayla, aralarındaki VE/VEYA seçimine göre grupla
                    string personGroup = "";
                    bool personFirstAdded = false;

                    string fromExpr = string.IsNullOrWhiteSpace(fromText) ? "" : buildFilter(fromText, new[] { "urn:schemas:httpmail:fromname" });
                    string toExpr = string.IsNullOrWhiteSpace(toText) ? "" : buildFilter(toText, new[] { "urn:schemas:httpmail:displayto" });
                    string ccExpr = string.IsNullOrWhiteSpace(ccText) ? "" : buildFilter(ccText, new[] { "urn:schemas:httpmail:displaycc" });

                    void AppendPersonExpr(string expr, string connector)
                    {
                        if (string.IsNullOrEmpty(expr)) return;
                        if (!personFirstAdded)
                        {
                            personGroup = expr;
                            personFirstAdded = true;
                        }
                        else
                        {
                            personGroup = "(" + personGroup + ") " + connector + " " + expr;
                        }
                    }

                    if (!string.IsNullOrEmpty(fromExpr))
                    {
                        AppendPersonExpr(fromExpr, "AND");
                    }

                    if (!string.IsNullOrEmpty(toExpr))
                    {
                        string connector = personFirstAdded ? NormalizeLogic(fromLogic) : "AND";
                        AppendPersonExpr(toExpr, connector);
                    }

                    if (!string.IsNullOrEmpty(ccExpr))
                    {
                        string connector;
                        if (!string.IsNullOrEmpty(toExpr))
                            connector = NormalizeLogic(toLogic);
                        else if (!string.IsNullOrEmpty(fromExpr))
                            connector = NormalizeLogic(fromLogic);
                        else
                            connector = "AND";

                        AppendPersonExpr(ccExpr, connector);
                    }

                    if (!string.IsNullOrEmpty(personGroup)) mandatoryAnd.Add(personGroup);

                    // Nihai AND birleşimi
                    string filterBody = "";
                    foreach (var expr in mandatoryAnd)
                    {
                        if (string.IsNullOrEmpty(filterBody)) filterBody = expr;
                        else filterBody = "(" + filterBody + ") AND " + expr;
                    }

                    if (string.IsNullOrEmpty(filterBody)) filterBody = "1=1";

                    string filter = "@SQL=" + filterBody;
                    DebugLog($"SQL Filtre (AND blok sayısı {mandatoryAnd.Count}): {filter}");
                    
                    Outlook.Table table = null;
                    try
                    {
                        table = folder.GetTable(filter, Outlook.OlTableContents.olUserItems);
                        DebugLog($"Table oluşturuldu, EndOfTable: {table.EndOfTable}");
                    }
                    catch (Exception ex)
                    {
                        DebugLog($"Table oluşturma hatası: {ex.Message}");
                        continue;
                    }

                    // Kolonları ekle - Tüm gerekli kolonları baştan ekle (performans için)
                    table.Columns.Add("EntryID");
                    table.Columns.Add("Subject");
                    table.Columns.Add("SenderName");
                    table.Columns.Add("SentOn");
                    table.Columns.Add("UnRead");  // Okunma durumu
                    table.Columns.Add("Importance"); // Önem derecesi
                    
                    // To ve CC kolonlarını da ekle
                    try
                    {
                        table.Columns.Add("To");
                        table.Columns.Add("CC");
                    }
                    catch { }
                    
                    // Body kolonunu da ekle (GetItemFromID'den çok daha hızlı)
                    try
                    {
                        table.Columns.Add("Body");
                    }
                    catch { }
                    
                    // Ek bilgisi için MAPI property (PR_HASATTACH = 0x0E1B000B)
                    // Outlook Table API'de "HasAttachments" adıyla property yok,
                    // MAPI schema veya property tag kullanılmalı
                    const string PR_HASATTACH = "http://schemas.microsoft.com/mapi/proptag/0x0E1B000B";
                    try
                    {
                        table.Columns.Add(PR_HASATTACH);
                    }
                    catch { }
                    
                    int rowCount = 0;

                    while (!table.EndOfTable)
                    {
                        rowCount++;
                        if (worker.CancellationPending)
                        {
                            e.Cancel = true;
                            if (table != null) Marshal.ReleaseComObject(table);
                            return;
                        }

                        Outlook.Row row = null;
                        try
                        {
                            row = table.GetNextRow();
                        string subject = row["Subject"]?.ToString() ?? "(Konu yok)";
                        string senderName = row["SenderName"]?.ToString() ?? "";
                        string time = row["SentOn"]?.ToString() ?? "";
                        string entryId = row["EntryID"]?.ToString();
                        
                        // EntryID boşsa bu satırı atla
                        if (string.IsNullOrEmpty(entryId))
                        {
                            if (row != null) Marshal.ReleaseComObject(row);
                            continue;
                        }
                        
                        string toRecipients = "";
                        string ccRecipients = "";
                        bool hasAttachments = false;
                        bool isUnread = false;
                        int importance = 1; // 0=Low, 1=Normal, 2=High
                        DateTime mailDate = DateTime.MinValue;
                        
                        try
                        {
                            toRecipients = row["To"]?.ToString() ?? "";
                            ccRecipients = row["CC"]?.ToString() ?? "";
                        }
                        catch { }
                        
                        // Table'dan ek, okunma ve önem bilgilerini al (GetItemFromID'den çok daha hızlı)
                        // PR_HASATTACH MAPI property tag kullanılıyor
                        const string PR_HASATTACH_COL = "http://schemas.microsoft.com/mapi/proptag/0x0E1B000B";
                        try
                        {
                            object hasAttObj = row[PR_HASATTACH_COL];
                            if (hasAttObj != null) hasAttachments = Convert.ToBoolean(hasAttObj);
                        }
                        catch { }
                        
                        try
                        {
                            object unreadObj = row["UnRead"];
                            if (unreadObj != null) isUnread = Convert.ToBoolean(unreadObj);
                        }
                        catch { }
                        
                        try
                        {
                            object impObj = row["Importance"];
                            if (impObj != null) importance = Convert.ToInt32(impObj);
                        }
                        catch { }
                        
                        // Tarih parse et
                        try
                        {
                            object sentOnObj = row["SentOn"];
                            if (sentOnObj != null && sentOnObj is DateTime)
                            {
                                mailDate = (DateTime)sentOnObj;
                            }
                            else if (!string.IsNullOrEmpty(time))
                            {
                                DateTime.TryParse(time, out mailDate);
                            }
                        }
                        catch { }
                        
                        // ÖNCELİKLİ FİLTRELER - Hızlı eleme için Table verisiyle kontrol et
                        // Bu filtreleri geçemezse GetItemFromID çağırmaya gerek yok
                        
                        // Tarih filtresi (hızlı eleme)
                        if (dateFrom.HasValue && mailDate != DateTime.MinValue && mailDate < dateFrom.Value)
                        {
                            continue;
                        }
                        if (dateTo.HasValue && mailDate != DateTime.MinValue && mailDate > dateTo.Value)
                        {
                            continue;
                        }
                        
                        // Okunma durumu filtresi (hızlı eleme)
                        if (readFilter == "Okunmamış" && !isUnread)
                        {
                            continue;
                        }
                        else if (readFilter == "Okunmuş" && isUnread)
                        {
                            continue;
                        }
                        
                        // Önem derecesi filtresi (hızlı eleme)
                        if (importanceFilter == "Yüksek" && importance != 2)
                        {
                            continue;
                        }
                        else if (importanceFilter == "Normal" && importance != 1)
                        {
                            continue;
                        }
                        else if (importanceFilter == "Düşük" && importance != 0)
                        {
                            continue;
                        }
                        
                        // Ek filtresi (hızlı eleme)
                        if (attachmentFilter == "Eki Olan" && !hasAttachments)
                        {
                            continue;
                        }
                        else if (attachmentFilter == "Eki Olmayan" && hasAttachments)
                        {
                            continue;
                        }

                        // Eşleşme metnini belirle
                        string matchText = "";
                        bool isSubjectMatch = false;
                        bool shouldAdd = false;
                        // hasAttachments zaten Table'dan alındı, tekrar tanımlamaya gerek yok
                        
                        // TAM İFADE + EK KELİME ARAMASI
                        // Örnek: "hayırlı olsun" songül -> tam ifade "hayırlı olsun" VE ek kelime "songül" bulunmalı
                        if (isExactPhrase && exactPhrases.Count > 0)
                        {
                            // PERFORMANS: Debug log sadece EnableDebugLog true ise
                            DebugLog($"====== [Row {rowCount}] DETAYLI ANALIZ ======");
                            DebugLog($"Konu: '{subject}' | Gönderen: '{senderName}'");
                            
                            // İçerik alanlarını Table'dan al (GetItemFromID'den çok daha hızlı)
                            string body = null;
                            try
                            {
                                body = row["Body"]?.ToString() ?? "";
                            }
                            catch { body = ""; }

                            // Body boşsa mail nesnesinden fallback oku (RTF/HTML içeriği yakalamak için)
                            if (string.IsNullOrEmpty(body))
                            {
                                try
                                {
                                    object mailObj = ns.GetItemFromID(entryId, folder.StoreID);
                                    if (mailObj is Outlook.MailItem fallbackMail)
                                    {
                                        body = fallbackMail.Body ?? fallbackMail.HTMLBody ?? "";
                                        Marshal.ReleaseComObject(fallbackMail);
                                    }
                                    else if (mailObj != null)
                                    {
                                        Marshal.ReleaseComObject(mailObj);
                                    }
                                }
                                catch { /* sessiz geç */ }
                            }
                            
                            // Tüm aranabilir alanları birleştir (Table verisiyle - hızlı)
                            string allSearchableText = (subject ?? "") + " " + (senderName ?? "") + " " + 
                                                       (toRecipients ?? "") + " " + (ccRecipients ?? "") + " " + body;
                            
                            // ADIM 1: TÜM tam ifadelerin bulunup bulunmadığını kontrol et
                            bool allPhrasesFound = true;
                            string firstPhraseMatch = null;
                            string firstPhraseLocation = null;
                            
                            foreach (string phrase in exactPhrases)
                            {
                                bool phraseFound = false;
                                
                                // Konuda ara
                                if (TurkishIndexOf(subject, phrase) >= 0)
                                {
                                    phraseFound = true;
                                    if (firstPhraseMatch == null)
                                    {
                                        firstPhraseMatch = phrase;
                                        firstPhraseLocation = "Konu";
                                        isSubjectMatch = true;
                                    }
                                    DebugLog($"  -> Tam ifade '{phrase}' KONUDA bulundu!");
                                }
                                // Gönderende ara
                                else if (TurkishIndexOf(senderName, phrase) >= 0)
                                {
                                    phraseFound = true;
                                    if (firstPhraseMatch == null)
                                    {
                                        firstPhraseMatch = phrase;
                                        firstPhraseLocation = "Gönderen";
                                    }
                                    DebugLog($"  -> Tam ifade '{phrase}' GÖNDERENDE bulundu!");
                                }
                                // Alıcılarda ara
                                else if (TurkishIndexOf(toRecipients + " " + ccRecipients, phrase) >= 0)
                                {
                                    phraseFound = true;
                                    if (firstPhraseMatch == null)
                                    {
                                        firstPhraseMatch = phrase;
                                        firstPhraseLocation = "Alıcı";
                                    }
                                    DebugLog($"  -> Tam ifade '{phrase}' ALICILARDA bulundu!");
                                }
                                // İçerikte ara
                                else if (!string.IsNullOrEmpty(body) && TurkishIndexOf(body, phrase) >= 0)
                                {
                                    phraseFound = true;
                                    if (firstPhraseMatch == null)
                                    {
                                        firstPhraseMatch = phrase;
                                        firstPhraseLocation = "İçerik";
                                    }
                                    DebugLog($"  -> Tam ifade '{phrase}' İÇERİKTE bulundu!");
                                }
                                
                                if (!phraseFound)
                                {
                                    allPhrasesFound = false;
                                    DebugLog($"  -> Tam ifade '{phrase}' HİÇBİR YERDE BULUNAMADI!");
                                    break;
                                }
                            }
                            
                            // ADIM 2: Ek kelimeler varsa onların da bulunup bulunmadığını kontrol et
                            bool allAdditionalKeywordsFound = true;
                            string firstKeywordMatch = null;
                            string firstKeywordLocation = null;
                            
                            if (allPhrasesFound && additionalKeywords.Count > 0)
                            {
                                DebugLog($"  Ek kelimeler kontrol ediliyor...");
                                
                                // Tüm ek kelimeleri kontrol et
                                foreach (string keyword in additionalKeywords)
                                {
                                    bool keywordFound = false;
                                    
                                    // Tüm alanlarda ara
                                    if (TurkishIndexOf(allSearchableText, keyword) >= 0)
                                    {
                                        keywordFound = true;
                                        
                                        // Hangi alanda bulunduğunu belirle
                                        if (firstKeywordMatch == null)
                                        {
                                            firstKeywordMatch = keyword;
                                            if (TurkishIndexOf(subject, keyword) >= 0)
                                                firstKeywordLocation = "Konu";
                                            else if (TurkishIndexOf(senderName, keyword) >= 0)
                                                firstKeywordLocation = "Gönderen";
                                            else if (TurkishIndexOf(toRecipients + " " + ccRecipients, keyword) >= 0)
                                                firstKeywordLocation = "Alıcı";
                                            else
                                                firstKeywordLocation = "İçerik";
                                        }
                                        
                                        DebugLog($"    -> Ek kelime '{keyword}' {firstKeywordLocation}'DA bulundu!");
                                    }
                                    
                                    if (!keywordFound)
                                    {
                                        allAdditionalKeywordsFound = false;
                                        DebugLog($"    -> Ek kelime '{keyword}' BULUNAMADI!");
                                        break;
                                    }
                                }
                            }
                            else if (!allPhrasesFound)
                            {
                                // Tam ifade bulunamadıysa ek kelimeleri de kontrol etmiyoruz
                                DebugLog($"  Tam ifade bulunamadığı için ek kelimeler kontrol edilmiyor.");
                            }
                            
                            // SONUÇ: Hem tam ifadeler hem ek kelimeler bulunduysa ekle
                            if (allPhrasesFound && allAdditionalKeywordsFound)
                            {
                                shouldAdd = true;
                                
                                // Eşleşme metnini oluştur
                                if (firstPhraseMatch != null && firstPhraseLocation != null)
                                {
                                    string contextText = "";
                                    switch (firstPhraseLocation)
                                    {
                                        case "Konu": contextText = subject; break;
                                        case "Gönderen": contextText = senderName; break;
                                        case "Alıcı": contextText = toRecipients + " " + ccRecipients; break;
                                        case "İçerik": contextText = body; break;
                                    }
                                    matchText = firstPhraseLocation + ": " + ExtractMatchContext(contextText, firstPhraseMatch);
                                    
                                    // Ek kelime bilgisini de ekle
                                    if (additionalKeywords.Count > 0 && firstKeywordMatch != null)
                                    {
                                        matchText += $" (+{firstKeywordLocation}: {firstKeywordMatch})";
                                    }
                                }
                                
                                DebugLog($"  *** ESLESTI - Sonuçlara ekleniyor! ***");
                            }
                            else
                            {
                                DebugLog($"  ESLESMEDI - Atlanıyor.");
                            }
                        }
                        // KELİME ARAMASI - TÜM kelimeler eşleşmeli (AND mantığı)
                        else
                        {
                            // İçerik alanlarını hazırla
                            string body = null;
                            try
                            {
                                body = row["Body"]?.ToString() ?? "";
                            }
                            catch { body = ""; }

                            // Body boşsa mail nesnesinden fallback oku
                            if (string.IsNullOrEmpty(body))
                            {
                                try
                                {
                                    object mailObj = ns.GetItemFromID(entryId, folder.StoreID);
                                    if (mailObj is Outlook.MailItem fallbackMail)
                                    {
                                        body = fallbackMail.Body ?? fallbackMail.HTMLBody ?? "";
                                        Marshal.ReleaseComObject(fallbackMail);
                                    }
                                    else if (mailObj != null)
                                    {
                                        Marshal.ReleaseComObject(mailObj);
                                    }
                                }
                                catch { /* sessiz geç */ }
                            }
                            
                            // Tüm aranabilir alanları birleştir (konu, gönderen, alıcılar, içerik)
                            string allSearchableText = (subject ?? "") + " " + (senderName ?? "") + " " + 
                                                       (toRecipients ?? "") + " " + (ccRecipients ?? "") + " " + body;
                            
                            // TÜM kelimelerin mail'de bulunup bulunmadığını kontrol et
                            bool allKeywordsFound = true;
                            
                            foreach (string keyword in searchKeywords)
                            {
                                if (TurkishIndexOf(allSearchableText, keyword) < 0)
                                {
                                    allKeywordsFound = false;
                                    break;
                                }
                            }
                            
                            // Tüm kelimeler bulunduysa, eşleşme metnini oluştur
                            if (allKeywordsFound)
                            {
                                shouldAdd = true;
                                
                                // İlk bulunan kelimenin eşleşme yerini göster
                                foreach (string keyword in searchKeywords)
                                {
                                    if (TurkishIndexOf(subject, keyword) >= 0)
                                    {
                                        matchText = "Konu: " + ExtractMatchContext(subject, keyword);
                                        isSubjectMatch = true;
                                        break;
                                    }
                                    else if (TurkishIndexOf(senderName, keyword) >= 0)
                                    {
                                        matchText = "Gönderen: " + ExtractMatchContext(senderName, keyword);
                                        break;
                                    }
                                    else if (TurkishIndexOf(toRecipients, keyword) >= 0 || TurkishIndexOf(ccRecipients, keyword) >= 0)
                                    {
                                        matchText = "Alıcı: " + ExtractMatchContext(toRecipients + " " + ccRecipients, keyword);
                                        break;
                                    }
                                    else if (!string.IsNullOrEmpty(body) && TurkishIndexOf(body, keyword) >= 0)
                                    {
                                        matchText = "İçerik: " + ExtractMatchContext(body, keyword);
                                        break;
                                    }
                                }
                                
                                // Eşleşme metni boşsa genel bir açıklama yaz
                                if (string.IsNullOrEmpty(matchText))
                                {
                                    if (searchKeywords.Count == 0)
                                    {
                                        if (!string.IsNullOrEmpty(subjectTextFilter))
                                            matchText = "Konu filtresi: " + subjectTextFilter;
                                        else if (!string.IsNullOrEmpty(fromText))
                                            matchText = "Gönderen filtresi: " + fromText;
                                        else if (!string.IsNullOrEmpty(toText))
                                            matchText = "Alıcı filtresi: " + toText;
                                        else if (!string.IsNullOrEmpty(ccText))
                                            matchText = "Bilgi filtresi: " + ccText;
                                        else
                                            matchText = "Filtre eşleşmesi";
                                    }
                                    else
                                    {
                                        matchText = "Tüm kelimeler bulundu";
                                    }
                                }
                            }
                        }
                        
                        // Eşleşme yoksa bu sonucu atla
                        if (!shouldAdd)
                        {
                            if (row != null) Marshal.ReleaseComObject(row);
                            continue;
                        }

                        var result = new SearchResult
                        {
                            Subject = subject,
                            MatchText = matchText,
                            Sender = senderName,
                            Time = time,
                            FolderName = folder.Name,
                            EntryID = entryId,
                            StoreID = folder.StoreID,
                            IsSubjectMatch = isSubjectMatch,
                            HasAttachments = hasAttachments
                        };

                        batchResults.Add(result);
                        processedCount++;
                        
                        // Row nesnesini temizle
                        if (row != null) Marshal.ReleaseComObject(row);

                        // Batch boyutuna ulaşınca UI'ya gönder
                        if (batchResults.Count >= BATCH_SIZE)
                        {
                            worker.ReportProgress(processedCount, new SearchProgress 
                            { 
                                Results = new List<SearchResult>(batchResults),
                                TotalCount = processedCount 
                            });
                            batchResults.Clear();
                        }
                        }
                        catch
                        {
                            // Row işlenirken hata olursa temizle ve devam et
                            if (row != null) try { Marshal.ReleaseComObject(row); } catch { }
                        }
                    }
                    
                    DebugLog($"Klasör '{folder.Name}' tarandı, toplam satır: {rowCount}");
                    
                    if (table != null) Marshal.ReleaseComObject(table);
                }
                catch (Exception ex)
                {
                    DebugLog($"Klasör hatası ({folder.Name}): {ex.Message}");
                    Console.WriteLine($"Error searching folder {folder.Name}: {ex.Message}");
                }
            }
            
            DebugLog($"========== ARAMA TAMAMLANDI - Toplam sonuç: {processedCount} ==========");

            // Kalan sonuçları gönder
            if (batchResults.Count > 0)
            {
                worker.ReportProgress(processedCount, new SearchProgress 
                { 
                    Results = new List<SearchResult>(batchResults),
                    TotalCount = processedCount 
                });
            }

            // Arama tamamlandı
            e.Result = new SearchProgress 
            { 
                Results = new List<SearchResult>(),
                TotalCount = processedCount 
            };
        }

        private void SearchWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            var progress = e.UserState as SearchProgress;
            if (progress != null)
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => {
                        UpdateGridWithResults(progress.Results);
                        lblStatus.Text = $"Aranıyor... {progress.TotalCount} sonuç bulundu";
                    }));
                }
                else
                {
                    UpdateGridWithResults(progress.Results);
                    lblStatus.Text = $"Aranıyor... {progress.TotalCount} sonuç bulundu";
                }
            }
        }

        private void SearchWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Action updateUI = () =>
            {
                isSearching = false;
                btnSearch.Text = "Ara";

                if (e.Cancelled)
                {
                    lblStatus.Text = $"Arama iptal edildi. {gridResults.Rows.Count} sonuç gösteriliyor";
                }
                else if (e.Error != null)
                {
                    MessageBox.Show("Arama sırasında hata oluştu: " + e.Error.Message);
                    lblStatus.Text = "Hata oluştu";
                }
                else
                {
                    lblStatus.Text = $"Arama tamamlandı. {gridResults.Rows.Count} sonuç bulundu";
                }
                
                // X butonunu ve placeholder'ı güncelle
                UpdateClearButtonVisibility();
                if (gridResults.Rows.Count == 0)
                {
                    ShowPreviewPlaceholder("Sonuç bulunamadı");
                }
                else
                {
                    // Seçim varsa önizlemeyi güncelle, yoksa placeholder'ı koru
                    if (gridResults.SelectedRows.Count > 0)
                    {
                        var selectedTag = gridResults.SelectedRows[0].Tag as ItemLocation;
                        if (selectedTag != null)
                        {
                            ShowPreview(selectedTag.EntryID, selectedTag.StoreID);
                        }
                    }
                    else
                    {
                        ShowPreviewPlaceholder("Önizlemek için bir sonuç seçin");
                    }
                }
            };

            if (this.InvokeRequired)
            {
                this.Invoke(updateUI);
            }
            else
            {
                updateUI();
            }
        }

        private void UpdateGridWithResults(List<SearchResult> results)
        {
            // Sadece yeni sonuçları ekle, grid'i temizleme (seçim korunsun)
            bool hadSelection = gridResults.SelectedRows.Count > 0;
            
            suppressSelectionChanged = true;
            try
            {
                foreach (var result in results)
                {
                    // Mükerrer kontrolü
                    if (addedEntryIds.Contains(result.EntryID))
                    {
                        continue;
                    }
                    
                    addedEntryIds.Add(result.EntryID);
                    
                    // Tarihe göre doğru konumu bul (en yeni en üstte)
                    int insertIndex = FindInsertPosition(result.Time);
                    
                    // Konu başlığına ek ikonu ekle
                    string subjectDisplay = result.Subject;
                    if (result.HasAttachments)
                    {
                        subjectDisplay = "[+] " + subjectDisplay;
                    }
                    
                    gridResults.Rows.Insert(insertIndex, subjectDisplay, result.MatchText, result.Sender, result.Time, result.FolderName);
                    gridResults.Rows[insertIndex].Tag = new ItemLocation { EntryID = result.EntryID, StoreID = result.StoreID };
                    
                    // Konu eşleşmelerini vurgula
                    if (result.IsSubjectMatch)
                    {
                        gridResults.Rows[insertIndex].DefaultCellStyle.BackColor = HighlightColor;
                    }
                }

                // Kullanıcı henüz seçim yapmadıysa otomatik seçim bırakma
                if (!hadSelection)
                {
                    gridResults.ClearSelection();
                }
            }
            finally
            {
                suppressSelectionChanged = false;
            }
            
            lastAddedCount = results.Count;
        }

        private int FindInsertPosition(string timeString)
        {
            // Eğer grid boşsa başa ekle
            if (gridResults.Rows.Count == 0)
            {
                return 0;
            }

            DateTime newTime;
            if (!DateTime.TryParse(timeString, out newTime))
            {
                return gridResults.Rows.Count; // Parse edilemezse en sona ekle
            }

            // En yeniden başlayarak tarihleri karşılaştır
            for (int i = 0; i < gridResults.Rows.Count; i++)
            {
                string existingTimeStr = gridResults.Rows[i].Cells["Time"].Value?.ToString();
                DateTime existingTime;
                
                if (DateTime.TryParse(existingTimeStr, out existingTime))
                {
                    // Yeni tarih daha yeni ise bu pozisyona ekle
                    if (newTime > existingTime)
                    {
                        return i;
                    }
                }
            }

            // Tüm tarihlerden daha eski, en sona ekle
            return gridResults.Rows.Count;
        }

        private string ExtractMatchContext(string text, string query)
        {
            if (string.IsNullOrEmpty(text)) return "";
            
            int index = TurkishIndexOf(text, query);
            if (index < 0) return text;

            // Eşleşmenin etrafından 30 karakter al
            int start = Math.Max(0, index - 15);
            int end = Math.Min(text.Length, index + query.Length + 15);
            
            string result = text.Substring(start, end - start);
            
            if (start > 0) result = "..." + result;
            if (end < text.Length) result = result + "...";
            
            return result;
        }

        private List<Outlook.Folder> GetSelectedFolders(TreeNodeCollection nodes)
        {
            List<Outlook.Folder> list = new List<Outlook.Folder>();
            foreach (TreeNode node in nodes)
            {
                if (node.Checked && node.Tag is Outlook.Folder folder)
                {
                    list.Add(folder);
                }
                list.AddRange(GetSelectedFolders(node.Nodes));
            }
            return list;
        }

        private void GridResults_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var tag = gridResults.Rows[e.RowIndex].Tag as ItemLocation;
                if (tag != null)
                {
                    try
                    {
                        Outlook.NameSpace ns = Globals.ThisAddIn.Application.GetNamespace("MAPI");
                        object item = ns.GetItemFromID(tag.EntryID, tag.StoreID);
                        if (item is Outlook.MailItem mail)
                        {
                            mail.Display();
                        }
                        else if (item is Outlook.MeetingItem meeting)
                        {
                            meeting.Display();
                        }
                        else
                        {
                            // Try generic display
                            ((dynamic)item).Display();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Öğe açılırken hata oluştu: " + ex.Message);
                    }
                }
            }
        }

        private class ItemLocation
        {
            public string EntryID { get; set; }
            public string StoreID { get; set; }
        }

        private class SearchParameters
        {
            public string Query { get; set; }
            public List<Outlook.Folder> Folders { get; set; }
            public string AttachmentFilter { get; set; } // "Tümü", "Eki Olan", "Eki Olmayan"
            public List<string> SearchKeywords { get; set; } // Arama kelimeleri (tüm terimler)
            public List<string> ExactPhrases { get; set; } // Tırnak içi tam ifadeler
            public List<string> AdditionalKeywords { get; set; } // Tırnak dışı ek kelimeler
            public bool IsExactPhrase { get; set; } // Tırnak içi tam ifade araması var mı?
            public string ReadFilter { get; set; } // "Tümü", "Okunmamış", "Okunmuş"
            public string ImportanceFilter { get; set; } // "Tümü", "Yüksek", "Normal", "Düşük"
            public DateTime? DateFrom { get; set; }
            public DateTime? DateTo { get; set; }
            public string FromText { get; set; }
            public string ToText { get; set; }
            public string CcText { get; set; }
            public string SubjectText { get; set; }
            public string FromLogic { get; set; }
            public string ToLogic { get; set; }
            public string CcLogic { get; set; }
            public string SubjectLogic { get; set; }
        }

        private class SearchResult
        {
            public string Subject { get; set; }
            public string MatchText { get; set; }
            public string Sender { get; set; }
            public string Time { get; set; }
            public string FolderName { get; set; }
            public string EntryID { get; set; }
            public string StoreID { get; set; }
            public bool IsSubjectMatch { get; set; }
            public bool HasAttachments { get; set; } // Ek var mı kontrolü
        }

        private class SearchProgress
        {
            public List<SearchResult> Results { get; set; }
            public int TotalCount { get; set; }
        }
    }
}
