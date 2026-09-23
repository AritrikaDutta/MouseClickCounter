using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MouseClickCounter
{
    public sealed class MouseHook : IDisposable
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_MOUSEHWHEEL = 0x020E;

        private LowLevelMouseProc _proc;
        private IntPtr _hookId = IntPtr.Zero;
        private bool _disposed;

        public event Action<MouseButton> Clicked;
        public event Action Scrolled;

        public void Start()
        {
            if (_hookId != IntPtr.Zero) return;
            _proc = HookCallback;
            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                _hookId = SetWindowsHookEx(WH_MOUSE_LL, _proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
            if (_hookId == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to install mouse hook.");
            }
        }

        public void Stop()
        {
            if (_hookId == IntPtr.Zero) return;
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                if (msg == WM_MOUSEWHEEL || msg == WM_MOUSEHWHEEL)
                {
                    var handler = Scrolled;
                    if (handler != null)
                    {
                        try { handler(); }
                        catch { /* never break the hook chain */ }
                    }
                }
                else
                {
                    MouseButton? button = null;
                    if (msg == WM_LBUTTONDOWN) button = MouseButton.Left;
                    else if (msg == WM_RBUTTONDOWN) button = MouseButton.Right;
                    else if (msg == WM_MBUTTONDOWN) button = MouseButton.Middle;

                    if (button.HasValue)
                    {
                        var handler = Clicked;
                        if (handler != null)
                        {
                            try { handler(button.Value); }
                            catch { /* never break the hook chain */ }
                        }
                    }
                }
            }
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
