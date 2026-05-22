using System;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NModbus;

namespace WindCore.Core.Modbus;

/// <summary>
/// Modbus 通讯封装 - 支持保持寄存器/输入寄存器/线圈读写
/// 覆盖 RS232/RS485（串口）和 RJ45（TCP）通讯方式
/// </summary>
public class ModbusClient : IDisposable
{
    private readonly IModbusMaster? _tcpMaster;
    private readonly IModbusMaster? _serialMaster;
    private readonly TcpClient? _tcpClient;
    private readonly ModbusConfig _config;
    private readonly object _lock = new();

    public ModbusClient(ModbusConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));

        if (config.TransportType == ModbusTransportType.Tcp)
        {
            _tcpClient = new TcpClient();
            _tcpClient.Connect(config.Host!, config.Port);
            var factory = new ModbusFactory();
            _tcpMaster = factory.CreateMaster(_tcpClient);
            _tcpMaster.Transport.ReadTimeout = config.TimeoutMs;
            _tcpMaster.Transport.WriteTimeout = config.TimeoutMs;
        }
        else if (config.TransportType == ModbusTransportType.Serial)
        {
            var serialPort = new SerialPort(config.SerialPortName!, config.BaudRate, config.Parity, config.DataBits, config.StopBits)
            {
                ReadTimeout = config.TimeoutMs,
                WriteTimeout = config.TimeoutMs,
            };
            serialPort.Open();
            var factory = new ModbusFactory();
            var streamResource = new SerialPortStreamResource(serialPort.BaseStream);
            var transport = factory.CreateRtuTransport(streamResource);
            _serialMaster = factory.CreateMaster(transport);
            _serialMaster.Transport.ReadTimeout = config.TimeoutMs;
            _serialMaster.Transport.WriteTimeout = config.TimeoutMs;
        }
        else
        {
            throw new ArgumentException("Unsupported transport type", nameof(config));
        }
    }

    private IModbusMaster Master => _tcpMaster ?? _serialMaster ?? throw new InvalidOperationException("Modbus master not initialized");

    /// <summary>
    /// 读保持寄存器（功能码 03）
    /// </summary>
    public ushort[] ReadHoldingRegisters(byte slaveAddress, ushort startAddress, ushort count)
    {
        lock (_lock)
        {
            return Master.ReadHoldingRegisters(slaveAddress, startAddress, count);
        }
    }

    /// <summary>
    /// 读输入寄存器（功能码 04）
    /// </summary>
    public ushort[] ReadInputRegisters(byte slaveAddress, ushort startAddress, ushort count)
    {
        lock (_lock)
        {
            return Master.ReadInputRegisters(slaveAddress, startAddress, count);
        }
    }

    /// <summary>
    /// 读线圈（功能码 01）
    /// </summary>
    public bool[] ReadCoils(byte slaveAddress, ushort startAddress, ushort count)
    {
        lock (_lock)
        {
            return Master.ReadCoils(slaveAddress, startAddress, count);
        }
    }

    /// <summary>
    /// 写单个保持寄存器（功能码 06）
    /// </summary>
    public void WriteSingleRegister(byte slaveAddress, ushort address, ushort value)
    {
        lock (_lock)
        {
            Master.WriteSingleRegister(slaveAddress, address, value);
        }
    }

    /// <summary>
    /// 写多个保持寄存器（功能码 10）
    /// </summary>
    public void WriteMultipleRegisters(byte slaveAddress, ushort startAddress, ushort[] values)
    {
        lock (_lock)
        {
            Master.WriteMultipleRegisters(slaveAddress, startAddress, values);
        }
    }

    /// <summary>
    /// 写单个线圈（功能码 05）
    /// </summary>
    public void WriteSingleCoil(byte slaveAddress, ushort address, bool value)
    {
        lock (_lock)
        {
            Master.WriteSingleCoil(slaveAddress, address, value);
        }
    }

    /// <summary>
    /// 写多个线圈（功能码 15）
    /// </summary>
    public void WriteMultipleCoils(byte slaveAddress, ushort startAddress, bool[] values)
    {
        lock (_lock)
        {
            Master.WriteMultipleCoils(slaveAddress, startAddress, values);
        }
    }

    /// <summary>
    /// 寄存器值转为浮点数（两个寄存器组合成一个 float32）
    /// </summary>
    public static float RegistersToFloat(ushort high, ushort low)
    {
        var bytes = new byte[4];
        bytes[0] = (byte)(low & 0xFF);
        bytes[1] = (byte)(low >> 8);
        bytes[2] = (byte)(high & 0xFF);
        bytes[3] = (byte)(high >> 8);
        return BitConverter.ToSingle(bytes, 0);
    }

    /// <summary>
    /// 浮点数转为寄存器数组
    /// </summary>
    public static ushort[] FloatToRegisters(float value)
    {
        var bytes = BitConverter.GetBytes(value);
        return new ushort[]
        {
            (ushort)((bytes[3] << 8) | bytes[2]),
            (ushort)((bytes[1] << 8) | bytes[0]),
        };
    }

    public void Dispose()
    {
        _tcpMaster?.Dispose();
        _serialMaster?.Dispose();
        _tcpClient?.Close();
        _tcpClient?.Dispose();
    }
}

/// <summary>
/// Modbus 连接配置
/// </summary>
public class ModbusConfig
{
    /// <summary>
    /// 传输类型（TCP 或 串口）
    /// </summary>
    public ModbusTransportType TransportType { get; set; }

    // TCP 相关
    /// <summary>
    /// TCP 主机地址
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    /// TCP 端口（默认 502）
    /// </summary>
    public int Port { get; set; } = 502;

    // 串口相关
    /// <summary>
    /// 串口名称（如 COM1）
    /// </summary>
    public string? SerialPortName { get; set; }

    /// <summary>
    /// 波特率（默认 9600）
    /// </summary>
    public int BaudRate { get; set; } = 9600;

    /// <summary>
    /// 校验位（默认 None）
    /// </summary>
    public Parity Parity { get; set; } = Parity.None;

    /// <summary>
    /// 数据位（默认 8）
    /// </summary>
    public int DataBits { get; set; } = 8;

    /// <summary>
    /// 停止位（默认 One）
    /// </summary>
    public StopBits StopBits { get; set; } = StopBits.One;

    /// <summary>
    /// 超时时间（毫秒，默认 3000）
    /// </summary>
    public int TimeoutMs { get; set; } = 3000;
}

/// <summary>
/// Modbus 传输类型
/// </summary>
public enum ModbusTransportType
{
    Tcp,
    Serial,
}
