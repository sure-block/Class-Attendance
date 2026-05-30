using System.Text.Json;
using System.Text.Encodings.Web;

namespace ClasslandAttendance.Services;

public class RosterService
{
    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ClasslandAttendance");

    private static readonly string RosterPath = Path.Combine(DataDir, "roster.json");

    private static readonly List<string> DefaultRoster = new()
    {
        "张三", "李四", "王五", "赵六", "钱七",
        "孙八", "周九", "吴十", "郑十一", "王十二"
    };

    public List<string> Load()
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            if (!File.Exists(RosterPath))
            {
                Save(DefaultRoster);
                return new List<string>(DefaultRoster);
            }
            var json = File.ReadAllText(RosterPath);
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>(DefaultRoster);
        }
        catch (Exception ex)
        {
            // 记录错误并返回默认名单
            System.Diagnostics.Debug.WriteLine($"加载名单失败: {ex.Message}");
            return new List<string>(DefaultRoster);
        }
    }

    public void Save(List<string> roster)
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            };
            File.WriteAllText(RosterPath, JsonSerializer.Serialize(roster, options));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"保存名单失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 从TXT文件导入名单（每行一个姓名）
    /// </summary>
    public List<string> ImportFromTxt(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("文件不存在", filePath);
            }

            var lines = File.ReadAllLines(filePath)
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrEmpty(line))
                .Distinct()
                .ToList();

            if (lines.Count == 0)
            {
                throw new InvalidOperationException("文件中没有有效的姓名");
            }

            return lines;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"导入名单失败: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 清空名单
    /// </summary>
    public void Clear()
    {
        try
        {
            Save(new List<string>());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"清空名单失败: {ex.Message}");
            throw;
        }
    }
}
