namespace Vintagestory.API.Common;

public delegate void LogEntryDelegate(EnumLogType logType, string message, params object[] args);
