using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockBeeHiveKilnDoor : BlockGeneric
{
	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		BlockPos pos = blockSel.Position;
		IBlockAccessor ba = world.BlockAccessor;
		if (ba.GetBlock(pos, 1).Id == 0 && CanPlaceBlock(world, byPlayer, blockSel, ref failureCode))
		{
			return placeDoor(world, byPlayer, itemstack, blockSel, pos, ba);
		}
		return false;
	}

	public bool placeDoor(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, BlockPos pos, IBlockAccessor ba)
	{
		ba.SetBlock(BlockId, pos);
		BEBehaviorDoor behaviorDoor = ba.GetBlockEntity(pos)?.GetBehavior<BEBehaviorDoor>();
		BlockEntityBeeHiveKiln obj = behaviorDoor.Blockentity as BlockEntityBeeHiveKiln;
		behaviorDoor.RotateYRad = BEBehaviorDoor.getRotateYRad(byPlayer, blockSel);
		behaviorDoor.RotateYRad += ((behaviorDoor.RotateYRad == -(float)Math.PI) ? (-(float)Math.PI) : ((float)Math.PI));
		behaviorDoor.SetupRotationsAndColSelBoxes(initalSetup: true);
		obj.Orientation = BlockFacing.HorizontalFromAngle(behaviorDoor.RotateYRad - (float)Math.PI / 2f);
		obj.Init();
		double totalHoursHeatReceived = itemstack.Attributes.GetDouble("totalHoursHeatReceived");
		obj.TotalHoursHeatReceived = totalHoursHeatReceived;
		obj.TotalHoursLastUpdate = world.Calendar.TotalHours;
		if (world.Side == EnumAppSide.Server)
		{
			GetBehavior<BlockBehaviorDoor>().placeMultiblockParts(world, pos);
		}
		return true;
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockPos pos = blockSel.Position;
		if (byPlayer.WorldData.EntityControls.CtrlKey && world.BlockAccessor.GetBlockEntity(pos) is BlockEntityBeeHiveKiln besc)
		{
			besc.Interact(byPlayer);
			(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			return true;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1f)
	{
		ItemStack[] drops = base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
		BlockEntityBeeHiveKiln blockEntityGroundStorage = world.BlockAccessor.GetBlockEntity<BlockEntityBeeHiveKiln>(pos);
		drops[0].Attributes["totalHoursHeatReceived"] = new DoubleAttribute(blockEntityGroundStorage.TotalHoursHeatReceived);
		return drops;
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		BlockEntityBeeHiveKiln blockEntity = world.BlockAccessor.GetBlockEntity<BlockEntityBeeHiveKiln>(selection.Position);
		if (blockEntity != null && !blockEntity.StructureComplete)
		{
			return new WorldInteraction[2]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-mulblock-struc-show",
					HotKeyCodes = new string[1] { "ctrl" },
					MouseButton = EnumMouseButton.Right
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-mulblock-struc-hide",
					HotKeyCodes = new string[2] { "ctrl", "shift" },
					MouseButton = EnumMouseButton.Right
				}
			};
		}
		return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);
	}
}
