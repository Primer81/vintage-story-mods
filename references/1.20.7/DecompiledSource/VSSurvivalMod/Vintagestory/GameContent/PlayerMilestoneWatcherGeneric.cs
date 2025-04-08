using Newtonsoft.Json.Linq;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public abstract class PlayerMilestoneWatcherGeneric : PlayerMilestoneWatcherBase
{
	public int QuantityGoal;

	public int QuantityAchieved;

	public bool Dirty;

	public bool MilestoneReached()
	{
		return QuantityAchieved >= QuantityGoal;
	}

	public override void ToJson(JsonObject job)
	{
		if (QuantityAchieved > 0)
		{
			job.Token["achieved"] = new JValue(QuantityAchieved);
		}
	}

	public override void FromJson(JsonObject job)
	{
		QuantityAchieved = job["achieved"].AsInt();
	}

	public void Skip()
	{
		Dirty = true;
		QuantityAchieved = QuantityGoal;
	}

	public void Restart()
	{
		Dirty = true;
		QuantityAchieved = 0;
	}
}
