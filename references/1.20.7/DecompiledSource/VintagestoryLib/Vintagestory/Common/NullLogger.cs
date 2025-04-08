using Vintagestory.API.Common;

namespace Vintagestory.Common;

public class NullLogger : Logger
{
	public NullLogger()
		: base("Null", clearOldFiles: true, 10, 1024)
	{
	}

	public override string getLogFile(EnumLogType logType)
	{
		return null;
	}

	public override bool printToConsole(EnumLogType logType)
	{
		return false;
	}

	public override bool printToDebugWindow(EnumLogType logType)
	{
		return false;
	}
}
