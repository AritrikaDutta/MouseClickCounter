using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MouseClickCounter
{
    public sealed class TrayApplicationContext : ApplicationContext
    {
        private readonly ClickStore _store;
        private readonly MouseHook _mouseHook;
        private readonly KeyboardHook _keyboardHook;
        private readonly NotifyIcon _tray;
        private readonly Timer _flushTimer;
        private readonly Form _syncForm;
        private TodayForm _todayForm;
        private Icon _icon;

        public TrayApplicationContext(ClickStore store)
        {
            _store = store;
            _icon = AppIcons.CreateTrayIcon();

            _syncForm = new Form();
            _syncForm.FormBorderStyle = FormBorderStyle.FixedToolWindow;
            _syncForm.ShowInTaskbar = false;
            _syncForm.Opacity = 0;
            _syncForm.Size = new Size(0, 0);
            _syncForm.Load += delegate { _syncForm.Hide(); };
            _syncForm.Show();

            _tray = new NotifyIcon();
            _tray.Icon = _icon;
            _tray.Visible = true;
            _tray.Text = "Clicks: 0 | Keys: 0";
            _tray.DoubleClick += delegate { ShowToday(); };

            var menu = new ContextMenuStrip();
            menu.Items.Add("Show today", null, delegate { ShowToday(); });
            menu.Items.Add("Open data folder", null, delegate { ExplorerHelper.OpenFolder(_store.DataDirectory); });
            menu.Items.Add(new ToolStripSeparator());

            var startupItem = new ToolStripMenuItem("Start with Windows");
            startupItem.CheckOnClick = true;
            startupItem.Checked = IsStartupEnabled();
            startupItem.CheckedChanged += delegate
            {
                SetStartup(startupItem.Checked);
            };
            menu.Items.Add(startupItem);

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, delegate { ExitApp(); });
            _tray.ContextMenuStrip = menu;

            _mouseHook = new MouseHook();
            _mouseHook.Clicked += OnClicked;
            _mouseHook.Scrolled += OnScrolled;
            _mouseHook.Start();

            _keyboardHook = new KeyboardHook();
            _keyboardHook.KeyTyped += OnKeyTyped;
            _keyboardHook.Start();

            _flushTimer = new Timer();
            _flushTimer.Interval = 30000;
            _flushTimer.Tick += delegate
            {
                _store.Flush();
                UpdateTrayText();
            };
            _flushTimer.Start();

            UpdateTrayText();
            _tray.ShowBalloonTip(2500, "Activity Counter",
                "Tracking clicks, scrolls, and keys. Double-click the tray icon for today.",
                ToolTipIcon.Info);
        }

        private void OnClicked(MouseButton button)
        {
            _store.RecordClick(button);
            RefreshTrayAsync();
        }

        private void OnScrolled()
        {
            _store.RecordScroll();
            RefreshTrayAsync();
        }

        private void OnKeyTyped(KeyKind kind)
        {
            _store.RecordKey(kind);
            RefreshTrayAsync();
        }

        private void RefreshTrayAsync()
        {
            try
            {
                if (_syncForm != null && _syncForm.IsHandleCreated && !_syncForm.IsDisposed)
                {
                    _syncForm.BeginInvoke(new Action(UpdateTrayText));
                }
            }
            catch
            {
            }
        }

        private void UpdateTrayText()
        {
            var text = "C:" + _store.TodayTotal.ToString("N0")
                + " S:" + _store.TodayScrolls.ToString("N0")
                + " K:" + _store.TodayKeys.ToString("N0");
            if (text.Length > 63) text = text.Substring(0, 63);
            _tray.Text = text;
        }

        private void ShowToday()
        {
            if (_todayForm == null || _todayForm.IsDisposed)
            {
                _todayForm = new TodayForm(_store);
                _todayForm.FormClosed += delegate { _todayForm = null; };
                _todayForm.Show();
            }
            else
            {
                _todayForm.RefreshView();
                _todayForm.Activate();
            }
        }

        private void ExitApp()
        {
            _flushTimer.Stop();
            _store.Flush();
            _mouseHook.Dispose();
            _keyboardHook.Dispose();
            _tray.Visible = false;
            _tray.Dispose();
            if (_icon != null) _icon.Dispose();
            if (_todayForm != null && !_todayForm.IsDisposed) _todayForm.Close();
            if (_syncForm != null && !_syncForm.IsDisposed) _syncForm.Close();
            ExitThread();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try { _store.Flush(); } catch { }
                try { if (_mouseHook != null) _mouseHook.Dispose(); } catch { }
                try { if (_keyboardHook != null) _keyboardHook.Dispose(); } catch { }
                try { if (_tray != null) { _tray.Visible = false; _tray.Dispose(); } } catch { }
                try { if (_icon != null) _icon.Dispose(); } catch { }
                try { if (_syncForm != null && !_syncForm.IsDisposed) _syncForm.Dispose(); } catch { }
            }
            base.Dispose(disposing);
        }

        private static string StartupValueName
        {
            get { return "MouseClickCounter"; }
        }

        private static bool IsStartupEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", false))
                {
                    if (key == null) return false;
                    var value = key.GetValue(StartupValueName) as string;
                    return !string.IsNullOrEmpty(value);
                }
            }
            catch
            {
                return false;
            }
        }

        private static void SetStartup(bool enabled)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key == null) return;
                    if (enabled)
                    {
                        var exe = Application.ExecutablePath;
                        key.SetValue(StartupValueName, "\"" + exe + "\"");
                    }
                    else
                    {
                        key.DeleteValue(StartupValueName, false);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not update startup setting:\n" + ex.Message,
                    "Activity Counter", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
