using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockGroundAndSideAttachable : Block
{
	private Dictionary<string, Cuboidi> attachmentAreas;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		Dictionary<string, RotatableCube> areas = Attributes?["attachmentAreas"].AsObject<Dictionary<string, RotatableCube>>();
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

	public override string GetHeldTpIdleAnimation(ItemSlot activeHotbarSlot, Entity forEntity, EnumHand hand)
	{
		IPlayer player = (forEntity as EntityPlayer)?.Player;
		if (forEntity.AnimManager.IsAnimationActive("sleep", "wave", "cheer", "shrug", "cry", "nod", "facepalm", "bow", "laugh", "rage", "scythe", "bowaim", "bowhit"))
		{
			return null;
		}
		if (player?.InventoryManager?.ActiveHotbarSlot != null && !player.InventoryManager.ActiveHotbarSlot.Empty && hand == EnumHand.Left)
		{
			ItemStack stack = player.InventoryManager.ActiveHotbarSlot.Itemstack;
			if (stack != null && stack.Collectible?.GetHeldTpIdleAnimation(player.InventoryManager.ActiveHotbarSlot, forEntity, EnumHand.Right) != null)
			{
				return null;
			}
			if (player != null && (player.Entity?.Controls.LeftMouseDown).GetValueOrDefault())
			{
				string anim = stack?.Collectible?.GetHeldTpHitAnimation(player.InventoryManager.ActiveHotbarSlot, forEntity);
				if (anim != null && anim != "knap")
				{
					return null;
				}
			}
		}
		if (hand != 0)
		{
			return "holdinglanternrighthand";
		}
		return "holdinglanternlefthand";
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (byPlayer.Entity.Controls.ShiftKey)
		{
			failureCode = "__ignore__";
			return false;
		}
		if (!CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return false;
		}
		if ((blockSel.Face.IsHorizontal || blockSel.Face == BlockFacing.UP) && TryAttachTo(world, blockSel.Position, blockSel.Face, itemstack))
		{
			return true;
		}
		BlockFacing[] faces = BlockFacing.ALLFACES;
		for (int i = 0; i < faces.Length; i++)
		{
			if (faces[i] != BlockFacing.DOWN && TryAttachTo(world, blockSel.Position, faces[i], itemstack))
			{
				return true;
			}
		}
		failureCode = "requireattachable";
		return false;
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		Block block = world.BlockAccessor.GetBlock(CodeWithVariant("orientation", "up"));
		return new ItemStack[1]
		{
			new ItemStack(block)
		};
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return new ItemStack(world.BlockAccessor.GetBlock(CodeWithVariant("orientation", "up")));
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		if (HasBehavior<BlockBehaviorUnstableFalling>())
		{
			base.OnNeighbourBlockChange(world, pos, neibpos);
		}
		else if (!CanStay(world.BlockAccessor, pos))
		{
			world.BlockAccessor.BreakBlock(pos, null);
		}
	}

	private bool TryAttachTo(IWorldAccessor world, BlockPos blockpos, BlockFacing onBlockFace, ItemStack byItemstack)
	{
		BlockPos attachingBlockPos = blockpos.AddCopy(onBlockFace.Opposite);
		Block block = world.BlockAccessor.GetBlock(attachingBlockPos);
		Cuboidi attachmentArea = null;
		attachmentAreas?.TryGetValue(onBlockFace.Opposite.Code, out attachmentArea);
		if (block.CanAttachBlockAt(world.BlockAccessor, this, attachingBlockPos, onBlockFace, attachmentArea))
		{
			int blockId = world.BlockAccessor.GetBlock(CodeWithVariant("orientation", onBlockFace.Code)).BlockId;
			world.BlockAccessor.SetBlock(blockId, blockpos, byItemstack);
			return true;
		}
		return false;
	}

	private bool CanStay(IBlockAccessor blockAccessor, BlockPos pos)
	{
		BlockFacing facing = BlockFacing.FromCode(Variant["orientation"]);
		BlockPos attachingBlockPos = pos.AddCopy(facing.Opposite);
		Block block = blockAccessor.GetBlock(attachingBlockPos);
		Cuboidi attachmentArea = null;
		attachmentAreas?.TryGetValue(facing.Opposite.Code, out attachmentArea);
		return block.CanAttachBlockAt(blockAccessor, this, attachingBlockPos, facing, attachmentArea);
	}

	public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Cuboidi attachmentArea = null)
	{
		return false;
	}

	public override AssetLocation GetRotatedBlockCode(int angle)
	{
		if (Variant["orientation"] == "up")
		{
			return Code;
		}
		BlockFacing oldFacing = BlockFacing.FromCode(Variant["orientation"]);
		BlockFacing newFacing = BlockFacing.HORIZONTALS_ANGLEORDER[((360 - angle) / 90 + oldFacing.HorizontalAngleIndex) % 4];
		return CodeWithParts(newFacing.Code);
	}

	public override AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis)
	{
		BlockFacing facing = BlockFacing.FromCode(Variant["orientation"]);
		if (facing.Axis == axis)
		{
			return CodeWithVariant("orientation", facing.Opposite.Code);
		}
		return Code;
	}

	public override bool TryPlaceBlockForWorldGen(IBlockAccessor blockAccessor, BlockPos pos, BlockFacing onBlockFace, IRandom worldGenRand, BlockPatchAttributes attributes = null)
	{
		if (CanStay(blockAccessor, pos))
		{
			return base.TryPlaceBlockForWorldGen(blockAccessor, pos, onBlockFace, worldGenRand, attributes);
		}
		return false;
	}
}
