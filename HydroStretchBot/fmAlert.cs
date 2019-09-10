using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using HydroStretchBot.Properties;

namespace HydroStretchBot {
    public partial class fmAlert : Form {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

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

        public struct LASTINPUTINFO {
            public uint cbSize;
            public uint dwTime;
        }

        /// <summary>
        /// Idle threshold time, 5 minutes.
        /// </summary>
        public uint UserIdleThreshold = 5 * 60 * 1000;

        /// <summary>
        /// Number of seconds the user has been working.
        /// </summary>
        public uint WorkingSeconds { get; set; }

        /// <summary>
        /// How many seconds to wait for break alert.
        /// </summary>
        public uint BreakThreshold = 60 * 60;

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

            // Setup timer, with an interval of 1 second.
            this.Timer = new Timer {
                Enabled = true,
                Interval = 1000
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
            var isUserIdle = this.IsUserIdle();

            if (isUserIdle) {
                this.WorkingSeconds = 0;
            }
            else {
                this.WorkingSeconds++;
            }

            // Seconds till next break.
            var seconds = this.BreakThreshold - this.WorkingSeconds;
            uint minutes = 0;

            if (seconds > 60) {
                minutes = seconds / 60;
                seconds -= minutes * 60;
            }

            this.lbText.Text = string.Format(
                "Time to next break is {0} min{1} and {2} sec{3}",
                minutes,
                minutes == 1 ? "" : "s",
                seconds,
                seconds == 1 ? "" : "s");

            if (this.WorkingSeconds < this.BreakThreshold) {
                return;
            }

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

        /// <summary>
        /// Get user idle time.
        /// </summary>
        public uint GetIdleTime() {
            var lastInPut = new LASTINPUTINFO();
            lastInPut.cbSize = (uint) Marshal.SizeOf(lastInPut);
            GetLastInputInfo(ref lastInPut);

            return (uint) Environment.TickCount - lastInPut.dwTime;
        }

        /// <summary>
        /// Returns true if user idle time is greater than set threshold.
        /// </summary>
        public bool IsUserIdle() {
            return this.GetIdleTime() > this.UserIdleThreshold;
        }
    }
}