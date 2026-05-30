using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using ClasslandAttendance.ViewModels;
using System;

namespace ClasslandAttendance.Views;

public class CountToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => $"共 {value} 条记录";
    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => !string.IsNullOrEmpty(value as string) ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public class EqualsToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null || parameter == null) return Visibility.Collapsed;
        return value.ToString() == parameter.ToString() ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotImplementedException();
}

public sealed partial class SettingsPage : Page
{
    public SettingsViewModel ViewModel => App.SettingsViewModel;

    public SettingsPage()
    {
        this.InitializeComponent();
    }

    private void RefreshLogFiles_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.LoadLogFileNames();
    }

    private void RemoveLateQueryFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string fileName)
        {
            ViewModel.LateQuerySelectedFiles.Remove(fileName);
        }
    }

    private void RemoveEarliestQueryFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string fileName)
        {
            ViewModel.EarliestQuerySelectedFiles.Remove(fileName);
        }
    }
}
