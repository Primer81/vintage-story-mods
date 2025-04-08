using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockLog : Block
{
	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return Drops[0].ResolvedItemstack.Clone();
	}

	public override void AddMiningTierInfo(StringBuilder sb)
	{
		if (Code.PathStartsWith("log-grown"))
		{
			int woodTier = Attributes?["treeFellingGroupSpreadIndex"].AsInt() ?? 0;
			woodTier += RequiredMiningTier - 4;
			if (woodTier < RequiredMiningTier)
			{
				woodTier = RequiredMiningTier;
			}
			string tierName = "?";
			if (woodTier < Block.miningTierNames.Length)
			{
				tierName = Block.miningTierNames[woodTier];
			}
			sb.AppendLine(Lang.Get("Requires tool tier {0} ({1}) to break", woodTier, (tierName == "?") ? tierName : Lang.Get(tierName)));
		}
		else
		{
			base.AddMiningTierInfo(sb);
		}
	}
}
