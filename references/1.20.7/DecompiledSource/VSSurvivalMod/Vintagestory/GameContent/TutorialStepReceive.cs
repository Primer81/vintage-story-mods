using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.GameContent;

public class TutorialStepReceive : TutorialStepGeneric
{
	private static Dictionary<EnumReceiveType, string> receiveEventMapping = new Dictionary<EnumReceiveType, string>
	{
		{
			EnumReceiveType.Collect,
			"onitemcollected"
		},
		{
			EnumReceiveType.Craft,
			"onitemcrafted"
		},
		{
			EnumReceiveType.Knap,
			"onitemknapped"
		},
		{
			EnumReceiveType.Clayform,
			"onitemclayformed"
		},
		{
			EnumReceiveType.Grab,
			"onitemgrabbed"
		}
	};

	private PlayerReceiveItemWatcher rwatcher;

	protected EnumReceiveType receiveType;

	protected override PlayerMilestoneWatcherGeneric watcher => rwatcher;

	public TutorialStepReceive(ICoreClientAPI capi, string text, ItemStackMatcherDelegate matcher, EnumReceiveType enumReceiveType, int goal)
		: base(capi, text)
	{
		receiveType = enumReceiveType;
		rwatcher = new PlayerReceiveItemWatcher();
		rwatcher.StackMatcher = matcher;
		rwatcher.QuantityGoal = goal;
		rwatcher.MatchEventName = receiveEventMapping[enumReceiveType];
	}

	public override RichTextComponentBase[] GetText(CairoFont font)
	{
		if (rwatcher.QuantityGoal > 1)
		{
			_ = " " + Lang.Get("({0}/{1} collected)", rwatcher.QuantityAchieved, rwatcher.QuantityGoal);
		}
		_ = receiveType;
		string vtmlCode = Lang.Get(text, (rwatcher.QuantityAchieved >= rwatcher.QuantityGoal) ? rwatcher.QuantityGoal : (rwatcher.QuantityGoal - rwatcher.QuantityAchieved));
		vtmlCode = Lang.Get("tutorialstep-numbered", index + 1, vtmlCode);
		return VtmlUtil.Richtextify(capi, vtmlCode, font);
	}

	public override bool OnItemStackReceived(ItemStack stack, string eventName)
	{
		rwatcher.OnItemStackReceived(stack, eventName);
		if (rwatcher.Dirty)
		{
			rwatcher.Dirty = false;
			return true;
		}
		return false;
	}
}
