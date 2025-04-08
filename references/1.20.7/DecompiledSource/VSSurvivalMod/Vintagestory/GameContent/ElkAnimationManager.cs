using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class ElkAnimationManager : AnimationManager
{
	public string animAppendix = "-antlers";

	public override void ResetAnimation(string animCode)
	{
		base.ResetAnimation(animCode);
		base.ResetAnimation(animCode + animAppendix);
	}

	public override void StopAnimation(string code)
	{
		base.StopAnimation(code);
		base.StopAnimation(code + animAppendix);
	}

	public override bool StartAnimation(AnimationMetaData animdata)
	{
		return base.StartAnimation(animdata);
	}
}
