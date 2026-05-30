using ClasslandAttendance.Models;

namespace ClasslandAttendance.Services;

public class AttendanceService
{
    private readonly LogService _logService;
    private bool _isStopped; // 是否已停止签到

    public AttendanceService(LogService logService)
    {
        _logService = logService;
    }

    public bool IsStopped => _isStopped;

    public void CheckIn(Person person)
    {
        if (person.IsCheckedIn) return;
        person.IsCheckedIn = true;
        person.CheckInTime = DateTime.Now;

        _logService.Append(new AttendanceLog
        {
            Operation = "签到",
            Name = person.Name,
            Time = person.CheckInTime.Value.TimeOfDay,
            Status = !_isStopped
        });
    }

    public void CancelCheckIn(Person person)
    {
        if (!person.IsCheckedIn) return;
        var time = DateTime.Now.TimeOfDay;
        person.IsCheckedIn = false;
        person.CheckInTime = null;

        _logService.Append(new AttendanceLog
        {
            Operation = "取消签到",
            Name = person.Name,
            Time = time,
            Status = true
        });
    }

    public void StopSession(List<string>? notCheckedInNames = null)
    {
        _isStopped = true;
        
        // 如果有未签到名单，写入日志文件末尾
        if (notCheckedInNames != null && notCheckedInNames.Count > 0)
        {
            _logService.AppendNotCheckedInList(notCheckedInNames);
        }
    }

    public void StartNewSession()
    {
        _isStopped = false;
        _logService.ResetSession();
    }
}
