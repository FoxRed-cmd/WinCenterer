using Microsoft.Win32;
using System.IO;
using System;
using System.Reflection;
using WindowsCenterer;
using Microsoft.Win32.TaskScheduler;
using WindowsCenterer.Properties;

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
        using (TaskService ts = new TaskService())
        {
            if (ts.FindTask("WinCenterer") != null)
                IsAutoStart = true;
            else
                IsAutoStart = false;
        }
    }

    public static void WriteSettings()
    {
        using (TaskService ts = new TaskService())
        {
            if (IsAutoStart && ts.FindTask("WinCenterer") == null)
            {
                TaskDefinition td = ts.NewTask();
                td.Triggers.Add(new LogonTrigger() { UserId = $"{Environment.UserDomainName}\\{Environment.UserName}", Enabled = true });
                td.Actions.Add(new ExecAction(Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe")));
                td.Principal.RunLevel = TaskRunLevel.Highest;
                td.Settings.DisallowStartIfOnBatteries = false;
                ts.RootFolder.RegisterTaskDefinition(@"WinCenterer", td);

                IsAutoStart = true;
            }
            else
            {
                ts.RootFolder.DeleteTask("WinCenterer");
                IsAutoStart = false;
            }
        }
    }

    public static void CheckThemeChange()
    {
        using (_wrReg = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
        {
            if (_wrReg is null)
                return;

            if (_wrReg.GetValue("SystemUsesLightTheme") != null)
                IsLightTheme = Convert.ToBoolean(_wrReg.GetValue("SystemUsesLightTheme"));
        }
    }
}