using CommunityToolkit.Mvvm.ComponentModel;

namespace ClasslandAttendance.Models;

public partial class Person : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isCheckedIn;

    [ObservableProperty]
    private DateTime? _checkInTime;

    // 在名单中的原始顺序，用于未签到列表排序
    public int RosterIndex { get; set; }
}
