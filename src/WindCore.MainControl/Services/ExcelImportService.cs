using System;
using System.Collections.Generic;
using System.IO;
using MiniExcelLibs;

namespace WindCore.MainControl.Services;

/// <summary>
/// Excel 配置导入服务 — 用 MiniExcel 解析 .xlsx 文件
/// </summary>
public static class ExcelImportService
{
    public static List<AlarmConfigRow> ImportAlarmConfig(string filePath)
    {
        var result = new List<AlarmConfigRow>();
        var rows = MiniExcel.Query<Dictionary<string, object>>(filePath, sheetName: "报警配置");
        foreach (var row in rows)
        {
            result.Add(new AlarmConfigRow
            {
                Level = ToInt(row, "Level"),
                System = ToString(row, "System"),
                Point = ToString(row, "Point"),
                Threshold = ToDouble(row, "Threshold"),
                Action = ToString(row, "Action"),
            });
        }
        return result;
    }

    public static List<ExperimentTypeRow> ImportExperimentTypes(string filePath)
    {
        var result = new List<ExperimentTypeRow>();
        var rows = MiniExcel.Query<Dictionary<string, object>>(filePath, sheetName: "试验类型");
        foreach (var row in rows)
        {
            result.Add(new ExperimentTypeRow
            {
                Name = ToString(row, "Name"),
                WarmupSec = ToInt(row, "WarmupSec"),
                SteadyStateSec = ToInt(row, "SteadyStateSec"),
                AutoSave = ToBool(row, "AutoSave"),
            });
        }
        return result;
    }

    public static List<ChannelMappingRow> ImportChannelMappings(string filePath)
    {
        var result = new List<ChannelMappingRow>();
        var rows = MiniExcel.Query<Dictionary<string, object>>(filePath, sheetName: "通道映射");
        foreach (var row in rows)
        {
            result.Add(new ChannelMappingRow
            {
                ChannelId = ToString(row, "ChannelId"),
                ChannelName = ToString(row, "ChannelName"),
                Unit = ToString(row, "Unit"),
                RegisterAddress = ToInt(row, "RegisterAddress"),
            });
        }
        return result;
    }

    private static int ToInt(Dictionary<string, object> row, string key)
    {
        if (row.TryGetValue(key, out var v)) return Convert.ToInt32(v);
        return 0;
    }

    private static double ToDouble(Dictionary<string, object> row, string key)
    {
        if (row.TryGetValue(key, out var v)) return Convert.ToDouble(v);
        return 0;
    }

    private static string ToString(Dictionary<string, object> row, string key)
    {
        if (row.TryGetValue(key, out var v)) return v?.ToString() ?? "";
        return "";
    }

    private static bool ToBool(Dictionary<string, object> row, string key)
    {
        if (row.TryGetValue(key, out var v)) return Convert.ToBoolean(v);
        return false;
    }
}

public class AlarmConfigRow
{
    public int Level { get; set; }
    public string System { get; set; } = "";
    public string Point { get; set; } = "";
    public double Threshold { get; set; }
    public string Action { get; set; } = "";
}

public class ExperimentTypeRow
{
    public string Name { get; set; } = "";
    public int WarmupSec { get; set; }
    public int SteadyStateSec { get; set; }
    public bool AutoSave { get; set; }
}

public class ChannelMappingRow
{
    public string ChannelId { get; set; } = "";
    public string ChannelName { get; set; } = "";
    public string Unit { get; set; } = "";
    public int RegisterAddress { get; set; }
}
