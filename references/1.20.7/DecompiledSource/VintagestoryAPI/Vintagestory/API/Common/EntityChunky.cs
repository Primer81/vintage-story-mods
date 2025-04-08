using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

public class EntityChunky : Entity
{
	protected IMiniDimension blocks;

	/// <summary>
	/// Used to map chunks from load/save game and server-client packets to this specific entity.
	/// The position of saved chunks will include a reference to this index
	/// </summary>
	protected int subDimensionIndex;

	/// <summary>
	/// Whether or not the EntityChunky is interactable.
	/// </summary>
	public override bool IsInteractable => false;

	public override bool ApplyGravity => false;

	public override double SwimmingOffsetY => base.SwimmingOffsetY;

	public EntityChunky()
		: base(GlobalConstants.DefaultSimulationRange)
	{
		Stats = new EntityStats(this);
		WatchedAttributes.SetAttribute("dim", new IntAttribute());
	}

	public static EntityChunky CreateAndLinkWithDimension(ICoreServerAPI sapi, IMiniDimension dimension)
	{
		EntityChunky obj = (EntityChunky)sapi.World.ClassRegistry.CreateEntity("EntityChunky");
		obj.Code = new AssetLocation("chunky");
		obj.AssociateWithDimension(dimension);
		return obj;
	}

	public void AssociateWithDimension(IMiniDimension blocks)
	{
		this.blocks = blocks;
		subDimensionIndex = blocks.subDimensionId;
		(WatchedAttributes.GetAttribute("dim") as IntAttribute).value = subDimensionIndex;
		ServerPos.SetFrom(blocks.CurrentPos);
		Pos = blocks.CurrentPos;
	}

	public override void Initialize(EntityProperties properties, ICoreAPI api, long chunkindex3d)
	{
		World = api.World;
		Api = api;
		base.Properties = properties;
		Class = properties.Class;
		InChunkIndex3d = chunkindex3d;
		alive = WatchedAttributes.GetInt("entityDead") == 0;
		WatchedAttributes.RegisterModifiedListener("onFire", base.updateOnFire);
		if (base.Properties.CollisionBoxSize != null || properties.SelectionBoxSize != null)
		{
			updateColSelBoxes();
		}
		DoInitialActiveCheck(api);
		base.Properties.Initialize(this, api);
		LocalEyePos.Y = base.Properties.EyeHeight;
		TriggerOnInitialized();
		Swimming = (FeetInLiquid = World.BlockAccessor.GetBlock(Pos.AsBlockPos, 2).IsLiquid());
	}

	public override void OnGameTick(float dt)
	{
		if (blocks == null)
		{
			Die(EnumDespawnReason.Removed);
			return;
		}
		if (blocks.subDimensionId == 0)
		{
			Pos.Yaw = 0f;
			Pos.Pitch = 0f;
			Pos.Roll = 0f;
			return;
		}
		if (World.Side == EnumAppSide.Client)
		{
			base.OnGameTick(dt);
		}
		else
		{
			foreach (EntityBehavior behavior in base.SidedProperties.Behaviors)
			{
				behavior.OnGameTick(dt);
			}
		}
		_ = Alive;
	}

	public override void OnReceivedServerPos(bool isTeleport)
	{
		ServerPos.SetFrom(Pos);
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		if (base.SidedProperties == null)
		{
			return;
		}
		foreach (EntityBehavior behavior in base.SidedProperties.Behaviors)
		{
			behavior.OnEntityDespawn(despawn);
		}
		WatchedAttributes.OnModified.Clear();
	}

	public override void OnReceivedServerAnimations(int[] activeAnimations, int activeAnimationsCount, float[] activeAnimationSpeeds)
	{
	}

	public override void UpdateDebugAttributes()
	{
	}

	public override void StartAnimation(string code)
	{
	}

	public override void StopAnimation(string code)
	{
	}

	public override void Die(EnumDespawnReason reason = EnumDespawnReason.Death, DamageSource damageSourceForDeath = null)
	{
		if (!Alive)
		{
			return;
		}
		Alive = false;
		if (blocks != null)
		{
			if (Api.Side == EnumAppSide.Server)
			{
				blocks.ClearChunks();
				blocks.UnloadUnusedServerChunks();
			}
			else
			{
				((IClientWorldAccessor)World).SetBlocksPreviewDimension(-1);
			}
		}
		DespawnReason = new EntityDespawnData
		{
			Reason = reason,
			DamageSourceForDeath = damageSourceForDeath
		};
	}

	public override void OnCollideWithLiquid()
	{
		base.OnCollideWithLiquid();
	}

	public override bool ShouldReceiveDamage(DamageSource damageSource, float damage)
	{
		return false;
	}

	public override bool ReceiveDamage(DamageSource damageSource, float damage)
	{
		return base.ReceiveDamage(damageSource, damage);
	}

	public override void FromBytes(BinaryReader reader, bool forClient)
	{
		base.FromBytes(reader, forClient);
		subDimensionIndex = WatchedAttributes.GetInt("dim", 1);
		if (Api is ICoreClientAPI capi)
		{
			IMiniDimension dimension = capi.World.GetOrCreateDimension(subDimensionIndex, new Vec3d(Pos));
			blocks = dimension;
			blocks.CurrentPos = Pos;
		}
	}
}
