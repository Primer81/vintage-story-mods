using System;

namespace Vintagestory.API.Client;

/// <summary>
/// Represent a custom network channel for sending messages between client and server
/// </summary>
public interface IClientNetworkChannel : INetworkChannel
{
	/// <summary>
	/// True if the server is listening on this channel
	/// </summary>
	bool Connected { get; }

	/// <summary>
	/// Registers a handler for when you send a packet with given messageId. Must be registered in the same order as on the server.
	/// </summary>
	/// <param name="type"></param>
	new IClientNetworkChannel RegisterMessageType(Type type);

	/// <summary>
	/// Registers a handler for when you send a packet with given messageId. Must be registered in the same order as on the server.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	new IClientNetworkChannel RegisterMessageType<T>();

	/// <summary>
	/// Registers a handler for when you send a packet with given messageId
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="handler"></param>
	IClientNetworkChannel SetMessageHandler<T>(NetworkServerMessageHandler<T> handler);

	/// <summary>
	/// Sends a packet to the server
	/// </summary>
	/// <param name="message"></param>
	void SendPacket<T>(T message);
}
