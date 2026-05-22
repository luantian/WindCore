using System;
using System.IO;
using NModbus.IO;

namespace WindCore.Core.Modbus;

/// <summary>
/// Wraps a System.IO.Stream to implement IStreamResource for NModbus.
/// </summary>
public class SerialPortStreamResource : IStreamResource
{
    private readonly Stream _stream;

    public SerialPortStreamResource(Stream stream)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    }

    public int ReadTimeout
    {
        get => _stream.ReadTimeout;
        set => _stream.ReadTimeout = value;
    }

    public int WriteTimeout
    {
        get => _stream.WriteTimeout;
        set => _stream.WriteTimeout = value;
    }

    public int InfiniteTimeout => Timeout.Infinite;

    public void DiscardInBuffer()
    {
        // Stream doesn't support this; no-op
    }

    public void Write(byte[] buffer, int offset, int count)
    {
        _stream.Write(buffer, offset, count);
    }

    public int Read(byte[] buffer, int offset, int count)
    {
        return _stream.Read(buffer, offset, count);
    }

    public void Dispose()
    {
        // Don't dispose the underlying stream - the owner (SerialPort) manages it
    }
}
