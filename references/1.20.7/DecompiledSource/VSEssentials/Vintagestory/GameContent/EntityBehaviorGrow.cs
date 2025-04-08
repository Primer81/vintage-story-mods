using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityBehaviorGrow : EntityBehavior
{
	private ITreeAttribute growTree;

	private JsonObject typeAttributes;

	private long callbackId;

	internal float HoursToGrow { get; set; }

	internal AssetLocation[] AdultEntityCodes => AssetLocation.toLocations(typeAttributes["adultEntityCodes"].AsArray(new string[0]));

	internal double TimeSpawned
	{
		get
		{
			return growTree.GetDouble("timeSpawned");
		}
		set
		{
			growTree.SetDouble("timeSpawned", value);
		}
	}

	public EntityBehaviorGrow(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
	{
		base.Initialize(properties, typeAttributes);
		this.typeAttributes = typeAttributes;
		HoursToGrow = typeAttributes["hoursToGrow"].AsFloat(96f);
		growTree = entity.WatchedAttributes.GetTreeAttribute("grow");
		if (growTree == null)
		{
			entity.WatchedAttributes.SetAttribute("grow", growTree = new TreeAttribute());
			TimeSpawned = entity.World.Calendar.TotalHours;
		}
		callbackId = entity.World.RegisterCallback(CheckGrowth, 3000);
	}

	private void CheckGrowth(float dt)
	{
		callbackId = 0L;
		if (!entity.Alive)
		{
			return;
		}
		if (entity.World.Calendar.TotalHours >= TimeSpawned + (double)HoursToGrow)
		{
			AssetLocation[] entityCodes = AdultEntityCodes;
			if (entityCodes.Length == 0)
			{
				return;
			}
			AssetLocation code = entityCodes[entity.World.Rand.Next(entityCodes.Length)];
			EntityProperties adultType = entity.World.GetEntityType(code);
			if (adultType == null)
			{
				entity.World.Logger.Error("Misconfigured entity. Entity with code '{0}' is configured (via Grow behavior) to grow into '{1}', but no such entity type was registered.", entity.Code, code);
				return;
			}
			Cuboidf collisionBox = adultType.SpawnCollisionBox;
			if (entity.World.CollisionTester.IsColliding(entity.World.BlockAccessor, collisionBox, entity.ServerPos.XYZ, alsoCheckTouch: false))
			{
				callbackId = entity.World.RegisterCallback(CheckGrowth, 3000);
				return;
			}
			Entity adult = entity.World.ClassRegistry.CreateEntity(adultType);
			adult.ServerPos.SetFrom(entity.ServerPos);
			adult.Pos.SetFrom(adult.ServerPos);
			adult.WatchedAttributes.SetInt("generation", entity.WatchedAttributes.GetInt("generation"));
			adult.WatchedAttributes.SetDouble("birthTotalDays", entity.World.Calendar.TotalDays);
			if (entity.Properties.Client != null && entity.Properties.Client.TexturesAlternatesCount > 0 && adultType.Client != null && entity.Properties.Client.TexturesAlternatesCount == adultType.Client.TexturesAlternatesCount && entity.WatchedAttributes.HasAttribute("textureIndex"))
			{
				adult.WatchedAttributes.SetAttribute("textureIndex", entity.WatchedAttributes.GetAttribute("textureIndex"));
			}
			entity.Die(EnumDespawnReason.Expire);
			entity.World.SpawnEntity(adult);
		}
		else
		{
			callbackId = entity.World.RegisterCallback(CheckGrowth, 3000);
			double age = entity.World.Calendar.TotalHours - TimeSpawned;
			if (age >= 0.1 * (double)HoursToGrow)
			{
				float newAge = (float)(age / (double)HoursToGrow - 0.1);
				if (newAge >= 1.01f * growTree.GetFloat("age"))
				{
					growTree.SetFloat("age", newAge);
					entity.WatchedAttributes.MarkPathDirty("grow");
				}
			}
		}
		entity.World.FrameProfiler.Mark("checkgrowth");
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		entity.World.UnregisterCallback(callbackId);
	}

	public override string PropertyName()
	{
		return "grow";
	}
}
