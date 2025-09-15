// C:/Users/furkan.cakir/Desktop/FurkanPRS/Kodlar/Projeler/test/outlook2/outlook2/Ribbon1.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Office = Microsoft.Office.Core;
using Outlook = Microsoft.Office.Interop.Outlook;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using stdole;
namespace outlook2
{
    [ComVisible(true)]
    public class Ribbon1 : Office.IRibbonExtensibility
    {
        private Office.IRibbonUI ribbon;

        public Ribbon1()
        {
        }

        public string GetCustomUI(string ribbonID)
        {
            return GetResourceText("outlook2.Ribbon1.xml");
        }

        public void Ribbon_Load(Office.IRibbonUI ribbonUI)
        {
            this.ribbon = ribbonUI;
        }

        public void OnImproveSelectionClick(Office.IRibbonControl control)
        {
            Globals.ThisAddIn.ImproveSelectedText();
        }

        public void OnTranslateTrEnClick(Office.IRibbonControl control)
        {
            Globals.ThisAddIn.TranslateSelectedTextTrEn();
        }

        public void OnCopyPolishSettingsClick(Office.IRibbonControl control)
        {
            try
            {
                using (var form = new SettingsForm())
                {
                    form.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ayarlar penceresi açılamadı:\n" + ex.Message, "Hata");
            }
        }

        public IPictureDisp GetButtonImage(Office.IRibbonControl control)
        {
            try
            {
                if (control == null) return null;

                string fileName = null;
                string resourceName = null;
                switch (control.Id)
                {
                    case "btnShowSelectedText":
                        fileName = "IYILESTIR.ico";
                        resourceName = "IYILESTIR";
                        break;
                    case "btnTranslateTrEn":
                        fileName = "EN-TR.ico";
                        resourceName = "EN_TR";
                        break;
                    case "btnCopyPolishSettings":
                        fileName = "AYARLAR.ico";
                        resourceName = "AYARLAR";
                        break;
                }

                Image img = null;

                // Try embedded resources first
                if (!string.IsNullOrEmpty(resourceName))
                {
                    object res = outlook2.Properties.Resources.ResourceManager.GetObject(resourceName);
                    if (res is Icon ricn)
                    {
                        img = ricn.ToBitmap();
                    }
                    else if (res is Bitmap rbmp)
                    {
                        img = rbmp;
                    }
                }

                // Load from output directory first using the provided .ico files
                if (img == null && !string.IsNullOrEmpty(fileName))
                {
                    string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    string iconPath = Path.Combine(baseDir ?? string.Empty, fileName);
                    if (File.Exists(iconPath))
                    {
                        using (var iconFromFile = new Icon(iconPath))
                        {
                            img = iconFromFile.ToBitmap();
                        }
                    }
                }

                if (img != null)
                {
                    // Render at 32x32; Ribbon will scale if needed
                    int target = 32;
                    using (var sized = ResizeImage(img, target, target))
                    {
                        return PictureConverter.ImageToPictureDisp(new Bitmap(sized));
                    }
                }
            }
            catch { }

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
                        return "TR > EN";
                    case "btnCopyPolishSettings":
                        return "CopyPolish Ayarları";
                }
            }
            catch { }
            return null;
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
