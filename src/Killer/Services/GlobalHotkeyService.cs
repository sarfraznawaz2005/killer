using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Killer.Services;

public class GlobalHotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HotkeyId = 0x4B49; // arbitrary, spells "KI"

    private HwndSource? _source;
    private bool _registered;

    public event Action? HotkeyPressed;

    public bool Register(Window window, ModifierKeys modifiers, Key key)
    {
        Unregister();

        var handle = new WindowInteropHelper(window).EnsureHandle();
        _source = HwndSource.FromHwnd(handle);
        _source.AddHook(HwndHook);

        var virtualKey = (uint)KeyInterop.VirtualKeyFromKey(key);
        var nativeModifiers = ToNativeModifiers(modifiers);

        _registered = RegisterHotKey(handle, HotkeyId, nativeModifiers, virtualKey);
        return _registered;
    }

    public void Unregister()
    {
        if (_source is null)
        {
            return;
        }

        if (_registered)
        {
            UnregisterHotKey(_source.Handle, HotkeyId);
            _registered = false;
        }

        _source.RemoveHook(HwndHook);
        _source = null;
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            HotkeyPressed?.Invoke();
            handled = true;
        }

        return IntPtr.Zero;
    }

    private static uint ToNativeModifiers(ModifierKeys modifiers)
    {
        uint result = 0;
        if (modifiers.HasFlag(ModifierKeys.Alt)) result |= 0x0001;
        if (modifiers.HasFlag(ModifierKeys.Control)) result |= 0x0002;
        if (modifiers.HasFlag(ModifierKeys.Shift)) result |= 0x0004;
        if (modifiers.HasFlag(ModifierKeys.Windows)) result |= 0x0008;
        return result;
    }

    public void Dispose() => Unregister();

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
