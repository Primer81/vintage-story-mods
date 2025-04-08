using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityBehaviorTiredness : EntityBehavior
{
	public Random Rand;

	private double hoursTotal;

	private long listenerId;

	public float Tiredness
	{
		get
		{
			return entity.WatchedAttributes.GetTreeAttribute("tiredness").GetFloat("tiredness");
		}
		set
		{
			entity.WatchedAttributes.GetTreeAttribute("tiredness").SetFloat("tiredness", value);
			entity.WatchedAttributes.MarkPathDirty("tiredness");
		}
	}

	public bool IsSleeping
	{
		get
		{
			ITreeAttribute attr = entity.WatchedAttributes.GetTreeAttribute("tiredness");
			if (attr != null)
			{
				return attr.GetInt("isSleeping") > 0;
			}
			return false;
		}
		set
		{
			entity.WatchedAttributes.GetTreeAttribute("tiredness").SetInt("isSleeping", value ? 1 : 0);
			entity.WatchedAttributes.MarkPathDirty("tiredness");
		}
	}

	public EntityBehaviorTiredness(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
	{
		ITreeAttribute tiredTree = entity.WatchedAttributes.GetTreeAttribute("tiredness");
		if (tiredTree == null)
		{
			entity.WatchedAttributes.SetAttribute("tiredness", tiredTree = new TreeAttribute());
			Tiredness = typeAttributes["currenttiredness"].AsFloat();
		}
		listenerId = entity.World.RegisterGameTickListener(SlowTick, 3000);
		hoursTotal = entity.World.Calendar.TotalHours;
	}

	private void SlowTick(float dt)
	{
		bool sleeping = IsSleeping;
		if (sleeping && (entity as EntityAgent)?.MountedOn == null)
		{
			sleeping = (IsSleeping = false);
		}
		if (!sleeping && entity.World.Side != EnumAppSide.Client)
		{
			float hoursPassed = (float)(entity.World.Calendar.TotalHours - hoursTotal);
			Tiredness = GameMath.Clamp(Tiredness + hoursPassed * 0.75f, 0f, entity.World.Calendar.HoursPerDay / 2f);
			hoursTotal = entity.World.Calendar.TotalHours;
		}
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		base.OnEntityDespawn(despawn);
		entity.World.UnregisterGameTickListener(listenerId);
	}

	public override string PropertyName()
	{
		return "tiredness";
	}
}
