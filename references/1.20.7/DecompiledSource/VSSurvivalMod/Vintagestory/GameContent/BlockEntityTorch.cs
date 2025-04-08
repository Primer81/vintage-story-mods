using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class BlockEntityTorch : BlockEntityTransient, ITemperatureSensitive
{
	public bool IsHot => true;

	public override void Initialize(ICoreAPI api)
	{
		CheckIntervalMs = 1000;
		base.Initialize(api);
	}

	public override void CheckTransition(float dt)
	{
		if (Api.World.Rand.NextDouble() < 0.3)
		{
			base.CheckTransition(dt);
		}
	}

	public void CoolNow(float amountRel)
	{
		if (!(Api.World.Rand.NextDouble() < (double)(amountRel * 5f)))
		{
			return;
		}
		Api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish"), Pos, 0.25, null, randomizePitch: false, 16f);
		if (Api.World.Rand.NextDouble() < 0.2 + (double)(amountRel / 2f))
		{
			Block block = Api.World.BlockAccessor.GetBlock(Pos);
			if (block.Attributes != null)
			{
				string toCode = block.CodeWithVariant("state", "extinct").ToShortString();
				tryTransition(toCode);
			}
		}
	}
}
