using System.Windows;
using STSApplication.core;
using STSApplication.model;

namespace STSApplication
{
    public partial class MainWindow : Window
    {
        private readonly NotifyIcon _notifyIcon = new();

        public MainWindow()
        {
            SaunaTemperatureClient.InitUpdate(60, UpdateTrayIcon);
            InitializeComponent();
            _notifyIcon.Visible = true;
            Hide();
        }

        private void UpdateTrayIcon(LabelContent Content)
        {
            Bitmap bitmap = ApplicationResource.meter64x64;
            Font font = new("Arial", 48, System.Drawing.FontStyle.Regular);
            Brush brush = new SolidBrush(Color.White);
            Graphics graphics = Graphics.FromImage(bitmap);
            graphics.DrawString(Content.Temperature.ToString(), font, brush, -4, 2);
            _notifyIcon.Text = Content.Temperature.ToString() + "°C";
            _notifyIcon.Icon = System.Drawing.Icon.FromHandle(bitmap.GetHicon());
        }
    }
}