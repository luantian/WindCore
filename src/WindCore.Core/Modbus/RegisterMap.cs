namespace WindCore.Core.Modbus;

/// <summary>
/// PLC 寄存器地址映射表
/// 所有浮点值使用 2 个寄存器 (Float32, 高字在前)
/// 实际地址需与 PLC 程序员确认后修改
/// </summary>
public static class RegisterMap
{
    // === 动力系统 (FC04 输入寄存器) ===
    public const ushort MotorShaTemp      = 30001; // 轴温 °C
    public const ushort MotorWindingTemp  = 30003; // 绕组温度 °C
    public const ushort MotorShaVibration = 30005; // 轴振 mm/s
    public const ushort MotorRpm          = 30007; // 电机转速 r/min
    public const ushort MotorFrequency    = 30009; // 频率 Hz
    public const ushort MotorCurrent      = 30011; // 电流 A

    // === 冷却系统 ===
    public const ushort CoolingTankLevel  = 30020; // 水箱液位 %
    public const ushort CoolingOutTemp    = 30022; // 出水温度 °C
    public const ushort CoolingReturnTemp = 30024; // 回水温度 °C
    public const ushort CoolingFlow       = 30026; // 流量 L/min
    public const ushort CoolingPressure   = 30028; // 压力 MPa

    // === 台架系统 ===
    public const ushort PitchSetpoint     = 30040; // 俯仰角设定值 °
    public const ushort PitchActual       = 30042; // 俯仰角实际值 °
    public const ushort YawSetpoint       = 30044; // 侧滑角设定值 °
    public const ushort YawActual         = 30046; // 侧滑角实际值 °

    // === RAT 负载系统 ===
    public const ushort RatThrust         = 30060; // 推力 N
    public const ushort RatRpm            = 30062; // 转速 r/min
    public const ushort RatTorque         = 30064; // 扭矩 N·m
    public const ushort RatPower          = 30066; // 功率 kW

    // === 桨距角 ===
    public const ushort BladePitch1       = 30080; // 桨叶 #1 °
    public const ushort BladePitch2       = 30082; // 桨叶 #2 °
    public const ushort BladePitch3       = 30084; // 桨叶 #3 °

    // === 龙门架 ===
    public const ushort GantryX           = 30100; // X 位置 mm
    public const ushort GantryY           = 30102; // Y 位置 mm
    public const ushort GantryZ           = 30104; // Z 位置 mm

    // === 风速 ===
    public const ushort WindSpeed         = 30120; // 风速 m/s
}
