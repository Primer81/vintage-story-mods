using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockStoneCoffinSection : Block
{
	public BlockFacing Orientation => BlockFacing.FromCode(Variant["side"]);

	public bool ControllerBlock => EntityClass != null;

	public bool IsCompleteCoffin(BlockPos pos)
	{
		if (api.World.BlockAccessor.GetBlock(pos.AddCopy(Orientation.Opposite)) is BlockStoneCoffinSection otherblock)
		{
			return otherblock.Orientation == Orientation.Opposite;
		}
		return false;
	}

	public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		int temp = GetTemperature(api.World, pos);
		int extraGlow = GameMath.Clamp((temp - 550) / 2, 0, 255);
		for (int j = 0; j < sourceMesh.FlagsCount; j++)
		{
			sourceMesh.Flags[j] &= -256;
			sourceMesh.Flags[j] |= extraGlow;
		}
		int[] incade = ColorUtil.getIncandescenceColor(temp);
		float ina = GameMath.Clamp((float)incade[3] / 255f, 0f, 1f);
		for (int i = 0; i < lightRgbsByCorner.Length; i++)
		{
			int num = lightRgbsByCorner[i];
			int r = num & 0xFF;
			int g = (num >> 8) & 0xFF;
			int b = (num >> 16) & 0xFF;
			int a = (num >> 24) & 0xFF;
			lightRgbsByCorner[i] = (GameMath.Mix(a, 0, Math.Min(1f, 1.5f * ina)) << 24) | (GameMath.Mix(b, incade[2], ina) << 16) | (GameMath.Mix(g, incade[1], ina) << 8) | GameMath.Mix(r, incade[0], ina);
		}
	}

	public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
	{
		if (blockFace == BlockFacing.UP && block.FirstCodePart() == "stonecoffinlid")
		{
			return true;
		}
		return base.CanAttachBlockAt(blockAccessor, block, pos, blockFace, attachmentArea);
	}

	public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
	{
		BlockPos pos = blockSel.Position;
		Block blockToPlace = this;
		if (byPlayer != null && !byPlayer.Entity.Controls.ShiftKey)
		{
			BlockFacing[] hORIZONTALS = BlockFacing.HORIZONTALS;
			foreach (BlockFacing face in hORIZONTALS)
			{
				if (world.BlockAccessor.GetBlock(pos.AddCopy(face)) is BlockStoneCoffinSection neib && neib.Orientation == face)
				{
					blockToPlace = api.World.GetBlock(CodeWithVariant("side", face.Opposite.Code));
					break;
				}
			}
		}
		world.BlockAccessor.SetBlock(blockToPlace.BlockId, pos);
		return true;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockPos pos = blockSel.Position;
		if (!ControllerBlock)
		{
			pos = GetControllerBlockPositionOrNull(blockSel.Position);
		}
		if (pos == null)
		{
			return base.OnBlockInteractStart(world, byPlayer, blockSel);
		}
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityStoneCoffin besc)
		{
			besc.Interact(byPlayer, !ControllerBlock);
			(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			return true;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public int GetTemperature(IWorldAccessor world, BlockPos pos)
	{
		if (!ControllerBlock)
		{
			pos = GetControllerBlockPositionOrNull(pos);
		}
		if (pos == null)
		{
			return 0;
		}
		if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityStoneCoffin besc)
		{
			return besc.CoffinTemperature;
		}
		return 0;
	}

	private BlockPos GetControllerBlockPositionOrNull(BlockPos pos)
	{
		if (!ControllerBlock && IsCompleteCoffin(pos))
		{
			return pos.AddCopy(Orientation.Opposite);
		}
		return null;
	}

	public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
	{
		BlockPos npos;
		if ((npos = GetControllerBlockPositionOrNull(pos)) != null)
		{
			return api.World.BlockAccessor.GetBlock(npos).GetPlacedBlockInfo(world, npos, forPlayer);
		}
		return base.GetPlacedBlockInfo(world, pos, forPlayer);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		BlockPos npos;
		if ((npos = GetControllerBlockPositionOrNull(selection.Position)) != null)
		{
			BlockSelection nsele = selection.Clone();
			nsele.Position = npos;
			return api.World.BlockAccessor.GetBlock(npos).GetPlacedBlockInteractionHelp(world, nsele, forPlayer);
		}
		BlockEntityStoneCoffin blockEntity = world.BlockAccessor.GetBlockEntity<BlockEntityStoneCoffin>(selection.Position);
		if (blockEntity != null && !blockEntity.StructureComplete)
		{
			return new WorldInteraction[2]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-mulblock-struc-show",
					HotKeyCodes = new string[1] { "shift" },
					MouseButton = EnumMouseButton.Right
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-mulblock-struc-hide",
					HotKeyCodes = new string[1] { "ctrl" },
					MouseButton = EnumMouseButton.Right
				}
			};
		}
		return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
	}

	public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
		BlockPos npos;
		if ((npos = GetControllerBlockPositionOrNull(pos)) != null)
		{
			world.BlockAccessor.BreakBlock(npos, byPlayer, dropQuantityMultiplier);
		}
		if (ControllerBlock && IsCompleteCoffin(pos))
		{
			world.BlockAccessor.BreakBlock(pos.AddCopy(Orientation.Opposite), byPlayer, dropQuantityMultiplier);
		}
	}
}
