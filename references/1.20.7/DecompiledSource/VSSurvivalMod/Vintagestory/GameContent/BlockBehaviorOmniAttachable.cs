using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockBehaviorOmniAttachable : BlockBehavior
{
	public string facingCode = "orientation";

	private Dictionary<string, Cuboidi> attachmentAreas;

	public BlockBehaviorOmniAttachable(Block block)
		: base(block)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		facingCode = properties["facingCode"].AsString("orientation");
		Dictionary<string, RotatableCube> areas = properties["attachmentAreas"].AsObject<Dictionary<string, RotatableCube>>();
		if (areas == null)
		{
			return;
		}
		attachmentAreas = new Dictionary<string, Cuboidi>();
		foreach (KeyValuePair<string, RotatableCube> val in areas)
		{
			val.Value.Origin.Set(8.0, 8.0, 8.0);
			attachmentAreas[val.Key] = val.Value.RotatedCopy().ConvertToCuboidi();
		}
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
	{
		handling = EnumHandling.PreventDefault;
		if (TryAttachTo(world, byPlayer, blockSel.Position, blockSel.HitPosition, blockSel.Face, itemstack))
		{
			return true;
		}
		BlockFacing[] faces = BlockFacing.ALLFACES;
		for (int i = 0; i < faces.Length; i++)
		{
			if (TryAttachTo(world, byPlayer, blockSel.Position, blockSel.HitPosition, faces[i], itemstack))
			{
				return true;
			}
		}
		failureCode = "requireattachable";
		return false;
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropQuantityMultiplier, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		Block droppedblock = world.BlockAccessor.GetBlock(block.CodeWithVariant(facingCode, "up"));
		return new ItemStack[1]
		{
			new ItemStack(droppedblock)
		};
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		return new ItemStack(world.BlockAccessor.GetBlock(block.CodeWithVariant(facingCode, "up")));
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		if (!CanStay(world, pos))
		{
			world.BlockAccessor.BreakBlock(pos, null);
		}
	}

	private bool TryAttachTo(IWorldAccessor world, IPlayer byPlayer, BlockPos blockpos, Vec3d hitPosition, BlockFacing onBlockFace, ItemStack itemstack)
	{
		BlockPos attachingBlockPos = blockpos.AddCopy(onBlockFace.Opposite);
		Block attachingBlock = world.BlockAccessor.GetBlock(world.BlockAccessor.GetBlockId(attachingBlockPos));
		Block obj = world.BlockAccessor.GetBlock(blockpos);
		Cuboidi attachmentArea = null;
		attachmentAreas?.TryGetValue(onBlockFace.Code, out attachmentArea);
		if (obj.Replaceable >= 6000 && attachingBlock.CanAttachBlockAt(world.BlockAccessor, block, attachingBlockPos, onBlockFace, attachmentArea))
		{
			world.BlockAccessor.GetBlock(block.CodeWithVariant(facingCode, onBlockFace.Code)).DoPlaceBlock(world, byPlayer, new BlockSelection
			{
				Position = blockpos,
				HitPosition = hitPosition,
				Face = onBlockFace
			}, itemstack);
			return true;
		}
		return false;
	}

	private bool CanStay(IWorldAccessor world, BlockPos pos)
	{
		BlockFacing facing = BlockFacing.FromCode(block.Variant[facingCode]);
		BlockPos attachingBlockPos = pos.AddCopy(facing.Opposite);
		Block obj = world.BlockAccessor.GetBlock(world.BlockAccessor.GetBlockId(attachingBlockPos));
		BlockFacing onFace = facing;
		Cuboidi attachmentArea = null;
		attachmentAreas?.TryGetValue(facing.Code, out attachmentArea);
		return obj.CanAttachBlockAt(world.BlockAccessor, block, attachingBlockPos, onFace, attachmentArea);
	}

	public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, ref EnumHandling handled, Cuboidi attachmentArea = null)
	{
		handled = EnumHandling.PreventDefault;
		return false;
	}

	public override AssetLocation GetRotatedBlockCode(int angle, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		if (block.Variant[facingCode] == "up" || block.Variant[facingCode] == "down")
		{
			return block.Code;
		}
		BlockFacing newFacing = BlockFacing.HORIZONTALS_ANGLEORDER[((360 - angle) / 90 + BlockFacing.FromCode(block.Variant[facingCode]).HorizontalAngleIndex) % 4];
		return block.CodeWithParts(newFacing.Code);
	}

	public override AssetLocation GetVerticallyFlippedBlockCode(ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		if (!(block.Variant[facingCode] == "up"))
		{
			return block.CodeWithVariant(facingCode, "up");
		}
		return block.CodeWithVariant(facingCode, "down");
	}

	public override AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis, ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		BlockFacing facing = BlockFacing.FromCode(block.Variant[facingCode]);
		if (facing.Axis == axis)
		{
			return block.CodeWithVariant(facingCode, facing.Opposite.Code);
		}
		return block.Code;
	}
}
