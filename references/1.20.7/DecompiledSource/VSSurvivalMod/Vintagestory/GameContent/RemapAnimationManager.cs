using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class RemapAnimationManager : AnimationManager
{
	public Dictionary<string, string> remaps;

	public string idleAnim = "crawlidle";

	public RemapAnimationManager()
	{
	}

	public RemapAnimationManager(Dictionary<string, string> remaps)
	{
		this.remaps = remaps;
	}

	public override bool StartAnimation(string configCode)
	{
		if (remaps.ContainsKey(configCode.ToLowerInvariant()))
		{
			configCode = remaps[configCode];
		}
		StopIdle();
		return base.StartAnimation(configCode);
	}

	private void StopIdle()
	{
		StopAnimation(idleAnim);
	}

	public override bool StartAnimation(AnimationMetaData animdata)
	{
		if (remaps.ContainsKey(animdata.Animation))
		{
			animdata = animdata.Clone();
			animdata.Animation = remaps[animdata.Animation];
			animdata.CodeCrc32 = AnimationMetaData.GetCrc32(animdata.Animation);
		}
		StopIdle();
		return base.StartAnimation(animdata);
	}

	public override void StopAnimation(string code)
	{
		base.StopAnimation(code);
		if (remaps.ContainsKey(code))
		{
			base.StopAnimation(remaps[code]);
		}
	}

	public override void TriggerAnimationStopped(string code)
	{
		base.TriggerAnimationStopped(code);
		if (entity.Alive && ActiveAnimationsByAnimCode.Count == 0)
		{
			StartAnimation(new AnimationMetaData
			{
				Code = "idle",
				Animation = "idle",
				EaseOutSpeed = 10000f,
				EaseInSpeed = 10000f
			});
		}
	}

	public override void OnReceivedServerAnimations(int[] activeAnimations, int activeAnimationsCount, float[] activeAnimationSpeeds)
	{
		base.OnReceivedServerAnimations(activeAnimations, activeAnimationsCount, activeAnimationSpeeds);
	}
}
