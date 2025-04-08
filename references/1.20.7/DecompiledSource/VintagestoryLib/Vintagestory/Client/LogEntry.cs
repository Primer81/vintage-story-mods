using Vintagestory.API.Common;

namespace Vintagestory.Client;

internal class LogEntry
{
	public EnumAppSide Side;

	public EnumLogType Logtype;

	public string Message;

	public object[] args;
}
