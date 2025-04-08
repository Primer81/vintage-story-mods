using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common.Database;

namespace Vintagestory.Server;

internal class ServerSystemSendChunks : ServerSystem
{
	private Packet_ServerChunk[] chunkPackets = new Packet_ServerChunk[2048];

	private int chunksSent;

	private FastList<ServerChunkWithCoord> chunksToSend = new FastList<ServerChunkWithCoord>();

	private FastList<ServerMapChunkWithCoord> mapChunksToSend = new FastList<ServerMapChunkWithCoord>();

	private FastList<long> toRemove = new FastList<long>();

	public override void Dispose()
	{
		chunkPackets = null;
		chunksToSend = null;
		mapChunksToSend = null;
		toRemove = null;
	}

	public override int GetUpdateInterval()
	{
		if (!server.IsDedicatedServer)
		{
			return 0;
		}
		return MagicNum.ChunkRequestTickTime;
	}

	public ServerSystemSendChunks(ServerMain server)
		: base(server)
	{
		server.clientAwarenessEvents[EnumClientAwarenessEvent.ChunkTransition].Add(OnClientLeaveChunk);
	}

	public override void OnServerTick(float dt)
	{
		if (server.RunPhase != EnumServerRunPhase.RunGame)
		{
			return;
		}
		IPlayer[] onlinePlayers = server.AllOnlinePlayers;
		foreach (IMiniDimension value in server.LoadedMiniDimensions.Values)
		{
			value.CollectChunksForSending(onlinePlayers);
		}
		foreach (ConnectedClient client in server.Clients.Values)
		{
			if (client.State == EnumClientState.Connected || client.State == EnumClientState.Playing)
			{
				sendAndEnqueueChunks(client);
			}
		}
	}

	private void OnClientLeaveChunk(ClientStatistics clientstats)
	{
		clientstats.client.CurrentChunkSentRadius = 0;
	}

	private void sendAndEnqueueChunks(ConnectedClient client)
	{
		int desiredRadius = (int)Math.Ceiling((float)client.WorldData.Viewdistance / (float)MagicNum.ServerChunkSize);
		int finalChunkRadius = Math.Min(server.Config.MaxChunkRadius, desiredRadius);
		if (client.CurrentChunkSentRadius > finalChunkRadius && client.forceSendChunks.Count == 0 && client.forceSendMapChunks.Count == 0)
		{
			return;
		}
		chunksToSend.Clear();
		mapChunksToSend.Clear();
		toRemove.Clear();
		int countChunks = MagicNum.ChunksToSendPerTick * ((!client.IsLocalConnection) ? 1 : 8);
		List<long> unsentMapChunks = new List<long>(1);
		foreach (long index2d2 in client.forceSendMapChunks)
		{
			Vec2i pos2 = server.WorldMap.MapChunkPosFromChunkIndex2D(index2d2);
			ServerMapChunk mpc = null;
			server.loadedMapChunks.TryGetValue(index2d2, out mpc);
			if (mpc != null)
			{
				server.SendPacketFast(client.Id, mpc.ToPacket(pos2.X, pos2.Y));
			}
			else
			{
				unsentMapChunks.Add(index2d2);
			}
		}
		client.forceSendMapChunks.Clear();
		foreach (long index2d in unsentMapChunks)
		{
			client.forceSendMapChunks.Add(index2d);
		}
		foreach (long index3d in client.forceSendChunks)
		{
			ServerChunk chunk = server.GetLoadedChunk(index3d);
			if (chunk != null)
			{
				if (countChunks <= 0)
				{
					break;
				}
				ChunkPos pos = server.WorldMap.ChunkPosFromChunkIndex3D(index3d);
				chunksToSend.Add(new ServerChunkWithCoord
				{
					chunk = chunk,
					pos = pos,
					withEntities = true
				});
				countChunks--;
				toRemove.Add(index3d);
			}
		}
		foreach (long sentChunk in toRemove)
		{
			client.forceSendChunks.Remove(sentChunk);
		}
		if (countChunks > 0 && server.SendChunks && client.CurrentChunkSentRadius < finalChunkRadius && loadSendableChunksAtCurrentRadius(client, countChunks, client.Player.Entity.Pos.Dimension) == 0 && ++client.CurrentChunkSentRadius <= finalChunkRadius && loadSendableChunksAtCurrentRadius(client, countChunks, client.Player.Entity.Pos.Dimension) == 0)
		{
			client.CurrentChunkSentRadius++;
		}
		foreach (ServerMapChunkWithCoord req2 in mapChunksToSend)
		{
			int chunkX = req2.chunkX;
			int chunkZ = req2.chunkZ;
			int regionX = chunkX / MagicNum.ChunkRegionSizeInChunks;
			int regionZ = chunkZ / MagicNum.ChunkRegionSizeInChunks;
			if (!client.DidSendMapRegion(server.WorldMap.MapRegionIndex2D(regionX, regionZ)))
			{
				server.SendPacketFast(client.Id, req2.mapchunk.MapRegion.ToPacket(regionX, regionZ));
				client.SetMapRegionSent(server.WorldMap.MapRegionIndex2D(regionX, regionZ));
			}
			for (int dx = -1; dx <= 1; dx++)
			{
				for (int dz = -1; dz <= 1; dz++)
				{
					int nRegionX = regionX + dx;
					int nRegionZ = regionZ + dz;
					long nindex2d = server.WorldMap.MapRegionIndex2D(nRegionX, nRegionZ);
					if (!client.DidSendMapRegion(nindex2d) && server.loadedMapRegions.TryGetValue(nindex2d, out var region))
					{
						server.SendPacketFast(client.Id, region.ToPacket(nRegionX, nRegionZ));
						client.SetMapRegionSent(nindex2d);
					}
				}
			}
			server.SendPacketFast(client.Id, req2.mapchunk.ToPacket(chunkX, chunkZ));
			client.SetMapChunkSent(server.WorldMap.MapChunkIndex2D(chunkX, chunkZ));
		}
		int cnt = 0;
		foreach (ServerChunkWithCoord req in chunksToSend)
		{
			chunkPackets[cnt++] = collectChunk(req.chunk, req.pos.X, req.pos.Y + req.pos.Dimension * 1024, req.pos.Z, client, req.withEntities);
			if (cnt >= 2048)
			{
				Packet_ServerChunks packet2 = new Packet_ServerChunks();
				packet2.SetChunks(chunkPackets, cnt, cnt);
				server.SendPacketFast(client.Id, new Packet_Server
				{
					Id = 10,
					Chunks = packet2
				});
				cnt = 0;
			}
		}
		if (cnt > 0)
		{
			Packet_ServerChunks packet = new Packet_ServerChunks();
			packet.SetChunks(chunkPackets, cnt, cnt);
			server.SendPacketFast(client.Id, new Packet_Server
			{
				Id = 10,
				Chunks = packet
			});
		}
	}

	private int loadSendableChunksAtCurrentRadius(ConnectedClient client, int countChunks, int dimension)
	{
		int requestChunkColumns = MagicNum.ChunksColumnsToRequestPerTick * ((!client.IsLocalConnection) ? 1 : 4);
		Vec2i[] points = ShapeUtil.GetOctagonPoints((int)client.Position.X / MagicNum.ServerChunkSize, (int)client.Position.Z / MagicNum.ServerChunkSize, client.CurrentChunkSentRadius);
		int sentOrRequested = 0;
		int offsetY = dimension * 1024;
		for (int i = 0; i < points.Length; i++)
		{
			int chunkX = points[i].X;
			int chunkZ = points[i].Y;
			bool mapChunkAdded = false;
			long index2d = server.WorldMap.MapChunkIndex2D(chunkX, chunkZ);
			if (!server.WorldMap.IsValidChunkPos(chunkX, offsetY, chunkZ))
			{
				continue;
			}
			for (int chunkY = 0; chunkY < server.WorldMap.ChunkMapSizeY; chunkY++)
			{
				long index3d = server.WorldMap.ChunkIndex3D(chunkX, chunkY + offsetY, chunkZ);
				if (client.DidSendChunk(index3d) || toRemove.Contains(index3d))
				{
					continue;
				}
				ServerChunk chunk = server.GetLoadedChunk(index3d);
				if (chunk != null)
				{
					if (countChunks > 0)
					{
						chunksToSend.Add(new ServerChunkWithCoord
						{
							chunk = chunk,
							pos = new ChunkPos(chunkX, chunkY, chunkZ, dimension)
						});
						countChunks--;
						if (!mapChunkAdded)
						{
							if (!client.DidSendMapChunk(index2d))
							{
								mapChunksToSend.Add(new ServerMapChunkWithCoord
								{
									chunkX = chunkX,
									chunkZ = chunkZ,
									mapchunk = (chunk.MapChunk as ServerMapChunk),
									index2d = index2d
								});
							}
							mapChunkAdded = true;
						}
					}
					sentOrRequested++;
				}
				else
				{
					if (requestChunkColumns <= 0)
					{
						continue;
					}
					if (!server.ChunkColumnRequested.ContainsKey(index2d) && server.AutoGenerateChunks)
					{
						server.ChunkColumnRequested[index2d] = 1;
						lock (server.requestedChunkColumnsLock)
						{
							server.requestedChunkColumns.Enqueue(index2d);
						}
						requestChunkColumns--;
					}
					sentOrRequested++;
				}
			}
		}
		return sentOrRequested;
	}

	private Packet_ServerChunk collectChunk(ServerChunk serverChunk, int chunkX, int chunkY, int chunkZ, ConnectedClient client, bool withEntities)
	{
		client.SetChunkSent(server.WorldMap.ChunkIndex3D(chunkX, chunkY, chunkZ));
		chunksSent++;
		return serverChunk.ToPacket(chunkX, chunkY, chunkZ, withEntities);
	}

	public static string performanceTest(ServerMain server)
	{
		Stopwatch stopwatch = null;
		int iterations = 5;
		int affinityMask = 1023;
		int cx = 15650;
		int cz = 15640;
		int cy = 3;
		Process proc = Process.GetCurrentProcess();
		if (RuntimeEnv.OS == OS.Mac)
		{
			ServerMain.Logger.Warning("Cannot set a processor to run the performance test on Mac, performance test may not show max capable");
		}
		else
		{
			affinityMask = ((IntPtr)proc.ProcessorAffinity).ToInt32();
			proc.ProcessorAffinity = new IntPtr(2);
		}
		proc.PriorityClass = ProcessPriorityClass.High;
		Thread.CurrentThread.Priority = ThreadPriority.Highest;
		stopwatch = Stopwatch.StartNew();
		ServerChunk chunk = server.GetLoadedChunk(server.WorldMap.ChunkIndex3D(cx, cy, cz));
		if (chunk != null)
		{
			while (--iterations >= 0)
			{
				Packet_ServerChunk psc = chunk.ToPacket(cx, cy, cz, withEntities: true);
				Packet_ServerChunks packet = new Packet_ServerChunks();
				packet.SetChunks(new Packet_ServerChunk[1] { psc }, 1, 1);
				server.Serialize(new Packet_Server
				{
					Id = 10,
					Chunks = packet
				});
			}
		}
		stopwatch.Stop();
		if (RuntimeEnv.OS != OS.Mac)
		{
			Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(affinityMask);
		}
		Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
		Thread.CurrentThread.Priority = ThreadPriority.Normal;
		return "-ServerPacketSending: " + stopwatch.ElapsedTicks;
	}
}
