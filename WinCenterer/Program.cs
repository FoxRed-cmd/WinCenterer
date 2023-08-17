using System.Text;
using WinCenterer.Properties;

public class Program
{
    static IntPtr _currentWindow;
    static IntPtr _currentWindowHotKeys;
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
        _keyboardHookManager.KeyDown += (s, e) =>
        {
           if (e.KeyCode == Keys.LControlKey)
                _countCtrlPressed++;
            if (_countCtrlPressed == 3)
            {
                _countCtrlPressed = 0;
                GoCenter(_currentWindowHotKeys);
            }   
        };

        _notifyIcon = new()
        {
            Visible = true,
            Icon = Resources.appiconW,
            Text = "Нажмите Ctrl 3 раза, чтобы центрировать",
        };

        SettingsHelper.ReadSettings();

        System.Windows.Forms.Timer timer = new()
        {
            Interval = 1500,
            Enabled = true,
        };
        timer.Tick += Timer_Tick;

        System.Windows.Forms.Timer timerHot = new()
        {
            Interval = 100,
            Enabled = true,
        };
        timerHot.Tick += TimerHot_Tick;

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

        Application.Run();
    }

    private static void TimerHot_Tick(object? sender, EventArgs e)
    {
        if (_currentWindowHotKeys != WindowHelper.GetForegroundWindow())
            _currentWindowHotKeys = WindowHelper.GetForegroundWindow();
    }

    private static void Timer_Tick(object sender, EventArgs e)
    {
        if (_currentWindow != WindowHelper.GetForegroundWindow())
            _currentWindow = WindowHelper.GetForegroundWindow();

        SettingsHelper.CheckThemeChange();
        _countCtrlPressed = 0;
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

        _windowName = new(256);
        _ = WindowHelper.GetWindowText(hwnd, _windowName, _windowName.Capacity);
        _foregroundTitle = _windowName.ToString();

        switch (_foregroundTitle.ToLower())
        {
            case "": 
            case "поиск":
            case "search":
            case "центр уведомлений":
            case "action center":
                return;
        }

        WindowHelper.CenterWindow(hwnd);
    }
}