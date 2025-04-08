using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityBehaviorCommandable : EntityBehavior
{
	public bool Sit
	{
		get
		{
			return entity.WatchedAttributes.GetBool("commandSit");
		}
		set
		{
			entity.WatchedAttributes.SetBool("commandSit", value);
		}
	}

	public string GuardedName
	{
		get
		{
			return entity.WatchedAttributes.GetString("guardedName");
		}
		set
		{
			entity.WatchedAttributes.SetString("guardedName", value);
		}
	}

	public EntityBehaviorCommandable(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		GuardedName = GetGuardedEntity()?.GetName() ?? "";
	}

	public override void OnEntitySpawn()
	{
		setupTaskBlocker();
	}

	public override void OnEntityLoaded()
	{
		setupTaskBlocker();
	}

	private void setupTaskBlocker()
	{
		if (entity.Api.Side != EnumAppSide.Server)
		{
			return;
		}
		EntityBehaviorTaskAI bhtaskAi = entity.GetBehavior<EntityBehaviorTaskAI>();
		if (bhtaskAi != null)
		{
			bhtaskAi.TaskManager.OnShouldExecuteTask += (IAiTask task) => !Sit || task is AiTaskIdle || task is AiTaskLookAround;
		}
	}

	public override void OnInteract(EntityAgent byEntity, ItemSlot itemslot, Vec3d hitPosition, EnumInteractMode mode, ref EnumHandling handled)
	{
		Sit = !Sit;
	}

	public override void GetInfoText(StringBuilder infotext)
	{
		base.GetInfoText(infotext);
		if (Sit)
		{
			infotext.AppendLine(Lang.Get("Waits"));
		}
		else
		{
			infotext.AppendLine(Lang.Get("Follows {0}", GuardedName));
		}
		ITreeAttribute healthTree = entity.WatchedAttributes.GetTreeAttribute("health");
		if (healthTree != null)
		{
			infotext.AppendLine(Lang.Get("commandable-entity-healthpoints", healthTree.GetFloat("currenthealth"), healthTree.GetFloat("maxhealth")));
		}
	}

	public override string PropertyName()
	{
		return "commandable";
	}

	public Entity GetGuardedEntity()
	{
		string uid = entity.WatchedAttributes.GetString("guardedPlayerUid");
		if (uid != null)
		{
			return entity.World.PlayerByUid(uid)?.Entity;
		}
		long id = entity.WatchedAttributes.GetLong("guardedEntityId", 0L);
		return entity.World.GetEntityById(id);
	}
}
