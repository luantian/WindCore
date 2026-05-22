using System;
using WindCore.Core.Modbus;
using WindCore.MainControl.ViewModels;

namespace WindCore.MainControl.Services;

/// <summary>
/// PLC 通讯服务 - 周期轮询 Modbus 寄存器，将实时数据写入 DataService
/// 连接失败时自动回退到模拟模式
/// </summary>
public class CommunicationService : IDisposable
{
    private readonly DataService _dataService;
    private readonly System.Timers.Timer _pollTimer;
    private ModbusClient? _modbusClient;
    private readonly byte _slaveAddress = 1;
    private volatile bool _isConnected;

    public bool IsConnected => _isConnected;

    public CommunicationService(DataService dataService)
    {
        _dataService = dataService;
        _pollTimer = new System.Timers.Timer(100) { AutoReset = true };
        _pollTimer.Elapsed += (_, _) => PollCycle();
    }

    /// <summary>
    /// 连接 PLC 并启动轮询
    /// </summary>
    public bool Connect(string ip, int port = 502)
    {
        try
        {
            _modbusClient?.Dispose();
            var config = new ModbusConfig
            {
                TransportType = ModbusTransportType.Tcp,
                Host = ip,
                Port = port,
                TimeoutMs = 3000,
            };
            _modbusClient = new ModbusClient(config);
            _isConnected = true;
            _pollTimer.Start();
            return true;
        }
        catch (Exception ex)
        {
            _isConnected = false;
            _modbusClient?.Dispose();
            _modbusClient = null;
            System.Diagnostics.Debug.WriteLine($"[CommService] Connect failed: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 断开连接并停止轮询
    /// </summary>
    public void Disconnect()
    {
        _pollTimer.Stop();
        _isConnected = false;
        _modbusClient?.Dispose();
        _modbusClient = null;
    }

    private void PollCycle()
    {
        if (!_isConnected || _modbusClient == null) return;

        try
        {
            // 批量读取所有输入寄存器 (FC04)
            // 从 30001 到 30120，共 60 个寄存器地址范围
            // 实际按段读取减少不必要的数据
            var regs = _modbusClient.ReadInputRegisters(_slaveAddress, RegisterMap.MotorShaTemp, 120);

            // 动力系统
            _dataService.MotorTemp = ModbusClient.RegistersToFloat(
                regs[RegisterMap.MotorShaTemp - RegisterMap.MotorShaTemp],
                regs[RegisterMap.MotorShaTemp + 1 - RegisterMap.MotorShaTemp]);
            _dataService.MotorWindingTemp = ModbusClient.RegistersToFloat(
                regs[RegisterMap.MotorWindingTemp - RegisterMap.MotorShaTemp],
                regs[RegisterMap.MotorWindingTemp + 1 - RegisterMap.MotorShaTemp]);
            _dataService.MotorRpm = ModbusClient.RegistersToFloat(
                regs[RegisterMap.MotorRpm - RegisterMap.MotorShaTemp],
                regs[RegisterMap.MotorRpm + 1 - RegisterMap.MotorShaTemp]);
            _dataService.MotorFrequency = ModbusClient.RegistersToFloat(
                regs[RegisterMap.MotorFrequency - RegisterMap.MotorShaTemp],
                regs[RegisterMap.MotorFrequency + 1 - RegisterMap.MotorShaTemp]);
            _dataService.MotorCurrent = ModbusClient.RegistersToFloat(
                regs[RegisterMap.MotorCurrent - RegisterMap.MotorShaTemp],
                regs[RegisterMap.MotorCurrent + 1 - RegisterMap.MotorShaTemp]);

            // 冷却系统
            _dataService.TankLevel = ModbusClient.RegistersToFloat(
                regs[RegisterMap.CoolingTankLevel - RegisterMap.MotorShaTemp],
                regs[RegisterMap.CoolingTankLevel + 1 - RegisterMap.MotorShaTemp]);
            _dataService.CoolingOutTemp = ModbusClient.RegistersToFloat(
                regs[RegisterMap.CoolingOutTemp - RegisterMap.MotorShaTemp],
                regs[RegisterMap.CoolingOutTemp + 1 - RegisterMap.MotorShaTemp]);
            _dataService.CoolingReturnTemp = ModbusClient.RegistersToFloat(
                regs[RegisterMap.CoolingReturnTemp - RegisterMap.MotorShaTemp],
                regs[RegisterMap.CoolingReturnTemp + 1 - RegisterMap.MotorShaTemp]);
            _dataService.CoolingFlow = ModbusClient.RegistersToFloat(
                regs[RegisterMap.CoolingFlow - RegisterMap.MotorShaTemp],
                regs[RegisterMap.CoolingFlow + 1 - RegisterMap.MotorShaTemp]);
            _dataService.CoolingPressure = ModbusClient.RegistersToFloat(
                regs[RegisterMap.CoolingPressure - RegisterMap.MotorShaTemp],
                regs[RegisterMap.CoolingPressure + 1 - RegisterMap.MotorShaTemp]);

            // 台架系统
            _dataService.PitchSetpoint = ModbusClient.RegistersToFloat(
                regs[RegisterMap.PitchSetpoint - RegisterMap.MotorShaTemp],
                regs[RegisterMap.PitchSetpoint + 1 - RegisterMap.MotorShaTemp]);
            _dataService.PitchActual = ModbusClient.RegistersToFloat(
                regs[RegisterMap.PitchActual - RegisterMap.MotorShaTemp],
                regs[RegisterMap.PitchActual + 1 - RegisterMap.MotorShaTemp]);
            _dataService.YawSetpoint = ModbusClient.RegistersToFloat(
                regs[RegisterMap.YawSetpoint - RegisterMap.MotorShaTemp],
                regs[RegisterMap.YawSetpoint + 1 - RegisterMap.MotorShaTemp]);
            _dataService.YawActual = ModbusClient.RegistersToFloat(
                regs[RegisterMap.YawActual - RegisterMap.MotorShaTemp],
                regs[RegisterMap.YawActual + 1 - RegisterMap.MotorShaTemp]);

            // RAT 负载系统
            _dataService.RatThrust = ModbusClient.RegistersToFloat(
                regs[RegisterMap.RatThrust - RegisterMap.MotorShaTemp],
                regs[RegisterMap.RatThrust + 1 - RegisterMap.MotorShaTemp]);
            _dataService.RatRpm = ModbusClient.RegistersToFloat(
                regs[RegisterMap.RatRpm - RegisterMap.MotorShaTemp],
                regs[RegisterMap.RatRpm + 1 - RegisterMap.MotorShaTemp]);
            _dataService.RatTorque = ModbusClient.RegistersToFloat(
                regs[RegisterMap.RatTorque - RegisterMap.MotorShaTemp],
                regs[RegisterMap.RatTorque + 1 - RegisterMap.MotorShaTemp]);
            _dataService.RatPower = ModbusClient.RegistersToFloat(
                regs[RegisterMap.RatPower - RegisterMap.MotorShaTemp],
                regs[RegisterMap.RatPower + 1 - RegisterMap.MotorShaTemp]);

            // 桨距角
            _dataService.BladePitch1 = ModbusClient.RegistersToFloat(
                regs[RegisterMap.BladePitch1 - RegisterMap.MotorShaTemp],
                regs[RegisterMap.BladePitch1 + 1 - RegisterMap.MotorShaTemp]);
            _dataService.BladePitch2 = ModbusClient.RegistersToFloat(
                regs[RegisterMap.BladePitch2 - RegisterMap.MotorShaTemp],
                regs[RegisterMap.BladePitch2 + 1 - RegisterMap.MotorShaTemp]);
            _dataService.BladePitch3 = ModbusClient.RegistersToFloat(
                regs[RegisterMap.BladePitch3 - RegisterMap.MotorShaTemp],
                regs[RegisterMap.BladePitch3 + 1 - RegisterMap.MotorShaTemp]);
            _dataService.BladePitchAvg = (_dataService.BladePitch1 + _dataService.BladePitch2 + _dataService.BladePitch3) / 3.0;

            // 龙门架
            _dataService.GantryX = ModbusClient.RegistersToFloat(
                regs[RegisterMap.GantryX - RegisterMap.MotorShaTemp],
                regs[RegisterMap.GantryX + 1 - RegisterMap.MotorShaTemp]);
            _dataService.GantryY = ModbusClient.RegistersToFloat(
                regs[RegisterMap.GantryY - RegisterMap.MotorShaTemp],
                regs[RegisterMap.GantryY + 1 - RegisterMap.MotorShaTemp]);
            _dataService.GantryZ = ModbusClient.RegistersToFloat(
                regs[RegisterMap.GantryZ - RegisterMap.MotorShaTemp],
                regs[RegisterMap.GantryZ + 1 - RegisterMap.MotorShaTemp]);

            // 风速
            _dataService.WindSpeed = ModbusClient.RegistersToFloat(
                regs[RegisterMap.WindSpeed - RegisterMap.MotorShaTemp],
                regs[RegisterMap.WindSpeed + 1 - RegisterMap.MotorShaTemp]);
        }
        catch (Exception ex)
        {
            _isConnected = false;
            System.Diagnostics.Debug.WriteLine($"[CommService] Poll error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Disconnect();
        _pollTimer.Dispose();
    }
}
