using System;
using System.Runtime.InteropServices;

public class WindowFocusTracker
{
    private const int EVENT_OBJECT_FOCUS = 0x8005;
    private const int WINEVENT_OUTOFCONTEXT = 0x0000;

    private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport("user32.dll")]
    public static extern bool GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    public static event EventHandler<IntPtr> WindowFocusChanged;

    private static WinEventDelegate _eventDelegate;
    private static IntPtr _hookHandle;

    public static void StartTracking()
    {
        _eventDelegate = new WinEventDelegate(WindowEventCallback);
        _hookHandle = SetWinEventHook(EVENT_OBJECT_FOCUS, EVENT_OBJECT_FOCUS, IntPtr.Zero, _eventDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
    }

    public static void StopTracking()
    {
        UnhookWinEvent(_hookHandle);
    }

    private static void WindowEventCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        WindowFocusChanged?.Invoke(null, hwnd);
    }
}