using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class OnHurtAimingAccuracy : AccuracyModifier
{
	private float accuracyPenalty;

	public OnHurtAimingAccuracy(EntityAgent entity)
		: base(entity)
	{
	}

	public override void Update(float dt, ref float accuracy)
	{
		accuracyPenalty = GameMath.Clamp(accuracyPenalty - dt / 3f, 0f, 0.4f);
	}

	public override void OnHurt(float damage)
	{
		if (damage > 3f)
		{
			float rangedAcc = entity.Stats.GetBlended("rangedWeaponsAcc");
			accuracyPenalty = -0.4f / Math.Max(1f, rangedAcc);
		}
	}
}
