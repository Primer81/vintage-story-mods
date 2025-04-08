using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public abstract class TutorialStepBase
{
	protected string code;

	public string text;

	public int index;

	public abstract bool Complete { get; }

	public abstract RichTextComponentBase[] GetText(CairoFont font);

	public virtual bool OnItemStackReceived(ItemStack stack, string eventName)
	{
		return false;
	}

	public virtual bool OnBlockPlaced(BlockPos pos, Block block, ItemStack withStackInHands)
	{
		return false;
	}

	public virtual bool OnBlockLookedAt(BlockSelection currentBlockSelection)
	{
		return false;
	}

	public virtual bool OnHotkeyPressed(string hotkeycode, KeyCombination keyComb)
	{
		return false;
	}

	public virtual bool OnAction(EnumEntityAction action, bool on)
	{
		return false;
	}

	public virtual void FromJson(JsonObject job)
	{
	}

	public virtual void ToJson(JsonObject job)
	{
	}

	public static TutorialStepReceive Grab(ICoreClientAPI capi, string code, string text, ItemStackMatcherDelegate matcher, int goal)
	{
		return new TutorialStepReceive(capi, text, matcher, EnumReceiveType.Grab, goal)
		{
			code = code
		};
	}

	public static TutorialStepReceive Collect(ICoreClientAPI capi, string code, string text, ItemStackMatcherDelegate matcher, int goal)
	{
		return new TutorialStepReceive(capi, text, matcher, EnumReceiveType.Collect, goal)
		{
			code = code
		};
	}

	public static TutorialStepReceive Craft(ICoreClientAPI capi, string code, string text, ItemStackMatcherDelegate matcher, int goal)
	{
		return new TutorialStepReceive(capi, text, matcher, EnumReceiveType.Craft, goal)
		{
			code = code
		};
	}

	public static TutorialStepReceive Knap(ICoreClientAPI capi, string code, string text, ItemStackMatcherDelegate matcher, int goal)
	{
		return new TutorialStepReceive(capi, text, matcher, EnumReceiveType.Knap, goal)
		{
			code = code
		};
	}

	public static TutorialStepReceive Clayform(ICoreClientAPI capi, string code, string text, ItemStackMatcherDelegate matcher, int goal)
	{
		return new TutorialStepReceive(capi, text, matcher, EnumReceiveType.Clayform, goal)
		{
			code = code
		};
	}

	public static TutorialStepPlaceBlock Place(ICoreClientAPI capi, string code, string text, BlockMatcherDelegate matcher, int goal)
	{
		return new TutorialStepPlaceBlock(capi, text, matcher, goal)
		{
			code = code
		};
	}

	public static TutorialStepLookatBlock LookAt(ICoreClientAPI capi, string code, string text, BlockLookatMatcherDelegate matcher)
	{
		return new TutorialStepLookatBlock(capi, text, matcher, 1)
		{
			code = code
		};
	}

	public static TutorialStepPressHotkeys Press(ICoreClientAPI capi, string code, string text, params string[] hotkeycodes)
	{
		return new TutorialStepPressHotkeys(capi, text, hotkeycodes)
		{
			code = code
		};
	}

	public abstract void Skip();

	public abstract void Restart();
}
