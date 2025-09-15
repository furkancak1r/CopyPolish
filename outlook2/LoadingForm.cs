using System;
using System.Drawing;
using System.Windows.Forms;

namespace outlook2
{
    public class LoadingForm : Form
    {
        private readonly Label _label;
        private readonly ProgressBar _progress;

        public LoadingForm(string message = null)
        {
            Text = "Lütfen bekleyin";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            ClientSize = new Size(420, 110);

            _label = new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = string.IsNullOrWhiteSpace(message) ? "Yapay zekadan yanıt bekleniyor..." : message,
                Location = new Point(16, 16),
                Size = new Size(388, 28)
            };

            _progress = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Location = new Point(16, 56),
                Size = new Size(388, 18)
            };

            Controls.Add(_label);
            Controls.Add(_progress);
        }
    }
}

