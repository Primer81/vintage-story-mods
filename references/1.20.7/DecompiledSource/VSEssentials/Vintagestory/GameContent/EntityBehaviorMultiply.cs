using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class EntityBehaviorMultiply : EntityBehaviorMultiplyBase
{
	private JsonObject typeAttributes;

	private long callbackId;

	private AssetLocation[] spawnEntityCodes;

	private bool eatAnyway;

	internal float PregnancyDays => typeAttributes["pregnancyDays"].AsFloat(3f);

	internal string RequiresNearbyEntityCode => typeAttributes["requiresNearbyEntityCode"].AsString("");

	internal float RequiresNearbyEntityRange => typeAttributes["requiresNearbyEntityRange"].AsFloat(5f);

	public float SpawnQuantityMin => typeAttributes["spawnQuantityMin"].AsFloat(1f);

	public float SpawnQuantityMax => typeAttributes["spawnQuantityMax"].AsFloat(2f);

	public double TotalDaysLastBirth
	{
		get
		{
			return multiplyTree.GetDouble("totalDaysLastBirth", -9999.0);
		}
		set
		{
			multiplyTree.SetDouble("totalDaysLastBirth", value);
			entity.WatchedAttributes.MarkPathDirty("multiply");
		}
	}

	public double TotalDaysPregnancyStart
	{
		get
		{
			return multiplyTree.GetDouble("totalDaysPregnancyStart");
		}
		set
		{
			multiplyTree.SetDouble("totalDaysPregnancyStart", value);
			entity.WatchedAttributes.MarkPathDirty("multiply");
		}
	}

	public bool IsPregnant
	{
		get
		{
			return multiplyTree.GetBool("isPregnant");
		}
		set
		{
			multiplyTree.SetBool("isPregnant", value);
			entity.WatchedAttributes.MarkPathDirty("multiply");
		}
	}

	public override bool ShouldEat
	{
		get
		{
			if (!eatAnyway)
			{
				if (!IsPregnant && GetSaturation() < base.PortionsEatenForMultiply)
				{
					return base.TotalDaysCooldownUntil <= entity.World.Calendar.TotalDays;
				}
				return false;
			}
			return true;
		}
	}

	public EntityBehaviorMultiply(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		typeAttributes = attributes;
		if (entity.World.Side == EnumAppSide.Server)
		{
			if (!multiplyTree.HasAttribute("totalDaysLastBirth"))
			{
				TotalDaysLastBirth = -9999.0;
			}
			callbackId = entity.World.RegisterCallback(CheckMultiply, 3000);
		}
	}

	protected virtual void CheckMultiply(float dt)
	{
		if (!entity.Alive)
		{
			callbackId = 0L;
			return;
		}
		callbackId = entity.World.RegisterCallback(CheckMultiply, 3000);
		if (entity.World.Calendar == null)
		{
			return;
		}
		double daysNow = entity.World.Calendar.TotalDays;
		if (!IsPregnant)
		{
			if (TryGetPregnant())
			{
				IsPregnant = true;
				TotalDaysPregnancyStart = daysNow;
			}
			return;
		}
		if (daysNow - TotalDaysPregnancyStart > (double)PregnancyDays)
		{
			Random rand = entity.World.Rand;
			float q = SpawnQuantityMin + (float)rand.NextDouble() * (SpawnQuantityMax - SpawnQuantityMin);
			TotalDaysLastBirth = daysNow;
			base.TotalDaysCooldownUntil = daysNow + (base.MultiplyCooldownDaysMin + rand.NextDouble() * (base.MultiplyCooldownDaysMax - base.MultiplyCooldownDaysMin));
			IsPregnant = false;
			entity.WatchedAttributes.MarkPathDirty("multiply");
			GiveBirth(q);
		}
		entity.World.FrameProfiler.Mark("multiply");
	}

	protected virtual void GiveBirth(float q)
	{
		Random rand = entity.World.Rand;
		int generation = entity.WatchedAttributes.GetInt("generation");
		if (spawnEntityCodes == null)
		{
			PopulateSpawnEntityCodes();
		}
		if (spawnEntityCodes == null)
		{
			return;
		}
		while (q > 1f || rand.NextDouble() < (double)q)
		{
			q -= 1f;
			AssetLocation SpawnEntityCode = spawnEntityCodes[rand.Next(spawnEntityCodes.Length)];
			EntityProperties childType = entity.World.GetEntityType(SpawnEntityCode);
			if (childType != null)
			{
				Entity childEntity = entity.World.ClassRegistry.CreateEntity(childType);
				childEntity.ServerPos.SetFrom(entity.ServerPos);
				childEntity.ServerPos.Motion.X += (rand.NextDouble() - 0.5) / 20.0;
				childEntity.ServerPos.Motion.Z += (rand.NextDouble() - 0.5) / 20.0;
				childEntity.Pos.SetFrom(childEntity.ServerPos);
				childEntity.Attributes.SetString("origin", "reproduction");
				childEntity.WatchedAttributes.SetInt("generation", generation + 1);
				entity.World.SpawnEntity(childEntity);
			}
		}
	}

	protected virtual void PopulateSpawnEntityCodes()
	{
		JsonObject sec = typeAttributes["spawnEntityCodes"];
		if (!sec.Exists)
		{
			sec = typeAttributes["spawnEntityCode"];
			if (sec.Exists)
			{
				spawnEntityCodes = new AssetLocation[1]
				{
					new AssetLocation(sec.AsString(""))
				};
			}
		}
		else if (sec.IsArray())
		{
			SpawnEntityProperties[] codes = sec.AsArray<SpawnEntityProperties>();
			spawnEntityCodes = new AssetLocation[codes.Length];
			for (int i = 0; i < codes.Length; i++)
			{
				spawnEntityCodes[i] = new AssetLocation(codes[i].Code ?? "");
			}
		}
		else
		{
			spawnEntityCodes = new AssetLocation[1]
			{
				new AssetLocation(sec.AsString(""))
			};
		}
	}

	public override void TestCommand(object arg)
	{
		GiveBirth((int)arg);
	}

	protected virtual bool TryGetPregnant()
	{
		if (entity.World.Rand.NextDouble() > 0.06)
		{
			return false;
		}
		if (base.TotalDaysCooldownUntil > entity.World.Calendar.TotalDays)
		{
			return false;
		}
		ITreeAttribute tree = entity.WatchedAttributes.GetTreeAttribute("hunger");
		if (tree == null)
		{
			return false;
		}
		float saturation = tree.GetFloat("saturation");
		if (saturation >= base.PortionsEatenForMultiply)
		{
			Entity maleentity = null;
			if (RequiresNearbyEntityCode != null && (maleentity = GetRequiredEntityNearby()) == null)
			{
				return false;
			}
			if (entity.World.Rand.NextDouble() < 0.2)
			{
				tree.SetFloat("saturation", saturation - 1f);
				return false;
			}
			tree.SetFloat("saturation", saturation - base.PortionsEatenForMultiply);
			if (maleentity != null)
			{
				ITreeAttribute maletree = maleentity.WatchedAttributes.GetTreeAttribute("hunger");
				if (maletree != null)
				{
					saturation = maletree.GetFloat("saturation");
					maletree.SetFloat("saturation", Math.Max(0f, saturation - 1f));
				}
			}
			IsPregnant = true;
			TotalDaysPregnancyStart = entity.World.Calendar.TotalDays;
			entity.WatchedAttributes.MarkPathDirty("multiply");
			return true;
		}
		return false;
	}

	protected virtual Entity GetRequiredEntityNearby()
	{
		if (RequiresNearbyEntityCode == null)
		{
			return null;
		}
		return entity.World.GetNearestEntity(entity.ServerPos.XYZ, RequiresNearbyEntityRange, RequiresNearbyEntityRange, delegate(Entity e)
		{
			if (e.WildCardMatch(new AssetLocation(RequiresNearbyEntityCode)))
			{
				if (e.WatchedAttributes.GetBool("doesEat"))
				{
					ITreeAttribute obj = e.WatchedAttributes["hunger"] as ITreeAttribute;
					if (obj == null || !(obj.GetFloat("saturation") >= 1f))
					{
						goto IL_005c;
					}
				}
				return true;
			}
			goto IL_005c;
			IL_005c:
			return false;
		});
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		entity.World.UnregisterCallback(callbackId);
	}

	public override string PropertyName()
	{
		return "multiply";
	}

	public override void GetInfoText(StringBuilder infotext)
	{
		multiplyTree = entity.WatchedAttributes.GetTreeAttribute("multiply");
		if (IsPregnant)
		{
			infotext.AppendLine(Lang.Get("Is pregnant"));
		}
		else
		{
			if (!entity.Alive)
			{
				return;
			}
			ITreeAttribute tree = entity.WatchedAttributes.GetTreeAttribute("hunger");
			if (tree != null)
			{
				float saturation = tree.GetFloat("saturation");
				infotext.AppendLine(Lang.Get("Portions eaten: {0}", saturation));
			}
			double daysLeft = base.TotalDaysCooldownUntil - entity.World.Calendar.TotalDays;
			if (daysLeft > 0.0)
			{
				if (daysLeft > 3.0)
				{
					infotext.AppendLine(Lang.Get("Several days left before ready to mate"));
				}
				else
				{
					infotext.AppendLine(Lang.Get("Less than 3 days before ready to mate"));
				}
			}
			else
			{
				infotext.AppendLine(Lang.Get("Ready to mate"));
			}
		}
	}
}
