using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockBehaviorDecor : BlockBehavior
{
	private BlockFacing[] sides;

	private bool sidedVariants;

	private bool nwOrientable;

	public BlockBehaviorDecor(Block block)
		: base(block)
	{
		block.decorBehaviorFlags = 1;
	}

	public override void Initialize(JsonObject properties)
	{
		string[] sidenames = properties["sides"].AsArray(new string[0]);
		sides = new BlockFacing[sidenames.Length];
		for (int i = 0; i < sidenames.Length; i++)
		{
			if (sidenames[i] != null)
			{
				sides[i] = BlockFacing.FromFirstLetter(sidenames[i][0]);
			}
		}
		sidedVariants = properties["sidedVariants"].AsBool();
		nwOrientable = properties["nwOrientable"].AsBool();
		if (properties["drawIfCulled"].AsBool())
		{
			block.decorBehaviorFlags |= 2;
		}
		if (properties["alternateZOffset"].AsBool())
		{
			block.decorBehaviorFlags |= 4;
		}
		if (properties["notFullFace"].AsBool())
		{
			block.decorBehaviorFlags |= 8;
		}
		if (properties["removable"].AsBool())
		{
			block.decorBehaviorFlags |= 16;
		}
		if (sidedVariants)
		{
			block.decorBehaviorFlags |= 32;
		}
		block.DecorThickness = properties["thickness"].AsFloat(1f / 32f);
		base.Initialize(properties);
	}

	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handling, ref string failureCode)
	{
		handling = EnumHandling.PreventDefault;
		for (int i = 0; i < sides.Length; i++)
		{
			if (sides[i] != blockSel.Face)
			{
				continue;
			}
			BlockPos pos = blockSel.Position.AddCopy(blockSel.Face.Opposite);
			Block blockToPlace;
			if (sidedVariants)
			{
				blockToPlace = world.BlockAccessor.GetBlock(block.CodeWithParts(blockSel.Face.Opposite.Code));
				if (blockToPlace == null)
				{
					failureCode = "decorvariantnotfound";
					return false;
				}
			}
			else if (nwOrientable)
			{
				string code = ((Block.SuggestedHVOrientation(byPlayer, blockSel)[0].Axis == EnumAxis.X) ? "we" : "ns");
				blockToPlace = world.BlockAccessor.GetBlock(block.CodeWithParts(code));
				if (blockToPlace == null)
				{
					failureCode = "decorvariantnotfound";
					return false;
				}
			}
			else
			{
				blockToPlace = block;
			}
			Block attachingBlock = world.BlockAccessor.GetBlock(pos);
			IAcceptsDecor iad = attachingBlock.GetInterface<IAcceptsDecor>(world, pos);
			if (iad != null && iad.CanAccept(blockToPlace))
			{
				if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Survival)
				{
					int decorId = iad.GetDecor(blockSel.Face);
					if (decorId > 0)
					{
						Block decor = world.BlockAccessor.GetBlock(decorId);
						ItemStack itemStack = new ItemStack(decor.Id, decor.ItemClass, 1, new TreeAttribute(), world);
						world.SpawnItemEntity(itemStack, pos.AddCopy(blockSel.Face).ToVec3d());
					}
				}
				iad.SetDecor(blockToPlace, blockSel.Face);
				return true;
			}
			EnumBlockMaterial mat = attachingBlock.GetBlockMaterial(world.BlockAccessor, pos);
			if (!attachingBlock.CanAttachBlockAt(world.BlockAccessor, blockToPlace, pos, blockSel.Face) || mat == EnumBlockMaterial.Snow || mat == EnumBlockMaterial.Ice)
			{
				failureCode = "decorrequiressolid";
				return false;
			}
			DecorBits decorPosition = new DecorBits(blockSel.Face);
			Block decorBlock = world.BlockAccessor.GetDecor(pos, decorPosition);
			if (world.BlockAccessor.SetDecor(blockToPlace, pos, decorPosition))
			{
				if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Survival && decorBlock != null && (decorBlock.decorBehaviorFlags & 0x10u) != 0)
				{
					ItemStack itemStack2 = decorBlock.OnPickBlock(world, pos);
					world.SpawnItemEntity(itemStack2, pos.AddCopy(blockSel.Face).ToVec3d());
				}
				return true;
			}
			failureCode = "existingdecorinplace";
			return false;
		}
		failureCode = "cannotplacedecorhere";
		return false;
	}

	public override AssetLocation GetRotatedBlockCode(int angle, ref EnumHandling handled)
	{
		if (nwOrientable)
		{
			handled = EnumHandling.PreventDefault;
			string[] angles = new string[2] { "ns", "we" };
			int index = angle / 90;
			if (block.LastCodePart() == "we")
			{
				index++;
			}
			return block.CodeWithParts(angles[index % 2]);
		}
		return base.GetRotatedBlockCode(angle, ref handled);
	}
}
