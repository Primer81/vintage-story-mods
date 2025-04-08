using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemDryGrass : Item
{
	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (blockSel == null || byEntity?.World == null || !byEntity.Controls.ShiftKey)
		{
			base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
			return;
		}
		IWorldAccessor world = byEntity.World;
		Block firepitBlock = world.GetBlock(new AssetLocation("firepit-construct1"));
		if (firepitBlock == null)
		{
			base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
			return;
		}
		BlockPos onPos = (blockSel.DidOffset ? blockSel.Position : blockSel.Position.AddCopy(blockSel.Face));
		IPlayer byPlayer = byEntity.World.PlayerByUid((byEntity as EntityPlayer)?.PlayerUID);
		if (!byEntity.World.Claims.TryAccess(byPlayer, onPos, EnumBlockAccessFlags.BuildOrBreak))
		{
			return;
		}
		Block block = world.BlockAccessor.GetBlock(onPos.DownCopy());
		Block aimedBlock = world.BlockAccessor.GetBlock(blockSel.Position);
		if (aimedBlock is BlockGroundStorage)
		{
			BlockEntityGroundStorage bec = world.BlockAccessor.GetBlockEntity<BlockEntityGroundStorage>(blockSel.Position);
			if (bec.Inventory[3].Empty && bec.Inventory[2].Empty && bec.Inventory[1].Empty && bec.Inventory[0].Itemstack.Collectible is ItemFirewood)
			{
				if (bec.Inventory[0].StackSize == bec.Capacity)
				{
					string useless = "";
					if (!firepitBlock.CanPlaceBlock(world, byPlayer, new BlockSelection
					{
						Position = onPos,
						Face = BlockFacing.UP
					}, ref useless))
					{
						return;
					}
					world.BlockAccessor.SetBlock(firepitBlock.BlockId, onPos);
					if (firepitBlock.Sounds != null)
					{
						world.PlaySoundAt(firepitBlock.Sounds.Place, blockSel.Position.X, blockSel.Position.InternalY, blockSel.Position.Z, byPlayer);
					}
					itemslot.Itemstack.StackSize--;
				}
				handHandling = EnumHandHandling.PreventDefault;
			}
			else if (!(aimedBlock is BlockPitkiln) && (world.GetBlock(new AssetLocation("pitkiln")) as BlockPitkiln).TryCreateKiln(world, byPlayer, blockSel.Position))
			{
				handHandling = EnumHandHandling.PreventDefault;
			}
			return;
		}
		string useless2 = "";
		if (block.CanAttachBlockAt(byEntity.World.BlockAccessor, firepitBlock, onPos.DownCopy(), BlockFacing.UP) && firepitBlock.CanPlaceBlock(world, byPlayer, new BlockSelection
		{
			Position = onPos,
			Face = BlockFacing.UP
		}, ref useless2))
		{
			world.BlockAccessor.SetBlock(firepitBlock.BlockId, onPos);
			if (firepitBlock.Sounds != null)
			{
				world.PlaySoundAt(firepitBlock.Sounds.Place, blockSel.Position.X, blockSel.Position.InternalY, blockSel.Position.Z, byPlayer);
			}
			itemslot.Itemstack.StackSize--;
			handHandling = EnumHandHandling.PreventDefaultAction;
		}
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				HotKeyCode = "shift",
				ActionLangCode = "heldhelp-createfirepit",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
