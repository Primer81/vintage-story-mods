using System;
using System.Diagnostics;

namespace Vintagestory.API.Common;

/// <summary>
/// Base implementation for <see cref="T:Vintagestory.API.Common.ILogger" /> which implements all
/// methods besides a new abstract method <see cref="M:Vintagestory.API.Common.LoggerBase.LogImpl(Vintagestory.API.Common.EnumLogType,System.String,System.Object[])" />.
/// </summary>
public abstract class LoggerBase : ILogger
{
	private static readonly object[] _emptyArgs;

	public static string SourcePath;

	public bool TraceLog { get; set; }

	public event LogEntryDelegate EntryAdded;

	static LoggerBase()
	{
		_emptyArgs = new object[0];
		try
		{
			throw new DummyLoggerException("Exception for the logger to load some exception related info");
		}
		catch (DummyLoggerException e)
		{
			SourcePath = new StackTrace(e, fNeedFileInfo: true).GetFrame(0).GetFileName().Split("VintagestoryApi")[0];
		}
	}

	public void ClearWatchers()
	{
		this.EntryAdded = null;
	}

	/// <summary>
	/// This is the only method necessary to be overridden by the
	/// implementing class, actually does the logging as necessary.
	/// </summary>
	protected abstract void LogImpl(EnumLogType logType, string format, params object[] args);

	public void Log(EnumLogType logType, string format, params object[] args)
	{
		LogImpl(logType, format, args);
		this.EntryAdded?.Invoke(logType, format, args);
	}

	public void Log(EnumLogType logType, string message)
	{
		Log(logType, message, _emptyArgs);
	}

	public void Chat(string format, params object[] args)
	{
		Log(EnumLogType.Chat, format, args);
	}

	public void Chat(string message)
	{
		Log(EnumLogType.Chat, message, _emptyArgs);
	}

	public void Event(string format, params object[] args)
	{
		Log(EnumLogType.Event, format, args);
	}

	public void Event(string message)
	{
		Log(EnumLogType.Event, message, _emptyArgs);
	}

	public void StoryEvent(string format, params object[] args)
	{
		Log(EnumLogType.StoryEvent, format, args);
	}

	public void StoryEvent(string message)
	{
		Log(EnumLogType.StoryEvent, message, _emptyArgs);
	}

	public void Build(string format, params object[] args)
	{
		Log(EnumLogType.Build, format, args);
	}

	public void Build(string message)
	{
		Log(EnumLogType.Build, message, _emptyArgs);
	}

	public void VerboseDebug(string format, params object[] args)
	{
		Log(EnumLogType.VerboseDebug, format, args);
	}

	public void VerboseDebug(string message)
	{
		Log(EnumLogType.VerboseDebug, message, _emptyArgs);
	}

	public void Debug(string format, params object[] args)
	{
		Log(EnumLogType.Debug, format, args);
	}

	public void Debug(string message)
	{
		Log(EnumLogType.Debug, message, _emptyArgs);
	}

	public void Notification(string format, params object[] args)
	{
		Log(EnumLogType.Notification, format, args);
	}

	public void Notification(string message)
	{
		Log(EnumLogType.Notification, message, _emptyArgs);
	}

	public void Warning(string format, params object[] args)
	{
		Log(EnumLogType.Warning, format, args);
	}

	public void Warning(string message)
	{
		Log(EnumLogType.Warning, message, _emptyArgs);
	}

	public void Warning(Exception e)
	{
		Log(EnumLogType.Error, "Exception: {0}\n{1}", e.Message, CleanStackTrace(e.StackTrace));
	}

	public void Error(string format, params object[] args)
	{
		Log(EnumLogType.Error, format, args);
	}

	public void Error(string message)
	{
		Log(EnumLogType.Error, message, _emptyArgs);
	}

	public void Error(Exception e)
	{
		Log(EnumLogType.Error, "Exception: {0}\n{1}", e.Message, CleanStackTrace(e.StackTrace));
	}

	/// <summary>
	/// Remove the full path from the stacktrace of the machine that compiled the code
	/// </summary>
	/// <param name="stackTrace"></param>
	/// <returns></returns>
	public static string CleanStackTrace(string stackTrace)
	{
		if (stackTrace == null || stackTrace.Length < 150)
		{
			stackTrace += RemoveThreeLines(Environment.StackTrace);
		}
		return stackTrace.Replace(SourcePath, "");
	}

	private static string RemoveThreeLines(string s)
	{
		int i;
		if ((i = s.IndexOf('\n')) > 0)
		{
			s = s.Substring(i + 1);
		}
		if ((i = s.IndexOf('\n')) > 0)
		{
			s = s.Substring(i + 1);
		}
		if ((i = s.IndexOf('\n')) > 0)
		{
			s = s.Substring(i + 1);
		}
		return s;
	}

	public void Fatal(string format, params object[] args)
	{
		Log(EnumLogType.Fatal, format, args);
	}

	public void Fatal(string message)
	{
		Log(EnumLogType.Fatal, message, _emptyArgs);
	}

	public void Fatal(Exception e)
	{
		Log(EnumLogType.Error, "Exception: {0}\n{1}", e.Message, CleanStackTrace(e.StackTrace));
	}

	public void Audit(string format, params object[] args)
	{
		Log(EnumLogType.Audit, format, args);
	}

	public void Audit(string message)
	{
		Log(EnumLogType.Audit, message, _emptyArgs);
	}

	public void Worldgen(string format, params object[] args)
	{
		Log(EnumLogType.Worldgen, format, args);
	}

	public void Worldgen(Exception e)
	{
		Log(EnumLogType.Worldgen, "Exception: {0}\n{1}", e.Message, CleanStackTrace(e.StackTrace));
	}

	public void Worldgen(string message)
	{
		Log(EnumLogType.Worldgen, message, _emptyArgs);
	}
}
