using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using HydroStretchBot.Properties;

namespace HydroStretchBot {
    public partial class fmAlert : Form {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        //Flash both the window caption and taskbar button.
        //This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags. 
        public const uint FLASHW_ALL = 3;

        // Flash continuously until the window comes to the foreground. 
        public const uint FLASHW_TIMERNOFG = 12;

        [StructLayout(LayoutKind.Sequential)]
        public struct FLASHWINFO {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        /// <summary>
        /// The hours the dialog has been displayed.
        /// </summary>
        private readonly List<string> HoursDisplayed = new List<string>();

        /// <summary>
        /// Main timer.
        /// </summary>
        private Timer Timer { get; set; }

        /// <summary>
        /// Init form.
        /// </summary>
        public fmAlert() {
            InitializeComponent();
        }

        /// <summary>
        /// Load location and setup timer.
        /// </summary>
        private void FmAlert_Load(object sender, EventArgs e) {
            // Set location.
            if (Settings.Default.WindowTop != -1 &&
                Settings.Default.WindowLeft != -1) {

                this.Location = new Point(
                    Settings.Default.WindowLeft,
                    Settings.Default.WindowTop);
            }

            // Setup timer.
            this.Timer = new Timer {
                Enabled = true,
                Interval = 500
            };

            this.Timer.Tick += TimerTick;
            this.Timer.Start();
        }

        /// <summary>
        /// Save location.
        /// </summary>
        private void FmAlert_ResizeEnd(object sender, EventArgs e) {
            Settings.Default.WindowTop = this.Location.Y;
            Settings.Default.WindowLeft = this.Location.X;
            Settings.Default.Save();
        }

        /// <summary>
        /// Reset timer.
        /// </summary>
        private void BtOk_Click(object sender, EventArgs e) {
            this.SendToBack();
            this.Timer.Start();
        }

        /// <summary>
        /// Update UI.
        /// </summary>
        private void TimerTick(object sender, EventArgs e) {
            var dt = DateTime.Now;
            var nh = dt.Hour + 1;

            if (nh == 24) {
                nh = 0;
            }

            var nd = new DateTime(
                dt.Year,
                dt.Month,
                dt.Day,
                nh, 0, 0);

            var ts = nd - dt;

            var s = (int) ts.TotalSeconds;
            var m = 0;

            if (s > 60) {
                m = s / 60;
                s -= m * 60;
            }

            this.lbText.Text = string.Format(
                "Time to next break is {0} min{1} and {2} sec{3}",
                m,
                m == 1 ? "" : "s",
                s,
                s == 1 ? "" : "s");

            if (m > 0 ||
                s > 0) {

                return;
            }

            var dts = dt.ToString("yyyy-MM-dd HH");

            if (this.HoursDisplayed.Contains(dts)) {
                return;
            }

            this.HoursDisplayed.Add(dts);

            this.lbText.Text = "It's time to take a break. Get up and stretch your legs and get something to drink.";
            this.Timer.Stop();

            this.BringToFront();
            this.FlashWindowEx();
        }

        /// <summary>
        /// Flash the window.
        /// </summary>
        public bool FlashWindowEx() {
            var hWnd = this.Handle;
            var fInfo = new FLASHWINFO();

            fInfo.cbSize = Convert.ToUInt32(Marshal.SizeOf(fInfo));
            fInfo.hwnd = hWnd;
            fInfo.dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG;
            fInfo.uCount = uint.MaxValue;
            fInfo.dwTimeout = 0;

            return FlashWindowEx(ref fInfo);
        }
    }
}