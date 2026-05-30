namespace ClasslandAttendance.Models;

public class AttendanceLog
{
    public string Operation { get; set; } = string.Empty; // 签到 / 取消签到
    public string Name { get; set; } = string.Empty;
    public TimeSpan Time { get; set; }
    public bool Status { get; set; } // true=正常, false=停止签到后

    /// <summary>
    /// 转义CSV字段，防止Excel公式注入
    /// </summary>
    private static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return field;
        
        // 如果字段包含特殊字符，用引号包裹
        if (field.StartsWith("=") || field.StartsWith("+") || 
            field.StartsWith("-") || field.StartsWith("@") ||
            field.Contains(",") || field.Contains("\"") || 
            field.Contains("\n"))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        
        return field;
    }

    public string ToCsvLine()
    {
        var operation = EscapeCsvField(Operation);
        var name = EscapeCsvField(Name);
        var time = Time.ToString(@"hh\:mm\:ss");
        var status = Status.ToString().ToLowerInvariant();
        
        return $"{operation},{name},{time},{status}";
    }

    public static AttendanceLog? FromCsvLine(string line)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            var parts = ParseCsvLine(line);
            if (parts.Length < 4) return null;

            if (!TimeSpan.TryParse(parts[2].Trim(), out var time))
                return null;

            return new AttendanceLog
            {
                Operation = UnescapeCsvField(parts[0]),
                Name = UnescapeCsvField(parts[1]),
                Time = time,
                Status = parts[3].Trim().ToLowerInvariant() == "true"
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 解析CSV行，正确处理引号包裹的字段
    /// </summary>
    private static string[] ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var currentField = new System.Text.StringBuilder();
        bool inQuotes = false;
        int i = 0;

        while (i < line.Length)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // 转义的引号 ""
                    currentField.Append('"');
                    i += 2;
                }
                else
                {
                    // 切换引号状态
                    inQuotes = !inQuotes;
                    i++;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                // 字段分隔符
                fields.Add(currentField.ToString());
                currentField.Clear();
                i++;
            }
            else
            {
                currentField.Append(c);
                i++;
            }
        }

        // 添加最后一个字段
        fields.Add(currentField.ToString());

        return fields.ToArray();
    }

    /// <summary>
    /// 反转义CSV字段
    /// </summary>
    private static string UnescapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return field;

        // 移除首尾引号
        if (field.StartsWith("\"") && field.EndsWith("\""))
        {
            field = field.Substring(1, field.Length - 2);
        }

        // 还原转义的引号
        return field.Replace("\"\"", "\"");
    }
}
