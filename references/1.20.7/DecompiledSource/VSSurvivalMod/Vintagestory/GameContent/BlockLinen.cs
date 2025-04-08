using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace Vintagestory.GameContent;

public class BlockLinen : BlockSimpleCoating
{
	public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
	{
		if (api.World.BlockAccessor.GetBlockEntity(blockSel.Position.AddCopy(blockSel.Face.Opposite)) is BlockEntityBarrel)
		{
			return false;
		}
		return base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (blockSel != null)
		{
			BlockEntityBarrel beba = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityBarrel;
			ItemSlot liqslot = beba?.Inventory[1];
			if (beba != null && !liqslot.Empty && liqslot.Itemstack.Item?.Code?.Path == "cottagecheeseportion")
			{
				WaterTightContainableProps props = BlockLiquidContainerBase.GetContainableProps(liqslot.Itemstack);
				if ((float)liqslot.Itemstack.StackSize / props.ItemsPerLitre < 25f)
				{
					(api as ICoreClientAPI)?.TriggerIngameError(this, "notenough", Lang.Get("Need at least 25 litres to create a roll of cheese"));
					handHandling = EnumHandHandling.PreventDefault;
					return;
				}
				if (api.World.Side == EnumAppSide.Server)
				{
					ItemStack ccStack = beba.Inventory[1].TakeOut((int)(25f * props.ItemsPerLitre));
					BlockCheeseCurdsBundle obj = api.World.GetBlock(new AssetLocation("curdbundle")) as BlockCheeseCurdsBundle;
					ItemStack bundleStack = new ItemStack(obj);
					obj.SetContents(bundleStack, ccStack);
					slot.TakeOut(1);
					slot.MarkDirty();
					beba.MarkDirty(redrawOnClient: true);
					if (!byEntity.TryGiveItemStack(bundleStack))
					{
						api.World.SpawnItemEntity(bundleStack, byEntity.Pos.XYZ.AddCopy(0.0, 0.5, 0.0));
					}
					api.World.Logger.Audit("{0} Took 1x{1} from Barrel at {2}.", byEntity.GetName(), bundleStack.Collectible.Code, blockSel.Position);
				}
				handHandling = EnumHandHandling.PreventDefault;
				return;
			}
		}
		base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
	}
}
