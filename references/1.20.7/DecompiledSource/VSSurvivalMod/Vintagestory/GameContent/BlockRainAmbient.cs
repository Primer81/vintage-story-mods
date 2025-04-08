using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockRainAmbient : Block
{
	private ICoreClientAPI capi;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		capi = api as ICoreClientAPI;
	}

	public override float GetAmbientSoundStrength(IWorldAccessor world, BlockPos pos)
	{
		ClimateCondition conds = capi.World.Player.Entity.selfClimateCond;
		if (conds != null && conds.Rainfall > 0.1f && conds.Temperature > 3f && (world.BlockAccessor.GetRainMapHeightAt(pos) <= pos.Y || world.BlockAccessor.GetDistanceToRainFall(pos, 3) <= 2))
		{
			return conds.Rainfall;
		}
		return 0f;
	}
}
