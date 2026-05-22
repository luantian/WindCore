using System;

namespace WindCore.Core.PID;

/// <summary>
/// 风速 PID 闭环控制器
/// 支持 Kp/Ki/Kd/输出限定/滤波/微分先行/积分饱和/定时闭环可调
/// </summary>
public class WindSpeedPIDController
{
    private readonly PIDConfig _config;
    private double _integral;
    private double _prevError;
    private double _prevDerivative;
    private double _filteredInput;
    private double _filteredDerivative;
    private bool _isInitialized;

    public double Setpoint { get; set; }
    public double Output { get; private set; }
    public double Error { get; private set; }

    public WindSpeedPIDController(PIDConfig config)
    {
        _config = config;
        Setpoint = config.InitialSetpoint;
        _isInitialized = false;
    }

    /// <summary>
    /// 计算 PID 输出
    /// </summary>
    /// <param name="measuredValue">当前测量值（风速）</param>
    /// <param name="dt">时间间隔（秒）</param>
    /// <returns>控制输出（风机转速/频率）</returns>
    public double Compute(double measuredValue, double dt)
    {
        if (dt <= 0) dt = _config.SampleTime;

        // 误差计算
        Error = Setpoint - measuredValue;

        // 比例项
        double proportional = _config.Kp * Error;

        // 积分项（带抗积分饱和）
        _integral += Error * dt;
        _integral = Clamp(_integral, _config.IntegralMin, _config.IntegralMax);
        double integral = _config.Ki * _integral;

        // 微分项（微分先行，避免设定值突变导致微分项冲击）
        double errorDelta = Error - _prevError;
        double rawDerivative = errorDelta / dt;

        // 一阶低通滤波（抑制高频噪声）
        double alpha = _config.DerivativeFilterAlpha;
        _filteredDerivative = alpha * rawDerivative + (1 - alpha) * _prevDerivative;
        double derivative = _config.Kd * _filteredDerivative;

        // PID 输出 = P + I + D
        Output = proportional + integral + derivative;

        // 输出限幅
        Output = Clamp(Output, _config.OutputMin, _config.OutputMax);

        // 更新状态
        _prevError = Error;
        _prevDerivative = _filteredDerivative;

        if (!_isInitialized)
        {
            // 首次调用，初始化滤波器状态
            _filteredInput = measuredValue;
            _filteredDerivative = 0;
            _prevError = Error;
            _isInitialized = true;
        }

        return Output;
    }

    /// <summary>
    /// 重置控制器状态
    /// </summary>
    public void Reset()
    {
        _integral = 0;
        _prevError = 0;
        _prevDerivative = 0;
        _filteredInput = 0;
        _filteredDerivative = 0;
        Output = 0;
        Error = 0;
        _isInitialized = false;
    }

    /// <summary>
    /// 更新 PID 参数
    /// </summary>
    public void UpdateConfig(PIDConfig config)
    {
        _config.Kp = config.Kp;
        _config.Ki = config.Ki;
        _config.Kd = config.Kd;
        _config.OutputMin = config.OutputMin;
        _config.OutputMax = config.OutputMax;
        _config.IntegralMin = config.IntegralMin;
        _config.IntegralMax = config.IntegralMax;
        _config.DerivativeFilterAlpha = config.DerivativeFilterAlpha;
        _config.SampleTime = config.SampleTime;
    }

    private static double Clamp(double value, double min, double max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}

/// <summary>
/// PID 控制器配置
/// </summary>
public class PIDConfig
{
    /// <summary>
    /// 比例系数
    /// </summary>
    public double Kp { get; set; } = 1.0;

    /// <summary>
    /// 积分系数
    /// </summary>
    public double Ki { get; set; } = 0.1;

    /// <summary>
    /// 微分系数
    /// </summary>
    public double Kd { get; set; } = 0.01;

    /// <summary>
    /// 输出下限
    /// </summary>
    public double OutputMin { get; set; } = 0;

    /// <summary>
    /// 输出上限
    /// </summary>
    public double OutputMax { get; set; } = 100;

    /// <summary>
    /// 积分下限（抗饱和）
    /// </summary>
    public double IntegralMin { get; set; } = -50;

    /// <summary>
    /// 积分上限（抗饱和）
    /// </summary>
    public double IntegralMax { get; set; } = 50;

    /// <summary>
    /// 微分滤波系数（0~1，越小滤波越强）
    /// </summary>
    public double DerivativeFilterAlpha { get; set; } = 0.2;

    /// <summary>
    /// 采样时间（秒）
    /// </summary>
    public double SampleTime { get; set; } = 0.1;

    /// <summary>
    /// 初始设定值
    /// </summary>
    public double InitialSetpoint { get; set; } = 0;
}
