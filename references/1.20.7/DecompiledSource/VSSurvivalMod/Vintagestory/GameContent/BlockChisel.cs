using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockChisel : BlockMicroBlock, IWrenchOrientable
{
	private WorldInteraction[] interactions;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		interactions = new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "blockhelp-chisel-removedeco",
				MouseButton = EnumMouseButton.Right,
				Itemstacks = BlockUtil.GetKnifeStacks(api),
				GetMatchingStacks = delegate(WorldInteraction wi, BlockSelection bs, EntitySelection es)
				{
					BlockEntityChisel blockEntity = GetBlockEntity<BlockEntityChisel>(bs.Position);
					return (blockEntity?.DecorIds != null && blockEntity.DecorIds[bs.Face.Index] != 0) ? wi.Itemstacks : null;
				}
			}
		};
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		StringArrayAttribute obj = inSlot.Itemstack.Attributes["materials"] as StringArrayAttribute;
		if (obj == null || obj.value.Length <= 1)
		{
			IntArrayAttribute obj2 = inSlot.Itemstack.Attributes["materials"] as IntArrayAttribute;
			if (obj2 == null || obj2.value.Length <= 1)
			{
				return;
			}
		}
		dsc.AppendLine(Lang.Get("<font color=\"lightblue\">Multimaterial chiseled block</font>"));
	}

	public void Rotate(EntityAgent byEntity, BlockSelection blockSel, int dir)
	{
		BlockEntityChisel bechisel = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityChisel;
		if (byEntity.Controls.CtrlKey)
		{
			int rot = bechisel.DecorRotations;
			int bitshift = blockSel.Face.Index * 3;
			int facerot = (rot >> bitshift) & 7;
			rot &= ~(7 << bitshift);
			rot += ((facerot + 1) & 7) << bitshift;
			bechisel.DecorRotations = rot;
		}
		else
		{
			bechisel.RotateModel((dir > 0) ? 90 : (-90), null);
		}
		bechisel.MarkDirty(redrawOnClient: true);
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockEntityChisel obj = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityChisel;
		if (obj != null && obj.Interact(byPlayer, blockSel))
		{
			return true;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override bool TryToRemoveSoilFirst(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
	{
		return false;
	}

	public override bool IsSoilNonSoilMix(BlockEntityMicroBlock be)
	{
		return false;
	}

	public override bool IsSoilNonSoilMix(IBlockAccessor blockAccessor, BlockPos pos)
	{
		return false;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
