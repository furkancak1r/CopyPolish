using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Outlook = Microsoft.Office.Interop.Outlook;
using Office = Microsoft.Office.Core;
using System.Windows.Forms;
using System.Threading.Tasks;
using Word = Microsoft.Office.Interop.Word;

namespace outlook2
{
    public partial class ThisAddIn
    {
        private const string ModelName = "qwen/qwen3-coder:free";
        private const string SystemPromptImprove = @"Sen, bir e-postanın ana mesajını ve samimiyet tonunu koruyarak onu daha akıcı ve etkili hale getiren bir iletişim asistanısın. Aşağıdaki kurallara harfiyen uymalısın:

1.  TONU KORU (En Önemli Kural): Orijinal metin ne kadar samimi veya resmi ise, senin metnin de o seviyede olmalıdır. Samimi bir dili (""Selam abi"") asla aşırı resmi bir dile (""Sayın Yetkili"") çevirme.
2.  ANLAMI VE NİYETİ DEĞİŞTİRME: Cümlenin temel anlamını, amacını, isteğini veya sorusunu asla değiştirme. Bağlamı dikkatlice analiz et:
   - Yarım kalmış komut/istek: ""Dosyayı ilett"" → ""Dosyayı iletir misin?"" (Rica/soru anlamında)
   - Bilgilendirme: ""Dosyayı ilettim"" → ""Dosyayı ilettim"" (Geçmiş eylem)
   - Emir: ""Dosyayı ilet"" → ""Lütfen dosyayı iletir misiniz?"" (Kibar rica)
   Sadece dilbilgisi, akıcılık ve yazım hatalarını düzelt, anlam/niyet değiştirme.
3.  SELAMLAMAYI KORU: Orijinal metindeki selamlama ne ise (örn: ""Merhaba,""), yanıtın da birebir aynı selamlamayla başlamalıdır.
4.  FORMATLAMA KORU: Satır sonları, boşluklar, paragraf yapısını aynen koru. Eğer orijinalde boş satırlar varsa, onları da koru.
5.  GEREKSİZ BİLGİ EKLEME: Orijinal metinde olmayan bilgileri (""...bilginize sunarım"" gibi) ekleme.
6.  PLACEHOLDER KULLANMA: Yanıtına ""[ADINIZ]"" gibi yer tutucular ekleme.
7.  TEKNİK TOKEN GÖSTERME: Yanıtın asla '<|...|>' gibi teknik token'lar içermemeli.
8.  SADECE YENİDEN YAZILMIŞ METNİ DÖNDÜR: Yanıtın, sadece ve sadece yeniden yazılmış metni içermelidir, başka hiçbir şey değil.
9.  Mailleri her zaman daha kibar bir şekilde yaz. Emreder gibi yazma asla olmamalı.";

        private const string SystemPromptTranslate = @"You are a precise translator from Turkish to English. Follow these rules:

1. Preserve meaning and tone. Do not embellish.
2. Output only the English translation text, nothing else.
3. PRESERVE ALL FORMATTING: Keep exact line breaks, spacing, paragraphs, and structure.
4. If the original has empty lines between sentences/paragraphs, maintain them exactly.
5. Keep punctuation and greeting structures identical.";

        public void ImproveSelectedText()
        {
            try
            {
                var doc = GetActiveWordDocument();
                if (doc == null)
                {
                    MessageBox.Show("Bir e-posta düzenleyici bulunamadı.", "Bilgi");
                    return;
                }

                var selectedText = SafeGetSelectionText(doc);
                if (string.IsNullOrWhiteSpace(selectedText))
                {
                    MessageBox.Show("Herhangi bir metin seçilmedi.", "Uyarı");
                    return;
                }

                var fullText = GetFullBodyText(doc) ?? string.Empty;

                var apiKey = Properties.Settings.Default.CopyPolishApiKey;
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    MessageBox.Show("Lütfen önce CopyPolish API anahtarını ayarlayın.", "Gerekli Ayar");
                    using (var form = new SettingsForm()) form.ShowDialog();
                    apiKey = Properties.Settings.Default.CopyPolishApiKey;
                    if (string.IsNullOrWhiteSpace(apiKey)) return;
                }

                string userContent =
                    "Tüm e-posta içeriği (konuşma geçmişi ve önceki mesajlar dahil) aşağıdadır. Tüm bağlamı dikkatlice analiz et fakat sadece seçili metni düzelt.\n" +
                    "<EMAIL>\n" + fullText + "\n</EMAIL>\n" +
                    "Seçili bölüm aşağıdadır. Sadece bunun düzeltilmiş halini, başka bir şey olmadan döndür.\n" +
                    "<SELECTION>\n" + selectedText + "\n</SELECTION>";

                var loading = new LoadingForm("Yapay zekadan yanıt bekleniyor...");
                loading.Show();
                Cursor.Current = Cursors.WaitCursor;

                Task.Run(() =>
                {
                    return OpenRouterClient.Complete(
                        apiKey,
                        ModelName,
                        SystemPromptImprove,
                        userContent,
                        referer: "https://local.copy-polish",
                        title: "CopyPolish Outlook Add-in");
                })
                .ContinueWith(t =>
                {
                    loading.BeginInvoke((Action)(() =>
                    {
                        try
                        {
                            if (t.IsFaulted)
                            {
                                var err = t.Exception?.GetBaseException()?.Message ?? "Bilinmeyen hata";
                                MessageBox.Show("İşlem sırasında hata oluştu:\n" + err, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            else
                            {
                                ReplaceSelectionText(doc, t.Result);
                            }
                        }
                        finally
                        {
                            Cursor.Current = Cursors.Default;
                            loading.Close();
                            loading.Dispose();
                        }
                    }));
                });
            }
            catch (System.Runtime.InteropServices.COMException ex) when ((uint)ex.HResult == 0x800A180E)
            {
                MessageBox.Show("Bu komut e-posta modunda kullanılamıyor. Lütfen e-postayı düzenleme penceresinde (Yanıtla/İlet veya Düzenle) açıp tekrar deneyin.", "Kısıtlama");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Beklenmeyen bir hata oluştu:\n" + ex.Message, "Hata");
            }
        }

        public void TranslateSelectedTextTrEn()
        {
            try
            {
                var doc = GetActiveWordDocument();
                if (doc == null)
                {
                    MessageBox.Show("Bir e-posta düzenleyici bulunamadı.", "Bilgi");
                    return;
                }

                var selectedText = SafeGetSelectionText(doc);
                if (string.IsNullOrWhiteSpace(selectedText))
                {
                    MessageBox.Show("Herhangi bir metin seçilmedi.", "Uyarı");
                    return;
                }

                var fullText = GetFullBodyText(doc) ?? string.Empty;

                var apiKey = Properties.Settings.Default.CopyPolishApiKey;
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    MessageBox.Show("Lütfen önce CopyPolish API anahtarını ayarlayın.", "Gerekli Ayar");
                    using (var form = new SettingsForm()) form.ShowDialog();
                    apiKey = Properties.Settings.Default.CopyPolishApiKey;
                    if (string.IsNullOrWhiteSpace(apiKey)) return;
                }

                string userContent =
                    "Use the full email content below (including conversation history and previous messages) only as context. Analyze the entire context carefully but translate ONLY the selected part from Turkish to English. Output only the translation.\n" +
                    "<EMAIL>\n" + fullText + "\n</EMAIL>\n" +
                    "Selected segment to translate:\n" +
                    "<SELECTION>\n" + selectedText + "\n</SELECTION>";

                var loading = new LoadingForm("Çeviri yapılıyor, lütfen bekleyin...");
                loading.Show();
                Cursor.Current = Cursors.WaitCursor;

                Task.Run(() =>
                {
                    return OpenRouterClient.Complete(
                        apiKey,
                        ModelName,
                        SystemPromptTranslate,
                        userContent,
                        referer: "https://local.copy-polish",
                        title: "CopyPolish Outlook Add-in");
                })
                .ContinueWith(t =>
                {
                    loading.BeginInvoke((Action)(() =>
                    {
                        try
                        {
                            if (t.IsFaulted)
                            {
                                var err = t.Exception?.GetBaseException()?.Message ?? "Bilinmeyen hata";
                                MessageBox.Show("Çeviri sırasında hata oluştu:\n" + err, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            else
                            {
                                ReplaceSelectionText(doc, t.Result);
                            }
                        }
                        finally
                        {
                            Cursor.Current = Cursors.Default;
                            loading.Close();
                            loading.Dispose();
                        }
                    }));
                });
            }
            catch (System.Runtime.InteropServices.COMException ex) when ((uint)ex.HResult == 0x800A180E)
            {
                MessageBox.Show("Bu komut e-posta modunda kullanılamıyor. Lütfen e-postayı düzenleme penceresinde (Yanıtla/İlet veya Düzenle) açıp tekrar deneyin.", "Kısıtlama");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Beklenmeyen bir hata oluştu:\n" + ex.Message, "Hata");
            }
        }

        private Outlook.Inspector GetActiveInspectorSafe()
        {
            try { return this.Application.ActiveInspector(); }
            catch { return null; }
        }

        public Word.Document GetActiveWordDocument()
        {
            var inspector = GetActiveInspectorSafe();
            if (inspector != null)
            {
                var wordDoc = inspector.WordEditor as Word.Document;
                if (wordDoc != null) return wordDoc;
            }

            var explorer = this.Application.ActiveExplorer();
            if (explorer != null)
            {
                try
                {
                    var inlineDoc = explorer.ActiveInlineResponseWordEditor as Word.Document;
                    if (inlineDoc != null) return inlineDoc;
                }
                catch { }
            }
            return null;
        }

        private static string GetFullBodyText(Word.Document doc)
        {
            try { return doc?.Content?.Text; }
            catch { return null; }
        }

        private static void ReplaceSelectionText(Word.Document doc, string newText)
        {
            if (doc == null || newText == null) return;
            try
            {
                var sel = doc.Application.Selection;
                if (sel != null)
                {
                    sel.Text = newText;
                    sel.Collapse(Word.WdCollapseDirection.wdCollapseEnd);
                    sel.TypeText("\n");
                }
            }
            catch { }
        }
        protected override Microsoft.Office.Core.IRibbonExtensibility CreateRibbonExtensibilityObject()
        {
            return new Ribbon1();
        }

        private static string SafeGetSelectionText(Word.Document doc)
        {
            if (doc == null) return null;
            Word.Selection sel = null;
            try
            {
                sel = doc.Application.Selection;
                if (sel == null) return null;
                var text = sel.Text;
                if (text == null) return null;
                return text.Replace("\a", string.Empty).Trim();
            }
            catch (System.Runtime.InteropServices.COMException ex) when ((uint)ex.HResult == 0x800A180E)
            {
                return null;
            }
        }

        void Inspectors_NewInspector(Outlook.Inspector Inspector)
        {
        }

        private void ThisAddIn_Startup(object sender, System.EventArgs e)
        {
            this.Application.Inspectors.NewInspector += new Outlook.InspectorsEvents_NewInspectorEventHandler(Inspectors_NewInspector);
            try
            {
                var explorers = this.Application.Explorers;
                if (explorers != null)
                {
                    explorers.NewExplorer += (Outlook.Explorer ex) =>
                    {
                        try
                        {
                            ex.InlineResponse += (_) => { Ribbon1.Invalidate(); };
                            ex.InlineResponseClose += () => { Ribbon1.Invalidate(); };
                            ex.SelectionChange += () => { Ribbon1.Invalidate(); };
                        }
                        catch { }
                    };
                }
                var activeEx = this.Application.ActiveExplorer();
                if (activeEx != null)
                {
                    try
                    {
                        activeEx.InlineResponse += (_) => { Ribbon1.Invalidate(); };
                        activeEx.InlineResponseClose += () => { Ribbon1.Invalidate(); };
                        activeEx.SelectionChange += () => { Ribbon1.Invalidate(); };
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void ThisAddIn_Shutdown(object sender, System.EventArgs e)
        {
        }

        #region VSTO tarafından üretilen kod

        private void InternalStartup()
        {
            this.Startup += new System.EventHandler(ThisAddIn_Startup);
            this.Shutdown += new System.EventHandler(ThisAddIn_Shutdown);
        }
        
        #endregion
    }
}
  