using System;

namespace BopCustomTextures.Logging;

/// <summary>
/// Superset of BepInEx log levels, as also includes MixtapeEditor.
/// </summary>
[Flags]
public enum LogLevel
{
    None = 0,
    Fatal = 1,
    Error = 2,
    Warning = 4,
    Message = 8,
    Info = 0x10,
    Debug = 0x20,
    MixtapeEditor = 0x40,
    All = 0x7F
}