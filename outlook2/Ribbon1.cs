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

        public void OnShowTextButtonClick(Office.IRibbonControl control)
        {
            Globals.ThisAddIn.ShowSelectedText();
        }

        public IPictureDisp GetButtonImage(Office.IRibbonControl control)
        {
            try
            {
                if (control != null && control.Id == "btnShowSelectedText")
                {
                    // Try to load from resources by name "icon"
                    object obj = outlook2.Properties.Resources.ResourceManager.GetObject("icon");
                    Image img = null;

                    if (obj is Icon icn)
                    {
                        img = icn.ToBitmap();
                    }
                    else if (obj is Bitmap bmp)
                    {
                        img = bmp;
                    }

                    // Fallback to file path if resource missing
                    if (img == null)
                    {
                        string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                        string iconPath = Path.Combine(baseDir ?? string.Empty, "icon.ico");
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
                        return PictureConverter.ImageToPictureDisp(img);
                    }
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
    }
}
