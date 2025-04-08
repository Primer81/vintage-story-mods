using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class ModSystemFireFromLightning : ModSystem
{
	private ICoreServerAPI api;

	public override double ExecuteOrder()
	{
		return 1.0;
	}

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		api.ModLoader.GetModSystem<WeatherSystemServer>().OnLightningImpactEnd += ModSystemFireFromLightning_OnLightningImpactEnd;
	}

	private void ModSystemFireFromLightning_OnLightningImpactEnd(ref Vec3d impactPos, ref EnumHandling handling)
	{
		if (handling != 0 || !api.World.Config.GetBool("lightningFires"))
		{
			return;
		}
		Random rnd = api.World.Rand;
		BlockPos npos = impactPos.AsBlockPos.Add(rnd.Next(2) - 1, rnd.Next(2) - 1, rnd.Next(2) - 1);
		if (api.World.BlockAccessor.GetBlock(npos).CombustibleProps == null)
		{
			return;
		}
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing facing in aLLFACES)
		{
			BlockPos bpos = npos.AddCopy(facing);
			if (api.World.BlockAccessor.GetBlock(bpos).BlockId == 0 && api.ModLoader.GetModSystem<WeatherSystemBase>().GetEnvironmentWetness(bpos, 10.0) < 0.01)
			{
				api.World.BlockAccessor.SetBlock(api.World.GetBlock(new AssetLocation("fire")).BlockId, bpos);
				api.World.BlockAccessor.GetBlockEntity(bpos)?.GetBehavior<BEBehaviorBurning>()?.OnFirePlaced(facing, null);
			}
		}
	}
}
