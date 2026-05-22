using System;
using System.Linq;
using System.Collections.Generic;
using Dapper;
using Dm;

namespace WindCore.Core.Storage;

/// <summary>
/// 达梦数据库写入器
/// 表结构: experiment_data (experiment_id, timestamp, channel_id, channel_name, raw_value, engineering_value, unit)
/// </summary>
public class DatabaseWriter : IDisposable
{
    private readonly string _connectionString;
    private DmConnection? _connection;

    public DatabaseWriter(string host, int port, string dbName, string user, string password)
    {
        _connectionString = $"Server={host}:{port};Database={dbName};User Id={user};Password={password};";
    }

    public bool Connect()
    {
        try
        {
            _connection = new DmConnection(_connectionString);
            _connection.Open();
            return true;
        }
        catch
        {
            _connection?.Dispose();
            _connection = null;
            return false;
        }
    }

    public void WriteBatch(StoreItem[] items)
    {
        if (_connection == null || _connection.State != System.Data.ConnectionState.Open)
            return;

        var records = items.SelectMany(i => i.Channels.Select(c => new
        {
            ExperimentId = i.ExperimentId,
            Timestamp = i.Timestamp,
            ChannelId = c.ChannelId,
            ChannelName = c.ChannelName,
            RawValue = c.RawValue,
            EngineeringValue = c.EngineeringValue,
            Unit = c.Unit,
        })).ToList();

        if (records.Count == 0) return;

        const string sql = """
            INSERT INTO experiment_data
                (experiment_id, timestamp, channel_id, channel_name, raw_value, engineering_value, unit)
            VALUES
                (@ExperimentId, @Timestamp, @ChannelId, @ChannelName, @RawValue, @EngineeringValue, @Unit)
            """;

        _connection.Execute(sql, records);
    }

    public void Dispose()
    {
        _connection?.Close();
        _connection?.Dispose();
    }
}
