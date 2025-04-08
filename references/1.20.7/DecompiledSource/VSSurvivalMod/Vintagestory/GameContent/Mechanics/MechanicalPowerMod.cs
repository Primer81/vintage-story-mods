using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent.Mechanics;

public class MechanicalPowerMod : ModSystem, IRenderer, IDisposable
{
	public MechNetworkRenderer Renderer;

	private ICoreClientAPI capi;

	private ICoreServerAPI sapi;

	private IClientNetworkChannel clientNwChannel;

	private IServerNetworkChannel serverNwChannel;

	public ICoreAPI Api;

	private MechPowerData data = new MechPowerData();

	private bool allNetworksFullyLoaded = true;

	private List<MechanicalNetwork> nowFullyLoaded = new List<MechanicalNetwork>();

	public double RenderOrder => 0.0;

	public int RenderRange => 9999;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		base.Start(api);
		Api = api;
		if (api.World is IClientWorldAccessor)
		{
			(api as ICoreClientAPI).Event.RegisterRenderer(this, EnumRenderStage.Before, "mechanicalpowertick");
			clientNwChannel = ((ICoreClientAPI)api).Network.RegisterChannel("vsmechnetwork").RegisterMessageType(typeof(MechNetworkPacket)).RegisterMessageType(typeof(NetworkRemovedPacket))
				.RegisterMessageType(typeof(MechClientRequestPacket))
				.SetMessageHandler<MechNetworkPacket>(OnPacket)
				.SetMessageHandler<NetworkRemovedPacket>(OnNetworkRemovePacket);
		}
		else
		{
			api.World.RegisterGameTickListener(OnServerGameTick, 20);
			serverNwChannel = ((ICoreServerAPI)api).Network.RegisterChannel("vsmechnetwork").RegisterMessageType(typeof(MechNetworkPacket)).RegisterMessageType(typeof(NetworkRemovedPacket))
				.RegisterMessageType(typeof(MechClientRequestPacket))
				.SetMessageHandler<MechClientRequestPacket>(OnClientRequestPacket);
		}
	}

	public long getTickNumber()
	{
		return data.tickNumber;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		base.StartClientSide(api);
		capi = api;
		api.Event.BlockTexturesLoaded += onLoaded;
		api.Event.LeaveWorld += delegate
		{
			Renderer?.Dispose();
		};
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		base.StartServerSide(api);
		api.Event.SaveGameLoaded += Event_SaveGameLoaded;
		api.Event.GameWorldSave += Event_GameWorldSave;
		api.Event.ChunkDirty += Event_ChunkDirty;
	}

	protected void OnServerGameTick(float dt)
	{
		data.tickNumber++;
		foreach (MechanicalNetwork network in data.networksById.Values.ToList())
		{
			if (network.fullyLoaded && network.nodes.Count > 0)
			{
				network.ServerTick(dt, data.tickNumber);
			}
		}
	}

	protected void OnPacket(MechNetworkPacket networkMessage)
	{
		bool isNew = !data.networksById.ContainsKey(networkMessage.networkId);
		GetOrCreateNetwork(networkMessage.networkId).UpdateFromPacket(networkMessage, isNew);
	}

	protected void OnNetworkRemovePacket(NetworkRemovedPacket networkMessage)
	{
		data.networksById.Remove(networkMessage.networkId);
	}

	protected void OnClientRequestPacket(IServerPlayer player, MechClientRequestPacket networkMessage)
	{
		if (data.networksById.TryGetValue(networkMessage.networkId, out var nw))
		{
			nw.SendBlocksUpdateToClient(player);
		}
	}

	public void broadcastNetwork(MechNetworkPacket packet)
	{
		serverNwChannel.BroadcastPacket(packet);
	}

	private void Event_GameWorldSave()
	{
	}

	private void Event_SaveGameLoaded()
	{
		data = new MechPowerData();
	}

	private void onLoaded()
	{
		Renderer = new MechNetworkRenderer(capi, this);
	}

	internal void OnNodeRemoved(IMechanicalPowerDevice device)
	{
		if (device.Network != null)
		{
			RebuildNetwork(device.Network, device);
		}
	}

	public void RebuildNetwork(MechanicalNetwork network, IMechanicalPowerDevice nowRemovedNode = null)
	{
		network.Valid = false;
		if (Api.Side == EnumAppSide.Server)
		{
			DeleteNetwork(network);
		}
		if (network.nodes.Values.Count == 0)
		{
			return;
		}
		IMechanicalPowerNode[] nnodes = network.nodes.Values.ToArray();
		IMechanicalPowerNode[] array = nnodes;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].LeaveNetwork();
		}
		array = nnodes;
		foreach (IMechanicalPowerNode nnode in array)
		{
			if (!(nnode is IMechanicalPowerDevice))
			{
				continue;
			}
			IMechanicalPowerDevice newnode = Api.World.BlockAccessor.GetBlockEntity((nnode as IMechanicalPowerDevice).Position)?.GetBehavior<BEBehaviorMPBase>();
			if (newnode == null)
			{
				continue;
			}
			BlockFacing oldTurnDir = newnode.GetPropagationDirection();
			if (newnode.OutFacingForNetworkDiscovery != null && (nowRemovedNode == null || newnode.Position != nowRemovedNode.Position))
			{
				MechanicalNetwork newnetwork = newnode.CreateJoinAndDiscoverNetwork(newnode.OutFacingForNetworkDiscovery);
				bool reversed = newnode.GetPropagationDirection() == oldTurnDir.Opposite;
				newnetwork.Speed = (reversed ? (0f - network.Speed) : network.Speed);
				newnetwork.AngleRad = network.AngleRad;
				newnetwork.TotalAvailableTorque = (reversed ? (0f - network.TotalAvailableTorque) : network.TotalAvailableTorque);
				newnetwork.NetworkResistance = network.NetworkResistance;
				if (Api.Side == EnumAppSide.Server)
				{
					newnetwork.broadcastData();
				}
			}
		}
	}

	public void RemoveDeviceForRender(IMechanicalPowerRenderable device)
	{
		Renderer?.RemoveDevice(device);
	}

	public void AddDeviceForRender(IMechanicalPowerRenderable device)
	{
		Renderer?.AddDevice(device);
	}

	public override void Dispose()
	{
		base.Dispose();
		Renderer?.Dispose();
	}

	public MechanicalNetwork GetOrCreateNetwork(long networkId)
	{
		if (!data.networksById.TryGetValue(networkId, out var mw))
		{
			mw = (data.networksById[networkId] = new MechanicalNetwork(this, networkId));
		}
		testFullyLoaded(mw);
		return mw;
	}

	public void testFullyLoaded(MechanicalNetwork mw)
	{
		if (Api.Side == EnumAppSide.Server && !mw.fullyLoaded)
		{
			mw.fullyLoaded = mw.testFullyLoaded(Api);
			allNetworksFullyLoaded &= mw.fullyLoaded;
		}
	}

	private void Event_ChunkDirty(Vec3i chunkCoord, IWorldChunk chunk, EnumChunkDirtyReason reason)
	{
		if (allNetworksFullyLoaded || reason == EnumChunkDirtyReason.MarkedDirty)
		{
			return;
		}
		allNetworksFullyLoaded = true;
		nowFullyLoaded.Clear();
		foreach (MechanicalNetwork network in data.networksById.Values)
		{
			if (network.fullyLoaded)
			{
				continue;
			}
			allNetworksFullyLoaded = false;
			if (network.inChunks.ContainsKey(chunkCoord))
			{
				testFullyLoaded(network);
				if (network.fullyLoaded)
				{
					nowFullyLoaded.Add(network);
				}
			}
		}
		for (int i = 0; i < nowFullyLoaded.Count; i++)
		{
			RebuildNetwork(nowFullyLoaded[i]);
		}
	}

	public MechanicalNetwork CreateNetwork(IMechanicalPowerDevice powerProducerNode)
	{
		MechanicalNetwork nw = new MechanicalNetwork(this, data.nextNetworkId);
		nw.fullyLoaded = true;
		data.networksById[data.nextNetworkId] = nw;
		data.nextNetworkId++;
		return nw;
	}

	public void DeleteNetwork(MechanicalNetwork network)
	{
		data.networksById.Remove(network.networkId);
		serverNwChannel.BroadcastPacket(new NetworkRemovedPacket
		{
			networkId = network.networkId
		});
	}

	public void SendNetworkBlocksUpdateRequestToServer(long networkId)
	{
		clientNwChannel.SendPacket(new MechClientRequestPacket
		{
			networkId = networkId
		});
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (capi.IsGamePaused)
		{
			return;
		}
		foreach (MechanicalNetwork value in data.networksById.Values)
		{
			value.ClientTick(deltaTime);
		}
	}
}
