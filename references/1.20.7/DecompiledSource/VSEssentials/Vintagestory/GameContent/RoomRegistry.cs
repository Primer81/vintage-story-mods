using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class RoomRegistry : ModSystem
{
	protected Dictionary<long, ChunkRooms> roomsByChunkIndex = new Dictionary<long, ChunkRooms>();

	protected object roomsByChunkIndexLock = new object();

	private const int chunksize = 32;

	private int chunkMapSizeX;

	private int chunkMapSizeZ;

	private ICoreAPI api;

	private ICachingBlockAccessor blockAccess;

	private const int ARRAYSIZE = 29;

	private readonly int[] currentVisited = new int[24389];

	private readonly int[] skyLightXZChecked = new int[841];

	private const int MAXROOMSIZE = 14;

	private const int MAXCELLARSIZE = 7;

	private const int ALTMAXCELLARSIZE = 9;

	private const int ALTMAXCELLARVOLUME = 150;

	private int iteration;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		base.Start(api);
		this.api = api;
		api.Event.ChunkDirty += Event_ChunkDirty;
		blockAccess = api.World.GetCachingBlockAccessor(synchronize: false, relight: false);
	}

	public override void Dispose()
	{
		blockAccess?.Dispose();
		blockAccess = null;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		api.Event.BlockTexturesLoaded += init;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		api.Event.SaveGameLoaded += init;
		api.ChatCommands.GetOrCreate("debug").BeginSubCommand("rooms").RequiresPrivilege(Privilege.controlserver)
			.BeginSubCommand("list")
			.HandleWith(onRoomRegDbgCmdList)
			.EndSubCommand()
			.BeginSubCommand("hi")
			.WithArgs(api.ChatCommands.Parsers.OptionalInt("rindex"))
			.RequiresPlayer()
			.HandleWith(onRoomRegDbgCmdHi)
			.EndSubCommand()
			.BeginSubCommand("unhi")
			.RequiresPlayer()
			.HandleWith(onRoomRegDbgCmdUnhi)
			.EndSubCommand()
			.EndSubCommand();
	}

	private TextCommandResult onRoomRegDbgCmdHi(TextCommandCallingArgs args)
	{
		int rindex = (int)args.Parsers[0].GetValue();
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		BlockPos pos = player.Entity.Pos.XYZ.AsBlockPos;
		long index3d = MapUtil.Index3dL(pos.X / 32, pos.Y / 32, pos.Z / 32, chunkMapSizeX, chunkMapSizeZ);
		ChunkRooms chunkrooms;
		lock (roomsByChunkIndexLock)
		{
			roomsByChunkIndex.TryGetValue(index3d, out chunkrooms);
		}
		if (chunkrooms == null || chunkrooms.Rooms.Count == 0)
		{
			return TextCommandResult.Success("No rooms in this chunk");
		}
		if (chunkrooms.Rooms.Count - 1 < rindex || rindex < 0)
		{
			if (rindex == 0)
			{
				return TextCommandResult.Success("No room at this index");
			}
			return TextCommandResult.Success("Wrong index, select a number between 0 and " + (chunkrooms.Rooms.Count - 1));
		}
		Room room = chunkrooms.Rooms[rindex];
		if (args.Parsers[0].IsMissing)
		{
			room = null;
			foreach (Room croom in chunkrooms.Rooms)
			{
				if (croom.Contains(pos))
				{
					room = croom;
					break;
				}
			}
			if (room == null)
			{
				return TextCommandResult.Success("No room at your location");
			}
		}
		List<BlockPos> poses = new List<BlockPos>();
		List<int> colors = new List<int>();
		int sizex = room.Location.X2 - room.Location.X1 + 1;
		int sizey = room.Location.Y2 - room.Location.Y1 + 1;
		int sizez = room.Location.Z2 - room.Location.Z1 + 1;
		for (int dx = 0; dx < sizex; dx++)
		{
			for (int dy = 0; dy < sizey; dy++)
			{
				for (int dz = 0; dz < sizez; dz++)
				{
					int pindex = (dy * sizez + dz) * sizex + dx;
					if ((room.PosInRoom[pindex / 8] & (1 << pindex % 8)) > 0)
					{
						poses.Add(new BlockPos(room.Location.X1 + dx, room.Location.Y1 + dy, room.Location.Z1 + dz));
						colors.Add(ColorUtil.ColorFromRgba((room.ExitCount != 0) ? 100 : 0, (room.ExitCount == 0) ? 100 : 0, Math.Min(255, rindex * 30), 150));
					}
				}
			}
		}
		api.World.HighlightBlocks(player, 50, poses, colors);
		return TextCommandResult.Success();
	}

	private TextCommandResult onRoomRegDbgCmdUnhi(TextCommandCallingArgs args)
	{
		IServerPlayer player = args.Caller.Player as IServerPlayer;
		api.World.HighlightBlocks(player, 50, new List<BlockPos>(), new List<int>());
		return TextCommandResult.Success();
	}

	private TextCommandResult onRoomRegDbgCmdList(TextCommandCallingArgs args)
	{
		BlockPos pos = (args.Caller.Player as IServerPlayer).Entity.Pos.XYZ.AsBlockPos;
		long index3d = MapUtil.Index3dL(pos.X / 32, pos.Y / 32, pos.Z / 32, chunkMapSizeX, chunkMapSizeZ);
		ChunkRooms chunkrooms;
		lock (roomsByChunkIndexLock)
		{
			roomsByChunkIndex.TryGetValue(index3d, out chunkrooms);
		}
		if (chunkrooms == null || chunkrooms.Rooms.Count == 0)
		{
			return TextCommandResult.Success("No rooms here");
		}
		string response = chunkrooms.Rooms.Count + " Rooms here \n";
		lock (chunkrooms.roomsLock)
		{
			for (int i = 0; i < chunkrooms.Rooms.Count; i++)
			{
				Room room = chunkrooms.Rooms[i];
				int sizex = room.Location.X2 - room.Location.X1 + 1;
				int sizey = room.Location.Y2 - room.Location.Y1 + 1;
				int sizez = room.Location.Z2 - room.Location.Z1 + 1;
				response += $"{i} - bbox dim: {sizex}/{sizey}/{sizez}, mid: {(float)room.Location.X1 + (float)sizex / 2f}/{(float)room.Location.Y1 + (float)sizey / 2f}/{(float)room.Location.Z1 + (float)sizez / 2f}\n";
			}
		}
		return TextCommandResult.Success(response);
	}

	private void init()
	{
		chunkMapSizeX = api.World.BlockAccessor.MapSizeX / 32;
		chunkMapSizeZ = api.World.BlockAccessor.MapSizeZ / 32;
	}

	private void Event_ChunkDirty(Vec3i chunkCoord, IWorldChunk chunk, EnumChunkDirtyReason reason)
	{
		long index3d = MapUtil.Index3dL(chunkCoord.X, chunkCoord.Y, chunkCoord.Z, chunkMapSizeX, chunkMapSizeZ);
		FastSetOfLongs set = new FastSetOfLongs();
		set.Add(index3d);
		lock (roomsByChunkIndexLock)
		{
			roomsByChunkIndex.TryGetValue(index3d, out var chunkrooms);
			if (chunkrooms != null)
			{
				set.Add(index3d);
				for (int i = 0; i < chunkrooms.Rooms.Count; i++)
				{
					Cuboidi location = chunkrooms.Rooms[i].Location;
					int x1 = location.Start.X / 32;
					int x2 = location.End.X / 32;
					int y1 = location.Start.Y / 32;
					int y2 = location.End.Y / 32;
					int z1 = location.Start.Z / 32;
					int z2 = location.End.Z / 32;
					set.Add(MapUtil.Index3dL(x1, y1, z1, chunkMapSizeX, chunkMapSizeZ));
					if (z2 != z1)
					{
						set.Add(MapUtil.Index3dL(x1, y1, z2, chunkMapSizeX, chunkMapSizeZ));
					}
					if (y2 != y1)
					{
						set.Add(MapUtil.Index3dL(x1, y2, z1, chunkMapSizeX, chunkMapSizeZ));
						if (z2 != z1)
						{
							set.Add(MapUtil.Index3dL(x1, y2, z2, chunkMapSizeX, chunkMapSizeZ));
						}
					}
					if (x2 == x1)
					{
						continue;
					}
					set.Add(MapUtil.Index3dL(x2, y1, z1, chunkMapSizeX, chunkMapSizeZ));
					if (z2 != z1)
					{
						set.Add(MapUtil.Index3dL(x2, y1, z2, chunkMapSizeX, chunkMapSizeZ));
					}
					if (y2 != y1)
					{
						set.Add(MapUtil.Index3dL(x2, y2, z1, chunkMapSizeX, chunkMapSizeZ));
						if (z2 != z1)
						{
							set.Add(MapUtil.Index3dL(x2, y2, z2, chunkMapSizeX, chunkMapSizeZ));
						}
					}
				}
			}
			foreach (long index in set)
			{
				roomsByChunkIndex.Remove(index);
			}
		}
	}

	public Room GetRoomForPosition(BlockPos pos)
	{
		long index3d = MapUtil.Index3dL(pos.X / 32, pos.Y / 32, pos.Z / 32, chunkMapSizeX, chunkMapSizeZ);
		ChunkRooms chunkrooms;
		lock (roomsByChunkIndexLock)
		{
			roomsByChunkIndex.TryGetValue(index3d, out chunkrooms);
		}
		Room room;
		if (chunkrooms != null)
		{
			Room firstEnclosedRoom = null;
			Room firstOpenedRoom = null;
			for (int i = 0; i < chunkrooms.Rooms.Count; i++)
			{
				room = chunkrooms.Rooms[i];
				if (room.Contains(pos))
				{
					if (firstEnclosedRoom == null && room.ExitCount == 0)
					{
						firstEnclosedRoom = room;
					}
					if (firstOpenedRoom == null && room.ExitCount > 0)
					{
						firstOpenedRoom = room;
					}
				}
			}
			if (firstEnclosedRoom != null && firstEnclosedRoom.IsFullyLoaded(chunkrooms))
			{
				return firstEnclosedRoom;
			}
			if (firstOpenedRoom != null && firstOpenedRoom.IsFullyLoaded(chunkrooms))
			{
				return firstOpenedRoom;
			}
			room = FindRoomForPosition(pos, chunkrooms);
			chunkrooms.AddRoom(room);
			return room;
		}
		ChunkRooms rooms = new ChunkRooms();
		room = FindRoomForPosition(pos, rooms);
		rooms.AddRoom(room);
		lock (roomsByChunkIndexLock)
		{
			roomsByChunkIndex[index3d] = rooms;
			return room;
		}
	}

	private Room FindRoomForPosition(BlockPos pos, ChunkRooms otherRooms)
	{
		QueueOfInt bfsQueue = new QueueOfInt();
		int halfSize = 14;
		int maxSize = halfSize + halfSize;
		bfsQueue.Enqueue((halfSize << 10) | (halfSize << 5) | halfSize);
		int visitedIndex = (halfSize * 29 + halfSize) * 29 + halfSize;
		int iteration = ++this.iteration;
		currentVisited[visitedIndex] = iteration;
		int coolingWallCount = 0;
		int nonCoolingWallCount = 0;
		int skyLightCount = 0;
		int nonSkyLightCount = 0;
		int exitCount = 0;
		blockAccess.Begin();
		bool allChunksLoaded = true;
		int minx = halfSize;
		int miny = halfSize;
		int minz = halfSize;
		int maxx = halfSize;
		int maxy = halfSize;
		int maxz = halfSize;
		int posX = pos.X - halfSize;
		int posY = pos.Y - halfSize;
		int posZ = pos.Z - halfSize;
		BlockPos npos = new BlockPos();
		BlockPos bpos = new BlockPos();
		while (bfsQueue.Count > 0)
		{
			int num = bfsQueue.Dequeue();
			int dx = num >> 10;
			int dy = (num >> 5) & 0x1F;
			int dz = num & 0x1F;
			npos.Set(posX + dx, posY + dy, posZ + dz);
			bpos.Set(npos);
			if (dx < minx)
			{
				minx = dx;
			}
			else if (dx > maxx)
			{
				maxx = dx;
			}
			if (dy < miny)
			{
				miny = dy;
			}
			else if (dy > maxy)
			{
				maxy = dy;
			}
			if (dz < minz)
			{
				minz = dz;
			}
			else if (dz > maxz)
			{
				maxz = dz;
			}
			Block bBlock = blockAccess.GetBlock(bpos);
			BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
			foreach (BlockFacing facing in aLLFACES)
			{
				facing.IterateThruFacingOffsets(npos);
				int heatRetention = bBlock.GetRetention(bpos, facing, EnumRetentionType.Heat);
				if (bBlock.Id != 0 && heatRetention != 0)
				{
					if (heatRetention < 0)
					{
						coolingWallCount -= heatRetention;
					}
					else
					{
						nonCoolingWallCount += heatRetention;
					}
					continue;
				}
				if (!blockAccess.IsValidPos(npos))
				{
					nonCoolingWallCount++;
					continue;
				}
				Block block = blockAccess.GetBlock(npos);
				allChunksLoaded &= blockAccess.LastChunkLoaded;
				heatRetention = block.GetRetention(npos, facing.Opposite, EnumRetentionType.Heat);
				if (heatRetention != 0)
				{
					if (heatRetention < 0)
					{
						coolingWallCount -= heatRetention;
					}
					else
					{
						nonCoolingWallCount += heatRetention;
					}
					continue;
				}
				dx = npos.X - posX;
				dy = npos.Y - posY;
				dz = npos.Z - posZ;
				bool outsideCube = false;
				switch (facing.Index)
				{
				case 0:
					if (dz < minz)
					{
						outsideCube = dz < 0 || maxz - minz + 1 >= 14;
					}
					break;
				case 1:
					if (dx > maxx)
					{
						outsideCube = dx > maxSize || maxx - minx + 1 >= 14;
					}
					break;
				case 2:
					if (dz > maxz)
					{
						outsideCube = dz > maxSize || maxz - minz + 1 >= 14;
					}
					break;
				case 3:
					if (dx < minx)
					{
						outsideCube = dx < 0 || maxx - minx + 1 >= 14;
					}
					break;
				case 4:
					if (dy > maxy)
					{
						outsideCube = dy > maxSize || maxy - miny + 1 >= 14;
					}
					break;
				case 5:
					if (dy < miny)
					{
						outsideCube = dy < 0 || maxy - miny + 1 >= 14;
					}
					break;
				}
				if (outsideCube)
				{
					exitCount++;
					continue;
				}
				visitedIndex = (dx * 29 + dy) * 29 + dz;
				if (currentVisited[visitedIndex] == iteration)
				{
					continue;
				}
				currentVisited[visitedIndex] = iteration;
				int skyLightIndex = dx * 29 + dz;
				if (skyLightXZChecked[skyLightIndex] < iteration)
				{
					skyLightXZChecked[skyLightIndex] = iteration;
					if (blockAccess.GetLightLevel(npos, EnumLightLevelType.OnlySunLight) >= api.World.SunBrightness - 1)
					{
						skyLightCount++;
					}
					else
					{
						nonSkyLightCount++;
					}
				}
				bfsQueue.Enqueue((dx << 10) | (dy << 5) | dz);
			}
		}
		int sizex = maxx - minx + 1;
		int sizey = maxy - miny + 1;
		int sizez = maxz - minz + 1;
		byte[] posInRoom = new byte[(sizex * sizey * sizez + 7) / 8];
		int volumeCount = 0;
		for (int dx = 0; dx < sizex; dx++)
		{
			for (int dy = 0; dy < sizey; dy++)
			{
				visitedIndex = ((dx + minx) * 29 + (dy + miny)) * 29 + minz;
				for (int dz = 0; dz < sizez; dz++)
				{
					if (currentVisited[visitedIndex + dz] == iteration)
					{
						int index = (dy * sizez + dz) * sizex + dx;
						posInRoom[index / 8] = (byte)(posInRoom[index / 8] | (1 << index % 8));
						volumeCount++;
					}
				}
			}
		}
		bool isCellar = sizex <= 7 && sizey <= 7 && sizez <= 7;
		if (!isCellar && volumeCount <= 150)
		{
			isCellar = (sizex <= 9 && sizey <= 7 && sizez <= 7) || (sizex <= 7 && sizey <= 9 && sizez <= 7) || (sizex <= 7 && sizey <= 7 && sizez <= 9);
		}
		return new Room
		{
			CoolingWallCount = coolingWallCount,
			NonCoolingWallCount = nonCoolingWallCount,
			SkylightCount = skyLightCount,
			NonSkylightCount = nonSkyLightCount,
			ExitCount = exitCount,
			AnyChunkUnloaded = ((!allChunksLoaded) ? 1 : 0),
			Location = new Cuboidi(posX + minx, posY + miny, posZ + minz, posX + maxx, posY + maxy, posZ + maxz),
			PosInRoom = posInRoom,
			IsSmallRoom = (isCellar && exitCount == 0)
		};
	}
}
