using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClasslandAttendance.Models;
using ClasslandAttendance.Services;
using Microsoft.UI.Xaml;

namespace ClasslandAttendance.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly RosterService _rosterService;
    private readonly LogService _logService;
    private readonly StatisticsService _statisticsService;

    // ── 名单管理 ──────────────────────────────────────────
    public ObservableCollection<string> RosterItems { get; } = new();

    [ObservableProperty] private string _newPersonName = string.Empty;
    [ObservableProperty] private string? _selectedPerson;

    // ── 日志查看 ──────────────────────────────────────────
    public ObservableCollection<string> LogFileNames { get; } = new();
    [ObservableProperty] private string? _selectedLogFile;

    public ObservableCollection<AttendanceLog> DisplayedLogs { get; } = new();

    // ── 筛选条件 ──────────────────────────────────────────
    [ObservableProperty] private string _filterName = string.Empty;
    [ObservableProperty] private string _filterOperation = string.Empty; // 空=全部
    [ObservableProperty] private DateTimeOffset? _filterDateStart;
    [ObservableProperty] private DateTimeOffset? _filterDateEnd;

    // ── 统计结果 ──────────────────────────────────────────
    [ObservableProperty] private string _statsName = string.Empty;
    [ObservableProperty] private string _statsResult = string.Empty;
    [ObservableProperty] private string _lateThreshold = "09:00:00";

    // ── 迟到查询 ──────────────────────────────────────────
    public ObservableCollection<LateRecord> LateRecords { get; } = new();
    [ObservableProperty] private string _lateQueryName = string.Empty; // 空=查询所有人
    [ObservableProperty] private DateTimeOffset? _lateQueryDateStart;
    [ObservableProperty] private DateTimeOffset? _lateQueryDateEnd;
    [ObservableProperty] private bool _lateQueryUseDateRange = true; // true=日期范围, false=文件列表
    public ObservableCollection<string> LateQuerySelectedFiles { get; } = new(); // 选中的日志文件

    // ── 最早签到查询 ──────────────────────────────────────
    public ObservableCollection<EarliestCheckIn> EarliestCheckIns { get; } = new();
    [ObservableProperty] private string _earliestQueryTopCount = "0"; // 0=不限制
    [ObservableProperty] private DateTimeOffset? _earliestQueryDateStart;
    [ObservableProperty] private DateTimeOffset? _earliestQueryDateEnd;
    [ObservableProperty] private bool _earliestQueryUseDateRange = true; // true=日期范围, false=文件列表
    public ObservableCollection<string> EarliestQuerySelectedFiles { get; } = new(); // 选中的日志文件

    public SettingsViewModel(RosterService rosterService, LogService logService)
    {
        _rosterService = rosterService;
        _logService = logService;
        _statisticsService = new StatisticsService(logService, rosterService);
        LoadRoster();
        LoadLogFileNames();
    }

    // ── 名单操作 ──────────────────────────────────────────

    public void LoadRoster()
    {
        RosterItems.Clear();
        foreach (var name in _rosterService.Load())
            RosterItems.Add(name);
    }

    [RelayCommand]
    public void AddPerson()
    {
        var name = NewPersonName.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ShowMessage("提示", "请输入姓名");
            return;
        }

        if (name.Length > 50)
        {
            ShowMessage("错误", "姓名不能超过50个字符");
            return;
        }

        if (RosterItems.Contains(name))
        {
            ShowMessage("错误", $"姓名 \"{name}\" 已存在");
            return;
        }

        RosterItems.Add(name);
        SaveRoster();
        NewPersonName = string.Empty;
        ShowMessage("成功", $"已添加 {name}");
    }

    [RelayCommand]
    public void RemovePerson()
    {
        if (SelectedPerson == null)
        {
            ShowMessage("提示", "请先选择要删除的姓名");
            return;
        }

        RosterItems.Remove(SelectedPerson);
        var removedName = SelectedPerson;
        SelectedPerson = null;
        SaveRoster();
        ShowMessage("成功", $"已删除 {removedName}");
    }

    /// <summary>
    /// 从TXT文件导入名单
    /// </summary>
    [RelayCommand]
    public async Task ImportRoster()
    {
        try
        {
            // 打开文件选择对话框（需要在UI线程调用）
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            var hWnd = App.GetMainWindowHandle();
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);

            picker.FileTypeFilter.Add(".txt");
            var file = await picker.PickSingleFileAsync();

            if (file == null) return;

            var importedNames = _rosterService.ImportFromTxt(file.Path);

            // 合并名单，去重
            int addedCount = 0;
            foreach (var name in importedNames)
            {
                if (!RosterItems.Contains(name))
                {
                    RosterItems.Add(name);
                    addedCount++;
                }
            }

            SaveRoster();

            // 显示导入结果
            ShowMessage("导入成功", $"成功导入 {addedCount} 个姓名");
        }
        catch (FileNotFoundException)
        {
            ShowMessage("错误", "文件不存在");
        }
        catch (InvalidOperationException ex)
        {
            ShowMessage("错误", ex.Message);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"导入名单失败: {ex.Message}");
            ShowMessage("错误", $"导入失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 清空名单
    /// </summary>
    [RelayCommand]
    public void ClearRoster()
    {
        if (RosterItems.Count == 0)
        {
            ShowMessage("提示", "名单已为空");
            return;
        }

        try
        {
            RosterItems.Clear();
            _rosterService.Clear();
            ShowMessage("成功", "名单已清空");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"清空名单失败: {ex.Message}");
            ShowMessage("错误", $"清空失败: {ex.Message}");
        }
    }

    private void SaveRoster()
    {
        try
        {
            _rosterService.Save(RosterItems.ToList());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存名单失败: {ex.Message}");
            ShowMessage("错误", $"保存名单失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示消息对话框
    /// </summary>
    private async void ShowMessage(string title, string content)
    {
        try
        {
            var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "确定"
            };

            // 通过 App 获取主窗口
            var window = App.GetMainWindow();
            if (window != null && window.Content is FrameworkElement fe)
            {
                dialog.XamlRoot = fe.XamlRoot;
                await dialog.ShowAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"显示消息失败: {ex.Message}");
        }
    }

    // ── 日志操作 ──────────────────────────────────────────

    public void LoadLogFileNames()
    {
        LogFileNames.Clear();
        foreach (var f in _logService.GetAllLogFileNames())
            LogFileNames.Add(f);
    }

    [RelayCommand]
    public void LoadSelectedLog()
    {
        if (SelectedLogFile == null) return;
        var logs = _logService.ReadFile(SelectedLogFile);
        ApplyFilterToList(logs);
    }

    [RelayCommand]
    public void ApplyFilter()
    {
        List<AttendanceLog> logs;

        if (SelectedLogFile != null)
        {
            // 从选定文件读取
            logs = _logService.ReadFile(SelectedLogFile);
        }
        else if (FilterDateStart.HasValue && FilterDateEnd.HasValue)
        {
            // 使用日期范围筛选（跨文件）- 转换 DateTimeOffset 为 DateTime
            var startDate = FilterDateStart.Value.DateTime;
            var endDate = FilterDateEnd.Value.DateTime;
            logs = _logService.ReadByDateRange(startDate, endDate);
        }
        else
        {
            // 读取所有日志
            logs = _logService.ReadAll().Values.SelectMany(x => x).ToList();
        }

        ApplyFilterToList(logs);

        if (DisplayedLogs.Count == 0)
        {
            ShowMessage("提示", "未找到符合条件的记录");
        }
    }

    private void ApplyFilterToList(List<AttendanceLog> logs)
    {
        var filtered = logs.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(FilterName))
            filtered = filtered.Where(l => l.Name.Contains(FilterName.Trim()));

        if (!string.IsNullOrWhiteSpace(FilterOperation))
            filtered = filtered.Where(l => l.Operation == FilterOperation.Trim());

        // 日期范围筛选（需要结合日志文件的文件名中的日期信息）
        if (FilterDateStart.HasValue || FilterDateEnd.HasValue)
        {
            // 由于日志本身不包含日期，需要从文件名中提取日期
            // 这里假设调用者已经通过 ReadByDateRange 获取了正确范围的日志
            // 如果是跨文件查询且指定了日期范围，ReadByDateRange 已经处理了
            // 所以这里不需要额外过滤
        }

        DisplayedLogs.Clear();
        foreach (var log in filtered)
            DisplayedLogs.Add(log);
    }

    // ── 统计 ──────────────────────────────────────────────

    [RelayCommand]
    public void QueryStats()
    {
        var name = StatsName.Trim();
        if (string.IsNullOrEmpty(name))
        {
            StatsResult = "请输入姓名";
            return;
        }

        var allLogs = _logService.ReadAll().Values
            .SelectMany(x => x)
            .Where(l => l.Name == name && l.Operation == "签到")
            .ToList();

        if (allLogs.Count == 0)
        {
            StatsResult = $"未找到 {name} 的签到记录";
            return;
        }

        TimeSpan threshold = TimeSpan.TryParse(LateThreshold, out var t) ? t : TimeSpan.FromHours(9);

        int lateCount = allLogs.Count(l => l.Time > threshold);
        var earliest = allLogs.Min(l => l.Time);
        var latest = allLogs.Max(l => l.Time);
        int totalCount = allLogs.Count;

        StatsResult = $"姓名：{name}\n" +
                      $"总签到次数：{totalCount}\n" +
                      $"迟到次数（>{LateThreshold}）：{lateCount}\n" +
                      $"最早签到时间：{earliest:hh\\:mm\\:ss}\n" +
                      $"最晚签到时间：{latest:hh\\:mm\\:ss}";
    }

    // ── 迟到查询 ──────────────────────────────────────────

    [RelayCommand]
    public void QueryLateRecords()
    {
        TimeSpan threshold = TimeSpan.TryParse(LateThreshold, out var t) ? t : TimeSpan.FromHours(9);
        List<LateRecord> records;

        if (LateQueryUseDateRange)
        {
            // 使用日期范围查询
            if (!LateQueryDateStart.HasValue || !LateQueryDateEnd.HasValue)
            {
                ShowMessage("提示", "请选择日期范围");
                return;
            }

            var startDate = LateQueryDateStart.Value.DateTime;
            var endDate = LateQueryDateEnd.Value.DateTime;

            if (string.IsNullOrWhiteSpace(LateQueryName))
            {
                // 查询所有人
                records = _statisticsService.QueryLateRecordsByDateRange(startDate, endDate, threshold);
            }
            else
            {
                // 查询指定人员
                records = _statisticsService.QueryLateRecordsByName(LateQueryName.Trim(), startDate, endDate, threshold);
            }
        }
        else
        {
            // 使用文件列表查询
            if (LateQuerySelectedFiles.Count == 0)
            {
                ShowMessage("提示", "请选择至少一个日志文件");
                return;
            }

            var fileNames = LateQuerySelectedFiles.ToList();

            if (string.IsNullOrWhiteSpace(LateQueryName))
            {
                // 查询所有人
                records = _statisticsService.QueryLateRecordsByFiles(fileNames, threshold);
            }
            else
            {
                // 查询指定人员
                records = _statisticsService.QueryLateRecordsByNameInFiles(LateQueryName.Trim(), fileNames, threshold);
            }
        }

        LateRecords.Clear();
        foreach (var record in records)
            LateRecords.Add(record);

        if (LateRecords.Count == 0)
        {
            ShowMessage("提示", "未找到迟到记录");
        }
    }

    /// <summary>
    /// 添加文件到迟到查询的文件列表
    /// </summary>
    [RelayCommand]
    public void AddFileToLateQuery()
    {
        if (SelectedLogFile != null && !LateQuerySelectedFiles.Contains(SelectedLogFile))
        {
            LateQuerySelectedFiles.Add(SelectedLogFile);
        }
    }

    /// <summary>
    /// 从迟到查询的文件列表中移除
    /// </summary>
    [RelayCommand]
    public void RemoveFileFromLateQuery(string fileName)
    {
        LateQuerySelectedFiles.Remove(fileName);
    }

    // ── 最早签到查询 ──────────────────────────────────────

    [RelayCommand]
    public void QueryEarliestCheckIns()
    {
        int topCountValue = int.TryParse(EarliestQueryTopCount, out var value) ? value : 0;
        int? topCount = topCountValue > 0 ? topCountValue : null;

        if (EarliestQueryUseDateRange)
        {
            // 使用日期范围查询
            if (!EarliestQueryDateStart.HasValue || !EarliestQueryDateEnd.HasValue)
            {
                ShowMessage("提示", "请选择日期范围");
                return;
            }

            var startDate = EarliestQueryDateStart.Value.DateTime;
            var endDate = EarliestQueryDateEnd.Value.DateTime;

            var checkIns = _statisticsService.QueryEarliestCheckInsByDateRange(startDate, endDate, topCount);

            EarliestCheckIns.Clear();
            foreach (var checkIn in checkIns)
                EarliestCheckIns.Add(checkIn);
        }
        else
        {
            // 使用文件列表查询
            if (EarliestQuerySelectedFiles.Count == 0)
            {
                ShowMessage("提示", "请选择至少一个日志文件");
                return;
            }

            var fileNames = EarliestQuerySelectedFiles.ToList();
            var checkIns = _statisticsService.QueryEarliestCheckInsByFiles(fileNames, topCount);

            EarliestCheckIns.Clear();
            foreach (var checkIn in checkIns)
                EarliestCheckIns.Add(checkIn);
        }

        if (EarliestCheckIns.Count == 0)
        {
            ShowMessage("提示", "未找到签到记录");
        }
    }

    /// <summary>
    /// 添加文件到最早签到查询的文件列表
    /// </summary>
    [RelayCommand]
    public void AddFileToEarliestQuery()
    {
        if (SelectedLogFile != null && !EarliestQuerySelectedFiles.Contains(SelectedLogFile))
        {
            EarliestQuerySelectedFiles.Add(SelectedLogFile);
        }
    }

    /// <summary>
    /// 从最早签到查询的文件列表中移除
    /// </summary>
    [RelayCommand]
    public void RemoveFileFromEarliestQuery(string fileName)
    {
        EarliestQuerySelectedFiles.Remove(fileName);
    }
}
