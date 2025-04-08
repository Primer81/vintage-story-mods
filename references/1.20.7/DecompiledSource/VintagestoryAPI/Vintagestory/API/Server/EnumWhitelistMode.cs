namespace Vintagestory.API.Server;

public enum EnumWhitelistMode
{
	/// <summary>
	/// Singleplayer OpenToLan: All players can join, Dedicated server: Only whitelisted players can join
	/// </summary>
	Default,
	/// <summary>
	/// All players can join 
	/// </summary>
	Off,
	/// <summary>
	/// Only whitelisted players can join
	/// </summary>
	On
}
