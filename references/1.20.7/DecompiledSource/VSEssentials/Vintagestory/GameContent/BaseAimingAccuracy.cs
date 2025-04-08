using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BaseAimingAccuracy : AccuracyModifier
{
	public BaseAimingAccuracy(EntityAgent entity)
		: base(entity)
	{
	}

	public override void Update(float dt, ref float accuracy)
	{
		float rangedAcc = entity.Stats.GetBlended("rangedWeaponsAcc");
		float modspeed = entity.Stats.GetBlended("rangedWeaponsSpeed");
		float maxAccuracy = Math.Min(1f - 0.075f / rangedAcc, 1f);
		accuracy = GameMath.Clamp(base.SecondsSinceAimStart * modspeed * 1.7f, 0f, maxAccuracy);
		if (base.SecondsSinceAimStart >= 0.75f)
		{
			accuracy += GameMath.Sin(base.SecondsSinceAimStart * 8f) / 80f / Math.Max(1f, rangedAcc);
		}
	}
}
