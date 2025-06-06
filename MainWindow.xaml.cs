﻿using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Extensions.Configuration;
using STSApplication.core;
using STSApplication.model;

namespace STSApplication
{
    public partial class MainWindow : Window
    {
        private readonly NotifyIcon _notifyIcon = new();
        private readonly RotateTransform rotate;

        public MainWindow()
        {
            InitializeComponent();

            rotate = new RotateTransform(0, 278, 180);
            Needle.RenderTransform = rotate;

            var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            IConfiguration config = builder.Build();
            string host = config["AppSettings:Host"]!;
            int port = config.GetValue<int>("AppSettings:Port");
            string path = config["AppSettings:Path"]!;
            int retryInterval = config.GetValue<int>("AppSettings:RetryInterval");

            // Init fetching
            SaunaTemperatureClient client = new(host, port, path, retryInterval);
            client.InitUpdate(60, UpdateUI);

            _notifyIcon.Visible = true;
            _notifyIcon.DoubleClick +=
                delegate (object? sender, EventArgs args)
                {
                    Show();
                    WindowState = WindowState.Normal;
                };

            Hide();
        }

        private void UpdateUI(TemperatureReading reading)
        {
            // Label
            DateTime date = DateTimeOffset.FromUnixTimeMilliseconds(reading.Timestamp).LocalDateTime;
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            DateTimeFormatInfo dtfi = culture.DateTimeFormat;
            dtfi.TimeSeparator = ":";
            TimestampLabel.Content = "Last Updated: " + date.ToString("HH:mm:ss  dd.MM.yyyy", dtfi);

            // Needle angle
            UpdateNeedle(reading.Temperature);
            
            // Tray icon
            int fontSize = reading.Temperature >= 100 ? 24 : 32;
            Bitmap bitmap = ApplicationResource.meter_64x64;
            Font font = new("Verdana", fontSize, System.Drawing.FontStyle.Regular);
            System.Drawing.Brush brush = new SolidBrush(System.Drawing.Color.White);
            Graphics graphics = Graphics.FromImage(bitmap);
            graphics.DrawString(reading.Temperature.ToString(), font, brush, -4, 4);
            _notifyIcon.Text = reading.Temperature.ToString() + "°C";
            _notifyIcon.Icon = System.Drawing.Icon.FromHandle(bitmap.GetHicon());
        }

        private void UpdateNeedle(int temperature)
        {
            DoubleAnimation rotateAnimation = new()
            {
                From = rotate.Angle,
                To = (temperature - 20) * 1.8,
                Duration = TimeSpan.FromSeconds(1)
            };
            rotate.BeginAnimation(RotateTransform.AngleProperty, rotateAnimation);
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
            base.OnStateChanged(e);
        }
    }
}