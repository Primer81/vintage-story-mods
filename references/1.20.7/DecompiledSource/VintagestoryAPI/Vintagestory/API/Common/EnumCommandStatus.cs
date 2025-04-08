namespace Vintagestory.API.Common;

public enum EnumCommandStatus
{
	NoSuchCommand,
	Success,
	/// <summary>
	/// Command cannot execute at this point, likely doing an async call. Prints no output. 
	/// </summary>
	Deferred,
	/// <summary>
	/// The command encountered an issue
	/// </summary>
	Error,
	/// <summary>
	/// Command status is unknown because this is a legacy command using the old method of registering commands
	/// </summary>
	UnknownLegacy
}
