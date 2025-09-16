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

namespace CopyPolish
{
    public partial class ThisAddIn
    {
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

                var apiKey = Properties.Settings.Default.CopyPolishApiKey;
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    MessageBox.Show("Lütfen önce CopyPolish API anahtarını ayarlayın.", "Gerekli Ayar");
                    using (var form = new SettingsForm()) form.ShowDialog();
                    apiKey = Properties.Settings.Default.CopyPolishApiKey;
                    if (string.IsNullOrWhiteSpace(apiKey)) return;
                }

                string systemPrompt = Properties.Settings.Default.SystemPromptImprove;
                if (string.IsNullOrWhiteSpace(systemPrompt))
                {
                    MessageBox.Show("İyileştirme prompt'u ayarlarda boş. Lütfen ayarlardan doldurun.", "Eksik Ayar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string userContent;
                bool includeContext = Properties.Settings.Default.IncludeEmailContext;

                if (includeContext)
                {
                    var fullText = GetFullBodyText(doc) ?? string.Empty;
                    userContent =
                        "Tüm e-posta içeriği (konuşma geçmişi ve önceki mesajlar dahil) aşağıdadır. Tüm bağlamı dikkatlice analiz et fakat sadece seçili metni düzelt.\n" +
                        "<EMAIL>\n" + fullText + "\n</EMAIL>\n" +
                        "Seçili bölüm aşağıdadır. Sadece bunun düzeltilmiş halini, başka bir şey olmadan döndür.\n" +
                        "<SELECTION>\n" + selectedText + "\n</SELECTION>";
                }
                else
                {
                    userContent =
                        "Aşağıdaki seçili bölümü düzelt. Sadece bunun düzeltilmiş halini, başka bir şey olmadan döndür.\n" +
                        "<SELECTION>\n" + selectedText + "\n</SELECTION>";
                }


                var loading = new LoadingForm("Yapay zekadan yanıt bekleniyor...");
                loading.Show();
                Cursor.Current = Cursors.WaitCursor;

                Task.Run(() =>
                {
                    return OpenRouterClient.CompleteWithFallback(
                        apiKey,
                        ModelConfiguration.GetModelChain(),
                        systemPrompt,
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

                var apiKey = Properties.Settings.Default.CopyPolishApiKey;
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    MessageBox.Show("Lütfen önce CopyPolish API anahtarını ayarlayın.", "Gerekli Ayar");
                    using (var form = new SettingsForm()) form.ShowDialog();
                    apiKey = Properties.Settings.Default.CopyPolishApiKey;
                    if (string.IsNullOrWhiteSpace(apiKey)) return;
                }
                
                string systemPrompt = Properties.Settings.Default.SystemPromptTranslate;
                if (string.IsNullOrWhiteSpace(systemPrompt))
                {
                    MessageBox.Show("Çeviri prompt'u ayarlarda boş. Lütfen ayarlardan doldurun.", "Eksik Ayar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string userContent;
                bool includeContext = Properties.Settings.Default.IncludeEmailContext;

                if (includeContext)
                {
                    var fullText = GetFullBodyText(doc) ?? string.Empty;
                    userContent =
                        "Use the full email content below (including conversation history and previous messages) only as context. Analyze the entire context carefully but translate ONLY the selected part from Turkish to English. Output only the translation.\n" +
                        "<EMAIL>\n" + fullText + "\n</EMAIL>\n" +
                        "Selected segment to translate:\n" +
                        "<SELECTION>\n" + selectedText + "\n</SELECTION>";
                }
                else
                {
                    userContent =
                        "Translate ONLY the selected part from Turkish to English. Output only the translation.\n" +
                        "Selected segment to translate:\n" +
                        "<SELECTION>\n" + selectedText + "\n</SELECTION>";
                }


                var loading = new LoadingForm("Çeviri yapılıyor, lütfen bekleyin...");
                loading.Show();
                Cursor.Current = Cursors.WaitCursor;

                Task.Run(() =>
                {
                    return OpenRouterClient.CompleteWithFallback(
                        apiKey,
                        ModelConfiguration.GetModelChain(),
                        systemPrompt,
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
  