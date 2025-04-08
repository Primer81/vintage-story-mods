using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class EntityBehaviorMultiplyBase : EntityBehavior
{
	protected ITreeAttribute multiplyTree;

	private bool eatAnyway;

	public double MultiplyCooldownDaysMin { get; set; }

	public double MultiplyCooldownDaysMax { get; set; }

	public float PortionsEatenForMultiply { get; set; }

	public double TotalDaysCooldownUntil
	{
		get
		{
			return multiplyTree.GetDouble("totalDaysCooldownUntil");
		}
		set
		{
			multiplyTree.SetDouble("totalDaysCooldownUntil", value);
			entity.WatchedAttributes.MarkPathDirty("multiply");
		}
	}

	public virtual bool ShouldEat
	{
		get
		{
			if (!eatAnyway)
			{
				if (GetSaturation() < PortionsEatenForMultiply)
				{
					return TotalDaysCooldownUntil <= entity.World.Calendar.TotalDays;
				}
				return false;
			}
			return true;
		}
	}

	public virtual float PortionsLeftToEat => PortionsEatenForMultiply - GetSaturation();

	public EntityBehaviorMultiplyBase(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		eatAnyway = attributes["eatAnyway"].AsBool();
		MultiplyCooldownDaysMin = attributes["multiplyCooldownDaysMin"].AsFloat(6f);
		MultiplyCooldownDaysMax = attributes["multiplyCooldownDaysMax"].AsFloat(12f);
		PortionsEatenForMultiply = attributes["portionsEatenForMultiply"].AsFloat(3f);
		multiplyTree = entity.WatchedAttributes.GetTreeAttribute("multiply");
		if (entity.World.Side == EnumAppSide.Server && multiplyTree == null)
		{
			entity.WatchedAttributes.SetAttribute("multiply", multiplyTree = new TreeAttribute());
			double daysNow = entity.World.Calendar.TotalHours / 24.0;
			TotalDaysCooldownUntil = daysNow + (MultiplyCooldownDaysMin + entity.World.Rand.NextDouble() * (MultiplyCooldownDaysMax - MultiplyCooldownDaysMin));
		}
	}

	protected float GetSaturation()
	{
		return entity.WatchedAttributes.GetTreeAttribute("hunger")?.GetFloat("saturation") ?? 0f;
	}

	public override string PropertyName()
	{
		return "multiplybase";
	}

	public override void GetInfoText(StringBuilder infotext)
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
			if (saturation >= PortionsEatenForMultiply)
			{
				infotext.AppendLine(Lang.Get("Ready to lay"));
			}
		}
	}
}
