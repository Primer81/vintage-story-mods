using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class EntityBehaviorDespawn : EntityBehavior, ITimedDespawn
{
	private float minPlayerDistance = -1f;

	private float belowLightLevel = -1f;

	private float minSeconds = 30f;

	private float accumSeconds;

	private float accumOffset = 2.5f;

	private EnumDespawnMode despawnMode;

	private float deathTimeLocal;

	public float DeathTime
	{
		get
		{
			float? time = entity.Attributes.TryGetFloat("deathTime");
			return deathTimeLocal = ((!time.HasValue) ? 0f : time.Value);
		}
		set
		{
			if (value != deathTimeLocal)
			{
				entity.Attributes.SetFloat("deathTime", value);
				deathTimeLocal = value;
			}
		}
	}

	public float DespawnSeconds
	{
		get
		{
			return minSeconds;
		}
		set
		{
			minSeconds = value;
		}
	}

	public EntityBehaviorDespawn(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject typeAttributes)
	{
		JsonObject minDist = typeAttributes["minPlayerDistance"];
		minPlayerDistance = (minDist.Exists ? minDist.AsFloat() : (-1f));
		JsonObject belowLight = typeAttributes["belowLightLevel"];
		belowLightLevel = (belowLight.Exists ? belowLight.AsFloat() : (-1f));
		int minSecondsOverride = entity.Attributes.GetInt("minsecondsToDespawn");
		if (minSecondsOverride > 0)
		{
			minSeconds = minSecondsOverride;
		}
		else
		{
			minSeconds = typeAttributes["minSeconds"].AsFloat(30f);
			minSeconds += (float)((double)entity.EntityId / 5.0 % (double)(minSeconds / 20f));
		}
		JsonObject obj = typeAttributes["afterDays"];
		if (entity.WatchedAttributes.HasAttribute("despawnTotalDays"))
		{
			despawnMode = (obj.Exists ? EnumDespawnMode.AfterSecondsOrAfterDays : EnumDespawnMode.AfterSecondsOrAfterDaysIgnorePlayer);
		}
		else if (obj.Exists)
		{
			despawnMode = EnumDespawnMode.AfterSecondsOrAfterDays;
			entity.WatchedAttributes.SetDouble("despawnTotalDays", entity.World.Calendar.TotalDays + (double)obj.AsFloat(14f));
		}
		accumOffset += (float)((double)entity.EntityId / 200.0 % 1.0);
		deathTimeLocal = DeathTime;
	}

	public override void OnGameTick(float deltaTime)
	{
		if (!entity.Alive || entity.World.Side == EnumAppSide.Client || !((accumSeconds += deltaTime) > accumOffset))
		{
			return;
		}
		if (despawnMode == EnumDespawnMode.AfterSecondsOrAfterDaysIgnorePlayer && entity.World.Calendar.TotalDays > entity.WatchedAttributes.GetDouble("despawnTotalDays"))
		{
			entity.Die(EnumDespawnReason.Expire);
			accumSeconds = 0f;
			return;
		}
		bool playerInRange = PlayerInRange();
		if (playerInRange || LightLevelOk())
		{
			accumSeconds = 0f;
			DeathTime = 0f;
		}
		else if (despawnMode == EnumDespawnMode.AfterSecondsOrAfterDays && !playerInRange && entity.World.Calendar.TotalDays > entity.WatchedAttributes.GetDouble("despawnTotalDays"))
		{
			entity.Die(EnumDespawnReason.Expire);
			accumSeconds = 0f;
		}
		else if ((DeathTime += accumSeconds) > minSeconds)
		{
			entity.Die(EnumDespawnReason.Expire);
			accumSeconds = 0f;
		}
		else
		{
			accumSeconds = 0f;
		}
	}

	public bool PlayerInRange()
	{
		if (minPlayerDistance < 0f)
		{
			return false;
		}
		return entity.minHorRangeToClient < minPlayerDistance;
	}

	public bool LightLevelOk()
	{
		if (belowLightLevel < 0f)
		{
			return false;
		}
		EntityPos pos = entity.ServerPos;
		return (float)entity.World.BlockAccessor.GetLightLevel((int)pos.X, (int)pos.Y, (int)pos.Z, EnumLightLevelType.MaxLight) >= belowLightLevel;
	}

	public override string PropertyName()
	{
		return "timeddespawn";
	}

	public override void GetInfoText(StringBuilder infotext)
	{
		if (belowLightLevel >= 0f && !LightLevelOk() && entity.Alive)
		{
			infotext.AppendLine(Lang.Get("Deprived of light, might die soon"));
		}
		base.GetInfoText(infotext);
	}

	public void SetDespawnByCalendarDate(double totaldays)
	{
		entity.WatchedAttributes.SetDouble("despawnTotalDays", totaldays);
		despawnMode = EnumDespawnMode.AfterSecondsOrAfterDaysIgnorePlayer;
	}
}
