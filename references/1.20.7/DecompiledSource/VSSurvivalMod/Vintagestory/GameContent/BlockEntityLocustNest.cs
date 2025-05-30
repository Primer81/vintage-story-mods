using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class BlockEntityLocustNest : BlockEntitySpawner
{
	private long herdId;

	private int insideLocustCount;

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		requireSpawnOnWallSide = true;
	}

	protected override long GetNextHerdId()
	{
		if (herdId == 0L)
		{
			herdId = base.GetNextHerdId();
		}
		return herdId;
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(byItemStack);
		float corrupLocustNestChance = Math.Min(1f, 1.5f - (float)Pos.Y / (0.36f * (float)Api.World.SeaLevel));
		string entityCode = "locust-bronze";
		if (Api.World.Rand.NextDouble() < (double)corrupLocustNestChance)
		{
			entityCode = "locust-corrupt";
		}
		Data = new BESpawnerData
		{
			EntityCodes = new string[1] { entityCode },
			InGameHourInterval = 0.1f + 0.9f * (float)Api.World.Rand.NextDouble(),
			MaxCount = Api.World.Rand.Next(7) + 3,
			SpawnArea = new Cuboidi(-5, -5, -5, 5, 2, 5),
			GroupSize = 2 + Api.World.Rand.Next(4),
			SpawnOnlyAfterImport = (byItemStack?.Attributes?.GetBool("spawnOnlyAfterImport")).GetValueOrDefault(),
			InitialSpawnQuantity = 4 + Api.World.Rand.Next(7),
			MinPlayerRange = 36,
			SpawnRangeMode = EnumSpawnRangeMode.WhenInRange,
			RechargePerHour = 0.10000000149011612,
			InternalCapacity = 10,
			InternalCharge = 10.0
		};
	}

	public void OnBlockBreaking()
	{
		if (Api.Side == EnumAppSide.Client && Api.World.Rand.NextDouble() < 0.3)
		{
			(Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos, 123);
		}
	}

	public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] bytes)
	{
		base.OnReceivedClientPacket(fromPlayer, packetid, bytes);
		if (packetid != 123 || !(Api.World.Rand.NextDouble() < 0.25) || insideLocustCount <= 0)
		{
			return;
		}
		string[] entityCodes = Data.EntityCodes;
		if (entityCodes.Length == 0)
		{
			return;
		}
		int rnd = (Api as ICoreServerAPI).World.Rand.Next(entityCodes.Length);
		EntityProperties type = Api.World.GetEntityType(new AssetLocation(entityCodes[rnd]));
		if (type == null)
		{
			return;
		}
		Cuboidf collisionBox = new Cuboidf
		{
			X1 = (0f - type.CollisionBoxSize.X) / 2f,
			Z1 = (0f - type.CollisionBoxSize.X) / 2f,
			X2 = type.CollisionBoxSize.X / 2f,
			Z2 = type.CollisionBoxSize.X / 2f,
			Y2 = type.CollisionBoxSize.Y
		}.OmniNotDownGrowBy(0.1f);
		bool spawned = false;
		Vec3d spawnPos = new Vec3d();
		for (int tries = 0; tries < 15; tries++)
		{
			spawnPos.Set(Pos).Add(-0.5 + Api.World.Rand.NextDouble(), -1.0, -0.5 + Api.World.Rand.NextDouble());
			if (!collisionTester.IsColliding(Api.World.BlockAccessor, collisionBox, spawnPos, alsoCheckTouch: false))
			{
				if (herdId == 0L)
				{
					herdId = GetNextHerdId();
				}
				DoSpawn(type, spawnPos, herdId);
				spawned = true;
				break;
			}
		}
		if (spawned)
		{
			insideLocustCount--;
		}
	}

	protected override void OnGameTick(float dt)
	{
		base.OnGameTick(dt);
		if (Api.World.Rand.NextDouble() < 0.1)
		{
			insideLocustCount = Math.Min(insideLocustCount + 1, 15);
		}
	}

	protected override void DoSpawn(EntityProperties entityType, Vec3d spawnPosition, long herdId)
	{
		base.DoSpawn(entityType, spawnPosition, herdId);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		herdId = tree.GetLong("herdId", 0L);
		insideLocustCount = tree.GetInt("insideLocustCount");
		if (Data.EntityCodes == null || Data.EntityCodes.Length == 0)
		{
			Data.EntityCodes = new string[1] { "locust-bronze" };
		}
		else if (Data.EntityCodes[0] == "locust")
		{
			Data.EntityCodes[0] = "locust-bronze";
		}
		Data.MinPlayerRange = 36;
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetLong("herdId", herdId);
		tree.SetInt("insideLocustCount", insideLocustCount);
	}

	public override void OnLoadCollectibleMappings(IWorldAccessor worldForNewMappings, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
	{
		base.OnLoadCollectibleMappings(worldForNewMappings, oldBlockIdMapping, oldItemIdMapping, schematicSeed, resolveImports);
		if (resolveImports)
		{
			Data.WasImported = true;
			Data.LastSpawnTotalHours = 0.0;
		}
	}
}
