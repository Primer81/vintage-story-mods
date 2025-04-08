namespace Vintagestory.API.Server;

/// <summary>
/// The current connection state of a player thats currently connecting to the server
/// </summary>
public enum EnumClientState
{
	Offline,
	Connecting,
	Connected,
	Playing,
	Queued
}
