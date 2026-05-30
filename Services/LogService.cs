using ClasslandAttendance.Models;

namespace ClasslandAttendance.Services;

public class LogService
{
    private static readonly string LogDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ClasslandAttendance", "Logs");

    // 当前会话日志文件路径
    private string? _currentFilePath;

    // 用于跟踪今日的签到次数
    private static int _todaySessionCount = 0;
    private static DateTime _lastSessionDate = DateTime.MinValue;

    // 线程安全锁
    private readonly object _writeLock = new();

    /// <summary>
    /// 获取今日的签到会话序号
    /// </summary>
    private int GetTodaySessionCount()
    {
        var today = DateTime.Today;
        if (_lastSessionDate != today)
        {
            // 新的一天，重置计数
            _todaySessionCount = 0;
            _lastSessionDate = today;
        }
        _todaySessionCount++;
        return _todaySessionCount;
    }

    /// <summary>
    /// 获取当前时段（上午/下午）的会话序号
    /// </summary>
    private int GetPeriodSessionCount(string period)
    {
        var logFiles = Directory.Exists(LogDir)
            ? Directory.GetFiles(LogDir, $"*_{period}*.csv")
            : Array.Empty<string>();

        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var todayPeriodFiles = logFiles
            .Where(f => Path.GetFileName(f).StartsWith(today))
            .Count();

        return todayPeriodFiles + 1;
    }

    private string GetOrCreateCurrentFilePath()
    {
        if (_currentFilePath != null && File.Exists(_currentFilePath))
            return _currentFilePath;

        try
        {
            Directory.CreateDirectory(LogDir);
            var now = DateTime.Now;
            var datePart = now.ToString("yyyy-MM-dd");
            var period = now.Hour < 12 ? "上午" : "下午";
            var timeStr = $"{now.Hour}时{now.Minute}分{now.Second}秒";
            var todaySessionNum = GetTodaySessionCount();
            var periodSessionNum = GetPeriodSessionCount(period);

            var fileName = $"{datePart}_{period}_{timeStr}_今日第{todaySessionNum}次签到_{period}第{periodSessionNum}次签到.csv";
            _currentFilePath = Path.Combine(LogDir, fileName);
            return _currentFilePath;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"创建日志文件失败: {ex.Message}");
            throw;
        }
    }

    public void ResetSession()
    {
        _currentFilePath = null;
    }

    public void Append(AttendanceLog log)
    {
        lock (_writeLock)
        {
            try
            {
                var path = GetOrCreateCurrentFilePath();
                File.AppendAllLines(path, new[] { log.ToCsvLine() });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"写入日志失败: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// 在日志文件末尾追加未签到人员名单（CSV格式）
    /// </summary>
    public void AppendNotCheckedInList(List<string> notCheckedInNames)
    {
        lock (_writeLock)
        {
            try
            {
                var path = GetOrCreateCurrentFilePath();
                var lines = new List<string>();

                // 为每个未签到人员添加一条记录，保持CSV格式
                foreach (var name in notCheckedInNames)
                {
                    var log = new AttendanceLog
                    {
                        Operation = "未签到",
                        Name = name,
                        Time = DateTime.Now.TimeOfDay,
                        Status = false
                    };
                    lines.Add(log.ToCsvLine());
                }

                File.AppendAllLines(path, lines);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"写入未签到名单失败: {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>读取某天所有日志</summary>
    public List<(string FileName, List<AttendanceLog> Logs)> ReadByDate(DateTime date)
    {
        try
        {
            Directory.CreateDirectory(LogDir);
            var datePart = date.ToString("yyyy-MM-dd");
            var result = new List<(string, List<AttendanceLog>)>();

            var dayFiles = Directory.GetFiles(LogDir, $"{datePart}_*.csv")
                .OrderBy(f => f);

            foreach (var file in dayFiles)
            {
                var logs = File.ReadAllLines(file)
                    .Select(AttendanceLog.FromCsvLine)
                    .Where(l => l != null)
                    .Cast<AttendanceLog>()
                    .ToList();
                result.Add((Path.GetFileName(file), logs));
            }
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"读取日期日志失败: {ex.Message}");
            return new List<(string, List<AttendanceLog>)>();
        }
    }

    /// <summary>读取目录下所有日志文件</summary>
    public Dictionary<string, List<AttendanceLog>> ReadAll()
    {
        try
        {
            Directory.CreateDirectory(LogDir);
            var result = new Dictionary<string, List<AttendanceLog>>();
            foreach (var file in Directory.GetFiles(LogDir, "*.csv").OrderBy(f => f))
            {
                var logs = File.ReadAllLines(file)
                    .Select(AttendanceLog.FromCsvLine)
                    .Where(l => l != null)
                    .Cast<AttendanceLog>()
                    .ToList();
                result[Path.GetFileName(file)] = logs;
            }
            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"读取所有日志失败: {ex.Message}");
            return new Dictionary<string, List<AttendanceLog>>();
        }
    }

    /// <summary>返回所有日志文件名列表</summary>
    public List<string> GetAllLogFileNames()
    {
        try
        {
            Directory.CreateDirectory(LogDir);
            return Directory.GetFiles(LogDir, "*.csv")
                .Select(Path.GetFileName)
                .Where(f => f != null)
                .Cast<string>()
                .OrderByDescending(f => f)
                .ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"获取日志文件列表失败: {ex.Message}");
            return new List<string>();
        }
    }

    /// <summary>读取指定文件的日志</summary>
    public List<AttendanceLog> ReadFile(string fileName)
    {
        try
        {
            var path = Path.Combine(LogDir, fileName);
            if (!File.Exists(path)) return new();
            return File.ReadAllLines(path)
                .Select(AttendanceLog.FromCsvLine)
                .Where(l => l != null)
                .Cast<AttendanceLog>()
                .ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"读取日志文件失败: {ex.Message}");
            return new List<AttendanceLog>();
        }
    }

    /// <summary>读取指定日期范围内的日志</summary>
    public List<AttendanceLog> ReadByDateRange(DateTime startDate, DateTime endDate)
    {
        try
        {
            var allLogs = new List<AttendanceLog>();
            var currentDate = startDate.Date;
            var end = endDate.Date;

            while (currentDate <= end)
            {
                var dayLogs = ReadByDate(currentDate);
                foreach (var (_, logs) in dayLogs)
                {
                    allLogs.AddRange(logs);
                }
                currentDate = currentDate.AddDays(1);
            }

            return allLogs;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"读取日期范围日志失败: {ex.Message}");
            return new List<AttendanceLog>();
        }
    }
}
