using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockBehaviorReinforcable : BlockBehavior
{
	public BlockBehaviorReinforcable(Block block)
		: base(block)
	{
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
	{
		if (byPlayer == null)
		{
			return;
		}
		ModSystemBlockReinforcement modBre = world.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
		BlockReinforcement bre = modBre.GetReinforcment(pos);
		if (bre != null && bre.Strength > 0)
		{
			handling = EnumHandling.PreventDefault;
			world.PlaySoundAt(new AssetLocation("sounds/tool/breakreinforced"), pos, 0.0, byPlayer);
			if (!byPlayer.HasPrivilege("denybreakreinforced"))
			{
				modBre.ConsumeStrength(pos, 1);
				world.BlockAccessor.MarkBlockDirty(pos);
			}
		}
	}

	public override void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType, ref EnumHandling handling)
	{
		ModSystemBlockReinforcement modBre = world.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
		BlockReinforcement bre = modBre.GetReinforcment(pos);
		if (bre != null && bre.Strength > 0)
		{
			modBre.ConsumeStrength(pos, 2);
			world.BlockAccessor.MarkBlockDirty(pos);
			handling = EnumHandling.PreventDefault;
		}
		else
		{
			base.OnBlockExploded(world, pos, explosionCenter, blastType, ref handling);
		}
	}

	public override float GetMiningSpeedModifier(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
	{
		BlockReinforcement bre = world.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>().GetReinforcment(pos);
		if (bre != null && bre.Strength > 0 && byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			return 0.6f;
		}
		return 1f;
	}

	public override void OnBlockPlaced(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
	{
		if (world.Side == EnumAppSide.Server)
		{
			world.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>().ClearReinforcement(pos);
		}
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		ModSystemBlockReinforcement modBre = world.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>();
		if (modBre != null)
		{
			BlockReinforcement bre = modBre.GetReinforcment(pos);
			if (bre == null)
			{
				return null;
			}
			StringBuilder sb = new StringBuilder();
			if (bre.GroupUid != 0)
			{
				sb.AppendLine(Lang.Get(bre.Locked ? "Has been locked and reinforced by group {0}." : "Has been reinforced by group {0}.", bre.LastGroupname));
			}
			else
			{
				sb.AppendLine(Lang.Get(bre.Locked ? "Has been locked and reinforced by {0}." : "Has been reinforced by {0}.", bre.LastPlayername));
			}
			sb.AppendLine(Lang.Get("Strength: {0}", bre.Strength));
			return sb.ToString();
		}
		return null;
	}

	public static bool AllowRightClickPickup(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
	{
		BlockReinforcement bre = world.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>().GetReinforcment(pos);
		if (bre != null && bre.Strength > 0)
		{
			return false;
		}
		return true;
	}
}
