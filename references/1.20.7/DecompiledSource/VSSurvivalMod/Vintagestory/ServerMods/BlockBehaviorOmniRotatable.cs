using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods;

public class BlockBehaviorOmniRotatable : BlockBehavior
{
	private bool rotateH;

	private bool rotateV;

	private bool rotateV4;

	private string facing = "player";

	private bool rotateSides;

	private float dropChance = 1f;

	public string Rot => block.Variant["rot"];

	public BlockBehaviorOmniRotatable(Block block)
		: base(block)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);
		rotateH = properties["rotateH"].AsBool(rotateH);
		rotateV = properties["rotateV"].AsBool(rotateV);
		rotateV4 = properties["rotateV4"].AsBool(rotateV4);
		rotateSides = properties["rotateSides"].AsBool(rotateSides);
		facing = properties["facing"].AsString(facing);
		dropChance = properties["dropChance"].AsFloat(1f);
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
	{
		handling = EnumHandling.PreventDefault;
		AssetLocation blockCode = null;
		switch ((EnumSlabPlaceMode)((itemstack.Attributes != null) ? itemstack.Attributes.GetInt("slabPlaceMode") : 0))
		{
		case EnumSlabPlaceMode.Horizontal:
		{
			string side = ((blockSel.HitPosition.Y < 0.5) ? "down" : "up");
			if (blockSel.Face.IsVertical)
			{
				side = blockSel.Face.Opposite.Code;
			}
			blockCode = block.CodeWithVariant("rot", side);
			Block orientedBlock = world.BlockAccessor.GetBlock(blockCode);
			if (orientedBlock.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
			{
				world.BlockAccessor.SetBlock(orientedBlock.BlockId, blockSel.Position);
				return true;
			}
			return false;
		}
		case EnumSlabPlaceMode.Vertical:
		{
			string side2 = Block.SuggestedHVOrientation(byPlayer, blockSel)[0].Code;
			if (blockSel.Face.IsHorizontal)
			{
				side2 = blockSel.Face.Opposite.Code;
			}
			blockCode = block.CodeWithVariant("rot", side2);
			Block orientedBlock = world.BlockAccessor.GetBlock(blockCode);
			if (orientedBlock.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
			{
				world.BlockAccessor.SetBlock(orientedBlock.BlockId, blockSel.Position);
				return true;
			}
			return false;
		}
		default:
		{
			if (rotateSides)
			{
				if (!(facing == "block"))
				{
					blockCode = ((!blockSel.Face.IsVertical) ? block.CodeWithVariant("rot", BlockFacing.HorizontalFromYaw(byPlayer.Entity.Pos.Yaw).Code) : block.CodeWithVariant("rot", blockSel.Face.Opposite.Code));
				}
				else
				{
					double x = Math.Abs(blockSel.HitPosition.X - 0.5);
					double y = Math.Abs(blockSel.HitPosition.Y - 0.5);
					double z = Math.Abs(blockSel.HitPosition.Z - 0.5);
					switch (blockSel.Face.Axis)
					{
					case EnumAxis.X:
						blockCode = ((z < 0.3 && y < 0.3) ? block.CodeWithVariant("rot", blockSel.Face.Opposite.Code) : ((!(z > y)) ? block.CodeWithVariant("rot", (blockSel.HitPosition.Y < 0.5) ? "down" : "up") : block.CodeWithVariant("rot", (blockSel.HitPosition.Z < 0.5) ? "north" : "south")));
						break;
					case EnumAxis.Y:
						blockCode = ((z < 0.3 && x < 0.3) ? block.CodeWithVariant("rot", blockSel.Face.Opposite.Code) : ((!(z > x)) ? block.CodeWithVariant("rot", (blockSel.HitPosition.X < 0.5) ? "west" : "east") : block.CodeWithVariant("rot", (blockSel.HitPosition.Z < 0.5) ? "north" : "south")));
						break;
					case EnumAxis.Z:
						blockCode = ((x < 0.3 && y < 0.3) ? block.CodeWithVariant("rot", blockSel.Face.Opposite.Code) : ((!(x > y)) ? block.CodeWithVariant("rot", (blockSel.HitPosition.Y < 0.5) ? "down" : "up") : block.CodeWithVariant("rot", (blockSel.HitPosition.X < 0.5) ? "west" : "east")));
						break;
					}
				}
			}
			else if (rotateH || rotateV)
			{
				string h = "north";
				string v = "up";
				if (blockSel.Face.IsVertical)
				{
					v = blockSel.Face.Code;
					h = BlockFacing.HorizontalFromYaw(byPlayer.Entity.Pos.Yaw).Code;
				}
				else if (rotateV4)
				{
					h = ((!(facing == "block")) ? BlockFacing.HorizontalFromYaw(byPlayer.Entity.Pos.Yaw).Code : blockSel.Face.Opposite.Code);
					switch (blockSel.Face.Axis)
					{
					case EnumAxis.X:
						v = ((!(Math.Abs(blockSel.HitPosition.Z - 0.5) > Math.Abs(blockSel.HitPosition.Y - 0.5))) ? ((blockSel.HitPosition.Y < 0.5) ? "up" : "down") : ((blockSel.HitPosition.Z < 0.5) ? "left" : "right"));
						break;
					case EnumAxis.Z:
						v = ((!(Math.Abs(blockSel.HitPosition.X - 0.5) > Math.Abs(blockSel.HitPosition.Y - 0.5))) ? ((blockSel.HitPosition.Y < 0.5) ? "up" : "down") : ((blockSel.HitPosition.X < 0.5) ? "left" : "right"));
						break;
					}
				}
				else
				{
					v = ((blockSel.HitPosition.Y < 0.5) ? "up" : "down");
				}
				if (rotateH && rotateV)
				{
					blockCode = block.CodeWithVariants(new string[2] { "v", "rot" }, new string[2] { v, h });
				}
				else if (rotateH)
				{
					blockCode = block.CodeWithVariant("rot", h);
				}
				else if (rotateV)
				{
					blockCode = block.CodeWithVariant("rot", v);
				}
			}
			if (blockCode == null)
			{
				blockCode = block.Code;
			}
			Block orientedBlock = world.BlockAccessor.GetBlock(blockCode);
			if (orientedBlock.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
			{
				world.BlockAccessor.SetBlock(orientedBlock.BlockId, blockSel.Position);
				return true;
			}
			return false;
		}
		}
	}

	public override void OnCreatedByCrafting(ItemSlot[] allInputslots, ItemSlot outputSlot, GridRecipe byRecipe, ref EnumHandling handled)
	{
		ItemSlot inputSlot = allInputslots.FirstOrDefault((ItemSlot s) => !s.Empty);
		Block inBlock = inputSlot.Itemstack.Block;
		if (inBlock == null || !inBlock.HasBehavior<BlockBehaviorOmniRotatable>())
		{
			base.OnCreatedByCrafting(allInputslots, outputSlot, byRecipe, ref handled);
			return;
		}
		int nowMode = (inputSlot.Itemstack.Attributes.GetInt("slabPlaceMode") + 1) % 3;
		if (nowMode == 0)
		{
			outputSlot.Itemstack.Attributes.RemoveAttribute("slabPlaceMode");
		}
		else
		{
			outputSlot.Itemstack.Attributes.SetInt("slabPlaceMode", nowMode);
		}
		base.OnCreatedByCrafting(allInputslots, outputSlot, byRecipe, ref handled);
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		ItemStack[] drops = block.GetDrops(world, pos, null);
		if (drops == null || drops.Length == 0)
		{
			return new ItemStack(block);
		}
		return drops[0];
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropChanceMultiplier, ref EnumHandling handling)
	{
		if (dropChance < 1f && world.Rand.NextDouble() > (double)dropChance)
		{
			handling = EnumHandling.PreventDefault;
			return new ItemStack[0];
		}
		return base.GetDrops(world, pos, byPlayer, ref dropChanceMultiplier, ref handling);
	}

	public override bool CanAttachBlockAt(IBlockAccessor world, Block block, BlockPos pos, BlockFacing blockFace, ref EnumHandling handling, Cuboidi attachmentArea = null)
	{
		if (Rot == "down")
		{
			handling = EnumHandling.PreventDefault;
			if (blockFace != BlockFacing.DOWN)
			{
				if (attachmentArea != null)
				{
					return attachmentArea.Y2 < 8;
				}
				return false;
			}
			return true;
		}
		if (Rot == "up")
		{
			handling = EnumHandling.PreventDefault;
			if (blockFace != BlockFacing.UP)
			{
				if (attachmentArea != null)
				{
					return attachmentArea.Y1 > 7;
				}
				return false;
			}
			return true;
		}
		return base.CanAttachBlockAt(world, block, pos, blockFace, ref handling, attachmentArea);
	}

	public override AssetLocation GetRotatedBlockCode(int angle, ref EnumHandling handling)
	{
		BlockFacing curFacing = BlockFacing.FromCode(block.Variant["rot"]);
		if (curFacing.IsVertical)
		{
			return block.Code;
		}
		handling = EnumHandling.PreventDefault;
		BlockFacing newFacing = BlockFacing.HORIZONTALS_ANGLEORDER[((360 - angle) / 90 + curFacing.HorizontalAngleIndex) % 4];
		if (rotateV4)
		{
			string v = block.Variant["v"];
			if ((angle == 90 && (curFacing == BlockFacing.WEST || curFacing == BlockFacing.EAST)) || (angle == 270 && curFacing == BlockFacing.SOUTH))
			{
				if (block.Variant["v"] == "left")
				{
					v = "right";
				}
				if (block.Variant["v"] == "right")
				{
					v = "left";
				}
			}
			return block.CodeWithVariants(new string[2] { "rot", "v" }, new string[2] { newFacing.Code, v });
		}
		return block.CodeWithVariant("rot", newFacing.Code);
	}

	public override AssetLocation GetHorizontallyFlippedBlockCode(EnumAxis axis, ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		BlockFacing curFacing = BlockFacing.FromCode(block.Variant["rot"]);
		if (curFacing.Axis == axis)
		{
			return block.CodeWithVariant("rot", curFacing.Opposite.Code);
		}
		return block.Code;
	}

	public override AssetLocation GetVerticallyFlippedBlockCode(ref EnumHandling handling)
	{
		handling = EnumHandling.PreventDefault;
		BlockFacing curFacing = BlockFacing.FromCode(block.Variant["rot"]);
		if (curFacing.IsVertical)
		{
			return block.CodeWithVariant("rot", curFacing.Opposite.Code);
		}
		curFacing = BlockFacing.FromCode(block.Variant["v"]);
		if (curFacing != null && curFacing.IsVertical)
		{
			return block.CodeWithParts(curFacing.Opposite.Code, block.LastCodePart());
		}
		return block.Code;
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		int @int = itemstack.Attributes.GetInt("slabPlaceMode");
		if (@int == 2)
		{
			renderinfo.Transform = renderinfo.Transform.Clone();
			renderinfo.Transform.Rotation.X = -80f;
			renderinfo.Transform.Rotation.Y = 0f;
			renderinfo.Transform.Rotation.Z = -22.5f;
		}
		if (@int == 1)
		{
			renderinfo.Transform = renderinfo.Transform.Clone();
			renderinfo.Transform.Rotation.X = 5f;
		}
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
	}

	public override string GetHeldBlockInfo(IWorldAccessor world, ItemSlot inSlot)
	{
		return (EnumSlabPlaceMode)inSlot.Itemstack.Attributes.GetInt("slabPlaceMode") switch
		{
			EnumSlabPlaceMode.Auto => Lang.Get("slab-placemode-auto") + "\n", 
			EnumSlabPlaceMode.Horizontal => Lang.Get("slab-placemode-horizontal") + "\n", 
			EnumSlabPlaceMode.Vertical => Lang.Get("slab-placemode-vertical") + "\n", 
			_ => base.GetHeldBlockInfo(world, inSlot), 
		};
	}
}
