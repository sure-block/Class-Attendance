namespace ClasslandAttendance.Models;

/// <summary>
/// 迟到记录信息
/// </summary>
public class LateRecord
{
    public string Name { get; set; } = string.Empty;
    public TimeSpan Time { get; set; }
    public DateTime Date { get; set; }
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 格式化日期显示（yyyy-MM-dd）
    /// </summary>
    public string DateDisplay => Date.ToString("yyyy-MM-dd");
}

/// <summary>
/// 最早签到记录
/// </summary>
public class EarliestCheckIn
{
    public string Name { get; set; } = string.Empty;
    public TimeSpan Time { get; set; }
    public DateTime Date { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int Count { get; set; } // 最早签到次数

    /// <summary>
    /// 格式化日期显示（yyyy-MM-dd）
    /// </summary>
    public string DateDisplay => Date.ToString("yyyy-MM-dd");
}
