using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockDoor : BlockBaseDoor
{
	private bool airtight;

	public override void OnLoaded(ICoreAPI api)
	{
		airtight = Variant["material"] != "log";
		base.OnLoaded(api);
	}

	public override string GetKnobOrientation()
	{
		return GetKnobOrientation(this);
	}

	public override BlockFacing GetDirection()
	{
		return BlockFacing.FromCode(Variant["horizontalorientation"]);
	}

	public bool IsSideSolid(BlockFacing facing)
	{
		BlockFacing facingWhenClosed = GetDirection().Opposite;
		BlockFacing facingWhenOpened = ((GetKnobOrientation() == "left") ? facingWhenClosed.GetCCW() : facingWhenClosed.GetCW());
		if (open || facingWhenClosed != facing)
		{
			if (open)
			{
				return facingWhenOpened == facing;
			}
			return false;
		}
		return true;
	}

	public string GetKnobOrientation(Block block)
	{
		return Variant["knobOrientation"];
	}

	public bool IsUpperHalf()
	{
		return Variant["part"] == "up";
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		BlockPos abovePos = blockSel.Position.AddCopy(0, 1, 0);
		IBlockAccessor ba = world.BlockAccessor;
		if (CanPlaceBlock(world, byPlayer, blockSel.AddPosCopy(0, 1, 0), ref failureCode) && CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			BlockFacing[] horVer = Block.SuggestedHVOrientation(byPlayer, blockSel);
			string knobOrientation = GetSuggestedKnobOrientation(ba, blockSel.Position, horVer[0]);
			AssetLocation downBlockCode = CodeWithVariants(new Dictionary<string, string>
			{
				{
					"horizontalorientation",
					horVer[0].Code
				},
				{ "part", "down" },
				{ "state", "closed" },
				{ "knobOrientation", knobOrientation }
			});
			Block downBlock = ba.GetBlock(downBlockCode);
			AssetLocation upBlockCode = downBlock.CodeWithVariant("part", "up");
			Block upBlock = ba.GetBlock(upBlockCode);
			ba.SetBlock(downBlock.BlockId, blockSel.Position);
			if (world.Side == EnumAppSide.Server)
			{
				world.BlockAccessor.TriggerNeighbourBlockUpdate(blockSel.Position);
			}
			ba.SetBlock(upBlock.BlockId, abovePos);
			if (world.Side == EnumAppSide.Server)
			{
				world.BlockAccessor.TriggerNeighbourBlockUpdate(abovePos);
			}
			return true;
		}
		return false;
	}

	private string GetSuggestedKnobOrientation(IBlockAccessor ba, BlockPos pos, BlockFacing facing)
	{
		string leftOrRight = "left";
		Block nBlock1 = ba.GetBlock(pos.AddCopy(facing.GetCW()));
		Block nBlock2 = ba.GetBlock(pos.AddCopy(facing.GetCCW()));
		bool isDoor1 = IsSameDoor(nBlock1);
		bool isDoor2 = IsSameDoor(nBlock2);
		if (isDoor1 && isDoor2)
		{
			leftOrRight = "left";
		}
		else if (isDoor1)
		{
			leftOrRight = ((GetKnobOrientation(nBlock1) == "right") ? "left" : "right");
		}
		else if (isDoor2)
		{
			leftOrRight = ((GetKnobOrientation(nBlock2) == "right") ? "left" : "right");
		}
		return leftOrRight;
	}

	public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos)
	{
		base.OnBlockRemoved(world, pos);
		BlockPos otherPos = pos.AddCopy(0, (!IsUpperHalf()) ? 1 : (-1), 0);
		Block otherPart = world.BlockAccessor.GetBlock(otherPos);
		if (otherPart is BlockDoor && ((BlockDoor)otherPart).IsUpperHalf() != IsUpperHalf())
		{
			world.BlockAccessor.SetBlock(0, otherPos);
			if (world.Side == EnumAppSide.Server)
			{
				world.BlockAccessor.TriggerNeighbourBlockUpdate(otherPos);
			}
		}
		if (world.BlockAccessor.GetBlock(pos) is BlockDoor)
		{
			world.BlockAccessor.SetBlock(0, pos);
			if (world.Side == EnumAppSide.Server)
			{
				world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
			}
		}
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		return new ItemStack[1] { OnPickBlock(world, pos) };
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		AssetLocation blockCode = CodeWithVariants(new Dictionary<string, string>
		{
			{ "horizontalorientation", "north" },
			{ "part", "down" },
			{ "state", "closed" },
			{ "knobOrientation", "left" }
		});
		return new ItemStack(world.BlockAccessor.GetBlock(blockCode));
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		base.OnNeighbourBlockChange(world, pos, neibpos);
	}

	protected override void Open(IWorldAccessor world, IPlayer byPlayer, BlockPos position)
	{
		float breakChance = Attributes["breakOnTriggerChance"].AsFloat();
		if (world.Side == EnumAppSide.Server && world.Rand.NextDouble() < (double)breakChance && byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			world.BlockAccessor.BreakBlock(position, byPlayer);
			world.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), position, 0.0);
			return;
		}
		AssetLocation newCode = CodeWithVariant("state", IsOpened() ? "closed" : "opened");
		Block newBlock = world.BlockAccessor.GetBlock(newCode);
		AssetLocation otherNewCode = newBlock.CodeWithVariant("part", IsUpperHalf() ? "down" : "up");
		world.BlockAccessor.ExchangeBlock(newBlock.BlockId, position);
		world.BlockAccessor.MarkBlockDirty(position);
		if (world.Side == EnumAppSide.Server)
		{
			world.BlockAccessor.TriggerNeighbourBlockUpdate(position);
		}
		BlockPos otherPos = position.AddCopy(0, (!IsUpperHalf()) ? 1 : (-1), 0);
		Block otherPart = world.BlockAccessor.GetBlock(otherPos);
		if (otherPart is BlockDoor && ((BlockDoor)otherPart).IsUpperHalf() != IsUpperHalf())
		{
			world.BlockAccessor.ExchangeBlock(world.BlockAccessor.GetBlock(otherNewCode).BlockId, otherPos);
			world.BlockAccessor.MarkBlockDirty(otherPos);
			if (world.Side == EnumAppSide.Server)
			{
				world.BlockAccessor.TriggerNeighbourBlockUpdate(otherPos);
			}
		}
	}

	protected override BlockPos TryGetConnectedDoorPos(BlockPos pos)
	{
		string knobOrientation = GetKnobOrientation();
		BlockFacing dir = GetDirection();
		if (!(knobOrientation == "left"))
		{
			return pos.AddCopy(dir.GetCW());
		}
		return pos.AddCopy(dir.GetCCW());
	}

	public override AssetLocation GetRotatedBlockCode(int angle)
	{
		int rotatedIndex = GameMath.Mod(BlockFacing.FromCode(LastCodePart(3)).HorizontalAngleIndex - angle / 90, 4);
		BlockFacing nowFacing = BlockFacing.HORIZONTALS_ANGLEORDER[rotatedIndex];
		return CodeWithVariant("horizontalorientation", nowFacing.Code);
	}

	public override int GetRetention(BlockPos pos, BlockFacing facing, EnumRetentionType type)
	{
		if (type == EnumRetentionType.Sound)
		{
			if (!IsSideSolid(facing))
			{
				return 0;
			}
			return 3;
		}
		if (!airtight)
		{
			return 0;
		}
		if (api.World.Config.GetBool("openDoorsNotSolid"))
		{
			if (!IsSideSolid(facing))
			{
				return 0;
			}
			return getInsulation(pos);
		}
		if (!IsSideSolid(facing) && !IsSideSolid(facing.Opposite))
		{
			return 3;
		}
		return getInsulation(pos);
	}

	private int getInsulation(BlockPos pos)
	{
		EnumBlockMaterial mat = GetBlockMaterial(api.World.BlockAccessor, pos);
		if (mat == EnumBlockMaterial.Ore || mat == EnumBlockMaterial.Stone || mat == EnumBlockMaterial.Soil || mat == EnumBlockMaterial.Ceramic)
		{
			return -1;
		}
		return 1;
	}

	public override float GetLiquidBarrierHeightOnSide(BlockFacing face, BlockPos pos)
	{
		if (!IsSideSolid(face))
		{
			return 0f;
		}
		if (!airtight)
		{
			return 0f;
		}
		JsonObject attributes = Attributes;
		if (attributes == null)
		{
			if (!IsUpperHalf())
			{
				return 1f;
			}
			return 0f;
		}
		return attributes["liquidBarrierHeight"].AsFloat(IsUpperHalf() ? 0f : 1f);
	}
}
