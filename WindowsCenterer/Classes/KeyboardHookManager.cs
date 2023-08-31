using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class KeyboardHookManager
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

    private LowLevelKeyboardProc _hookProc;
    private IntPtr _hookId = IntPtr.Zero;

    public event EventHandler<KeyEventArgs> KeyDown;
    public event EventHandler<KeyEventArgs> KeyUp;

    public void Start()
    {
        _hookProc = HookCallback;
        _hookId = SetHook(_hookProc);
    }

    public void Stop()
    {
        UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
    }

    public static void SendCtrlZ()
    {
        const byte VK_CONTROL = 0x11;
        const byte VK_Z = 0x5A;
        const int KEYEVENTF_KEYUP = 0x2;

        // Нажатие клавиши Ctrl
        keybd_event(VK_CONTROL, 0, 0, 0);
        // Нажатие клавиши Z
        keybd_event(VK_Z, 0, 0, 0);
        // Отпускание клавиш Ctrl и Z
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, 0);
        keybd_event(VK_Z, 0, KEYEVENTF_KEYUP, 0);
    }

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
        {
            int vkCode = Marshal.ReadInt32(lParam);

            KeyEventArgs eventArgs = new KeyEventArgs((Keys)vkCode);
            KeyDown?.Invoke(this, eventArgs);

            if (eventArgs.Handled)
            {
                return (IntPtr)1;
            }
        }
        else if (nCode >= 0 && (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP))
        {
            int vkCode = Marshal.ReadInt32(lParam);

            KeyEventArgs eventArgs = new KeyEventArgs((Keys)vkCode);
            KeyUp?.Invoke(this, eventArgs);

            if (eventArgs.Handled)
            {
                return (IntPtr)1;
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }
}