using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockGlassPane : BlockRainAmbient
{
	public BlockFacing Orientation { get; set; }

	public string Frame { get; set; }

	public string GlassType { get; set; }

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		Orientation = BlockFacing.FromFirstLetter(Variant["type"].Substring(0, 1));
		string w = Variant["wood"];
		Frame = ((w != null) ? string.Intern(w) : null);
		string g = Variant["glass"];
		GlassType = ((g != null) ? string.Intern(g) : null);
	}

	protected AssetLocation OrientedAsset(string orientation)
	{
		return CodeWithVariants(new string[3] { "glass", "wood", "type" }, new string[3] { GlassType, Frame, orientation });
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			BlockFacing horVer = OrientForPlacement(world.BlockAccessor, byPlayer, blockSel);
			string orientation = ((horVer == BlockFacing.NORTH || horVer == BlockFacing.SOUTH) ? "ns" : "ew");
			AssetLocation newCode = OrientedAsset(orientation);
			world.BlockAccessor.SetBlock(world.BlockAccessor.GetBlock(newCode).BlockId, blockSel.Position);
			return true;
		}
		return false;
	}

	protected virtual BlockFacing OrientForPlacement(IBlockAccessor world, IPlayer player, BlockSelection bs)
	{
		BlockFacing[] facings = Block.SuggestedHVOrientation(player, bs);
		BlockFacing suggested = ((facings.Length != 0) ? facings[0] : null);
		BlockPos pos = bs.Position;
		Block block = world.GetBlock(pos.UpCopy());
		Block downBlock = world.GetBlock(pos.DownCopy());
		int num = ((block is BlockGlassPane ub) ? ((ub.Orientation == BlockFacing.EAST) ? 1 : (-1)) : 0);
		int downConnect = ((downBlock is BlockGlassPane db) ? ((db.Orientation == BlockFacing.EAST) ? 1 : (-1)) : 0);
		int vertConnect = num + downConnect;
		if (vertConnect > 0)
		{
			return BlockFacing.EAST;
		}
		if (vertConnect < 0)
		{
			return BlockFacing.NORTH;
		}
		Block westBlock = world.GetBlock(pos.WestCopy());
		Block eastBlock = world.GetBlock(pos.EastCopy());
		Block northBlock = world.GetBlock(pos.NorthCopy());
		Block southBlock = world.GetBlock(pos.SouthCopy());
		int westConnect = ((westBlock is BlockGlassPane wb && wb.Orientation == BlockFacing.NORTH) ? 1 : 0);
		int eastConnect = ((eastBlock is BlockGlassPane eb && eb.Orientation == BlockFacing.NORTH) ? 1 : 0);
		int northConnect = ((northBlock is BlockGlassPane nb && nb.Orientation == BlockFacing.EAST) ? 1 : 0);
		int southConnect = ((southBlock is BlockGlassPane sb && sb.Orientation == BlockFacing.EAST) ? 1 : 0);
		if (westConnect + eastConnect - northConnect - southConnect > 0)
		{
			return BlockFacing.NORTH;
		}
		if (northConnect + southConnect - westConnect - eastConnect > 0)
		{
			return BlockFacing.EAST;
		}
		int westLight = westBlock.GetLightAbsorption(world, pos.WestCopy()) + eastBlock.GetLightAbsorption(world, pos.EastCopy());
		int northLight = northBlock.GetLightAbsorption(world, pos.NorthCopy()) + southBlock.GetLightAbsorption(world, pos.SouthCopy());
		if (westLight < northLight)
		{
			return BlockFacing.EAST;
		}
		if (westLight > northLight)
		{
			return BlockFacing.NORTH;
		}
		return suggested;
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		return new ItemStack[1] { OnPickBlock(world, pos) };
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return new ItemStack(world.BlockAccessor.GetBlock(OrientedAsset("ew")));
	}

	public override AssetLocation GetRotatedBlockCode(int angle)
	{
		BlockFacing nowFacing = BlockFacing.FromFirstLetter(Variant["type"][0].ToString() ?? "");
		BlockFacing rotatedFacing = BlockFacing.HORIZONTALS_ANGLEORDER[(nowFacing.HorizontalAngleIndex + angle / 90) % 4];
		string type = Variant["type"];
		if (nowFacing.Axis != rotatedFacing.Axis)
		{
			type = ((type == "ns") ? "ew" : "ns");
		}
		return CodeWithVariant("type", type);
	}

	public override int GetRetention(BlockPos pos, BlockFacing facing, EnumRetentionType type)
	{
		return 1;
	}
}
