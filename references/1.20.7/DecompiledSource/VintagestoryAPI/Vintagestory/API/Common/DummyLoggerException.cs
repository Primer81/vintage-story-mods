using System;

namespace Vintagestory.API.Common;

public class DummyLoggerException : Exception
{
	public DummyLoggerException(string message)
		: base(message)
	{
	}
}
