using System;
using System.Collections.Generic;
using Vintagestory.API.Client;

namespace Vintagestory.Common;

public abstract class NetworkChannelBase : INetworkChannel
{
	internal int channelId;

	internal string channelName;

	internal int nextHandlerId;

	internal Dictionary<Type, int> messageTypes = new Dictionary<Type, int>();

	public string ChannelName => channelName;

	public NetworkChannelBase(int channelId, string channelName)
	{
		this.channelId = channelId;
		this.channelName = channelName;
	}

	public INetworkChannel RegisterMessageType(Type type)
	{
		messageTypes[type] = nextHandlerId++;
		return this;
	}

	public INetworkChannel RegisterMessageType<T>()
	{
		messageTypes[typeof(T)] = nextHandlerId++;
		return this;
	}
}
