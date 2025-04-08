using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public abstract class BlockEntityTeleporterBase : BlockEntity
{
	protected TeleporterManager manager;

	protected Dictionary<long, TeleportingEntity> tpingEntities = new Dictionary<long, TeleportingEntity>();

	protected float TeleportWarmupSec = 3f;

	protected bool somebodyIsTeleporting;

	protected bool somebodyDidTeleport;

	private List<long> toremove = new List<long>();

	public long lastEntityCollideMs;

	public long lastOwnPlayerCollideMs;

	public bool tpLocationIsOffset;

	public BlockEntityTeleporterBase()
	{
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		manager = api.ModLoader.GetModSystem<TeleporterManager>();
	}

	public abstract Vec3d GetTarget(Entity forEntity);

	public virtual void OnEntityCollide(Entity entity)
	{
		if (!tpingEntities.TryGetValue(entity.EntityId, out var tpe))
		{
			Dictionary<long, TeleportingEntity> dictionary = tpingEntities;
			long entityId = entity.EntityId;
			TeleportingEntity obj = new TeleportingEntity
			{
				Entity = entity
			};
			tpe = obj;
			dictionary[entityId] = obj;
		}
		tpe.LastCollideMs = Api.World.ElapsedMilliseconds;
		if (Api.Side == EnumAppSide.Client)
		{
			lastEntityCollideMs = Api.World.ElapsedMilliseconds;
			if ((Api as ICoreClientAPI).World.Player.Entity == entity)
			{
				lastOwnPlayerCollideMs = Api.World.ElapsedMilliseconds;
			}
		}
	}

	protected virtual void HandleTeleportingServer(float dt)
	{
		if (toremove == null)
		{
			throw new Exception("BETeleporterBase: toremove is null, it shouldn't be!");
		}
		if (tpingEntities == null)
		{
			throw new Exception("BETeleporterBase: tpingEntities is null, it shouldn't be!");
		}
		if (Api == null)
		{
			throw new Exception("BETeleporterBase: Api is null, it shouldn't be!");
		}
		toremove.Clear();
		bool wasTeleporting = somebodyIsTeleporting;
		somebodyIsTeleporting &= tpingEntities.Count > 0;
		ICoreServerAPI sapi = Api as ICoreServerAPI;
		foreach (KeyValuePair<long, TeleportingEntity> val in tpingEntities)
		{
			if (val.Value == null)
			{
				throw new Exception("BETeleporterBase: val.Value is null, it shouldn't be!");
			}
			if (val.Value.Entity == null)
			{
				throw new Exception("BETeleporterBase: val.Value.Entity is null, it shouldn't be!");
			}
			if (val.Value.Entity.Teleporting)
			{
				continue;
			}
			val.Value.SecondsPassed += Math.Min(0.5f, dt);
			if (Api.World.ElapsedMilliseconds - val.Value.LastCollideMs > 100)
			{
				Block block = Api.World.CollisionTester.GetCollidingBlock(Api.World.BlockAccessor, val.Value.Entity.SelectionBox, val.Value.Entity.Pos.XYZ);
				if (block == null || block.GetType() != base.Block.GetType())
				{
					toremove.Add(val.Key);
					continue;
				}
			}
			if ((double)val.Value.SecondsPassed > 0.1 && !somebodyIsTeleporting)
			{
				somebodyIsTeleporting = true;
				MarkDirty();
			}
			Vec3d tpTarget = GetTarget(val.Value.Entity);
			if ((double)val.Value.SecondsPassed > 1.5 && tpTarget != null)
			{
				Vec3d targetPos2 = tpTarget.Clone();
				if (tpLocationIsOffset)
				{
					targetPos2.Add(Pos.X, Pos.Y, Pos.Z);
				}
				IWorldChunk chunk = Api.World.BlockAccessor.GetChunkAtBlockPos(targetPos2.AsBlockPos);
				if (chunk != null)
				{
					chunk.MapChunk.MarkFresh();
				}
				else
				{
					sapi.WorldManager.LoadChunkColumnPriority((int)targetPos2.X / 32, (int)targetPos2.Z / 32, new ChunkLoadOptions
					{
						KeepLoaded = false
					});
				}
			}
			if (val.Value.SecondsPassed > TeleportWarmupSec && tpTarget != null)
			{
				Vec3d targetPos = tpTarget.Clone();
				if (tpLocationIsOffset)
				{
					targetPos.Add(Pos.X, Pos.Y, Pos.Z);
				}
				val.Value.Entity.TeleportTo(targetPos);
				toremove.Add(val.Key);
				Entity e = val.Value.Entity;
				if (e is EntityPlayer)
				{
					Api.World.Logger.Audit("Teleporting player {0} from {1} to {2}", (e as EntityPlayer).GetBehavior<EntityBehaviorNameTag>().DisplayName, e.Pos.AsBlockPos, tpTarget);
				}
				else
				{
					Api.World.Logger.Audit("Teleporting entity {0} from {1} to {2}", e.Code, e.Pos.AsBlockPos, tpTarget);
				}
				didTeleport(val.Value.Entity);
				somebodyIsTeleporting = false;
				somebodyDidTeleport = true;
				MarkDirty();
			}
		}
		foreach (long entityid in toremove)
		{
			tpingEntities.Remove(entityid);
		}
		if (wasTeleporting && !somebodyIsTeleporting)
		{
			MarkDirty();
		}
	}

	protected virtual void didTeleport(Entity entity)
	{
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		somebodyIsTeleporting = tree.GetBool("somebodyIsTeleporting");
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("somebodyIsTeleporting", somebodyIsTeleporting);
		tree.SetBool("somebodyDidTeleport", somebodyDidTeleport);
		somebodyDidTeleport = false;
	}
}
