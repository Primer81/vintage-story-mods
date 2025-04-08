using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.Common.Network.Packets;
using Vintagestory.Server.Systems;

namespace Vintagestory.Server;

public class PhysicsManager : LoadBalancedTask
{
	public readonly ConcurrentQueue<IPhysicsTickable> toAdd = new ConcurrentQueue<IPhysicsTickable>();

	public readonly ConcurrentQueue<IPhysicsTickable> toRemove = new ConcurrentQueue<IPhysicsTickable>();

	private const float tickInterval = 1f / 30f;

	private readonly ICoreServerAPI sapi;

	private readonly ServerUdpNetwork udpNetwork;

	private readonly ServerMain server;

	private readonly LoadBalancer loadBalancer;

	private int maxPhysicsThreads;

	private int physicsTicknum;

	private readonly long listener;

	private float physicsTickAccum;

	private float attrUpdateAccum;

	private float trackingAccum;

	private readonly List<IPhysicsTickable> tickables = new List<IPhysicsTickable>();

	private ServerSystemEntitySimulation es;

	private CachingConcurrentDictionary<long, Entity> loadedEntities;

	private int udptick;

	private readonly List<Entity> toSendSpawn = new List<Entity>();

	private int currentTick;

	private static float rateModifier = 1f;

	private double[] positions;

	private EntityDespawnData outofRangeDespawnData = new EntityDespawnData
	{
		Reason = EnumDespawnReason.OutOfRange
	};

	private List<Packet_EntityAttributes> cliententitiesFullUpdate = new List<Packet_EntityAttributes>();

	private List<Packet_EntityAttributeUpdate> cliententitiesPartialUpdate = new List<Packet_EntityAttributeUpdate>();

	private List<Packet_EntityAttributes> cliententitiesDebugUpdate = new List<Packet_EntityAttributes>();

	private Dictionary<long, Packet_EntityAttributes> entitiesFullUpdate = new Dictionary<long, Packet_EntityAttributes>();

	private Dictionary<long, Packet_EntityAttributeUpdate> entitiesPartialUpdate = new Dictionary<long, Packet_EntityAttributeUpdate>();

	private Dictionary<long, Packet_EntityAttributes> entitiesDebugUpdate = new Dictionary<long, Packet_EntityAttributes>();

	public PhysicsManager(ICoreServerAPI sapi, ServerUdpNetwork udpNetwork)
	{
		this.sapi = sapi;
		this.udpNetwork = udpNetwork;
		maxPhysicsThreads = Math.Clamp(MagicNum.MaxPhysicsThreads, 1, 8);
		server = sapi.World as ServerMain;
		loadBalancer = new LoadBalancer(this, ServerMain.Logger);
		loadBalancer.CreateDedicatedThreads(maxPhysicsThreads, "physicsManager", server.Serverthreads);
		listener = server.RegisterGameTickListener(ServerTick, 1);
		loadedEntities = server.LoadedEntities;
		rateModifier = 1f;
		PhysicsBehaviorBase.InitServerMT(sapi);
	}

	public void Init()
	{
		es = server.Systems.First((ServerSystem s) => s is ServerSystemEntitySimulation) as ServerSystemEntitySimulation;
	}

	public void ServerTick(float dt)
	{
		ServerMain.FrameProfiler.Enter("physicsmanager-servertick");
		IPhysicsTickable addable;
		while (toAdd.Count > 0 && toAdd.TryDequeue(out addable))
		{
			if (addable != null)
			{
				tickables.Add(addable);
			}
		}
		IPhysicsTickable removable;
		while (toRemove.Count > 0 && toRemove.TryDequeue(out removable))
		{
			if (removable != null)
			{
				tickables.Remove(removable);
			}
		}
		physicsTickAccum += dt;
		if (physicsTickAccum > 0.4f)
		{
			int skippedTicks = (int)((physicsTickAccum - 0.4f) / (1f / 30f));
			if (ServerMain.FrameProfiler.Enabled)
			{
				ServerMain.Logger.Warning("Over 400ms tick. Skipping {0} physics ticks.", skippedTicks);
			}
			physicsTickAccum %= 0.4f;
		}
		ServerMain.FrameProfiler.Mark("physicsmanager-preparation");
		physicsTicknum = 0;
		while (physicsTickAccum > 1f / 30f)
		{
			physicsTickAccum -= 1f / 30f;
			TickFixedRate30TPS();
			physicsTicknum++;
		}
		float adjustedRate = (float)physicsTicknum * (1f / 30f) * rateModifier;
		foreach (IPhysicsTickable tickable in tickables)
		{
			try
			{
				tickable.AfterPhysicsTick(adjustedRate);
			}
			catch (Exception e)
			{
				ServerMain.Logger.Error(e);
			}
		}
		ServerMain.FrameProfiler.Mark("physicsmanager-afterphysicstick");
		attrUpdateAccum += dt;
		if ((double)attrUpdateAccum > 0.2)
		{
			UpdateTrackedEntitiesStates(server.Clients);
			attrUpdateAccum = 0f;
			SendAttributesViaTCP();
		}
		ServerMain.FrameProfiler.Mark("physicsmanager-send");
		SendEntitySpawns();
		ServerMain.FrameProfiler.Leave();
	}

	public void UpdateTrackedEntitiesStates(IDictionary<int, ConnectedClient> clients)
	{
		if (positions == null || positions.Length != clients.Count * 3)
		{
			positions = new double[clients.Count * 3];
		}
		int j = 0;
		foreach (ConnectedClient client3 in clients.Values)
		{
			if (client3.State != EnumClientState.Connected && client3.State != EnumClientState.Playing)
			{
				positions[j * 3] = double.MaxValue;
				positions[j * 3 + 1] = double.MaxValue;
				positions[j * 3 + 2] = double.MaxValue;
				j++;
			}
			else
			{
				EntityPos pos = client3.Position;
				positions[j * 3] = pos.X;
				positions[j * 3 + 1] = pos.Y;
				positions[j * 3 + 2] = pos.Z;
				j++;
			}
		}
		foreach (Entity entity in loadedEntities.Values)
		{
			double x = entity.ServerPos.X;
			double y = entity.ServerPos.Y;
			double z = entity.ServerPos.Z;
			double minrangeSq = double.MaxValue;
			double trackRange = Math.Max(es.trackingRangeSq, entity.SimulationRange * entity.SimulationRange);
			bool isTracked = entity.IsTracked > 0;
			int i = 0;
			foreach (ConnectedClient client2 in clients.Values)
			{
				double num = x - positions[i * 3];
				double dy = y - positions[i * 3 + 1];
				double dz = z - positions[i * 3 + 2];
				i++;
				double rangeSq = num * num + dy * dy + dz * dz;
				if (rangeSq < minrangeSq)
				{
					minrangeSq = rangeSq;
				}
				if ((!isTracked && rangeSq > trackRange) || (client2.State != EnumClientState.Connected && client2.State != EnumClientState.Playing))
				{
					continue;
				}
				bool trackedByClient = client2.TrackedEntities.ContainsKey(entity.EntityId);
				bool outofLoadedRange = !client2.DidSendChunk(entity.InChunkIndex3d) && entity.EntityId != client2.Player.Entity.EntityId && !entity.AllowOutsideLoadedRange;
				if (outofLoadedRange && !trackedByClient)
				{
					continue;
				}
				bool inRange = rangeSq < trackRange && !outofLoadedRange;
				if (trackedByClient || inRange)
				{
					if (trackedByClient && !inRange)
					{
						client2.TrackedEntities.Remove(entity.EntityId);
						client2.entitiesNowOutOfRange.Add(new EntityDespawn
						{
							ForClientId = client2.Id,
							DespawnData = outofRangeDespawnData,
							Entity = entity
						});
					}
					else if (!trackedByClient && inRange && client2.TrackedEntities.Count < MagicNum.TrackedEntitiesPerClient)
					{
						bool within50Blocks = rangeSq < 2500.0;
						client2.TrackedEntities.Add(entity.EntityId, within50Blocks);
						client2.entitiesNowInRange.Add(new EntityInRange
						{
							ForClientId = client2.Id,
							Entity = entity
						});
					}
				}
			}
			if (minrangeSq < trackRange)
			{
				entity.IsTracked = (byte)((minrangeSq >= 2500.0) ? 1 : 2);
			}
			else
			{
				entity.IsTracked = 0;
			}
		}
		using FastMemoryStream ms = new FastMemoryStream();
		foreach (ConnectedClient client in clients.Values)
		{
			if (client.entitiesNowInRange.Count > 0)
			{
				List<AnimationPacket> entityAnimPackets = new List<AnimationPacket>();
				foreach (EntityInRange nowInRange in client.entitiesNowInRange)
				{
					if (nowInRange.Entity is EntityPlayer entityPlayer)
					{
						server.PlayersByUid.TryGetValue(entityPlayer.PlayerUID, out var value);
						if (value != null)
						{
							server.SendPacket(nowInRange.ForClientId, ((ServerWorldPlayerData)value.WorldData).ToPacketForOtherPlayers(value));
						}
					}
					ms.Reset();
					BinaryWriter writer = new BinaryWriter(ms);
					server.SendPacket(nowInRange.ForClientId, ServerPackets.GetFullEntityPacket(nowInRange.Entity, ms, writer));
					entityAnimPackets.Add(new AnimationPacket(nowInRange.Entity));
				}
				BulkAnimationPacket bulkAnimationPacket = new BulkAnimationPacket
				{
					Packets = entityAnimPackets.ToArray()
				};
				udpNetwork.ServerNetworkChannel.SendPacket(bulkAnimationPacket, client.Player);
				client.entitiesNowInRange.Clear();
			}
			if (client.entitiesNowOutOfRange.Count > 0)
			{
				server.SendPacket(client.Id, ServerPackets.GetEntityDespawnPacket(client.entitiesNowOutOfRange));
				client.entitiesNowOutOfRange.Clear();
			}
		}
	}

	public void TickFixedRate30TPS()
	{
		_ = rateModifier;
		currentTick++;
		if (currentTick % 2 == 0)
		{
			udptick++;
			bool forceUpdate = udptick % 15 == 0;
			SendPositions(forceUpdate);
			ServerMain.FrameProfiler.Mark("physicsmanager-udp");
		}
		if (tickables.Count == 0)
		{
			return;
		}
		loadBalancer.SynchroniseWorkToMainThread(this);
		foreach (IPhysicsTickable tickable in tickables)
		{
			tickable.OnPhysicsTickDone();
		}
	}

	public void SendAttributesViaTCP()
	{
		entitiesFullUpdate.Clear();
		entitiesPartialUpdate.Clear();
		entitiesDebugUpdate.Clear();
		bool debugMode = server.Config.EntityDebugMode;
		FastMemoryStream ms = new FastMemoryStream();
		foreach (Entity entity in loadedEntities.Values)
		{
			if (entity.IsTracked != 0)
			{
				if (entity.WatchedAttributes.AllDirty)
				{
					ms.Reset();
					entitiesFullUpdate[entity.EntityId] = ServerPackets.GetEntityPacket(ms, entity);
				}
				else if (entity.WatchedAttributes.PartialDirty)
				{
					ms.Reset();
					entitiesPartialUpdate[entity.EntityId] = ServerPackets.GetEntityPartialAttributePacket(ms, entity);
				}
				if (debugMode && (entity.DebugAttributes.AllDirty || entity.DebugAttributes.PartialDirty))
				{
					ms.Reset();
					entitiesDebugUpdate[entity.EntityId] = ServerPackets.GetEntityDebugAttributePacket(ms, entity);
				}
			}
		}
		foreach (ConnectedClient client in server.Clients.Values)
		{
			if (client.State != EnumClientState.Connected && client.State != EnumClientState.Playing)
			{
				continue;
			}
			cliententitiesFullUpdate.Clear();
			cliententitiesPartialUpdate.Clear();
			cliententitiesDebugUpdate.Clear();
			foreach (KeyValuePair<long, bool> trackedEntity in client.TrackedEntities)
			{
				long entityId = trackedEntity.Key;
				if (entitiesFullUpdate.TryGetValue(entityId, out var pf))
				{
					cliententitiesFullUpdate.Add(pf);
				}
				if (entitiesPartialUpdate.TryGetValue(entityId, out var pp))
				{
					cliententitiesPartialUpdate.Add(pp);
				}
				if (debugMode && entitiesDebugUpdate.TryGetValue(entityId, out var pd))
				{
					cliententitiesDebugUpdate.Add(pd);
				}
			}
			if (entitiesFullUpdate.Count > 0 || entitiesPartialUpdate.Count > 0)
			{
				server.SendPacket(client.Id, ServerPackets.GetBulkEntityAttributesPacket(cliententitiesFullUpdate, cliententitiesPartialUpdate));
			}
			if (cliententitiesDebugUpdate.Count > 0)
			{
				server.SendPacket(client.Id, ServerPackets.GetBulkEntityDebugAttributesPacket(cliententitiesDebugUpdate));
			}
		}
		foreach (Entity value in loadedEntities.Values)
		{
			value.WatchedAttributes.MarkClean();
		}
	}

	public void SendPositions(bool forceUpdate)
	{
		Dictionary<long, Packet_EntityPosition> entityPositionPackets = new Dictionary<long, Packet_EntityPosition>();
		Dictionary<long, AnimationPacket> entityAnimPackets = new Dictionary<long, AnimationPacket>();
		foreach (Entity entity in loadedEntities.Values)
		{
			if (entity is EntityPlayer)
			{
				continue;
			}
			EntityAgent entityAgent = entity as EntityAgent;
			if (entity.IsTracked != 0)
			{
				if ((entity.AnimManager != null && entity.AnimManager.AnimationsDirty) || entity.IsTeleport)
				{
					entityAnimPackets[entity.EntityId] = new AnimationPacket(entity);
				}
				if (forceUpdate || !entity.ServerPos.BasicallySameAs(entity.PreviousServerPos) || (entityAgent != null && entityAgent.Controls.Dirty))
				{
					int tick = entity.Attributes.GetInt("tick");
					entityPositionPackets[entity.EntityId] = ServerPackets.getEntityPositionPacket(entity.ServerPos, entity, tick);
					entity.Attributes.SetInt("tick", tick + 1);
				}
			}
			if (entityAgent != null)
			{
				entityAgent.Controls.Dirty = false;
			}
			if (entity.AnimManager != null)
			{
				entity.AnimManager.AnimationsDirty = false;
			}
			entity.PreviousServerPos.SetFrom(entity.ServerPos);
			entity.IsTeleport = false;
		}
		List<Packet_EntityPosition> positionUpdate = new List<Packet_EntityPosition>();
		List<AnimationPacket> animationUpdate = new List<AnimationPacket>();
		foreach (ConnectedClient client in server.Clients.Values)
		{
			if (client.State != EnumClientState.Connected && client.State != EnumClientState.Playing)
			{
				continue;
			}
			positionUpdate.Clear();
			animationUpdate.Clear();
			foreach (long id in client.TrackedEntities.Keys)
			{
				if (entityPositionPackets.TryGetValue(id, out var pu))
				{
					positionUpdate.Add(pu);
				}
				if (entityAnimPackets.TryGetValue(id, out var au))
				{
					animationUpdate.Add(au);
				}
			}
			if (positionUpdate.Count > 8 && !client.IsSinglePlayerClient && !client.FallBackToTcp)
			{
				for (int i = 0; i < positionUpdate.Count; i += 8)
				{
					Packet_EntityPosition[] chunk = positionUpdate.Skip(i).Take(8).ToArray();
					Packet_BulkEntityPosition bulkPositionPacket = new Packet_BulkEntityPosition();
					bulkPositionPacket.SetEntityPositions(chunk);
					udpNetwork.SendBulkPositionPacket(bulkPositionPacket, client.Player);
				}
			}
			else if (positionUpdate.Count > 0)
			{
				Packet_BulkEntityPosition bulkPositionPacket2 = new Packet_BulkEntityPosition();
				bulkPositionPacket2.SetEntityPositions(positionUpdate.ToArray());
				udpNetwork.SendBulkPositionPacket(bulkPositionPacket2, client.Player);
			}
			if (animationUpdate.Count > 0)
			{
				BulkAnimationPacket bulkAnimationPacket = new BulkAnimationPacket
				{
					Packets = animationUpdate.ToArray()
				};
				udpNetwork.ServerNetworkChannel.SendPacket(bulkAnimationPacket, client.Player);
			}
		}
	}

	public void SendEntitySpawns()
	{
		float adjustedRate = 1f / 30f * rateModifier;
		lock (server.EntitySpawnSendQueue)
		{
			if (server.EntitySpawnSendQueue.Count <= 0)
			{
				return;
			}
			int squareDistance = MagicNum.DefaultEntityTrackingRange * MagicNum.ServerChunkSize * MagicNum.DefaultEntityTrackingRange * MagicNum.ServerChunkSize;
			try
			{
				foreach (ConnectedClient client2 in server.Clients.Values)
				{
					if ((client2.State != EnumClientState.Connected && client2.State != EnumClientState.Playing) || client2.Entityplayer == null)
					{
						continue;
					}
					foreach (Entity entity3 in server.EntitySpawnSendQueue)
					{
						if (entity3.ServerPos.InRangeOf(client2.Entityplayer.ServerPos, squareDistance))
						{
							client2.TrackedEntities[entity3.EntityId] = true;
							toSendSpawn.Add(entity3);
						}
					}
					if (toSendSpawn.Count > 0)
					{
						server.SendPacket(client2.Id, ServerPackets.GetEntitySpawnPacket(toSendSpawn));
					}
					toSendSpawn.Clear();
				}
				foreach (Entity entity2 in server.EntitySpawnSendQueue)
				{
					entity2.packet = null;
					foreach (EntityBehavior behavior in entity2.SidedProperties.Behaviors)
					{
						if (behavior is IPhysicsTickable tickable)
						{
							tickable.Ticking = true;
							tickable.OnPhysicsTick(adjustedRate);
							tickable.OnPhysicsTick(adjustedRate);
							tickable.AfterPhysicsTick(adjustedRate);
							break;
						}
					}
					entity2.Attributes.SetInt("tick", 2);
				}
				foreach (ConnectedClient client in server.Clients.Values)
				{
					if ((client.State != EnumClientState.Connected && client.State != EnumClientState.Playing) || client.Entityplayer == null)
					{
						continue;
					}
					foreach (Entity entity in server.EntitySpawnSendQueue)
					{
						if (entity.ServerPos.InRangeOf(client.Entityplayer.ServerPos, squareDistance))
						{
							Packet_EntityPosition posPacket = ServerPackets.getEntityPositionPacket(entity.ServerPos, entity, 1);
							Packet_Server packet = new Packet_Server
							{
								Id = 80,
								EntityPosition = posPacket
							};
							server.SendPacket(client.Id, packet);
						}
					}
				}
				server.EntitySpawnSendQueue.Clear();
			}
			catch (Exception e)
			{
				ServerMain.Logger.Error(e);
			}
		}
	}

	public void DoWork(int threadNumber)
	{
		float adjustedRate = 1f / 30f * rateModifier;
		List<IPhysicsTickable> tickables = this.tickables;
		FrameProfilerUtil frameProfiler = null;
		if (threadNumber == 1)
		{
			frameProfiler = ServerMain.FrameProfiler;
			if (frameProfiler == null)
			{
				throw new Exception("FrameProfiler on main thread was null - this should be impossible!");
			}
			if (!frameProfiler.Enabled)
			{
				frameProfiler = null;
			}
		}
		if (maxPhysicsThreads == 1)
		{
			if (frameProfiler == null)
			{
				try
				{
					foreach (IPhysicsTickable item in tickables)
					{
						item.OnPhysicsTick(adjustedRate);
					}
					return;
				}
				catch (Exception e)
				{
					ServerMain.Logger.Error("Error while enumerating tickables");
					ServerMain.Logger.Error("Tickables total count is " + tickables.Count);
					ServerMain.Logger.Error(e);
					int k = 0;
					foreach (IPhysicsTickable tickable in tickables)
					{
						try
						{
							tickable.OnPhysicsTick(adjustedRate);
						}
						catch (Exception ex)
						{
							if (tickable == null)
							{
								throw new Exception("tickable " + k + " was null, that shouldn't happen");
							}
							ServerMain.Logger.Error("The error is in a " + ((EntityBehavior)tickable).entity.Code.ToShortString());
							throw ex;
						}
						k++;
					}
					ServerMain.Logger.Error("The error did not recur when running the same code again!");
					return;
				}
			}
			try
			{
				frameProfiler.Enter("entityphysics-mainthread (" + tickables.Count + " entities, single-threaded)");
				foreach (IPhysicsTickable item2 in tickables)
				{
					item2.OnPhysicsTick(adjustedRate);
					frameProfiler.Mark("physicstick-oneentity");
				}
				frameProfiler.Leave();
				return;
			}
			catch (Exception ex2)
			{
				ServerMain.Logger.Error("Error while enumerating tickables with profiling");
				throw ex2;
			}
		}
		int count = tickables.Count;
		frameProfiler?.Enter("entityphysics-mainthread (" + count + " entities across all threads) (physics tick " + physicsTicknum + ")");
		int startpos = count * (threadNumber - 1) / maxPhysicsThreads;
		for (int j = startpos; j < count; j++)
		{
			IPhysicsTickable tickable3 = tickables[j];
			if (!tickable3.CanProceedOnThisThread())
			{
				break;
			}
			tickable3.OnPhysicsTick(adjustedRate);
			frameProfiler?.Mark("physicstick-oneentity");
		}
		if (startpos == 0)
		{
			startpos = count;
		}
		for (int i = startpos - 1; i >= 0; i--)
		{
			IPhysicsTickable tickable2 = tickables[i];
			if (!tickable2.CanProceedOnThisThread())
			{
				break;
			}
			tickable2.OnPhysicsTick(adjustedRate);
			frameProfiler?.Mark("physicstick-oneentity");
		}
		frameProfiler?.Leave();
	}

	public bool ShouldExit()
	{
		return server.stopped;
	}

	public void HandleException(Exception e)
	{
		ServerMain.Logger.Error("Error thrown while ticking physics:\n{0}\n{1}", e.Message, e.StackTrace);
	}

	public void StartWorkerThread(int threadNum)
	{
		try
		{
			while (tickables.Count < 120)
			{
				if (ShouldExit())
				{
					return;
				}
				Thread.Sleep(15);
			}
		}
		catch (Exception)
		{
		}
		server.EventManager.TriggerPhysicsThreadStart();
		loadBalancer.WorkerThreadLoop(this, threadNum);
	}

	public void Dispose()
	{
		server?.UnregisterGameTickListener(listener);
		tickables.Clear();
	}
}
