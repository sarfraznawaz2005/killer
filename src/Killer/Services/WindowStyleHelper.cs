using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Killer.Services;

public static class WindowStyleHelper
{
    private const int GWL_STYLE = -16;
    private const int WS_MAXIMIZEBOX = 0x10000;

    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    // Removing WS_MAXIMIZEBOX (rather than just ResizeMode) also disables double-click-
    // titlebar and drag-to-top-edge maximizing, not just the titlebar button.
    public static void DisableMaximize(Window window)
    {
        window.SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            var style = GetWindowLong(hwnd, GWL_STYLE);
            SetWindowLong(hwnd, GWL_STYLE, style & ~WS_MAXIMIZEBOX);
        };
    }
}
