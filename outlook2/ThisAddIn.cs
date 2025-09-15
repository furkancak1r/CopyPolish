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
  
