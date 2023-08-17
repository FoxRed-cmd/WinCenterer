using IWshRuntimeLibrary;
using Microsoft.Win32;
using System.Reflection;
using WinCenterer.Properties;

internal static class SettingsHelper
{

    public static bool IsAutoStart { get; set; } = false;
    public static bool IsLightTheme
    {
        get => _isLightTheme;
        set
        {
            if (_isLightTheme == value)
                return;

            _isLightTheme = value;

            if (IsLightTheme)
                Program._notifyIcon.Icon = Resources.appiconB;
            else
                Program._notifyIcon.Icon = Resources.appiconW;
        }
    }

    private static string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs\\Startup", "WinCenterer.lnk");
    private static RegistryKey _wrReg;
    private static bool _isLightTheme;

    public static void ReadSettings()
    {
        if (System.IO.File.Exists(_path))
            IsAutoStart = true;
        else
            IsAutoStart = false;
    }

    public static void WriteSettings()
    {
        if (IsAutoStart && !System.IO.File.Exists(_path))
        {
            Create(_path, Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe"));
            IsAutoStart = true;
        }
        else if (System.IO.File.Exists(_path))
        {
            System.IO.File.Delete(_path);
            IsAutoStart = false;
        }
    }

    public static void CheckThemeChange()
    {
        using (_wrReg = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
        {
            if (_wrReg.GetValue("SystemUsesLightTheme") != null)
                IsLightTheme = Convert.ToBoolean(_wrReg.GetValue("SystemUsesLightTheme"));
        }
    }

    private static void Create(string ShortcutPath, string TargetPath)
    {
        WshShell wshShell = new WshShell();
        IWshShortcut Shortcut = (IWshShortcut)wshShell.
            CreateShortcut(ShortcutPath);

        Shortcut.TargetPath = TargetPath;
        TargetPath = TargetPath.Remove(TargetPath.LastIndexOf("\\"));
        Shortcut.WorkingDirectory = TargetPath;

        Shortcut.Save();
    }
}