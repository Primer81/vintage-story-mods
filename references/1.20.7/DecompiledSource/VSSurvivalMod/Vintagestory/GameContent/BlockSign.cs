using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockSign : Block
{
	private WorldInteraction[] interactions;

	public TextAreaConfig signConfig;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		signConfig = new TextAreaConfig();
		if (Attributes != null)
		{
			signConfig = Attributes.AsObject(signConfig);
		}
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		interactions = ObjectCacheUtil.GetOrCreate(api, "signBlockInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (CollectibleObject current in api.World.Collectibles)
			{
				JsonObject attributes = current.Attributes;
				if (attributes != null && attributes["pigment"].Exists)
				{
					list.Add(new ItemStack(current));
				}
			}
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-sign-write",
					HotKeyCode = "shift",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray()
				}
			};
		});
	}

	public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (Variant["attachment"] == "wall")
		{
			return base.GetCollisionBoxes(blockAccessor, pos);
		}
		if (blockAccessor.GetBlockEntity(pos) is BlockEntitySign besign)
		{
			return besign.colSelBox;
		}
		return base.GetCollisionBoxes(blockAccessor, pos);
	}

	public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
	{
		if (Variant["attachment"] == "wall")
		{
			return base.GetCollisionBoxes(blockAccessor, pos);
		}
		if (blockAccessor.GetBlockEntity(pos) is BlockEntitySign besign)
		{
			return besign.colSelBox;
		}
		return base.GetSelectionBoxes(blockAccessor, pos);
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection bs, ref string failureCode)
	{
		BlockPos supportingPos = bs.Position.AddCopy(bs.Face.Opposite);
		Block supportingBlock = world.BlockAccessor.GetBlock(supportingPos);
		if (bs.Face.IsHorizontal)
		{
			if (!supportingBlock.CanAttachBlockAt(world.BlockAccessor, this, supportingPos, bs.Face))
			{
				JsonObject attributes = supportingBlock.GetAttributes(world.BlockAccessor, supportingPos);
				if (attributes == null || !attributes.IsTrue("partialAttachable"))
				{
					goto IL_00d0;
				}
			}
			Block wallblock = world.BlockAccessor.GetBlock(CodeWithParts("wall", bs.Face.Opposite.Code));
			if (!wallblock.CanPlaceBlock(world, byPlayer, bs, ref failureCode))
			{
				return false;
			}
			world.BlockAccessor.SetBlock(wallblock.BlockId, bs.Position);
			return true;
		}
		goto IL_00d0;
		IL_00d0:
		if (!CanPlaceBlock(world, byPlayer, bs, ref failureCode))
		{
			return false;
		}
		BlockFacing[] horVer = Block.SuggestedHVOrientation(byPlayer, bs);
		AssetLocation blockCode = CodeWithParts(horVer[0].Code);
		Block block = world.BlockAccessor.GetBlock(blockCode);
		world.BlockAccessor.SetBlock(block.BlockId, bs.Position);
		if (world.BlockAccessor.GetBlockEntity(bs.Position) is BlockEntitySign bect)
		{
			BlockPos targetPos = (bs.DidOffset ? bs.Position.AddCopy(bs.Face.Opposite) : bs.Position);
			double y = byPlayer.Entity.Pos.X - ((double)targetPos.X + bs.HitPosition.X);
			double dz = (double)(float)byPlayer.Entity.Pos.Z - ((double)targetPos.Z + bs.HitPosition.Z);
			float num = (float)Math.Atan2(y, dz);
			float deg45 = (float)Math.PI / 4f;
			float roundRad = (float)(int)Math.Round(num / deg45) * deg45;
			bect.MeshAngleRad = roundRad;
		}
		return true;
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		Block block = world.BlockAccessor.GetBlock(CodeWithParts("ground", "north"));
		if (block == null)
		{
			block = world.BlockAccessor.GetBlock(CodeWithParts("wall", "north"));
		}
		return new ItemStack[1]
		{
			new ItemStack(block)
		};
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		Block block = world.BlockAccessor.GetBlock(CodeWithParts("ground", "north"));
		if (block == null)
		{
			block = world.BlockAccessor.GetBlock(CodeWithParts("wall", "north"));
		}
		return new ItemStack(block);
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		base.OnNeighbourBlockChange(world, pos, neibpos);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockEntity entity = world.BlockAccessor.GetBlockEntity(blockSel.Position);
		if (entity is BlockEntitySign)
		{
			((BlockEntitySign)entity).OnRightClick(byPlayer);
			return true;
		}
		return true;
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

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}

	public override AssetLocation GetRotatedBlockCode(int angle)
	{
		int rotatedIndex = GameMath.Mod(BlockFacing.FromCode(LastCodePart()).HorizontalAngleIndex - angle / 90, 4);
		BlockFacing nowFacing = BlockFacing.HORIZONTALS_ANGLEORDER[rotatedIndex];
		return CodeWithParts(nowFacing.Code);
	}
}
