using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Outlook = Microsoft.Office.Interop.Outlook;
using Office = Microsoft.Office.Core;
using System.Windows.Forms;
using Word = Microsoft.Office.Interop.Word;

namespace outlook2
{
    public partial class ThisAddIn
    {
        private const string ModelName = "qwen/qwen3-coder:free";
        private const string SystemPromptImprove = @"Sen, bir e-postanın ana mesajını ve samimiyet tonunu koruyarak onu daha akıcı ve etkili hale getiren bir iletişim asistanısın. Aşağıdaki kurallara harfiyen uymalısın:

1.  TONU KORU (En Önemli Kural): Orijinal metin ne kadar samimi veya resmi ise, senin metnin de o seviyede olmalıdır. Samimi bir dili (""Selam abi"") asla aşırı resmi bir dile (""Sayın Yetkili"") çevirme.
2.  ANLAMI DEĞİŞTİRME: Cümlenin temel anlamını, amacını veya içeriği komutu asla değiştirme. Sadece dilbilgisi, akıcılık ve yazım hatalarını düzelt. Örneğin, 'Dosyayı ilet' komutunu 'Dosyayı iletiyorum' ifadesine çevirme.
3.  SELAMLAMAYI KORU: Orijinal metindeki selamlama ne ise (örn: ""Merhaba,""), yanıtın da birebir aynı selamlamayla başlamalıdır.
4.  GEREKSİZ BİLGİ EKLEME: Orijinal metinde olmayan bilgileri (""...bilginize sunarım"" gibi) ekleme.
5.  PLACEHOLDER KULLANMA: Yanıtına ""[ADINIZ]"" gibi yer tutucular ekleme.
6.  TEKNİK TOKEN GÖSTERME: Yanıtın asla '<|...|>' gibi teknik token'lar içermemeli.
7.  SADECE YENİDEN YAZILMIŞ METNİ DÖNDÜR: Yanıtın, sadece ve sadece yeniden yazılmış metni içermelidir, başka hiçbir şey değil.
8.  Mailleri her zaman daha kibar bir şekilde yaz. Emreder gibi yazma asla olmamalı.";

        private const string SystemPromptTranslate = @"You are a precise translator from Turkish to English. Follow these rules:

1. Preserve meaning and tone. Do not embellish.
2. Output only the English translation text, nothing else.
3. Keep formatting and line breaks when possible.";

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
                    "Tüm e-posta içeriği aşağıdadır. Bağlam olarak kullan fakat sadece seçili metni düzelt.\n" +
                    "<EMAIL>\n" + fullText + "\n</EMAIL>\n" +
                    "Seçili bölüm aşağıdadır. Sadece bunun düzeltilmiş halini, başka bir şey olmadan döndür.\n" +
                    "<SELECTION>\n" + selectedText + "\n</SELECTION>";

                string improved = OpenRouterClient.Complete(
                    apiKey,
                    ModelName,
                    SystemPromptImprove,
                    userContent,
                    referer: "https://local.copy-polish",
                    title: "CopyPolish Outlook Add-in");

                ReplaceSelectionText(doc, improved);
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
                    "Use the full email content below only as context. Translate ONLY the selected part from Turkish to English. Output only the translation.\n" +
                    "<EMAIL>\n" + fullText + "\n</EMAIL>\n" +
                    "Selected segment to translate:\n" +
                    "<SELECTION>\n" + selectedText + "\n</SELECTION>";

                string translated = OpenRouterClient.Complete(
                    apiKey,
                    ModelName,
                    SystemPromptTranslate,
                    userContent,
                    referer: "https://local.copy-polish",
                    title: "CopyPolish Outlook Add-in");

                ReplaceSelectionText(doc, translated);
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

        private Word.Document GetActiveWordDocument()
        {
            // 1) Inspector (compose window)
            var inspector = GetActiveInspectorSafe();
            if (inspector != null)
            {
                var wordDoc = inspector.WordEditor as Word.Document;
                if (wordDoc != null) return wordDoc;
            }

            // 2) Explorer inline response
            var explorer = this.Application.ActiveExplorer();
            if (explorer != null)
            {
                try
                {
                    var inlineDoc = explorer.ActiveInlineResponseWordEditor as Word.Document; // Outlook 2013+
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

        public void ShowSelectedText()
        {
            try
            {
                // 1) Açık e‑posta penceresi (Inspector)
                Outlook.Inspector inspector = this.Application.ActiveInspector();
                if (inspector != null)
                {
                    var wordDoc = inspector.WordEditor as Word.Document;
                    if (wordDoc != null)
                    {
                        string selectedText = SafeGetSelectionText(wordDoc);
                        if (!string.IsNullOrWhiteSpace(selectedText))
                        {
                            MessageBox.Show("Seçilen Metin: " + selectedText, "Seçim");
                        }
                        else
                        {
                            MessageBox.Show("Herhangi bir metin seçilmedi.", "Uyarı");
                        }
                        return;
                    }
                }

                // 2) Explorer içinde satır içi yanıtlama (inline compose) var mı?
                Outlook.Explorer explorer = this.Application.ActiveExplorer();
                if (explorer != null)
                {
                    try
                    {
                        var inlineDoc = explorer.ActiveInlineResponseWordEditor as Word.Document; // Outlook 2013+
                        if (inlineDoc != null)
                        {
                            string selectedText = SafeGetSelectionText(inlineDoc);
                            if (!string.IsNullOrWhiteSpace(selectedText))
                            {
                                MessageBox.Show("Seçilen Metin: " + selectedText, "Seçim");
                            }
                            else
                            {
                                MessageBox.Show("Herhangi bir metin seçilmedi.", "Uyarı");
                            }
                            return;
                        }
                    }
                    catch
                    {
                        // Özellik mevcut olmayabilir; sessizce devam et.
                    }
                }

                // Hiçbir Word düzenleyici bulunamadı
                MessageBox.Show("Bir e‑posta düzenleyici bulunamadı.", "Bilgi");
            }
            catch (System.Runtime.InteropServices.COMException ex) when ((uint)ex.HResult == 0x800A180E)
            {
                // Word e‑posta modunda bazı komutları kısıtlayabilir
                MessageBox.Show("Bu komut e‑posta modunda kullanılamıyor. Lütfen e‑postayı düzenleme penceresinde (Yanıtla/İlet veya Düzenle) açıp tekrar deneyin.", "Kısıtlama");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Beklenmeyen bir hata oluştu:\n" + ex.Message, "Hata");
            }
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
                // Seçim metninde bazen \a (BEL) ve satır sonu karakterleri bulunur.
                return text.Replace("\a", string.Empty).Trim();
            }
            catch (System.Runtime.InteropServices.COMException ex) when ((uint)ex.HResult == 0x800A180E)
            {
                return null; // E‑posta modunda desteklenmeyen senaryo
            }
        }

        private void ThisAddIn_Startup(object sender, System.EventArgs e)
        {
        }

        private void ThisAddIn_Shutdown(object sender, System.EventArgs e)
        {
            // Not: Outlook artık bu olayı oluşturmamaktadır. Outlook kapandığında 
            //    Outlook kapatıldığında çalıştırılmalıdır, bkz. https://go.microsoft.com/fwlink/?LinkId=506785
        }

        #region VSTO tarafından üretilen kod

        /// <summary>
        /// Tasarımcı desteği için gerekli metot - bu metodun 
        ///içeriğini kod düzenleyici ile değiştirmeyin.
        /// </summary>
        private void InternalStartup()
        {
            this.Startup += new System.EventHandler(ThisAddIn_Startup);
            this.Shutdown += new System.EventHandler(ThisAddIn_Shutdown);
        }
        
        #endregion
    }
}
  
