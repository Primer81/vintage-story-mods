using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockTermiteMound : BlockRequireSolidGround
{
	private static Dictionary<int, Block> mediumTermiteBlockCodeByRockid = new Dictionary<int, Block>();

	private static Dictionary<int, Block> largeTermiteBlockCodeByRockid = new Dictionary<int, Block>();

	private bool islarge;

	public override void OnUnloaded(ICoreAPI api)
	{
		mediumTermiteBlockCodeByRockid.Clear();
		largeTermiteBlockCodeByRockid.Clear();
	}

	public override void OnLoaded(ICoreAPI api)
	{
		islarge = Variant["size"] == "large";
		Block rockBlock = api.World.GetBlock(new AssetLocation("rock-" + Variant["rock"]));
		(islarge ? largeTermiteBlockCodeByRockid : mediumTermiteBlockCodeByRockid)[rockBlock.Id] = api.World.GetBlock(CodeWithVariant("rock", Variant["rock"]));
		base.OnLoaded(api);
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, BlockPatchAttributes attributes = null)
	{
		if (!HasSolidGround(blockAccessor, pos))
		{
			return false;
		}
		if (islarge)
		{
			if (!blockAccessor.GetBlockRaw(pos.X - 1, pos.InternalY - 1, pos.Z - 1, 1).SideSolid[BlockFacing.UP.Index])
			{
				return false;
			}
			if (!blockAccessor.GetBlockRaw(pos.X, pos.InternalY - 1, pos.Z - 1, 1).SideSolid[BlockFacing.UP.Index])
			{
				return false;
			}
			if (!blockAccessor.GetBlockRaw(pos.X + 1, pos.InternalY - 1, pos.Z - 1, 1).SideSolid[BlockFacing.UP.Index])
			{
				return false;
			}
			if (!blockAccessor.GetBlockRaw(pos.X + 1, pos.InternalY - 1, pos.Z, 1).SideSolid[BlockFacing.UP.Index])
			{
				return false;
			}
			if (!blockAccessor.GetBlockRaw(pos.X + 1, pos.InternalY - 1, pos.Z + 1, 1).SideSolid[BlockFacing.UP.Index])
			{
				return false;
			}
			if (!blockAccessor.GetBlockRaw(pos.X, pos.InternalY - 1, pos.Z + 1, 1).SideSolid[BlockFacing.UP.Index])
			{
				return false;
			}
			if (!blockAccessor.GetBlockRaw(pos.X - 1, pos.InternalY - 1, pos.Z + 1, 1).SideSolid[BlockFacing.UP.Index])
			{
				return false;
			}
			if (!blockAccessor.GetBlockRaw(pos.X - 1, pos.InternalY - 1, pos.Z, 1).SideSolid[BlockFacing.UP.Index])
			{
				return false;
			}
		}
		int ch = 32;
		int rockId = blockAccessor.GetMapChunkAtBlockPos(pos).TopRockIdMap[pos.Z % ch * ch + pos.X % ch];
		Block tblock = null;
		if (islarge)
		{
			largeTermiteBlockCodeByRockid.TryGetValue(rockId, out tblock);
		}
		else
		{
			mediumTermiteBlockCodeByRockid.TryGetValue(rockId, out tblock);
		}
		if (tblock != null)
		{
			blockAccessor.SetBlock(tblock.Id, pos);
		}
		return true;
	}
}
