using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemFlint : Item
{
	public override string GetHeldTpHitAnimation(ItemSlot slot, Entity byEntity)
	{
		if (slot.Itemstack?.Collectible == this)
		{
			EntityAgent obj = byEntity as EntityAgent;
			if (obj != null && obj.Controls.FloorSitting)
			{
				return "knapsitting";
			}
			return "knap";
		}
		return base.GetHeldTpHitAnimation(slot, byEntity);
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
	{
		IPlayer byPlayer = null;
		if (byEntity is EntityPlayer)
		{
			byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		if (byEntity.Controls.ShiftKey && blockSel != null)
		{
			IWorldAccessor world = byEntity.World;
			Block knappingBlock = world.GetBlock(new AssetLocation("knappingsurface"));
			if (knappingBlock == null)
			{
				return;
			}
			if (!world.BlockAccessor.GetBlock(blockSel.Position).CanAttachBlockAt(byEntity.World.BlockAccessor, knappingBlock, blockSel.Position, BlockFacing.UP))
			{
				if (api.Side == EnumAppSide.Client)
				{
					(api as ICoreClientAPI).TriggerIngameError(this, "cantplace", Lang.Get("Cannot place a knapping surface here"));
				}
				return;
			}
			BlockPos pos = blockSel.Position.AddCopy(blockSel.Face);
			if (!world.BlockAccessor.GetBlock(pos).IsReplacableBy(knappingBlock))
			{
				return;
			}
			BlockSelection placeSel = blockSel.Clone();
			placeSel.Position = pos;
			placeSel.DidOffset = true;
			string error = "";
			if (!knappingBlock.TryPlaceBlock(world, byPlayer, slot.Itemstack, placeSel, ref error))
			{
				(api as ICoreClientAPI)?.TriggerIngameError(this, "cantplace", Lang.Get("placefailure-" + error));
				return;
			}
			world.BlockAccessor.TriggerNeighbourBlockUpdate(pos);
			if (knappingBlock.Sounds != null)
			{
				world.PlaySoundAt(knappingBlock.Sounds.Place, pos, -0.5);
			}
			if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityKnappingSurface bec)
			{
				bec.BaseMaterial = slot.Itemstack.Clone();
				bec.BaseMaterial.StackSize = 1;
				if (byEntity.World is IClientWorldAccessor)
				{
					bec.OpenDialog(world as IClientWorldAccessor, pos, slot.Itemstack);
				}
			}
			slot.TakeOut(1);
			handling = EnumHandHandling.PreventDefaultAction;
		}
		else
		{
			base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
		}
	}

	public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
	{
		if (blockSel != null && byEntity.World.BlockAccessor.GetBlock(blockSel.Position) is BlockKnappingSurface && byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityKnappingSurface bea)
		{
			IPlayer byPlayer = null;
			if (byEntity is EntityPlayer)
			{
				byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
			}
			if (byPlayer != null && byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
			{
				bea.OnBeginUse(byPlayer, blockSel);
				handling = EnumHandHandling.PreventDefaultAction;
			}
		}
	}

	public override bool OnHeldAttackCancel(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
	{
		return false;
	}

	public override bool OnHeldAttackStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel)
	{
		return false;
	}

	public override void OnHeldAttackStop(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (blockSel == null || !(byEntity.World.BlockAccessor.GetBlock(blockSel.Position) is BlockKnappingSurface) || !(byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityKnappingSurface bea))
		{
			return;
		}
		IPlayer byPlayer = null;
		if (byEntity is EntityPlayer)
		{
			byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		if (byPlayer != null)
		{
			GetToolMode(slot, byPlayer, blockSel);
			if (byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use) && byEntity.World is IClientWorldAccessor)
			{
				bea.OnUseOver(byPlayer, blockSel.SelectionBoxIndex, blockSel.Face, mouseMode: true);
			}
		}
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return new WorldInteraction[1]
		{
			new WorldInteraction
			{
				ActionLangCode = "heldhelp-placetoknap",
				HotKeyCode = "shift",
				MouseButton = EnumMouseButton.Right
			}
		}.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
