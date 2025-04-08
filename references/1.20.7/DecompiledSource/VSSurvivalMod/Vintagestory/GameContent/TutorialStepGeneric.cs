using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public abstract class TutorialStepGeneric : TutorialStepBase
{
	protected ICoreClientAPI capi;

	protected abstract PlayerMilestoneWatcherGeneric watcher { get; }

	public override bool Complete => watcher.MilestoneReached();

	protected TutorialStepGeneric(ICoreClientAPI capi, string text)
	{
		this.capi = capi;
		base.text = text;
	}

	public override RichTextComponentBase[] GetText(CairoFont font)
	{
		string vtmlCode = Lang.Get(text, (watcher.QuantityAchieved >= watcher.QuantityGoal) ? watcher.QuantityGoal : (watcher.QuantityGoal - watcher.QuantityAchieved));
		return VtmlUtil.Richtextify(capi, vtmlCode, font);
	}

	public override void Restart()
	{
		watcher.Restart();
	}

	public override void Skip()
	{
		watcher.Skip();
	}

	public override void FromJson(JsonObject job)
	{
		watcher.FromJson(job[code]);
	}

	public override void ToJson(JsonObject job)
	{
		JObject childJobj = new JObject();
		watcher.ToJson(new JsonObject(childJobj));
		if (childJobj.Count > 0)
		{
			job.Token[code] = childJobj;
		}
	}
}
