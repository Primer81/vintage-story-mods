using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockSnowLayer : BlockLayered
{
	private int height;

	private bool canMelt;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		height = Variant["height"].ToInt();
		snowLevel = height;
		notSnowCovered = api.World.GetBlock(0);
		snowCovered1 = api.World.GetBlock(CodeWithVariant("height", "1"));
		snowCovered2 = api.World.GetBlock(CodeWithVariant("height", "2"));
		snowCovered3 = api.World.GetBlock(CodeWithVariant("height", "3"));
		canMelt = api.World.Config.GetBool("snowAccum", defaultValue: true);
	}

	public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object extra)
	{
		extra = null;
		if (!canMelt)
		{
			return false;
		}
		ClimateCondition conds = world.BlockAccessor.GetClimateAt(pos);
		if (conds != null)
		{
			return offThreadRandom.NextDouble() < (double)GameMath.Clamp((conds.Temperature - 0.5f) / (15f - 10f * conds.Rainfall), 0f, 1f);
		}
		return false;
	}

	public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object extra = null)
	{
		if (height == 1)
		{
			world.BlockAccessor.SetBlock(0, pos);
			return;
		}
		Block block = world.GetBlock(CodeWithVariant("height", (height - 1).ToString() ?? ""));
		world.BlockAccessor.SetBlock(block.Id, pos);
	}
}
