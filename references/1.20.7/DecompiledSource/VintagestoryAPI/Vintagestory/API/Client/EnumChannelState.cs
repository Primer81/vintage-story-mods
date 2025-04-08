namespace Vintagestory.API.Client;

/// <summary>
/// The state of a network channel
/// </summary>
public enum EnumChannelState
{
	/// <summary>
	/// No such channel was registered
	/// </summary>
	NotFound,
	/// <summary>
	/// This channel has been registered but he server did not send the server channel information yet
	/// </summary>
	Registered,
	/// <summary>
	/// This channel has been registered client and server side. It is ready to send and receive messages
	/// </summary>
	Connected,
	/// <summary>
	/// This channel has been registered only client side. You cannot send data on this channel
	/// </summary>
	NotConnected
}
