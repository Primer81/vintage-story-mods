using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockWaterLily : BlockPlant
{
	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		IceCheckOffset = -1;
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (CanPlantStay(world.BlockAccessor, blockSel.Position.UpCopy()))
		{
			blockSel = blockSel.Clone();
			blockSel.Position = blockSel.Position.Up();
			return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
		}
		failureCode = "requirefreshwater";
		return false;
	}

	public override bool CanPlantStay(IBlockAccessor blockAccessor, BlockPos pos)
	{
		Block block = blockAccessor.GetBlockBelow(pos, 1, 2);
		Block upblock = blockAccessor.GetBlock(pos, 2);
		if (block.IsLiquid() && block.LiquidLevel == 7 && block.LiquidCode == "water")
		{
			return upblock.Id == 0;
		}
		return false;
	}

	public override int GetColor(ICoreClientAPI capi, BlockPos pos)
	{
		int color = GetColorWithoutTint(capi, pos);
		return capi.World.ApplyColorMapOnRgba("climatePlantTint", "seasonalFoliage", color, pos.X, pos.Y, pos.Z);
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		CompositeTexture tex = Textures.First().Value;
		if (tex?.Baked == null)
		{
			return 0;
		}
		int color = capi.BlockTextureAtlas.GetRandomColor(tex.Baked.TextureSubId, rndIndex);
		return capi.World.ApplyColorMapOnRgba("climatePlantTint", "seasonalFoliage", color, pos.X, pos.Y, pos.Z);
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, BlockPatchAttributes attributes = null)
	{
		if (blockAccessor.GetBlockBelow(pos, 4, 2).Id != 0)
		{
			return false;
		}
		if (blockAccessor.GetBlockBelow(pos, 1, 1) is BlockPlant)
		{
			return false;
		}
		return base.TryPlaceBlockForWorldGen(blockAccessor, pos, onBlockFace, worldGenRand, attributes);
	}
}
