using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockFullCoating : Block
{
	private BlockFacing[] ownFacings;

	private Cuboidf[] selectionBoxes;

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor world, BlockPos pos)
	{
		return selectionBoxes;
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		string facingletters = Variant["coating"];
		ownFacings = new BlockFacing[facingletters.Length];
		selectionBoxes = new Cuboidf[ownFacings.Length];
		for (int i = 0; i < facingletters.Length; i++)
		{
			ownFacings[i] = BlockFacing.FromFirstLetter(facingletters[i]);
			switch (facingletters[i])
			{
			case 'n':
				selectionBoxes[i] = new Cuboidf(0f, 0f, 0f, 1f, 1f, 0.0625f);
				break;
			case 'e':
				selectionBoxes[i] = new Cuboidf(0f, 0f, 0f, 1f, 1f, 0.0625f).RotatedCopy(0f, 270f, 0f, new Vec3d(0.5, 0.5, 0.5));
				break;
			case 's':
				selectionBoxes[i] = new Cuboidf(0f, 0f, 0f, 1f, 1f, 0.0625f).RotatedCopy(0f, 180f, 0f, new Vec3d(0.5, 0.5, 0.5));
				break;
			case 'w':
				selectionBoxes[i] = new Cuboidf(0f, 0f, 0f, 1f, 1f, 0.0625f).RotatedCopy(0f, 90f, 0f, new Vec3d(0.5, 0.5, 0.5));
				break;
			case 'u':
				selectionBoxes[i] = new Cuboidf(0f, 0f, 0f, 1f, 0.0625f, 1f).RotatedCopy(180f, 0f, 0f, new Vec3d(0.5, 0.5, 0.5));
				break;
			case 'd':
				selectionBoxes[i] = new Cuboidf(0f, 0f, 0f, 1f, 0.0625f, 1f);
				break;
			}
		}
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return false;
		}
		return TryPlaceBlockForWorldGen(world.BlockAccessor, blockSel.Position, blockSel.Face);
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		int quantity = 0;
		for (int i = 0; i < ownFacings.Length; i++)
		{
			quantity += ((world.Rand.NextDouble() > (double)Drops[0].Quantity.nextFloat()) ? 1 : 0);
		}
		ItemStack stack = Drops[0].ResolvedItemstack.Clone();
		stack.StackSize = Math.Max(1, quantity);
		return new ItemStack[1] { stack };
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return new ItemStack(world.BlockAccessor.GetBlock(CodeWithVariant("coating", "d")));
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		string newFacingLetters = "";
		BlockFacing[] array = ownFacings;
		foreach (BlockFacing facing in array)
		{
			if (world.BlockAccessor.GetBlockOnSide(pos, facing).SideSolid[facing.Opposite.Index])
			{
				ReadOnlySpan<char> readOnlySpan = newFacingLetters;
				char reference = facing.Code[0];
				newFacingLetters = string.Concat(readOnlySpan, new ReadOnlySpan<char>(in reference));
			}
		}
		if (ownFacings.Length <= newFacingLetters.Length)
		{
			return;
		}
		if (newFacingLetters.Length == 0)
		{
			world.BlockAccessor.BreakBlock(pos, null);
			return;
		}
		int diff = newFacingLetters.Length - ownFacings.Length;
		for (int i = 0; i < diff; i++)
		{
			world.SpawnItemEntity(Drops[0].GetNextItemStack(), pos);
		}
		Block newblock = world.GetBlock(CodeWithVariant("coating", newFacingLetters));
		world.BlockAccessor.SetBlock(newblock.BlockId, pos);
	}

	public override bool CanAttachBlockAt(IBlockAccessor world, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
	{
		return false;
	}

	public string getSolidFacesAtPos(IBlockAccessor blockAccessor, BlockPos pos)
	{
		string facings = "";
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing facing in aLLFACES)
		{
			facing.IterateThruFacingOffsets(pos);
			if (blockAccessor.GetBlock(pos).SideSolid[facing.Opposite.Index])
			{
				facings += facing.Code.Substring(0, 1);
			}
		}
		BlockFacing.FinishIteratingAllFaces(pos);
		return facings;
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, BlockPatchAttributes attributes = null)
	{
		return TryPlaceBlockForWorldGen(blockAccessor, pos, onBlockFace);
	}

	public bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace)
	{
		float thup = 14f / 51f * (float)api.World.BlockAccessor.MapSizeY;
		float thdown = 0.0627451f * (float)api.World.BlockAccessor.MapSizeY;
		if ((float)pos.Y < thdown || (float)pos.Y > thup || blockAccessor.GetLightLevel(pos, EnumLightLevelType.OnlySunLight) > 15)
		{
			return false;
		}
		Block hblock = blockAccessor.GetBlock(pos);
		if (hblock.Replaceable < 6000 || hblock.IsLiquid())
		{
			return false;
		}
		string facings = getSolidFacesAtPos(blockAccessor, pos);
		if (facings.Length > 0)
		{
			Block block = blockAccessor.GetBlock(CodeWithVariant("coating", facings));
			blockAccessor.SetBlock(block.BlockId, pos);
		}
		return true;
	}
}
