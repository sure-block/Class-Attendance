using ClasslandAttendance.Models;

namespace ClasslandAttendance.Services;

public class StatisticsService
{
    private readonly LogService _logService;
    private readonly RosterService _rosterService;

    public StatisticsService(LogService logService, RosterService rosterService)
    {
        _logService = logService;
        _rosterService = rosterService;
    }

    /// <summary>
    /// 查询指定日期范围内的迟到记录（所有人，包括未签到）
    /// </summary>
    public List<LateRecord> QueryLateRecordsByDateRange(DateTime startDate, DateTime endDate, TimeSpan lateThreshold)
    {
        var records = new List<LateRecord>();
        var roster = _rosterService.Load(); // 获取名单中的所有人员
        var currentDate = startDate.Date;
        var end = endDate.Date;

        while (currentDate <= end)
        {
            var dayLogs = _logService.ReadByDate(currentDate);
            
            // 合并当天所有日志文件的签到记录
            var allLogsForDay = new Dictionary<string, AttendanceLog>();
            foreach (var (fileName, logs) in dayLogs)
            {
                var checkInLogs = logs.Where(l => l.Operation == "签到" && l.Status == true);
                foreach (var log in checkInLogs)
                {
                    // 如果同一个人有多次签到，取最早的
                    if (!allLogsForDay.ContainsKey(log.Name) || log.Time < allLogsForDay[log.Name].Time)
                    {
                        allLogsForDay[log.Name] = log;
                    }
                }
            }

            // 检查名单中的每个人
            foreach (var name in roster)
            {
                if (allLogsForDay.ContainsKey(name))
                {
                    // 已签到，检查是否迟到
                    var log = allLogsForDay[name];
                    if (log.Time > lateThreshold)
                    {
                        // 找到该签到的日志文件
                        string fileName = "未知";
                        foreach (var (file, logs) in dayLogs)
                        {
                            if (logs.Any(l => l.Name == name && l.Operation == "签到" && l.Status == true))
                            {
                                fileName = file;
                                break;
                            }
                        }

                        records.Add(new LateRecord
                        {
                            Name = name,
                            Time = log.Time,
                            Date = currentDate,
                            FileName = fileName
                        });
                    }
                }
                else
                {
                    // 未签到，也视为迟到
                    records.Add(new LateRecord
                    {
                        Name = name,
                        Time = TimeSpan.Zero, // 未签到用 00:00:00 表示
                        Date = currentDate,
                        FileName = "未签到"
                    });
                }
            }

            currentDate = currentDate.AddDays(1);
        }

        return records.OrderBy(r => r.Date).ThenBy(r => r.Name).ToList();
    }

    /// <summary>
    /// 查询指定日期范围内某人的迟到记录
    /// </summary>
    public List<LateRecord> QueryLateRecordsByName(string name, DateTime startDate, DateTime endDate, TimeSpan lateThreshold)
    {
        var allRecords = QueryLateRecordsByDateRange(startDate, endDate, lateThreshold);
        return allRecords.Where(r => r.Name == name).ToList();
    }

    /// <summary>
    /// 查询指定日志文件列表中的迟到记录（所有人，包括未签到）
    /// </summary>
    public List<LateRecord> QueryLateRecordsByFiles(List<string> fileNames, TimeSpan lateThreshold)
    {
        var records = new List<LateRecord>();
        var roster = _rosterService.Load(); // 获取名单中的所有人员

        foreach (var fileName in fileNames)
        {
            var logs = _logService.ReadFile(fileName);
            // 从文件名提取日期
            var date = ExtractDateFromFileName(fileName);

            // 合并该文件的签到记录
            var checkInMap = new Dictionary<string, AttendanceLog>();
            var checkInLogs = logs.Where(l => l.Operation == "签到" && l.Status == true);
            foreach (var log in checkInLogs)
            {
                if (!checkInMap.ContainsKey(log.Name) || log.Time < checkInMap[log.Name].Time)
                {
                    checkInMap[log.Name] = log;
                }
            }

            // 检查名单中的每个人
            foreach (var name in roster)
            {
                if (checkInMap.ContainsKey(name))
                {
                    // 已签到，检查是否迟到
                    var log = checkInMap[name];
                    if (log.Time > lateThreshold)
                    {
                        records.Add(new LateRecord
                        {
                            Name = name,
                            Time = log.Time,
                            Date = date,
                            FileName = fileName
                        });
                    }
                }
                else
                {
                    // 未签到，也视为迟到
                    records.Add(new LateRecord
                    {
                        Name = name,
                        Time = TimeSpan.Zero,
                        Date = date,
                        FileName = fileName
                    });
                }
            }
        }

        return records.OrderBy(r => r.Date).ThenBy(r => r.Name).ToList();
    }

    /// <summary>
    /// 查询指定日志文件列表中某人的迟到记录
    /// </summary>
    public List<LateRecord> QueryLateRecordsByNameInFiles(string name, List<string> fileNames, TimeSpan lateThreshold)
    {
        var allRecords = QueryLateRecordsByFiles(fileNames, lateThreshold);
        return allRecords.Where(r => r.Name == name).ToList();
    }

    /// <summary>
    /// 按姓名分组统计迟到次数（用于汇总显示）
    /// </summary>
    public Dictionary<string, List<LateRecord>> GroupLateRecordsByName(List<LateRecord> records)
    {
        var groups = new Dictionary<string, List<LateRecord>>();
        foreach (var record in records)
        {
            if (!groups.ContainsKey(record.Name))
                groups[record.Name] = new List<LateRecord>();
            groups[record.Name].Add(record);
        }
        return groups;
    }

    /// <summary>
    /// 查询指定日期范围内的最早签到记录（每人）
    /// </summary>
    public List<EarliestCheckIn> QueryEarliestCheckInsByDateRange(DateTime startDate, DateTime endDate, int? topCount = null)
    {
        var earliestMap = new Dictionary<string, EarliestCheckIn>();
        var currentDate = startDate.Date;
        var end = endDate.Date;

        while (currentDate <= end)
        {
            var dayLogs = _logService.ReadByDate(currentDate);
            foreach (var (fileName, logs) in dayLogs)
            {
                var checkInLogs = logs.Where(l => l.Operation == "签到" && l.Status == true).ToList();

                // 如果设置了人数限制，只取前N个签到的人
                if (topCount.HasValue && topCount.Value > 0)
                {
                    checkInLogs = checkInLogs.Take(topCount.Value).ToList();
                }

                foreach (var log in checkInLogs)
                {
                    if (!earliestMap.ContainsKey(log.Name))
                    {
                        earliestMap[log.Name] = new EarliestCheckIn
                        {
                            Name = log.Name,
                            Time = log.Time,
                            Date = currentDate,
                            FileName = fileName,
                            Count = 1
                        };
                    }
                    else
                    {
                        var existing = earliestMap[log.Name];
                        if (log.Time < existing.Time ||
                            (log.Time == existing.Time && currentDate < existing.Date))
                        {
                            earliestMap[log.Name] = new EarliestCheckIn
                            {
                                Name = log.Name,
                                Time = log.Time,
                                Date = currentDate,
                                FileName = fileName,
                                Count = existing.Count + 1
                            };
                        }
                        else
                        {
                            existing.Count++;
                        }
                    }
                }
            }
            currentDate = currentDate.AddDays(1);
        }

        return earliestMap.Values.OrderBy(e => e.Time).ThenBy(e => e.Date).ToList();
    }

    /// <summary>
    /// 查询指定日志文件列表中的最早签到记录
    /// </summary>
    public List<EarliestCheckIn> QueryEarliestCheckInsByFiles(List<string> fileNames, int? topCount = null)
    {
        var earliestMap = new Dictionary<string, EarliestCheckIn>();

        foreach (var fileName in fileNames)
        {
            var logs = _logService.ReadFile(fileName);
            var date = ExtractDateFromFileName(fileName);

            var checkInLogs = logs.Where(l => l.Operation == "签到" && l.Status == true).ToList();

            // 如果设置了人数限制，只取前N个签到的人
            if (topCount.HasValue && topCount.Value > 0)
            {
                checkInLogs = checkInLogs.Take(topCount.Value).ToList();
            }

            foreach (var log in checkInLogs)
            {
                if (!earliestMap.ContainsKey(log.Name))
                {
                    earliestMap[log.Name] = new EarliestCheckIn
                    {
                        Name = log.Name,
                        Time = log.Time,
                        Date = date,
                        FileName = fileName,
                        Count = 1
                    };
                }
                else
                {
                    var existing = earliestMap[log.Name];
                    if (log.Time < existing.Time)
                    {
                        earliestMap[log.Name] = new EarliestCheckIn
                        {
                            Name = log.Name,
                            Time = log.Time,
                            Date = date,
                            FileName = fileName,
                            Count = existing.Count + 1
                        };
                    }
                    else
                    {
                        existing.Count++;
                    }
                }
            }
        }

        return earliestMap.Values.OrderBy(e => e.Time).ToList();
    }

    /// <summary>
    /// 从文件名提取日期
    /// </summary>
    private DateTime ExtractDateFromFileName(string fileName)
    {
        try
        {
            // 文件名格式：2026-05-23_下午_21时58分6秒_今日第x次签到_下午第x次签到.csv
            var parts = fileName.Split('_');
            if (parts.Length > 0 && DateTime.TryParse(parts[0], out var date))
            {
                return date;
            }
        }
        catch
        {
            // 忽略解析错误
        }
        return DateTime.MinValue;
    }
}
