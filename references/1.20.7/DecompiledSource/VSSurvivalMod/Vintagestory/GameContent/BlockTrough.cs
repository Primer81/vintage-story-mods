using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockTrough : BlockTroughBase
{
	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		init();
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (blockSel != null)
		{
			BlockPos pos = blockSel.Position;
			if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityTrough betr)
			{
				bool num = betr.OnInteract(byPlayer, blockSel);
				if (num && world.Side == EnumAppSide.Client)
				{
					(byPlayer as IClientPlayer).TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
				}
				return num;
			}
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override AssetLocation GetRotatedBlockCode(int angle)
	{
		if (Math.Abs(angle) == 90 || Math.Abs(angle) == 270)
		{
			string orient = Variant["side"];
			return CodeWithVariant("side", (orient == "we") ? "ns" : "we");
		}
		return Code;
	}

	public override AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis)
	{
		BlockFacing facing = BlockFacing.FromCode(LastCodePart());
		if (facing.Axis == axis)
		{
			return CodeWithParts(facing.Opposite.Code);
		}
		return Code;
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		if (LastCodePart(1) == "feet")
		{
			BlockFacing facing = BlockFacing.FromCode(LastCodePart()).Opposite;
			pos = pos.AddCopy(facing);
		}
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityTrough betr)
		{
			StringBuilder dsc = new StringBuilder();
			betr.GetBlockInfo(forPlayer, dsc);
			return dsc.ToString();
		}
		return base.GetPlacedBlockInfo(world, pos, forPlayer);
	}

	public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
	{
		return capi.BlockTextureAtlas.GetRandomColor(Textures["wood"].Baked.TextureSubId, rndIndex);
	}

	public override int GetColorWithoutTint(ICoreClientAPI capi, BlockPos pos)
	{
		int texSubId = Textures["wood"].Baked.TextureSubId;
		return capi.BlockTextureAtlas.GetAverageColor(texSubId);
	}
}
