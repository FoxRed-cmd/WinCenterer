using System.Drawing;
using System;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using Timer = System.Windows.Forms.Timer;

public class WindowHelper
{
    private const int SWP_NOSIZE = 0x0001;
    private const int HWND_TOP = 0;
    private const int SWP_NOZORDER = 0x0004;
    private const int SWP_ASYNCWINDOWPOS = 0x4000;

    public static Timer _timer;

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    public static void CenterWindow(IntPtr hwnd)
    {
        RECT rect;
        GetWindowRect(hwnd, out rect);

        Size res = GetScreenResolution();

        if (res.Width == rect.Right && res.Height == rect.Bottom)
            return;

        int windowWidth = rect.Right - rect.Left;
        int windowHeight = rect.Bottom - rect.Top;

        int x = (res.Width - windowWidth) / 2;
        int y = (res.Height - 40 - windowHeight) / 2;

        // Set up the animation parameters
        const int animationDuration = 120; // milliseconds
        int animationInterval = 10; // milliseconds
        int animationSteps = animationDuration / animationInterval;
        int currentStep = 0;
        int startX = rect.Left;
        int startY = rect.Top;

        // Calculate the distance to move in each step
        int deltaX = (x - startX) / animationSteps;
        int deltaY = (y - startY) / animationSteps;

        // Create a timer to update the window position
        _timer = new Timer();
        _timer.Interval = animationInterval;
        _timer.Tick += (s, e) =>
        {
            currentStep++;

            int newX = startX + (deltaX * currentStep);
            int newY = startY + (deltaY * currentStep);

            // Update the window position
            SetWindowPos(hwnd, HWND_TOP, newX, newY, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_ASYNCWINDOWPOS);

            // Stop the animation when the desired position is reached
            if (currentStep >= animationSteps)
            {
                _timer.Stop();
                _timer.Dispose();
                SetWindowPos(hwnd, HWND_TOP, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_ASYNCWINDOWPOS);
            }
        };

        // Start the timer to begin the animation
        _timer.Start();

        SetForegroundWindow(hwnd);
    }

    private static Size GetScreenResolution()
    {
        var scope = new ManagementScope();
        scope.Connect();

        var query = new ObjectQuery("SELECT * FROM Win32_VideoController");

        using (var searcher = new ManagementObjectSearcher(scope, query))
        {
            var results = searcher.Get();
            foreach (var result in results)
            {
                return new Size()
                {
                    Height = Convert.ToInt32(result.GetPropertyValue("CurrentVerticalResolution")),
                    Width = Convert.ToInt32(result.GetPropertyValue("CurrentHorizontalResolution")),
                };
            }
        }

        return new Size();
    }
}