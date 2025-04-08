using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockGasifier : Block, IIgnitable
{
	EnumIgniteState IIgnitable.OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
	{
		if (api.World.BlockAccessor.GetBlockEntity(pos).GetBehavior<BEBehaviorJonasGasifier>().Lit)
		{
			if (!(secondsIgniting > 3f))
			{
				return EnumIgniteState.Ignitable;
			}
			return EnumIgniteState.IgniteNow;
		}
		return EnumIgniteState.NotIgnitable;
	}

	public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
	{
		BEBehaviorJonasGasifier beb = api.World.BlockAccessor.GetBlockEntity(pos).GetBehavior<BEBehaviorJonasGasifier>();
		if (beb.HasFuel && !beb.Lit)
		{
			if (!(secondsIgniting > 2f))
			{
				return EnumIgniteState.Ignitable;
			}
			return EnumIgniteState.IgniteNow;
		}
		return EnumIgniteState.NotIgnitablePreventDefault;
	}

	public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
	{
		BEBehaviorJonasGasifier behavior = api.World.BlockAccessor.GetBlockEntity(pos).GetBehavior<BEBehaviorJonasGasifier>();
		handling = EnumHandling.PreventDefault;
		behavior.Lit = true;
		behavior.burnStartTotalHours = api.World.Calendar.TotalHours;
		behavior.UpdateState();
	}
}
