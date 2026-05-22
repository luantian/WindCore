# WindCore

南京609风洞主控与数据库系统

## 技术栈

- **.NET 8.0** + **Avalonia 11** (跨平台 UI)
- **CommunityToolkit.Mvvm** (MVVM 框架)
- **NModbus** (PLC 通信)
- **Dapper** + **达梦数据库** (数据持久化)
- **LiveCharts2** + **ScottPlot** (图表)
- **Dock.Avalonia** (停靠布局)

## 项目结构

- `WindCore.Core` — 共享库（协议、Modbus、PID 等）
- `WindCore.MainControl` — 主控应用
- `WindCore.Database` — 数据库客户端应用
