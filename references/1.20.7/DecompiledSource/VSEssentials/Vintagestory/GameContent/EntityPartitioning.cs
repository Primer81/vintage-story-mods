using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class EntityPartitioning : ModSystem, IEntityPartitioning
{
	public delegate bool RangeTestDelegate(Entity e, Vec3d pos, double radiuSq);

	public const int partitionsLength = 8;

	private int gridSizeInBlocks;

	private ICoreAPI api;

	private ICoreClientAPI capi;

	private ICoreServerAPI sapi;

	public Dictionary<long, EntityPartitionChunk> Partitions = new Dictionary<long, EntityPartitionChunk>();

	private const int chunkSize = 32;

	private int chunkMapSizeX;

	private int chunkMapSizeZ;

	public double LargestTouchDistance;

	public override double ExecuteOrder()
	{
		return 0.0;
	}

	public override bool ShouldLoad(EnumAppSide side)
	{
		return true;
	}

	public override void Start(ICoreAPI api)
	{
		this.api = api;
		gridSizeInBlocks = 4;
		api.Event.PlayerDimensionChanged += Event_PlayerDimensionChanged;
	}

	private void Event_PlayerDimensionChanged(IPlayer byPlayer)
	{
		RePartitionPlayer(byPlayer.Entity);
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.Event.RegisterGameTickListener(OnClientTick, 32);
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		api.Event.RegisterGameTickListener(OnServerTick, 32);
		api.Event.PlayerSwitchGameMode += OnSwitchedGameMode;
	}

	private void OnClientTick(float dt)
	{
		partitionEntities(capi.World.LoadedEntities.Values);
	}

	private void OnServerTick(float dt)
	{
		partitionEntities(((CachingConcurrentDictionary<long, Entity>)sapi.World.LoadedEntities).Values);
	}

	private void partitionEntities(ICollection<Entity> entities)
	{
		chunkMapSizeX = api.World.BlockAccessor.MapSizeX / 32;
		chunkMapSizeZ = api.World.BlockAccessor.MapSizeZ / 32;
		double largestTouchDistance = 0.0;
		Partitions.Clear();
		foreach (Entity val in entities)
		{
			if (val.IsCreature)
			{
				largestTouchDistance = Math.Max(largestTouchDistance, val.GetTouchDistance());
			}
			PartitionEntity(val);
		}
		LargestTouchDistance = largestTouchDistance;
	}

	private void PartitionEntity(Entity entity)
	{
		EntityPos pos = entity.SidedPos;
		int lgx = (int)pos.X / gridSizeInBlocks % 8;
		int gridIndex = (int)pos.Z / gridSizeInBlocks % 8 * 8 + lgx;
		if (gridIndex >= 0)
		{
			long nowInChunkIndex3d = MapUtil.Index3dL((int)pos.X / 32, (int)pos.Y / 32, (int)pos.Z / 32, chunkMapSizeX, chunkMapSizeZ);
			if (!Partitions.TryGetValue(nowInChunkIndex3d, out var partition))
			{
				partition = (Partitions[nowInChunkIndex3d] = new EntityPartitionChunk());
			}
			List<Entity> list = partition.Add(entity, gridIndex);
			if (entity is EntityPlayer ep)
			{
				ep.entityListForPartitioning = list;
			}
		}
	}

	public void RePartitionPlayer(EntityPlayer entity)
	{
		entity.entityListForPartitioning?.Remove(entity);
		PartitionEntity(entity);
	}

	private void OnSwitchedGameMode(IServerPlayer player)
	{
		RePartitionPlayer(player.Entity);
	}

	[Obsolete("In version 1.19.2 and later, this searches only entities which are Creatures, which is probably what the caller wants but you should specify EnumEntitySearchType explicitly")]
	public Entity GetNearestEntity(Vec3d position, double radius, ActionConsumable<Entity> matches = null)
	{
		return GetNearestEntity(position, radius, matches, EnumEntitySearchType.Creatures);
	}

	public Entity GetNearestInteractableEntity(Vec3d position, double radius, ActionConsumable<Entity> matches = null)
	{
		if (matches == null)
		{
			return GetNearestEntity(position, radius, (Entity e) => e.IsInteractable, EnumEntitySearchType.Creatures);
		}
		return GetNearestEntity(position, radius, (Entity e) => matches(e) && e.IsInteractable, EnumEntitySearchType.Creatures);
	}

	public Entity GetNearestEntity(Vec3d position, double radius, ActionConsumable<Entity> matches, EnumEntitySearchType searchType)
	{
		Entity nearestEntity = null;
		double radiusSq = radius * radius;
		double nearestDistanceSq = radiusSq;
		if (api.Side == EnumAppSide.Client)
		{
			WalkEntities(position, radius, delegate(Entity e)
			{
				double num2 = e.Pos.SquareDistanceTo(position);
				if (num2 < nearestDistanceSq && matches(e))
				{
					nearestDistanceSq = num2;
					nearestEntity = e;
				}
				return true;
			}, onIsInRangePartition, searchType);
		}
		else
		{
			WalkEntities(position, radius, delegate(Entity e)
			{
				double num = e.ServerPos.SquareDistanceTo(position);
				if (num < nearestDistanceSq && matches(e))
				{
					nearestDistanceSq = num;
					nearestEntity = e;
				}
				return true;
			}, onIsInRangePartition, searchType);
		}
		return nearestEntity;
	}

	private bool onIsInRangeServer(Entity e, Vec3d pos, double radiusSq)
	{
		double num = e.ServerPos.X - pos.X;
		double dy = e.ServerPos.Y - pos.Y;
		double dz = e.ServerPos.Z - pos.Z;
		return num * num + dy * dy + dz * dz < radiusSq;
	}

	private bool onIsInRangeClient(Entity e, Vec3d pos, double radiusSq)
	{
		double num = e.Pos.X - pos.X;
		double dy = e.Pos.Y - pos.Y;
		double dz = e.Pos.Z - pos.Z;
		return num * num + dy * dy + dz * dz < radiusSq;
	}

	private bool onIsInRangePartition(Entity e, Vec3d pos, double radiusSq)
	{
		return true;
	}

	[Obsolete("In version 1.19.2 and later, this walks through Creature entities only, so recommended to call WalkEntityPartitions() specifying the type of search explicitly for clarity in the calling code")]
	public void WalkEntities(Vec3d centerPos, double radius, ActionConsumable<Entity> callback)
	{
		WalkEntities(centerPos, radius, callback, EnumEntitySearchType.Creatures);
	}

	[Obsolete("In version 1.19.2 and later, use WalkEntities specifying the searchtype (Creatures or Inanimate) explitly in the calling code.")]
	public void WalkInteractableEntities(Vec3d centerPos, double radius, ActionConsumable<Entity> callback)
	{
		WalkEntities(centerPos, radius, callback, EnumEntitySearchType.Creatures);
	}

	public void WalkEntities(Vec3d centerPos, double radius, ActionConsumable<Entity> callback, EnumEntitySearchType searchType)
	{
		if (api.Side == EnumAppSide.Client)
		{
			WalkEntities(centerPos, radius, callback, onIsInRangeClient, searchType);
		}
		else
		{
			WalkEntities(centerPos, radius, callback, onIsInRangeServer, searchType);
		}
	}

	public void WalkEntityPartitions(Vec3d centerPos, double radius, ActionConsumable<Entity> callback)
	{
		WalkEntities(centerPos, radius, callback, onIsInRangePartition, EnumEntitySearchType.Creatures);
	}

	private void WalkEntities(Vec3d centerPos, double radius, ActionConsumable<Entity> callback, RangeTestDelegate onRangeTest, EnumEntitySearchType searchType)
	{
		int dimension = (int)centerPos.Y / 32768;
		double num = centerPos.Y - (double)(dimension * 32768);
		int gridXMax = api.World.BlockAccessor.MapSizeX / gridSizeInBlocks - 1;
		int cyTop = api.World.BlockAccessor.MapSizeY / 32 - 1;
		int gridZMax = api.World.BlockAccessor.MapSizeZ / gridSizeInBlocks - 1;
		int mingx = (int)GameMath.Clamp((centerPos.X - radius) / (double)gridSizeInBlocks, 0.0, gridXMax);
		int maxgx = (int)GameMath.Clamp((centerPos.X + radius) / (double)gridSizeInBlocks, 0.0, gridXMax);
		int mincy = (int)GameMath.Clamp((num - radius) / 32.0, 0.0, cyTop);
		int maxcy = (int)GameMath.Clamp((num + radius) / 32.0, 0.0, cyTop);
		int mingz = (int)GameMath.Clamp((centerPos.Z - radius) / (double)gridSizeInBlocks, 0.0, gridZMax);
		int maxgz = (int)GameMath.Clamp((centerPos.Z + radius) / (double)gridSizeInBlocks, 0.0, gridZMax);
		double radiusSq = radius * radius;
		EntityPartitionChunk partitionChunk = null;
		long lastIndex3d = -1L;
		for (int cy = mincy; cy <= maxcy; cy++)
		{
			for (int gridX = mingx; gridX <= maxgx; gridX++)
			{
				int cx = gridX * gridSizeInBlocks / 32;
				int lgx = gridX % 8;
				for (int gridZ = mingz; gridZ <= maxgz; gridZ++)
				{
					int cz = gridZ * gridSizeInBlocks / 32;
					long index3d = MapUtil.Index3dL(cx, cy, cz, chunkMapSizeX, chunkMapSizeZ);
					if (index3d != lastIndex3d)
					{
						lastIndex3d = index3d;
						Partitions.TryGetValue(index3d, out partitionChunk);
					}
					if (partitionChunk == null)
					{
						continue;
					}
					int index = gridZ % 8 * 8 + lgx;
					List<Entity> entities = ((searchType == EnumEntitySearchType.Creatures) ? partitionChunk.Entities[index] : partitionChunk.InanimateEntities[index]);
					if (entities == null)
					{
						continue;
					}
					foreach (Entity entity in entities)
					{
						if (entity.Pos.Dimension == dimension && onRangeTest(entity, centerPos, radiusSq) && !callback(entity))
						{
							return;
						}
					}
				}
			}
		}
	}
}
