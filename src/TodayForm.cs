using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MouseClickCounter
{
    public sealed class TodayForm : Form
    {
        private readonly ClickStore _store;
        private readonly Label _totalLabel;
        private readonly Label _detailLabel;
        private readonly ListView _bucketList;
        private readonly Timer _refreshTimer;

        public TodayForm(ClickStore store)
        {
            _store = store;

            Text = "Activity Counter — Today";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = true;
            ShowInTaskbar = true;
            ClientSize = new Size(820, 440);
            Font = new Font("Segoe UI", 9F);

            _totalLabel = new Label();
            _totalLabel.AutoSize = false;
            _totalLabel.Location = new Point(16, 12);
            _totalLabel.Size = new Size(788, 32);
            _totalLabel.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            _totalLabel.Text = "Today: 0 clicks · 0 scrolls · 0 keys";

            _detailLabel = new Label();
            _detailLabel.AutoSize = false;
            _detailLabel.Location = new Point(16, 48);
            _detailLabel.Size = new Size(788, 40);
            _detailLabel.Text = "";

            _bucketList = new ListView();
            _bucketList.Location = new Point(16, 96);
            _bucketList.Size = new Size(788, 288);
            _bucketList.View = View.Details;
            _bucketList.FullRowSelect = true;
            _bucketList.GridLines = true;
            _bucketList.Columns.Add("Bucket", 120);
            _bucketList.Columns.Add("Left", 50);
            _bucketList.Columns.Add("Right", 50);
            _bucketList.Columns.Add("Mid", 45);
            _bucketList.Columns.Add("Clicks", 50);
            _bucketList.Columns.Add("Scrolls", 55);
            _bucketList.Columns.Add("Keys", 50);
            _bucketList.Columns.Add("Spaces", 55);
            _bucketList.Columns.Add("Enters", 50);
            _bucketList.Columns.Add("Backsp", 55);

            var openBtn = new Button();
            openBtn.Text = "Open data folder";
            openBtn.Location = new Point(16, 396);
            openBtn.Size = new Size(140, 28);
            openBtn.Click += delegate { ExplorerHelper.OpenFolder(_store.DataDirectory); };

            var closeBtn = new Button();
            closeBtn.Text = "Close";
            closeBtn.Location = new Point(708, 396);
            closeBtn.Size = new Size(96, 28);
            closeBtn.Click += delegate { Close(); };

            Controls.Add(_totalLabel);
            Controls.Add(_detailLabel);
            Controls.Add(_bucketList);
            Controls.Add(openBtn);
            Controls.Add(closeBtn);

            _refreshTimer = new Timer();
            _refreshTimer.Interval = 1000;
            _refreshTimer.Tick += delegate { RefreshView(); };
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            RefreshView();
            _refreshTimer.Start();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _refreshTimer.Stop();
            _refreshTimer.Dispose();
            base.OnFormClosed(e);
        }

        public void RefreshView()
        {
            var day = _store.Snapshot();
            if (day == null) return;

            _totalLabel.Text = "Today: " + day.Daily.Total.ToString("N0") + " clicks · "
                + day.Daily.Scrolls.ToString("N0") + " scrolls · "
                + day.Daily.Keys.ToString("N0") + " keys";
            _detailLabel.Text = day.Date + " (" + day.DayType + ")   "
                + "L " + day.Daily.Left.ToString("N0")
                + "  R " + day.Daily.Right.ToString("N0")
                + "  M " + day.Daily.Middle.ToString("N0")
                + "   |   Spaces " + day.Daily.Spaces.ToString("N0")
                + "  Enters " + day.Daily.Enters.ToString("N0")
                + "  Backspaces " + day.Daily.Backspaces.ToString("N0");

            _bucketList.BeginUpdate();
            _bucketList.Items.Clear();
            for (int i = 0; i < day.Buckets.Count; i++)
            {
                var b = day.Buckets[i];
                var item = new ListViewItem(b.Start + " – " + b.End);
                item.SubItems.Add(b.Counts.Left.ToString("N0"));
                item.SubItems.Add(b.Counts.Right.ToString("N0"));
                item.SubItems.Add(b.Counts.Middle.ToString("N0"));
                item.SubItems.Add(b.Counts.Total.ToString("N0"));
                item.SubItems.Add(b.Counts.Scrolls.ToString("N0"));
                item.SubItems.Add(b.Counts.Keys.ToString("N0"));
                item.SubItems.Add(b.Counts.Spaces.ToString("N0"));
                item.SubItems.Add(b.Counts.Enters.ToString("N0"));
                item.SubItems.Add(b.Counts.Backspaces.ToString("N0"));
                _bucketList.Items.Add(item);
            }
            _bucketList.EndUpdate();
        }
    }

    internal static class ExplorerHelper
    {
        public static void OpenFolder(string path)
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", path);
            }
            catch
            {
                MessageBox.Show("Could not open: " + path, "Activity Counter",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }

    public static class AppIcons
    {
        public static Icon CreateTrayIcon()
        {
            using (var bmp = new Bitmap(16, 16))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (var brush = new SolidBrush(Color.FromArgb(30, 136, 229)))
                {
                    g.FillEllipse(brush, 1, 1, 13, 13);
                }
                using (var pen = new Pen(Color.White, 1.5f))
                {
                    g.DrawEllipse(pen, 4, 4, 7, 7);
                }
                using (var brush = new SolidBrush(Color.White))
                {
                    g.FillRectangle(brush, 7, 10, 2, 4);
                }
                var hIcon = bmp.GetHicon();
                var icon = Icon.FromHandle(hIcon);
                var clone = (Icon)icon.Clone();
                DestroyIcon(hIcon);
                return clone;
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);
    }
}
