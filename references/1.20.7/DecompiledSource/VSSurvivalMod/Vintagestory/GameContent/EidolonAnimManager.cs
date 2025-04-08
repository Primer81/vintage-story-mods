using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class EidolonAnimManager : AnimationManager
{
	public override void OnReceivedServerAnimations(int[] activeAnimations, int activeAnimationsCount, float[] activeAnimationSpeeds)
	{
		base.OnReceivedServerAnimations(activeAnimations, activeAnimationsCount, activeAnimationSpeeds);
		if (ActiveAnimationsByAnimCode.ContainsKey("inactive"))
		{
			StopAnimation("idle");
		}
	}
}
