using System;

namespace Vintagestory.API.Common;

/// <summary>
/// Interface to the client's and server's event, debug and error logging utilty.
/// </summary>
public interface ILogger
{
	/// <summary>
	/// If true, will also print to Diagnostics.Debug.
	/// </summary>
	bool TraceLog { get; set; }

	/// <summary>
	/// Fired each time a new log entry has been added.
	/// </summary>
	event LogEntryDelegate EntryAdded;

	/// <summary>
	/// Removes any handler that registered to the EntryAdded event.
	/// This method is called when the client leaves a world or server shuts down.
	/// </summary>
	void ClearWatchers();

	/// <summary>
	/// Adds a new log entry with the specified log type, format string and arguments.
	/// </summary>
	void Log(EnumLogType logType, string format, params object[] args);

	/// <summary>
	/// Adds a new log entry with the specified log type and message.
	/// </summary>
	void Log(EnumLogType logType, string message);

	/// <summary>
	/// Adds a new <see cref="F:Vintagestory.API.Common.EnumLogType.Chat" /> log entry with the specified format string and arguments.
	/// </summary>
	void Chat(string format, params object[] args);

	/// <summary>
	/// Adds a new <see cref="F:Vintagestory.API.Common.EnumLogType.Chat" /> log entry with the specified message.
	/// </summary>
	void Chat(string message);

	/// <summary>
	/// Adds a new <see cref="F:Vintagestory.API.Common.EnumLogType.Event" /> log entry with the specified format string and arguments.
	/// </summary>
	void Event(string format, params object[] args);

	/// <summary>
	/// Adds a new <see cref="F:Vintagestory.API.Common.EnumLogType.Event" /> log entry with the specified message.
	/// </summary>
	void Event(string message);

	/// <summary>
	/// Adds a new <see cref="F:Vintagestory.API.Common.EnumLogType.StoryEvent" /> log entry with the specified format string and arguments.
	/// </summary>
	void StoryEvent(string format, params object[] args);

	/// <summary>
	/// Adds a new <see cref="F:Vintagestory.API.Common.EnumLogType.StoryEvent" /> log entry with the specified message.
	/// </summary>
	void StoryEvent(string message);

	/// <summary>
	/// Adds a new <see cref="F:Vintagestory.API.Common.EnumLogType.Build" /> log entry with the specified format string and arguments.
	/// </summary>
	void Build(string format, params object[] args);

	/// <summary>
	/// Adds a new <see cref="F:Vintagestory.API.Common.EnumLogType.Build" /> log entry with the specified message.
	/// </summary>
	void Build(string message);

	/// <summary>
	/// Adds a new <see cref="F:Vintagestory.API.Common.EnumLogType.VerboseDebug" /> log entry with the specified format string and arguments.
	/// </summary>
	void VerboseDebug(string format, params object[] args);

	/// <summary>
	/// Adds a new <see cref="F:Vintagestory.API.Common.EnumLogType.VerboseDebug" /> log entry with the specified message.
	/// </summary>
	void VerboseDebug(string message);

	/// <summary>
	/// Adds a new <see cref="F:Vintagestory.API.Common.EnumLogType.Debug" /> log entry with the specified format string and arguments.
	/// </summary>
	void Debug(string format, params object[] args);

	/// <summary>
	/// Adds a new <see cref="F:Vintagestory.API.Common.EnumLogType.Debug" /> log entry with the specified message.
	/// </summary>
	void Debug(string message);

	/// <summary>
	/// Adds a new <see cref="F:Vintagestory.API.Common.EnumLogType.Notification" /> log entry with the specified format string and arguments.
	/// </summary>
	void Notification(string format, params object[] args);

	/// <summary>
	/// Adds a new <see cref="F:Vintagestory.API.Common.EnumLogType.Notification" /> log entry with the specified message.
	/// </summary>
	void Notification(string message);

	/// <summary>
	/// Adds a new <see cref="F:Vintagestory.API.Common.EnumLogType.Warning" /> log entry with the specified format string and arguments.
	/// </summary>
	void Warning(string format, params object[] args);

	/// <summary>
	/// Adds a new <see cref="F:Vintagestory.API.Common.EnumLogType.Warning" /> log entry with the specified message.
	/// </summary>
	void Warning(string message);

	/// <summary>
	/// Convenience method for logging exceptions in try/catch blocks
	/// </summary>
	void Warning(Exception e);

	/// <summary>
	/// Adds a new <see cref="F:Vintagestory.API.Common.EnumLogType.Error" /> log entry with the specified format string and arguments.
	/// </summary>
	void Error(string format, params object[] args);

	/// <summary>
	/// Adds a new <see cref="F:Vintagestory.API.Common.EnumLogType.Error" /> log entry with the specified message.
	/// </summary>
	void Error(string message);

	/// <summary>
	/// Convenience method for logging exceptions in try/catch blocks
	/// </summary>
	void Error(Exception e);

	/// <summary>
	/// Adds a new <see cref="F:Vintagestory.API.Common.EnumLogType.Fatal" /> log entry with the specified format string and arguments.
	/// </summary>
	void Fatal(string format, params object[] args);

	/// <summary>
	/// Adds a new <see cref="F:Vintagestory.API.Common.EnumLogType.Fatal" /> log entry with the specified message.
	/// </summary>
	void Fatal(string message);

	/// <summary>
	/// Convenience method for logging exceptions in try/catch blocks
	/// </summary>
	void Fatal(Exception e);

	/// <summary>
	/// Adds a new <see cref="F:Vintagestory.API.Common.EnumLogType.Audit" /> log entry with the specified format string and arguments.
	/// </summary>
	void Audit(string format, params object[] args);

	/// <summary>
	/// Adds a new <see cref="F:Vintagestory.API.Common.EnumLogType.Audit" /> log entry with the specified message.
	/// </summary>
	void Audit(string message);
}
