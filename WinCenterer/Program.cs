using System.Diagnostics;
using System.Text;
using WinCenterer.Properties;

public class Program
{
    static IntPtr _currentWindow;
    public static NotifyIcon? _notifyIcon;
    static ContextMenuStrip? _menu;
    static KeyboardHookManager _keyboardHookManager;
    static StringBuilder _windowName = new(256);
    static string _foregroundTitle;
    static int _countCtrlPressed = 0;

    static void Main(string[] args)
    {
        string menuTitle = "Запускать при старте Windows";

        _keyboardHookManager = new();
        _keyboardHookManager.KeyUp += (s, e) =>
        {
            if (e.KeyCode != Keys.LControlKey)
                _countCtrlPressed = 0;

            if (e.KeyCode == Keys.LControlKey)
                _countCtrlPressed++;

            if (_countCtrlPressed == 3)
            {
                _countCtrlPressed = 0;
                GoCenter(_currentWindow);
            }   
        };

        _notifyIcon = new()
        {
            Visible = true,
            Icon = Resources.appiconW,
            Text = "Нажмите Ctrl 3 раза, чтобы центрировать",
        };

        SettingsHelper.ReadSettings();

        System.Windows.Forms.Timer timerCtrlPressed = new()
        {
            Interval = 2500,
            Enabled = true,
        };
        timerCtrlPressed.Tick += (s, e) =>
        {
            if (_countCtrlPressed != 0)
                _countCtrlPressed = 0;

            SettingsHelper.CheckThemeChange();
        };

        _menu = new();
        _menu.ShowImageMargin = false;
        _menu.ShowCheckMargin = true;
        _menu.Items.Add(menuTitle, null, (s, e) =>
        {
            var item = s as ToolStripItem;
            if (!SettingsHelper.IsAutoStart)
            {
                SettingsHelper.IsAutoStart = true;
                SettingsHelper.WriteSettings();
                ((ToolStripMenuItem)_menu.Items[0]).Checked = true;
            }
            else
            {
                SettingsHelper.IsAutoStart = false;
                SettingsHelper.WriteSettings();
                ((ToolStripMenuItem)_menu.Items[0]).Checked = false;
            }
        });

        _menu.Items.Add("Выход", null, (s, e) =>
        {
            Application.Exit();
        });

        if (SettingsHelper.IsAutoStart)
            ((ToolStripMenuItem)_menu.Items[0]).Checked = true;

        _notifyIcon.ContextMenuStrip = _menu;

        _notifyIcon.MouseClick += NotifyIcon_MouseClick;

        Application.ApplicationExit += (s, e) =>
        {
            _notifyIcon.Visible = false;
            _keyboardHookManager.Stop();
            _notifyIcon.Dispose();
        };

        _keyboardHookManager.Start();

        WindowFocusTracker.WindowFocusChanged += WindowFocusTracker_WindowFocusChanged;
        WindowFocusTracker.StartTracking();

        _currentWindow = WindowHelper.GetForegroundWindow();
        GetWindowTitle(_currentWindow);
        if (_foregroundTitle.ToLower() == "program manager")
            _currentWindow = IntPtr.Zero;

        Application.Run();
    }

    private static void GetWindowTitle(IntPtr hWnd)
    {
        _windowName = new(256);
        _ = WindowHelper.GetWindowText(hWnd, _windowName, _windowName.Capacity);
        _foregroundTitle = _windowName.ToString();
    }

    private static void WindowFocusTracker_WindowFocusChanged(object? sender, IntPtr hWnd)
    {
        GetWindowTitle(hWnd);

        switch (_foregroundTitle.ToLower())
        {
            case "поиск":
            case "search":
            case "центр уведомлений":
            case "action center":
            return;

            case "program manager":
                _currentWindow = IntPtr.Zero;
            return;
        }

        if (_foregroundTitle == "")
        {
            int id;
            WindowFocusTracker.GetWindowThreadProcessId(hWnd, out id);
            var proc = Process.GetProcessById(id);

            switch (proc.ProcessName)
            {
                case "explorer":
                    if (proc.MainWindowHandle == hWnd)
                        return;
                    _currentWindow = WindowHelper.GetForegroundWindow();
                return;
                default:
                    _currentWindow = WindowHelper.GetForegroundWindow();
                    return;

            }
        }

        _currentWindow = WindowHelper.GetForegroundWindow();
    }

    private static void NotifyIcon_MouseClick(object? sender, MouseEventArgs e)
    {
        switch (e.Button)
        {
            case MouseButtons.Left:
                GoCenter(_currentWindow);
                break;
            case MouseButtons.Middle:
                Application.Exit();
                break;
        }
    }

    private static void GoCenter(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
            return;

        GetWindowTitle(hwnd);

        switch (_foregroundTitle.ToLower())
        {
            case "поиск":
            case "search":
            case "центр уведомлений":
            case "action center":
                return;

            case "program manager":
                _currentWindow = IntPtr.Zero;
            return;
        }

        WindowHelper.CenterWindow(hwnd);
    }
}