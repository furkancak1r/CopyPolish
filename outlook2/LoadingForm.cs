using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace outlook2
{
    public class LoadingForm : Form
    {
        private readonly Label _titleLabel;
        private readonly Label _messageLabel;
        private readonly Panel _spinnerPanel;
        private readonly Timer _animationTimer;
        private float _rotationAngle = 0;

        public LoadingForm(string message = null)
        {
            Text = "";
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = true;
            ClientSize = new Size(400, 180);
            BackColor = Color.White;
            
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);

            _titleLabel = new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "CopyPolish AI",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50),
                Location = new Point(20, 20),
                Size = new Size(360, 35),
                BackColor = Color.Transparent
            };

            _messageLabel = new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = string.IsNullOrWhiteSpace(message) ? "Yapay zekadan yanıt bekleniyor..." : message,
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.FromArgb(100, 100, 100),
                Location = new Point(20, 130),
                Size = new Size(360, 30),
                BackColor = Color.Transparent
            };

            _spinnerPanel = new Panel
            {
                Location = new Point(170, 65),
                Size = new Size(60, 60),
                BackColor = Color.Transparent
            };

            _animationTimer = new Timer
            {
                Interval = 30,
                Enabled = true
            };
            _animationTimer.Tick += AnimationTimer_Tick;

            Controls.Add(_titleLabel);
            Controls.Add(_messageLabel);
            Controls.Add(_spinnerPanel);

            Paint += LoadingForm_Paint;
            _spinnerPanel.Paint += SpinnerPanel_Paint;
            
            Load += (s, e) => ApplyRoundedCorners();
        }

        private void ApplyRoundedCorners()
        {
            using (GraphicsPath path = GraphicsExtensions.CreateRoundedRectPath(new Rectangle(0, 0, Width, Height), 16))
            {
                Region = new Region(path);
            }
        }

        private void LoadingForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (SolidBrush brush = new SolidBrush(Color.White))
            {
                g.FillRoundedRectangle(brush, ClientRectangle, 16);
            }

            using (Pen shadowPen = new Pen(Color.FromArgb(30, 0, 0, 0), 2))
            {
                Rectangle shadowRect = new Rectangle(2, 2, Width - 4, Height - 4);
                g.DrawRoundedRectangle(shadowPen, shadowRect, 16);
            }

            using (Pen borderPen = new Pen(Color.FromArgb(220, 220, 220), 1))
            {
                g.DrawRoundedRectangle(borderPen, new Rectangle(0, 0, Width - 1, Height - 1), 16);
            }
        }

        private void SpinnerPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int centerX = _spinnerPanel.Width / 2;
            int centerY = _spinnerPanel.Height / 2;

            g.TranslateTransform(centerX, centerY);
            g.RotateTransform(_rotationAngle);

            for (int i = 0; i < 12; i++)
            {
                float angle = i * 30f;
                float opacity = 1.0f - (i * 0.08f);
                
                using (Pen pen = new Pen(Color.FromArgb((int)(255 * opacity), 74, 144, 226), 4))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    
                    g.RotateTransform(30);
                    g.DrawLine(pen, 0, -18, 0, -12);
                }
            }

            g.ResetTransform();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            _rotationAngle += 10f;
            if (_rotationAngle >= 360f)
                _rotationAngle = 0f;
            
            _spinnerPanel.Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer?.Stop();
                _animationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle rect, int radius)
        {
            using (GraphicsPath path = CreateRoundedRectPath(rect, radius))
            {
                g.FillPath(brush, path);
            }
        }

        public static void DrawRoundedRectangle(this Graphics g, Pen pen, Rectangle rect, int radius)
        {
            using (GraphicsPath path = CreateRoundedRectPath(rect, radius))
            {
                g.DrawPath(pen, path);
            }
        }

        public static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            
            return path;
        }
    }
}

