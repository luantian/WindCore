using System;
using System.Collections.Generic;
using WindCore.Core.Storage;
using WindCore.MainControl.ViewModels;

namespace WindCore.MainControl.Services;

/// <summary>
/// 存储服务 - 定时从 DataService 采集数据，写入 CSV 或数据库
/// </summary>
public class StorageService : IDisposable
{
    private readonly DataService _dataService;
    private readonly SettingsPanelViewModel _settings;
    private StorageEngine? _engine;
    private System.Timers.Timer? _writeTimer;
    private string _experimentId = "";
    private bool _isWriting;

    public bool IsWriting => _isWriting;

    public StorageService(DataService dataService, SettingsPanelViewModel settings)
    {
        _dataService = dataService;
        _settings = settings;

        var config = new StorageConfig
        {
            CsvBasePath = settings.DataPath,
        };
        _engine = new StorageEngine(config, msg => System.Diagnostics.Debug.WriteLine($"[StorageEngine] {msg}"));

        _experimentId = $"EXP_{DateTime.Now:yyyyMMdd_HHmmss}";
    }

    public void StartWriting(string? experimentId = null)
    {
        if (_isWriting) return;

        _experimentId = experimentId ?? $"EXP_{DateTime.Now:yyyyMMdd_HHmmss}";
        _isWriting = true;

        var intervalMs = Math.Max(_settings.SampleRate, 100);
        _writeTimer = new System.Timers.Timer(intervalMs) { AutoReset = true };
        _writeTimer.Elapsed += OnWriteTimer;
        _writeTimer.Start();

        System.Diagnostics.Debug.WriteLine($"[StorageService] Started writing experiment: {_experimentId}");
    }

    public void StopWriting()
    {
        _isWriting = false;
        _writeTimer?.Stop();
        _writeTimer?.Dispose();
        _writeTimer = null;
        System.Diagnostics.Debug.WriteLine("[StorageService] Stopped writing");
    }

    private void OnWriteTimer(object? sender, EventArgs e)
    {
        if (!_isWriting) return;

        StoreType storeType = _settings.StorageMode switch
        {
            "数据库联动" => StoreType.Database,
            _ => StoreType.Csv,
        };

        var item = new StoreItem
        {
            ExperimentId = _experimentId,
            Timestamp = DateTime.UtcNow,
            StoreType = storeType,
            Channels = CollectChannels(),
        };

        _engine?.Enqueue(item);
    }

    private List<ChannelDataItem> CollectChannels()
    {
        return new List<ChannelDataItem>
        {
            new() { ChannelId = "WS", ChannelName = "风速", RawValue = _dataService.WindSpeed, EngineeringValue = _dataService.WindSpeed, Unit = "m/s" },
            new() { ChannelId = "MRPM", ChannelName = "电机转速", RawValue = _dataService.MotorRpm, EngineeringValue = _dataService.MotorRpm, Unit = "r/min" },
            new() { ChannelId = "MTEMP", ChannelName = "轴温", RawValue = _dataService.MotorTemp, EngineeringValue = _dataService.MotorTemp, Unit = "°C" },
            new() { ChannelId = "MFREQ", ChannelName = "频率", RawValue = _dataService.MotorFrequency, EngineeringValue = _dataService.MotorFrequency, Unit = "Hz" },
            new() { ChannelId = "MCURR", ChannelName = "电流", RawValue = _dataService.MotorCurrent, EngineeringValue = _dataService.MotorCurrent, Unit = "A" },
            new() { ChannelId = "MWTEMP", ChannelName = "绕组温度", RawValue = _dataService.MotorWindingTemp, EngineeringValue = _dataService.MotorWindingTemp, Unit = "°C" },
            new() { ChannelId = "COTEMP", ChannelName = "出水温度", RawValue = _dataService.CoolingOutTemp, EngineeringValue = _dataService.CoolingOutTemp, Unit = "°C" },
            new() { ChannelId = "CRTEMP", ChannelName = "回水温度", RawValue = _dataService.CoolingReturnTemp, EngineeringValue = _dataService.CoolingReturnTemp, Unit = "°C" },
            new() { ChannelId = "CFLOW", ChannelName = "流量", RawValue = _dataService.CoolingFlow, EngineeringValue = _dataService.CoolingFlow, Unit = "L/min" },
            new() { ChannelId = "CPRESS", ChannelName = "压力", RawValue = _dataService.CoolingPressure, EngineeringValue = _dataService.CoolingPressure, Unit = "MPa" },
            new() { ChannelId = "CTLEVEL", ChannelName = "水箱液位", RawValue = _dataService.TankLevel, EngineeringValue = _dataService.TankLevel, Unit = "%" },
            new() { ChannelId = "PSET", ChannelName = "俯仰设定", RawValue = _dataService.PitchSetpoint, EngineeringValue = _dataService.PitchSetpoint, Unit = "°" },
            new() { ChannelId = "PACT", ChannelName = "俯仰实际", RawValue = _dataService.PitchActual, EngineeringValue = _dataService.PitchActual, Unit = "°" },
            new() { ChannelId = "YSET", ChannelName = "侧滑设定", RawValue = _dataService.YawSetpoint, EngineeringValue = _dataService.YawSetpoint, Unit = "°" },
            new() { ChannelId = "YACT", ChannelName = "侧滑实际", RawValue = _dataService.YawActual, EngineeringValue = _dataService.YawActual, Unit = "°" },
            new() { ChannelId = "RTHRUST", ChannelName = "RAT推力", RawValue = _dataService.RatThrust, EngineeringValue = _dataService.RatThrust, Unit = "N" },
            new() { ChannelId = "RRPM", ChannelName = "RAT转速", RawValue = _dataService.RatRpm, EngineeringValue = _dataService.RatRpm, Unit = "r/min" },
            new() { ChannelId = "RTORQUE", ChannelName = "RAT扭矩", RawValue = _dataService.RatTorque, EngineeringValue = _dataService.RatTorque, Unit = "N·m" },
            new() { ChannelId = "RPOWER", ChannelName = "RAT功率", RawValue = _dataService.RatPower, EngineeringValue = _dataService.RatPower, Unit = "kW" },
            new() { ChannelId = "BP1", ChannelName = "桨叶#1", RawValue = _dataService.BladePitch1, EngineeringValue = _dataService.BladePitch1, Unit = "°" },
            new() { ChannelId = "BP2", ChannelName = "桨叶#2", RawValue = _dataService.BladePitch2, EngineeringValue = _dataService.BladePitch2, Unit = "°" },
            new() { ChannelId = "BP3", ChannelName = "桨叶#3", RawValue = _dataService.BladePitch3, EngineeringValue = _dataService.BladePitch3, Unit = "°" },
            new() { ChannelId = "GX", ChannelName = "龙门架X", RawValue = _dataService.GantryX, EngineeringValue = _dataService.GantryX, Unit = "mm" },
            new() { ChannelId = "GY", ChannelName = "龙门架Y", RawValue = _dataService.GantryY, EngineeringValue = _dataService.GantryY, Unit = "mm" },
            new() { ChannelId = "GZ", ChannelName = "龙门架Z", RawValue = _dataService.GantryZ, EngineeringValue = _dataService.GantryZ, Unit = "mm" },
        };
    }

    public void Dispose()
    {
        StopWriting();
        _engine?.Dispose();
    }
}
