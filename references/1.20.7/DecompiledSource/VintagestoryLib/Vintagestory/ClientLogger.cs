using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace Vintagestory;

public class ClientLogger : Logger
{
	public ClientLogger()
		: base("Client", clearOldFiles: true, ClientSettings.ArchiveLogFileCount, ClientSettings.ArchiveLogFileMaxSizeMb)
	{
	}

	public override string getLogFile(EnumLogType logType)
	{
		switch (logType)
		{
		case EnumLogType.Audit:
			return Path.Combine(GamePaths.Logs, "client-audit.log");
		case EnumLogType.Chat:
			return Path.Combine(GamePaths.Logs, "client-chat.log");
		case EnumLogType.VerboseDebug:
		case EnumLogType.Debug:
			return Path.Combine(GamePaths.Logs, "client-debug.log");
		default:
			return Path.Combine(GamePaths.Logs, "client-main.log");
		}
	}

	public override bool printToConsole(EnumLogType logType)
	{
		if (logType != EnumLogType.VerboseDebug)
		{
			return logType != EnumLogType.StoryEvent;
		}
		return false;
	}

	public override bool printToDebugWindow(EnumLogType logType)
	{
		if (logType != EnumLogType.VerboseDebug)
		{
			return logType != EnumLogType.StoryEvent;
		}
		return false;
	}
}
