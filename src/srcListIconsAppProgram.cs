using System;
using System.Linq;
using System.Reflection;

var assembly = Assembly.LoadFrom("IconPacks.Avalonia.FontAwesome.dll");
var kindType = assembly.GetType("IconPacks.Avalonia.FontAwesome.PackIconFontAwesomeKind");
var names = Enum.GetNames(kindType!);
var matches = names.Where(n =>
    n.Contains("Valve") || n.Contains("Toggle") || n.Contains("Switch") ||
    n.Contains("Faucet") || n.Contains("Weight") || n.Contains("Dumbbell") ||
    n.Contains("Anchor") || n.Contains("Cube") || n.Contains("Ruler") ||
    n.Contains("Arrow") || n.Contains("Gauge") || n.Contains("Bell") ||
    n.Contains("Slider") || n.Contains("Wrench") || n.Contains("Tool") ||
    n.Contains("Compass") || n.Contains("Fan") || n.Contains("Gear") ||
    n.Contains("Desktop") || n.Contains("Wind") || n.Contains("Temperature") ||
    n.Contains("Snowflake") || n.Contains("Crosshair") || n.Contains("Flask") ||
    n.Contains("Shield") || n.Contains("UpDown") || n.Contains("Box")
).OrderBy(x => x);
foreach (var n in matches) Console.WriteLine(n);
Console.WriteLine($"Total icons: {names.Length}");
