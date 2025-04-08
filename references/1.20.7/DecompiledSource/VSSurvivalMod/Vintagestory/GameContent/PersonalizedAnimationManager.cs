using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class PersonalizedAnimationManager : AnimationManager
{
	public string Personality;

	public HashSet<string> PersonalizedAnimations = new HashSet<string>(new string[9] { "welcome", "idle", "walk", "run", "attack", "laugh", "hurt", "nod", "idle2" });

	public bool All;

	public override bool StartAnimation(string configCode)
	{
		if (PersonalizedAnimations.Contains(configCode.ToLowerInvariant()))
		{
			if (Personality == "formal" || Personality == "rowdy" || Personality == "lazy")
			{
				StopAnimation(Personality + "-idle");
				StopAnimation(Personality + "-idle2");
			}
			return base.StartAnimation(new AnimationMetaData
			{
				Animation = Personality + "-" + configCode,
				Code = Personality + "-" + configCode,
				BlendMode = EnumAnimationBlendMode.Average,
				EaseOutSpeed = 10000f,
				EaseInSpeed = 10000f
			}.Init());
		}
		return base.StartAnimation(configCode);
	}

	public AnimationMetaData Personalize(AnimationMetaData animdata)
	{
		if ((animdata.Code == "idle2" || animdata.Code == "laugh") && ActiveAnimationsByAnimCode.ContainsKey(Personality + "-welcome"))
		{
			return null;
		}
		if (Personality == "formal" || Personality == "rowdy" || Personality == "lazy")
		{
			StopAnimation(Personality + "-idle");
			StopAnimation(Personality + "-laugh");
			StopAnimation(Personality + "-idle2");
		}
		if (All || PersonalizedAnimations.Contains(animdata.Animation.ToLowerInvariant()))
		{
			animdata = animdata.Clone();
			animdata.Animation = Personality + "-" + animdata.Animation;
			animdata.Code = animdata.Animation;
			animdata.CodeCrc32 = AnimationMetaData.GetCrc32(animdata.Code);
		}
		return animdata;
	}

	public override bool StartAnimation(AnimationMetaData animdata)
	{
		animdata = Personalize(animdata);
		if (animdata == null)
		{
			return false;
		}
		return base.StartAnimation(animdata);
	}

	public override bool TryStartAnimation(AnimationMetaData animdata)
	{
		animdata = Personalize(animdata);
		if (animdata == null)
		{
			return false;
		}
		return base.TryStartAnimation(animdata);
	}

	public override void StopAnimation(string code)
	{
		base.StopAnimation(code);
		base.StopAnimation(Personality + "-" + code);
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

	public override bool IsAnimationActive(params string[] anims)
	{
		foreach (string anim in anims)
		{
			if (base.IsAnimationActive(anim, Personality + "-" + anim))
			{
				return true;
			}
		}
		return false;
	}
}
