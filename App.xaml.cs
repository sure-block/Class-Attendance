using Microsoft.UI.Xaml;
using WinRT.Interop;
using ClasslandAttendance.Services;
using ClasslandAttendance.ViewModels;

namespace ClasslandAttendance;

public partial class App : Application
{
    public static LogService LogService { get; } = new();
    public static RosterService RosterService { get; } = new();
    public static AttendanceService AttendanceService { get; } = new(LogService);
    public static AttendanceViewModel AttendanceViewModel { get; } = new(AttendanceService, RosterService);
    public static SettingsViewModel SettingsViewModel { get; } = new(RosterService, LogService);

    private static Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }

    /// <summary>
    /// 获取主窗口句柄，用于文件选择器等 Win32 API 调用
    /// </summary>
    public static IntPtr GetMainWindowHandle()
    {
        if (_window == null)
            throw new InvalidOperationException("窗口尚未创建");

        return WindowNative.GetWindowHandle(_window);
    }

    /// <summary>
    /// 获取主窗口实例，用于 ContentDialog 等需要 XamlRoot 的场景
    /// </summary>
    public static Window? GetMainWindow()
    {
        return _window;
    }
}
