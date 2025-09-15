using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Office = Microsoft.Office.Core;
using Outlook = Microsoft.Office.Interop.Outlook;
using stdole;

namespace CopyPolish
{
    [ComVisible(true)]
    public class Ribbon1 : Office.IRibbonExtensibility
    {
        private Office.IRibbonUI ribbon;
        internal static Office.IRibbonUI RibbonUI;

        public Ribbon1() { }

        public string GetCustomUI(string ribbonID)
        {
            try
            {
                Log($"--- GetCustomUI called for RibbonID = {ribbonID} ---");
                string resourceName = "";
                switch (ribbonID)
                {
                    case "Microsoft.Outlook.Mail.Compose":
                        resourceName = "CopyPolish.RibbonCompose.xml";
                        break;
                    case "Microsoft.Outlook.Mail.Read":
                        resourceName = "CopyPolish.RibbonRead.xml";
                        break;
                    case "Microsoft.Outlook.Explorer":
                        resourceName = "CopyPolish.RibbonExplorer.xml";
                        break;
                    default:
                        Log($"  -> Unknown RibbonID. Falling back to default.");
                        resourceName = "CopyPolish.Ribbon1.xml";
                        break;
                }

                Log($"  -> Mapped to resource: {resourceName}");
                string xmlContent = GetResourceText(resourceName);

                if (string.IsNullOrEmpty(xmlContent))
                {
                    Log($"  -> CRITICAL: GetResourceText returned NULL or EMPTY for {resourceName}. Ensure it's an Embedded Resource.");
                }
                else
                {
                    Log($"  -> Returning XML content. Length: {xmlContent.Length}");
                }
                return xmlContent;
            }
            catch (Exception ex)
            {
                Log("FATAL ERROR in GetCustomUI", ex);
                return null;
            }
        }

        public void Ribbon_Load(Office.IRibbonUI ribbonUI)
        {
            try
            {
                Log("Ribbon_Load called.");
                this.ribbon = ribbonUI;
                RibbonUI = ribbonUI;
                Log("Ribbon_Load successful.");
            }
            catch (Exception ex)
            {
                Log("ERROR in Ribbon_Load", ex);
            }
        }

        public void OnImproveSelectionClick(Office.IRibbonControl control)
        {
            try
            {
                Log($"OnImproveSelectionClick triggered by control: {control?.Id}");
                Globals.ThisAddIn.ImproveSelectedText();
            }
            catch (Exception ex)
            {
                Log("ERROR in OnImproveSelectionClick", ex);
            }
        }

        public void OnTranslateTrEnClick(Office.IRibbonControl control)
        {
            try
            {
                Log($"OnTranslateTrEnClick triggered by control: {control?.Id}");
                Globals.ThisAddIn.TranslateSelectedTextTrEn();
            }
            catch (Exception ex)
            {
                Log("ERROR in OnTranslateTrEnClick", ex);
            }
        }

        public void OnCopyPolishSettingsClick(Office.IRibbonControl control)
        {
            try
            {
                Log($"OnCopyPolishSettingsClick triggered by control: {control?.Id}");
                using (var form = new SettingsForm())
                {
                    form.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                Log("ERROR in OnCopyPolishSettingsClick", ex);
                MessageBox.Show("Ayarlar penceresi açılırken hata oluştu:\n" + ex.Message, "Hata");
            }
        }

        public bool GetSelectionDependentEnabled(Office.IRibbonControl control)
        {
            try
            {
                bool isEnabled = Globals.ThisAddIn.GetActiveWordDocument() != null;
                Log($"GetSelectionDependentEnabled called for '{control?.Id}'. Returning {isEnabled}.");
                return isEnabled;
            }
            catch (Exception ex)
            {
                Log("ERROR in GetSelectionDependentEnabled", ex);
                return false;
            }
        }

        public bool IsExplorerSelectionEnabled_Final(Office.IRibbonControl control)
        {
            Outlook.Selection selection = null;
            try
            {
                selection = Globals.ThisAddIn.Application.ActiveExplorer()?.Selection;
                bool isEnabled = selection != null && selection.Count > 0;
                Log($"IsExplorerSelectionEnabled_Final called for '{control?.Id}'. Selection count > 0: {isEnabled}.");
                return isEnabled;
            }
            catch (Exception ex)
            {
                Log("ERROR in IsExplorerSelectionEnabled_Final", ex);
                return false;
            }
            finally
            {
                if (selection != null)
                {
                    Marshal.ReleaseComObject(selection);
                }
            }
        }

        public static void Invalidate()
        {
            try { RibbonUI?.Invalidate(); } catch { }
        }

        public static void ActivateTab(string idMso)
        {
            try
            {
                Log($"ActivateTab requested: {idMso}");
                RibbonUI?.ActivateTabMso(idMso);
            }
            catch (Exception ex)
            {
                Log($"ERROR in ActivateTab for {idMso}", ex);
            }
        }

        public IPictureDisp GetButtonImage(Office.IRibbonControl control)
        {
            return null;
        }

        public string GetButtonLabel(Office.IRibbonControl control)
        {
            try
            {
                if (control == null) return null;
                switch (control.Id)
                {
                    case "btnShowSelectedText":
                        return "İyileştir";
                    case "btnTranslateTrEn":
                        return "Çeviri";
                    case "btnCopyPolishSettings":
                        return "CopyPolish Ayarları";
                }
            }
            catch (Exception ex)
            {
                Log("ERROR in GetButtonLabel", ex);
            }
            return null;
        }

        private static void Log(string msg, Exception ex = null)
        {
            try
            {
                Directory.CreateDirectory(@"C:\temp");
                string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {msg}{Environment.NewLine}";
                if (ex != null)
                {
                    logMessage += $"        EXCEPTION: {ex.GetType().Name} - {ex.Message}{Environment.NewLine}";
                    logMessage += $"        STACKTRACE: {ex.StackTrace}{Environment.NewLine}";
                }
                File.AppendAllText(@"C:\temp\ribbonlog.txt", logMessage);
            }
            catch
            {
                // Logging should never cause the application to crash.
            }
        }

        private static string GetResourceText(string resourceName)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string[] resourceNames = asm.GetManifestResourceNames();
            for (int i = 0; i < resourceNames.Length; ++i)
            {
                if (string.Compare(resourceName, resourceNames[i], StringComparison.OrdinalIgnoreCase) == 0)
                {
                    using (StreamReader resourceReader = new StreamReader(asm.GetManifestResourceStream(resourceNames[i])))
                    {
                        if (resourceReader != null)
                        {
                            return resourceReader.ReadToEnd();
                        }
                    }
                }
            }
            Log($"CRITICAL: Resource '{resourceName}' not found in assembly. Available resources: {string.Join(", ", resourceNames)}");
            return null;
        }

        private class PictureConverter : AxHost
        {
            private PictureConverter() : base("") { }
            public static IPictureDisp ImageToPictureDisp(Image image)
            {
                return (IPictureDisp)AxHost.GetIPictureDispFromPicture(image);
            }
        }
        private static Bitmap ResizeImage(Image source, int width, int height)
        {
            var bmp = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);
                g.DrawImage(source, new Rectangle(0, 0, width, height));
            }
            return bmp;
        }
    }
}
  