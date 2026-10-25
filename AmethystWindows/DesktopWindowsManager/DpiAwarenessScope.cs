using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using Vanara.PInvoke;

namespace AmethystWindows.DesktopWindowsManager
{
    // Use one physical-pixel coordinate space for cursor selection and monitor layout geometry.
    internal sealed class DpiAwarenessScope : IDisposable
    {
        private static readonly IntPtr PerMonitorAwareV2 = new IntPtr(-4);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);

        private readonly IntPtr previousContext;

        private DpiAwarenessScope(IntPtr previousContext)
        {
            this.previousContext = previousContext;
        }

        public static DpiAwarenessScope EnterPerMonitorAwareV2()
        {
            IntPtr previousContext = SetThreadDpiAwarenessContext(PerMonitorAwareV2);
            if (previousContext == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return new DpiAwarenessScope(previousContext);
        }

        public static HMONITOR GetMonitorAtPhysicalCursor()
        {
            using DpiAwarenessScope dpiAwarenessScope = EnterPerMonitorAwareV2();
            if (!GetPhysicalCursorPos(out Point cursorPosition))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return User32.MonitorFromPoint(cursorPosition, User32.MonitorFlags.MONITOR_DEFAULTTONEAREST);
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetPhysicalCursorPos(out Point point);

        public void Dispose()
        {
            SetThreadDpiAwarenessContext(previousContext);
        }
    }
}
