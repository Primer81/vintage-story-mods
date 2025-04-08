using Vintagestory.API.Common;

namespace Vintagestory.Common;

public class ModLogger : LoggerBase
{
	public ILogger Parent { get; }

	public ModContainer Mod { get; }

	public ModLogger(ILogger parent, ModContainer mod)
	{
		Parent = parent;
		Mod = mod;
	}

	protected override void LogImpl(EnumLogType logType, string message, params object[] args)
	{
		Parent.Log(logType, "[" + (Mod.Info?.ModID ?? Mod.FileName) + "] " + message, args);
	}
}
