using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityBehaviorDeadDecay : EntityBehavior
{
	private ITreeAttribute decayTree;

	private JsonObject typeAttributes;

	public float HoursToDecay { get; set; }

	public double TotalHoursDead
	{
		get
		{
			return decayTree.GetDouble("totalHoursDead");
		}
		set
		{
			decayTree.SetDouble("totalHoursDead", value);
		}
	}

	public EntityBehaviorDeadDecay(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
	{
		base.Initialize(properties, typeAttributes);
		(entity as EntityAgent).AllowDespawn = false;
		this.typeAttributes = typeAttributes;
		HoursToDecay = typeAttributes["hoursToDecay"].AsFloat(96f);
		decayTree = entity.WatchedAttributes.GetTreeAttribute("decay");
		if (decayTree == null)
		{
			entity.WatchedAttributes.SetAttribute("decay", decayTree = new TreeAttribute());
			TotalHoursDead = entity.World.Calendar.TotalHours;
		}
	}

	public override void OnGameTick(float deltaTime)
	{
		if (!entity.Alive && TotalHoursDead + (double)HoursToDecay < entity.World.Calendar.TotalHours)
		{
			DecayNow();
		}
		base.OnGameTick(deltaTime);
	}

	public void DecayNow()
	{
		if ((entity as EntityAgent).AllowDespawn)
		{
			return;
		}
		(entity as EntityAgent).AllowDespawn = true;
		if (typeAttributes["decayedBlock"].Exists)
		{
			AssetLocation blockcode = new AssetLocation(typeAttributes["decayedBlock"].AsString());
			Block decblock = entity.World.GetBlock(blockcode);
			double num = entity.ServerPos.X + (double)entity.SelectionBox.X1 - (double)entity.OriginSelectionBox.X1;
			double y = entity.ServerPos.Y + (double)entity.SelectionBox.Y1 - (double)entity.OriginSelectionBox.Y1;
			double z = entity.ServerPos.Z + (double)entity.SelectionBox.Z1 - (double)entity.OriginSelectionBox.Z1;
			BlockPos bonepos = new BlockPos((int)num, (int)y, (int)z);
			IBlockAccessor bl = entity.World.BlockAccessor;
			if (bl.GetBlock(bonepos).IsReplacableBy(decblock))
			{
				bl.SetBlock(decblock.BlockId, bonepos);
				bl.MarkBlockDirty(bonepos);
			}
			else
			{
				BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
				for (int i = 0; i < hORIZONTALS.Length; i++)
				{
					hORIZONTALS[i].IterateThruFacingOffsets(bonepos);
					if (entity.World.BlockAccessor.GetBlock(bonepos).IsReplacableBy(decblock))
					{
						entity.World.BlockAccessor.SetBlock(decblock.BlockId, bonepos);
						break;
					}
				}
			}
		}
		Vec3d pos = entity.SidedPos.XYZ + entity.CollisionBox.Center - entity.OriginCollisionBox.Center;
		pos.Y += entity.Properties.DeadCollisionBoxSize.Y / 2f;
		entity.World.SpawnParticles(new EntityCubeParticles(entity.World, entity.EntityId, pos, 0.15f, (int)(40f + entity.Properties.DeadCollisionBoxSize.X * 60f), 0.4f, 1f));
	}

	public override void OnEntityDeath(DamageSource damageSourceForDeath)
	{
		base.OnEntityDeath(damageSourceForDeath);
		TotalHoursDead = entity.World.Calendar.TotalHours;
		if (damageSourceForDeath != null && damageSourceForDeath.Source == EnumDamageSource.Void)
		{
			(entity as EntityAgent).AllowDespawn = true;
		}
	}

	public override string PropertyName()
	{
		return "deaddecay";
	}
}
