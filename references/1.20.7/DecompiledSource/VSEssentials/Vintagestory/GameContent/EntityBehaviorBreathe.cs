using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityBehaviorBreathe : EntityBehavior
{
	private ITreeAttribute oxygenTree;

	private float oxygenCached = -1f;

	private float maxOxygen;

	private Cuboidd tmp = new Cuboidd();

	private float breathAccum;

	private float padding = 0.1f;

	private Block suffocationSourceBlock;

	private float damageAccum;

	public float Oxygen
	{
		get
		{
			return oxygenCached = oxygenTree.GetFloat("currentoxygen");
		}
		set
		{
			if (value != oxygenCached)
			{
				oxygenCached = value;
				oxygenTree.SetFloat("currentoxygen", value);
				entity.WatchedAttributes.MarkPathDirty("oxygen");
			}
		}
	}

	public float MaxOxygen
	{
		get
		{
			return maxOxygen;
		}
		set
		{
			maxOxygen = value;
			oxygenTree.SetFloat("maxoxygen", value);
			entity.WatchedAttributes.MarkPathDirty("oxygen");
		}
	}

	public bool HasAir
	{
		get
		{
			return oxygenTree.GetBool("hasair");
		}
		set
		{
			if (oxygenTree.GetBool("hasair") != value)
			{
				oxygenTree.SetBool("hasair", value);
				entity.WatchedAttributes.MarkPathDirty("oxygen");
			}
		}
	}

	public EntityBehaviorBreathe(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
	{
		base.Initialize(properties, typeAttributes);
		oxygenTree = entity.WatchedAttributes.GetTreeAttribute("oxygen");
		if (oxygenTree == null)
		{
			entity.WatchedAttributes.SetAttribute("oxygen", oxygenTree = new TreeAttribute());
			float maxoxy = 40000f;
			if (entity is EntityPlayer)
			{
				maxoxy = entity.World.Config.GetAsInt("lungCapacity", 40000);
			}
			MaxOxygen = typeAttributes["maxoxygen"].AsFloat(maxoxy);
			Oxygen = typeAttributes["currentoxygen"].AsFloat(MaxOxygen);
			HasAir = true;
		}
		else
		{
			maxOxygen = oxygenTree.GetFloat("maxoxygen");
		}
		breathAccum = (float)entity.World.Rand.NextDouble();
	}

	public override void OnEntityRevive()
	{
		Oxygen = MaxOxygen;
	}

	public void Check()
	{
		maxOxygen = oxygenTree.GetFloat("maxoxygen");
		if (entity.World.Side == EnumAppSide.Client)
		{
			return;
		}
		bool nowHasAir = true;
		if (entity is EntityPlayer)
		{
			EntityPlayer plr = (EntityPlayer)entity;
			EnumGameMode mode = entity.World.PlayerByUid(plr.PlayerUID).WorldData.CurrentGameMode;
			if (mode == EnumGameMode.Creative || mode == EnumGameMode.Spectator)
			{
				HasAir = true;
				return;
			}
		}
		double eyeHeight = (entity.Swimming ? entity.Properties.SwimmingEyeHeight : entity.Properties.EyeHeight);
		double eyeHeightMod1 = (entity.SidedPos.Y + eyeHeight) % 1.0;
		BlockPos pos = new BlockPos((int)(entity.SidedPos.X + entity.LocalEyePos.X), (int)(entity.SidedPos.Y + eyeHeight), (int)(entity.SidedPos.Z + entity.LocalEyePos.Z), entity.SidedPos.Dimension);
		Block block = entity.World.BlockAccessor.GetBlock(pos, 3);
		JsonObject attributes = block.Attributes;
		if (attributes == null || attributes["asphyxiating"].AsBool(defaultValue: true))
		{
			Cuboidf[] collisionboxes = block.GetCollisionBoxes(entity.World.BlockAccessor, pos);
			Cuboidf box = new Cuboidf();
			if (collisionboxes != null)
			{
				for (int i = 0; i < collisionboxes.Length; i++)
				{
					box.Set(collisionboxes[i]);
					box.OmniGrowBy(0f - padding);
					tmp.Set((float)pos.X + box.X1, (float)pos.Y + box.Y1, (float)pos.Z + box.Z1, (float)pos.X + box.X2, (float)pos.Y + box.Y2, (float)pos.Z + box.Z2);
					box.OmniGrowBy(padding);
					if (tmp.Contains(entity.ServerPos.X + entity.LocalEyePos.X, entity.ServerPos.Y + entity.LocalEyePos.Y, entity.ServerPos.Z + entity.LocalEyePos.Z))
					{
						Cuboidd EntitySuffocationBox = entity.SelectionBox.ToDouble();
						if (tmp.Intersects(EntitySuffocationBox))
						{
							nowHasAir = false;
							suffocationSourceBlock = block;
							break;
						}
					}
				}
			}
		}
		if (block.IsLiquid() && (double)((float)block.LiquidLevel / 7f) > eyeHeightMod1)
		{
			nowHasAir = false;
		}
		HasAir = nowHasAir;
	}

	public override void OnGameTick(float deltaTime)
	{
		if (entity.State == EnumEntityState.Inactive)
		{
			return;
		}
		if (!HasAir)
		{
			float oxygen = (Oxygen = Math.Max(0f, Oxygen - deltaTime * 1000f));
			if (oxygen <= 0f && entity.World.Side == EnumAppSide.Server)
			{
				damageAccum += deltaTime;
				if ((double)damageAccum > 0.75)
				{
					damageAccum = 0f;
					DamageSource dmgsrc = new DamageSource
					{
						Source = EnumDamageSource.Block,
						SourceBlock = suffocationSourceBlock,
						Type = EnumDamageType.Suffocation
					};
					entity.ReceiveDamage(dmgsrc, 0.5f);
				}
			}
		}
		else
		{
			Oxygen = Math.Min(MaxOxygen, Oxygen + deltaTime * 10000f);
		}
		base.OnGameTick(deltaTime);
		breathAccum += deltaTime;
		if (breathAccum > 1f)
		{
			breathAccum = 0f;
			Check();
		}
	}

	public override string PropertyName()
	{
		return "breathe";
	}
}
