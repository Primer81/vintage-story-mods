using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockFenceGate : BlockBaseDoor
{
	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		CanStep = false;
	}

	public override string GetKnobOrientation()
	{
		return GetKnobOrientation(this);
	}

	public override BlockFacing GetDirection()
	{
		return BlockFacing.FromFirstLetter(Variant["type"]);
	}

	public string GetKnobOrientation(Block block)
	{
		return Variant["knobOrientation"];
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			BlockFacing[] horVer = Block.SuggestedHVOrientation(byPlayer, blockSel);
			string face = ((horVer[0] == BlockFacing.NORTH || horVer[0] == BlockFacing.SOUTH) ? "n" : "w");
			bool neighbourOpen;
			string knobOrientation = GetSuggestedKnobOrientation(world.BlockAccessor, blockSel.Position, horVer[0], out neighbourOpen);
			AssetLocation newCode = CodeWithVariants(new string[3] { "type", "state", "knobOrientation" }, new string[3]
			{
				face,
				neighbourOpen ? "opened" : "closed",
				knobOrientation
			});
			world.BlockAccessor.SetBlock(world.BlockAccessor.GetBlock(newCode).BlockId, blockSel.Position);
			return true;
		}
		return false;
	}

	private string GetSuggestedKnobOrientation(IBlockAccessor ba, BlockPos pos, BlockFacing facing, out bool neighbourOpen)
	{
		string leftOrRight = "left";
		Block nBlock1 = ba.GetBlock(pos.AddCopy(facing.GetCW()));
		Block nBlock2 = ba.GetBlock(pos.AddCopy(facing.GetCCW()));
		bool invert = facing == BlockFacing.EAST || facing == BlockFacing.SOUTH;
		bool isDoor1 = IsSameDoor(nBlock1);
		bool isDoor2 = IsSameDoor(nBlock2);
		if (isDoor1 && isDoor2)
		{
			leftOrRight = "left";
			neighbourOpen = (nBlock1 as BlockBaseDoor).IsOpened();
		}
		else if (isDoor1)
		{
			if (GetKnobOrientation(nBlock1) == "right")
			{
				leftOrRight = (invert ? "left" : "right");
				neighbourOpen = false;
			}
			else
			{
				leftOrRight = (invert ? "right" : "left");
				neighbourOpen = (nBlock1 as BlockBaseDoor).IsOpened();
			}
		}
		else if (isDoor2)
		{
			if (GetKnobOrientation(nBlock2) == "right")
			{
				leftOrRight = (invert ? "right" : "left");
				neighbourOpen = false;
			}
			else
			{
				leftOrRight = (invert ? "left" : "right");
				neighbourOpen = (nBlock2 as BlockBaseDoor).IsOpened();
			}
		}
		else
		{
			neighbourOpen = false;
			if ((nBlock1.Attributes?.IsTrue("isFence") ?? false) ^ (nBlock2.Attributes?.IsTrue("isFence") ?? false))
			{
				leftOrRight = ((invert ^ (nBlock2.Attributes?.IsTrue("isFence") ?? false)) ? "left" : "right");
			}
			else if (nBlock2.Replaceable >= 6000 && nBlock1.Replaceable < 6000)
			{
				leftOrRight = (invert ? "left" : "right");
			}
			else if (nBlock1.Replaceable >= 6000 && nBlock2.Replaceable < 6000)
			{
				leftOrRight = (invert ? "right" : "left");
			}
		}
		return leftOrRight;
	}

	protected override void Open(IWorldAccessor world, IPlayer byPlayer, BlockPos pos)
	{
		AssetLocation newCode = CodeWithVariant("state", IsOpened() ? "closed" : "opened");
		world.BlockAccessor.ExchangeBlock(world.BlockAccessor.GetBlock(newCode).BlockId, pos);
	}

	public override void Activate(IWorldAccessor world, Caller caller, BlockSelection blockSel, ITreeAttribute activationArgs)
	{
		if (activationArgs == null || !activationArgs.HasAttribute("opened") || activationArgs.GetBool("opened") != IsOpened())
		{
			Open(world, caller.Player, blockSel.Position);
		}
	}

	protected override BlockPos TryGetConnectedDoorPos(BlockPos pos)
	{
		string knobOrientation = GetKnobOrientation();
		BlockFacing dir = GetDirection();
		if (!(knobOrientation == "right"))
		{
			return pos.AddCopy(dir.GetCW());
		}
		return pos.AddCopy(dir.GetCCW());
	}

	public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		return GetHandbookDropsFromBreakDrops(handbookStack, forPlayer);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		Block block = world.BlockAccessor.GetBlock(CodeWithVariants(new string[4] { "type", "state", "knobOrientation", "cover" }, new string[4] { "n", "closed", "left", "free" }));
		return new ItemStack[1]
		{
			new ItemStack(block)
		};
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		return new ItemStack(world.BlockAccessor.GetBlock(CodeWithVariants(new string[4] { "type", "state", "knobOrientation", "cover" }, new string[4] { "n", "closed", "left", "free" })));
	}

	public override AssetLocation GetRotatedBlockCode(int angle)
	{
		BlockFacing nowFacing = BlockFacing.FromFirstLetter(Variant["type"]);
		BlockFacing rotatedFacing = BlockFacing.HORIZONTALS_ANGLEORDER[(nowFacing.HorizontalAngleIndex + angle / 90) % 4];
		string prevType = Variant["type"];
		string newType = prevType;
		if (nowFacing.Axis != rotatedFacing.Axis)
		{
			newType = ((prevType == "n") ? "w" : "n");
		}
		string knowbOr = Variant["knobOrientation"];
		if (prevType == "n" && newType == "w" && knowbOr == "right" && angle == 90)
		{
			knowbOr = "left";
		}
		else if (prevType == "n" && newType == "w" && knowbOr == "left" && angle == 90)
		{
			knowbOr = "right";
		}
		else if (prevType == "w" && newType == "n" && knowbOr == "right" && angle == 270)
		{
			knowbOr = "left";
		}
		else if (prevType == "w" && newType == "n" && knowbOr == "left" && angle == 270)
		{
			knowbOr = "right";
		}
		return CodeWithVariants(new string[2] { "type", "knobOrientation" }, new string[2] { newType, knowbOr });
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
	}
}
