using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntitySpawner : BlockEntity
{
	public BESpawnerData Data = new BESpawnerData().initDefaults();

	protected HashSet<long> spawnedEntities = new HashSet<long>();

	protected GuiDialogSpawner dlg;

	protected CollisionTester collisionTester = new CollisionTester();

	protected bool requireSpawnOnWallSide;

	protected virtual long GetNextHerdId()
	{
		return (Api as ICoreServerAPI).WorldManager.GetNextUniqueId();
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		if (api.Side == EnumAppSide.Server)
		{
			RegisterGameTickListener(OnGameTick, 2000);
			if (Data.InternalCapacity > 0)
			{
				(api as ICoreServerAPI).Event.OnEntityDespawn += Event_OnEntityDespawn;
			}
		}
	}

	private void Event_OnEntityDespawn(Entity entity, EntityDespawnData despawnData)
	{
		if ((despawnData == null || despawnData.Reason == EnumDespawnReason.Unload || despawnData.Reason == EnumDespawnReason.Expire) && spawnedEntities.Contains(entity.EntityId))
		{
			bool alive = false;
			try
			{
				EntityBehaviorHealth behavior = entity.GetBehavior<EntityBehaviorHealth>();
				alive = behavior != null && behavior.Health > 0f;
			}
			catch (Exception)
			{
			}
			if (alive)
			{
				Data.InternalCharge += 1.0;
			}
		}
	}

	protected virtual void OnGameTick(float dt)
	{
		if (Data.EntityCodes == null || Data.EntityCodes.Length == 0)
		{
			Data.LastSpawnTotalHours = Api.World.Calendar.TotalHours;
			return;
		}
		if (Data.InternalCapacity > 0)
		{
			double hoursPassed = Api.World.Calendar.TotalHours - Data.LastChargeUpdateTotalHours;
			if (hoursPassed > 0.015)
			{
				Data.InternalCharge = Math.Min(Data.InternalCapacity, Data.InternalCharge + hoursPassed * Data.RechargePerHour);
				Data.LastChargeUpdateTotalHours = Api.World.Calendar.TotalHours;
			}
		}
		ICoreServerAPI sapi = Api as ICoreServerAPI;
		int rnd = sapi.World.Rand.Next(Data.EntityCodes.Length);
		EntityProperties type = Api.World.GetEntityType(new AssetLocation(Data.EntityCodes[rnd]));
		if ((Data.InternalCapacity > 0 && Data.InternalCharge < 1.0) || (Data.LastSpawnTotalHours + (double)Data.InGameHourInterval > Api.World.Calendar.TotalHours && Data.InitialSpawnQuantity <= 0) || !IsAreaLoaded() || (Data.SpawnOnlyAfterImport && !Data.WasImported))
		{
			return;
		}
		if (Data.SpawnRangeMode > EnumSpawnRangeMode.IgnorePlayerRange)
		{
			IPlayer player = Api.World.NearestPlayer(Pos.X, Pos.InternalY, Pos.Z);
			if (player?.Entity?.ServerPos == null)
			{
				return;
			}
			double distanceSq = player.Entity.ServerPos.SquareDistanceTo(Pos.ToVec3d());
			if ((Data.SpawnRangeMode == EnumSpawnRangeMode.WhenInRange && distanceSq > (double)(Data.MinPlayerRange * Data.MinPlayerRange)) || (Data.SpawnRangeMode == EnumSpawnRangeMode.WhenOutsideOfRange && distanceSq < (double)(Data.MaxPlayerRange * Data.MaxPlayerRange)) || (Data.SpawnRangeMode == EnumSpawnRangeMode.WithinMinMaxRange && (distanceSq < (double)(Data.MinPlayerRange * Data.MinPlayerRange) || distanceSq > (double)(Data.MaxPlayerRange * Data.MaxPlayerRange))))
			{
				return;
			}
		}
		if (type == null)
		{
			return;
		}
		long[] entitylist = spawnedEntities.ToArray();
		for (int j = 0; j < entitylist.Length; j++)
		{
			sapi.World.LoadedEntities.TryGetValue(entitylist[j], out var entity);
			if (entity == null || !entity.Alive)
			{
				spawnedEntities.Remove(entitylist[j]);
			}
		}
		if (spawnedEntities.Count >= Data.MaxCount)
		{
			Data.LastSpawnTotalHours = Api.World.Calendar.TotalHours;
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
		Cuboidf collisionBox2 = new Cuboidf
		{
			X1 = (0f - type.CollisionBoxSize.X) / 2f,
			Z1 = (0f - type.CollisionBoxSize.X) / 2f,
			X2 = type.CollisionBoxSize.X / 2f,
			Z2 = type.CollisionBoxSize.X / 2f,
			Y2 = type.CollisionBoxSize.Y
		};
		int q = Data.GroupSize;
		long herdId = 0L;
		Vec3d spawnPos = new Vec3d();
		BlockPos spawnBlockPos = new BlockPos();
		while (q-- > 0)
		{
			for (int tries = 0; tries < 15; tries++)
			{
				spawnPos.Set(Pos).Add(0.5 + (double)Data.SpawnArea.MinX + Api.World.Rand.NextDouble() * (double)Data.SpawnArea.SizeX, (double)Data.SpawnArea.MinY + Api.World.Rand.NextDouble() * (double)Data.SpawnArea.SizeY, 0.5 + (double)Data.SpawnArea.MinZ + Api.World.Rand.NextDouble() * (double)Data.SpawnArea.SizeZ);
				if (collisionTester.IsColliding(Api.World.BlockAccessor, collisionBox, spawnPos, alsoCheckTouch: false))
				{
					continue;
				}
				if (requireSpawnOnWallSide)
				{
					bool haveWall = false;
					int i = 0;
					while (!haveWall && i < 6)
					{
						BlockFacing face = BlockFacing.ALLFACES[i];
						spawnBlockPos.Set(spawnPos).Add(face.Normali);
						haveWall = Api.World.BlockAccessor.IsSideSolid(spawnBlockPos.X, spawnBlockPos.Y, spawnBlockPos.Z, face.Opposite);
						if (haveWall)
						{
							Cuboidd entityPos = collisionBox2.ToDouble().Translate(spawnPos);
							Cuboidd blockPos = Cuboidf.Default().ToDouble().Translate(spawnBlockPos);
							switch (face.Index)
							{
							case 0:
								spawnPos.Z -= blockPos.Z2 - entityPos.Z1 + 0.009999999776482582;
								break;
							case 1:
								spawnPos.X += blockPos.X1 - entityPos.X2 - 0.009999999776482582;
								break;
							case 2:
								spawnPos.Z += blockPos.Z1 - entityPos.Z2 - 0.009999999776482582;
								break;
							case 3:
								spawnPos.X -= blockPos.X2 - entityPos.X1 + 0.009999999776482582;
								break;
							case 4:
								spawnPos.Y += blockPos.Y1 - entityPos.Y2 - 0.009999999776482582;
								break;
							case 5:
								spawnPos.Y -= blockPos.Y2 - entityPos.Y1 + 0.009999999776482582;
								break;
							}
						}
						i++;
					}
					if (!haveWall)
					{
						continue;
					}
				}
				if (herdId == 0L)
				{
					herdId = GetNextHerdId();
				}
				DoSpawn(type, spawnPos, herdId);
				Data.LastSpawnTotalHours = Api.World.Calendar.TotalHours;
				if (Data.InitialQuantitySpawned > 0)
				{
					Data.InitialQuantitySpawned--;
				}
				if (Data.RemoveAfterSpawnCount > 0)
				{
					Data.RemoveAfterSpawnCount--;
					if (Data.RemoveAfterSpawnCount == 0)
					{
						Api.World.BlockAccessor.SetBlock(0, Pos);
					}
				}
				return;
			}
		}
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(byItemStack);
		Data.LastSpawnTotalHours = Api.World.Calendar.TotalHours;
		if (byItemStack == null)
		{
			Data.InternalCharge = Data.InternalCapacity;
			return;
		}
		byte[] data = byItemStack.Attributes.GetBytes("spawnerData");
		if (data == null)
		{
			return;
		}
		try
		{
			Data = SerializerUtil.Deserialize<BESpawnerData>(data);
		}
		catch
		{
			Data = new BESpawnerData().initDefaults();
		}
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (Api is ICoreServerAPI sapi)
		{
			sapi.Event.OnEntityDespawn -= Event_OnEntityDespawn;
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		if (Api is ICoreServerAPI sapi)
		{
			sapi.Event.OnEntityDespawn -= Event_OnEntityDespawn;
		}
	}

	protected virtual void DoSpawn(EntityProperties entityType, Vec3d spawnPosition, long herdid)
	{
		if (Data.InternalCapacity > 0)
		{
			Data.InternalCharge -= 1.0;
		}
		MarkDirty(redrawOnClient: true);
		Entity entity = Api.World.ClassRegistry.CreateEntity(entityType);
		if (entity is EntityAgent agent)
		{
			agent.HerdId = herdid;
		}
		entity.ServerPos.SetPosWithDimension(spawnPosition);
		entity.ServerPos.SetYaw((float)Api.World.Rand.NextDouble() * ((float)Math.PI * 2f));
		entity.Pos.SetFrom(entity.ServerPos);
		entity.Attributes.SetString("origin", "entityspawner");
		Api.World.SpawnEntity(entity);
		spawnedEntities.Add(entity.EntityId);
	}

	public bool IsAreaLoaded()
	{
		ICoreServerAPI sapi = Api as ICoreServerAPI;
		int sizeX = sapi.WorldManager.MapSizeX / 32;
		int sizeY = sapi.WorldManager.MapSizeY / 32;
		int sizeZ = sapi.WorldManager.MapSizeZ / 32;
		int num = GameMath.Clamp((Pos.X + Data.SpawnArea.MinX) / 32, 0, sizeX - 1);
		int maxcx = GameMath.Clamp((Pos.X + Data.SpawnArea.MaxX) / 32, 0, sizeX - 1);
		int mincy = GameMath.Clamp((Pos.Y + Data.SpawnArea.MinY) / 32, 0, sizeY - 1);
		int maxcy = GameMath.Clamp((Pos.Y + Data.SpawnArea.MaxY) / 32, 0, sizeY - 1);
		int mincz = GameMath.Clamp((Pos.Z + Data.SpawnArea.MinZ) / 32, 0, sizeZ - 1);
		int maxcz = GameMath.Clamp((Pos.Z + Data.SpawnArea.MaxZ) / 32, 0, sizeZ - 1);
		for (int cx = num; cx <= maxcx; cx++)
		{
			for (int cy = mincy; cy <= maxcy; cy++)
			{
				for (int cz = mincz; cz <= maxcz; cz++)
				{
					if (sapi.WorldManager.GetChunk(cx, cy, cz) == null)
					{
						return false;
					}
				}
			}
		}
		return true;
	}

	internal void OnInteract(IPlayer byPlayer)
	{
		if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			return;
		}
		if (Api.Side == EnumAppSide.Server)
		{
			(Api as ICoreServerAPI).Network.SendBlockEntityPacket(byPlayer as IServerPlayer, Pos, 1000, SerializerUtil.Serialize(Data));
			return;
		}
		dlg = new GuiDialogSpawner(Pos, Api as ICoreClientAPI);
		dlg.spawnerData = Data;
		dlg.TryOpen();
		dlg.OnClosed += delegate
		{
			dlg?.Dispose();
			dlg = null;
		};
	}

	public override void OnReceivedServerPacket(int packetid, byte[] bytes)
	{
		if (packetid == 1000)
		{
			Data = SerializerUtil.Deserialize<BESpawnerData>(bytes);
			GuiDialogSpawner guiDialogSpawner = dlg;
			if (guiDialogSpawner != null && guiDialogSpawner.IsOpened())
			{
				dlg.UpdateFromServer(Data);
			}
		}
	}

	public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] bytes)
	{
		if (packetid == 1001)
		{
			Data = SerializerUtil.Deserialize<BESpawnerData>(bytes);
			MarkDirty();
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		Data.ToTreeAttributes(tree);
		tree["spawnedEntities"] = new LongArrayAttribute(spawnedEntities.ToArray());
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		Data = new BESpawnerData();
		Data.FromTreeAttributes(tree);
		long[] values = (tree["spawnedEntities"] as LongArrayAttribute)?.value;
		spawnedEntities = new HashSet<long>((values == null) ? new long[0] : values);
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

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		base.GetBlockInfo(forPlayer, dsc);
		if (Api is ICoreClientAPI capi && capi.Settings.Bool["extendedDebugInfo"])
		{
			dsc.AppendLine("Charge capacity: " + Data.InternalCapacity);
			dsc.AppendLine("Internal charge: " + Data.InternalCharge);
		}
	}
}
