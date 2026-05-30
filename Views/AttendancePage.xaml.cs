using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using ClasslandAttendance.Models;
using ClasslandAttendance.ViewModels;
using System;

namespace ClasslandAttendance.Views;

/// <summary>将 DateTime? 转为时间字符串</summary>
public class DateTimeToTimeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateTime dt) return dt.ToString("HH:mm:ss");
        return string.Empty;
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

/// <summary>将 bool 转为 Visibility</summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is true ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public sealed partial class AttendancePage : Page
{
    public AttendanceViewModel ViewModel => App.AttendanceViewModel;

    public AttendancePage()
    {
        this.InitializeComponent();
    }

    private void CheckIn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Person person)
            ViewModel.CheckIn(person);
    }

    private void CancelCheckIn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Person person)
            ViewModel.CancelCheckIn(person);
    }

    private async void StopButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.IsStopped)
        {
            // 停止签到前二次确认
            var dialog = new ContentDialog
            {
                Title = "确认停止",
                Content = "确定要停止签到吗？停止后的签到记录将被标记为异常。",
                PrimaryButtonText = "确定停止",
                CloseButtonText = "取消"
            };
            dialog.XamlRoot = this.XamlRoot;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                ViewModel.ToggleStopCommand.Execute(null);
            }
        }
        else
        {
            // 重新签到前二次确认
            var dialog = new ContentDialog
            {
                Title = "确认重新签到",
                Content = "确定要重新开始签到吗？这将清空当前所有签到记录。",
                PrimaryButtonText = "确定重置",
                CloseButtonText = "取消"
            };
            dialog.XamlRoot = this.XamlRoot;

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                ViewModel.ToggleStopCommand.Execute(null);
            }
        }
    }
}
