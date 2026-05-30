using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClasslandAttendance.Models;
using ClasslandAttendance.Services;

namespace ClasslandAttendance.ViewModels;

public partial class AttendanceViewModel : ObservableObject
{
    private readonly AttendanceService _attendanceService;
    private readonly RosterService _rosterService;

    public ObservableCollection<Person> CheckedInList { get; } = new();
    public ObservableCollection<Person> NotCheckedInList { get; } = new();

    [ObservableProperty]
    private string _countText = "0 / 0";

    [ObservableProperty]
    private bool _isStopped;

    [ObservableProperty]
    private string _stopButtonText = "停止签到";

    public AttendanceViewModel(AttendanceService attendanceService, RosterService rosterService)
    {
        _attendanceService = attendanceService;
        _rosterService = rosterService;
        LoadRoster();
    }

    public void LoadRoster()
    {
        CheckedInList.Clear();
        NotCheckedInList.Clear();

        var names = _rosterService.Load();
        for (int i = 0; i < names.Count; i++)
        {
            NotCheckedInList.Add(new Person { Name = names[i], RosterIndex = i });
        }
        RefreshCount();
    }

    [RelayCommand]
    public void CheckIn(Person person)
    {
        if (person == null) return;
        _attendanceService.CheckIn(person);

        // 从未签到移到已签到
        NotCheckedInList.Remove(person);
        // 已签到按签到时间升序插入
        int insertIndex = CheckedInList
            .TakeWhile(p => p.CheckInTime <= person.CheckInTime)
            .Count();
        CheckedInList.Insert(insertIndex, person);
        RefreshCount();
    }

    [RelayCommand]
    public void CancelCheckIn(Person person)
    {
        if (person == null) return;
        _attendanceService.CancelCheckIn(person);

        CheckedInList.Remove(person);
        // 未签到按名单顺序插入
        int insertIndex = NotCheckedInList
            .TakeWhile(p => p.RosterIndex < person.RosterIndex)
            .Count();
        NotCheckedInList.Insert(insertIndex, person);
        RefreshCount();
    }

    [RelayCommand]
    public void ToggleStop()
    {
        if (!IsStopped)
        {
            // 获取未签到人员名单
            var notCheckedInNames = NotCheckedInList.Select(p => p.Name).ToList();
            
            // 停止签到并写入未签到名单
            _attendanceService.StopSession(notCheckedInNames);
            IsStopped = true;
            StopButtonText = "重新签到";
        }
        else
        {
            // 重新开始新的签到会话
            _attendanceService.StartNewSession();
            IsStopped = false;
            StopButtonText = "停止签到";
            // 重新加载名单，清空本次签到
            LoadRoster();
        }
    }

    private void RefreshCount()
    {
        int total = CheckedInList.Count + NotCheckedInList.Count;
        CountText = $"{CheckedInList.Count} / {total}";
    }
}
