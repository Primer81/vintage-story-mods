using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class SprintAimingAccuracy : AccuracyModifier
{
	private float accuracyPenalty;

	public SprintAimingAccuracy(EntityAgent entity)
		: base(entity)
	{
	}

	public override void Update(float dt, ref float accuracy)
	{
		float rangedAcc = entity.Stats.GetBlended("rangedWeaponsAcc");
		if (entity.Controls.TriesToMove && entity.Controls.Sprint)
		{
			accuracyPenalty = GameMath.Clamp(accuracyPenalty + dt / 0.75f, 0f, 0.3f);
		}
		else
		{
			accuracyPenalty = GameMath.Clamp(accuracyPenalty - dt / 2f, 0f, 0.3f);
		}
		accuracy -= accuracyPenalty / Math.Max(1f, rangedAcc);
	}
}
