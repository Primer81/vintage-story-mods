using System;

namespace Vintagestory.API.Common;

[Flags]
public enum EnumAppSide
{
	Server = 1,
	Client = 2,
	Universal = 3
}
