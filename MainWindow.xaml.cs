using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ClasslandAttendance.Views;
using WinRT.Interop;

namespace ClasslandAttendance;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // 隐藏系统原生标题栏，使用自定义标题栏
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(TitleBarGrid);

        // 禁用系统默认的关闭按钮（X按钮）
        var windowHandle = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(windowHandle);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = false;
        }

        // 监听 Frame 导航事件，控制返回按钮显示
        ContentFrame.Navigated += ContentFrame_Navigated;

        // 初始导航到签到页
        ContentFrame.Navigate(typeof(AttendancePage));
    }

    private void ContentFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        // 如果在设置页面，显示返回按钮
        BackButton.Visibility = ContentFrame.CurrentSourcePageType == typeof(SettingsPage) 
            ? Visibility.Visible 
            : Visibility.Collapsed;
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        // 从设置页面返回签到页面
        if (ContentFrame.CanGoBack)
        {
            ContentFrame.GoBack();
        }
        else
        {
            ContentFrame.Navigate(typeof(AttendancePage));
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (ContentFrame.CurrentSourcePageType == typeof(SettingsPage))
            ContentFrame.Navigate(typeof(AttendancePage));
        else
            ContentFrame.Navigate(typeof(SettingsPage));
    }
}
