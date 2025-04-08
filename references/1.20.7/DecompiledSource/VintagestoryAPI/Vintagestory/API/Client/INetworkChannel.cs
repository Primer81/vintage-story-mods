using System;

namespace Vintagestory.API.Client;

public interface INetworkChannel
{
	/// <summary>
	/// The channel name this channel was registered with
	/// </summary>
	string ChannelName { get; }

	/// <summary>
	/// Registers a handler for when you send a packet with given messageId
	/// </summary>
	/// <param name="type"></param>
	INetworkChannel RegisterMessageType(Type type);

	/// <summary>
	/// Registers a handler for when you send a packet with given messageId
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	INetworkChannel RegisterMessageType<T>();
}
