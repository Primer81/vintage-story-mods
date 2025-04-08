using Vintagestory.API.Client;

namespace Vintagestory.API.Common;

public interface INetworkAPI
{
	/// <summary>
	/// Supplies you with your very own and personal network channel with which you can send packets to the server. Use the same channelName on the client and server to have them link up.
	/// </summary>
	/// <param name="channelName">Unique channel identifier</param>
	/// <returns></returns>
	INetworkChannel RegisterChannel(string channelName);

	/// <summary>
	/// Supplies you with your very own and personal network channel with which you can send packets to the server. Use the same channelName on the client and server to have them link up.
	/// Do not send larger messages then 508 bytes since some clients may be behind NAT/firwalls that may drop your packets if they get fragmented
	/// </summary>
	/// <param name="channelName">Unique channel identifier</param>
	/// <returns></returns>
	INetworkChannel RegisterUdpChannel(string channelName);

	/// <summary>
	/// Returns a previously registered channeled, null otherwise
	/// </summary>
	/// <param name="channelName"></param>
	/// <returns></returns>
	INetworkChannel GetChannel(string channelName);

	/// <summary>
	/// Returns a previously registered channeled, null otherwise
	/// </summary>
	/// <param name="channelName"></param>
	/// <returns></returns>
	INetworkChannel GetUdpChannel(string channelName);
}
