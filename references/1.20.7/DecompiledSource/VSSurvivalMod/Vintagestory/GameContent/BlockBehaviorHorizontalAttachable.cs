using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockBehaviorHorizontalAttachable : BlockBehavior
{
	private bool handleDrops = true;

	private string dropBlockFace = "north";

	private string dropBlock;

	private Dictionary<string, Cuboidi> attachmentAreas;

	public BlockBehaviorHorizontalAttachable(Block block)
		: base(block)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		handleDrops = properties["handleDrops"].AsBool(defaultValue: true);
		if (properties["dropBlockFace"].Exists)
		{
			dropBlockFace = properties["dropBlockFace"].AsString();
		}
		if (properties["dropBlock"].Exists)
		{
			dropBlock = properties["dropBlock"].AsString();
		}
		Dictionary<string, RotatableCube> areas = properties["attachmentAreas"].AsObject<Dictionary<string, RotatableCube>>();
		attachmentAreas = new Dictionary<string, Cuboidi>();
		if (areas != null)
		{
			foreach (KeyValuePair<string, RotatableCube> val in areas)
			{
				val.Value.Origin.Set(8.0, 8.0, 8.0);
				attachmentAreas[val.Key] = val.Value.RotatedCopy().ConvertToCuboidi();
			}
			return;
		}
		attachmentAreas["up"] = properties["attachmentArea"].AsObject<Cuboidi>();
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
	{
		handling = EnumHandling.PreventDefault;
		if (blockSel.Face.IsHorizontal && TryAttachTo(world, byPlayer, blockSel, itemstack, ref failureCode))
		{
			return true;
		}
		BlockFacing[] faces = BlockFacing.HORIZONTALS;
		blockSel = blockSel.Clone();
		for (int i = 0; i < faces.Length; i++)
		{
			blockSel.Face = faces[i];
			if (TryAttachTo(world, byPlayer, blockSel, itemstack, ref failureCode))
			{
				return true;
			}
		}
		failureCode = "requirehorizontalattachable";
		return false;
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropQuantityMultiplier, ref EnumHandling handled)
	{
		if (handleDrops)
		{
			handled = EnumHandling.PreventDefault;
			if (dropBlock == null)
			{
				return new ItemStack[1]
				{
					new ItemStack(world.BlockAccessor.GetBlock(block.CodeWithParts(dropBlockFace)))
				};
			}
			return new ItemStack[1]
			{
				new ItemStack(world.BlockAccessor.GetBlock(new AssetLocation(dropBlock)))
			};
		}
		handled = EnumHandling.PassThrough;
		return null;
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		if (dropBlock != null)
		{
			return new ItemStack(world.BlockAccessor.GetBlock(new AssetLocation(dropBlock)));
		}
		return new ItemStack(world.BlockAccessor.GetBlock(block.CodeWithParts(dropBlockFace)));
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		if (!CanBlockStay(world, pos))
		{
			world.BlockAccessor.BreakBlock(pos, null);
		}
	}

	private bool TryAttachTo(IWorldAccessor world, IPlayer player, BlockSelection blockSel, ItemStack itemstack, ref string failureCode)
	{
		BlockFacing oppositeFace = blockSel.Face.Opposite;
		BlockPos attachingBlockPos = blockSel.Position.AddCopy(oppositeFace);
		Block obj = world.BlockAccessor.GetBlock(attachingBlockPos);
		Block orientedBlock = world.BlockAccessor.GetBlock(block.CodeWithParts(oppositeFace.Code));
		Cuboidi attachmentArea = null;
		attachmentAreas?.TryGetValue(oppositeFace.Code, out attachmentArea);
		if (obj.CanAttachBlockAt(world.BlockAccessor, block, attachingBlockPos, blockSel.Face, attachmentArea) && orientedBlock.CanPlaceBlock(world, player, blockSel, ref failureCode))
		{
			orientedBlock.DoPlaceBlock(world, player, blockSel, itemstack);
			return true;
		}
		return false;
	}

	private bool CanBlockStay(IWorldAccessor world, BlockPos pos)
	{
		BlockFacing facing = BlockFacing.FromCode(block.Code.Path.Split('-')[^1]);
		Block obj = world.BlockAccessor.GetBlock(pos.AddCopy(facing));
		Cuboidi attachmentArea = null;
		attachmentAreas?.TryGetValue(facing.Code, out attachmentArea);
		return obj.CanAttachBlockAt(world.BlockAccessor, block, pos.AddCopy(facing), facing.Opposite, attachmentArea);
	}

	public override bool CanAttachBlockAt(IBlockAccessor world, Block block, BlockPos pos, BlockFacing blockFace, ref EnumHandling handled, Cuboidi attachmentArea = null)
	{
		handled = EnumHandling.PreventDefault;
		return false;
	}

	public override AssetLocation GetRotatedBlockCode(int angle, ref EnumHandling handled)
	{
		handled = EnumHandling.PreventDefault;
		int rotatedIndex = GameMath.Mod(BlockFacing.FromCode(block.LastCodePart()).HorizontalAngleIndex - angle / 90, 4);
		BlockFacing nowFacing = BlockFacing.HORIZONTALS_ANGLEORDER[rotatedIndex];
		return block.CodeWithParts(nowFacing.Code);
	}

	public override AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis, ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		BlockFacing facing = BlockFacing.FromCode(block.LastCodePart());
		if (facing.Axis == axis)
		{
			return block.CodeWithParts(facing.Opposite.Code);
		}
		return block.Code;
	}
}
