using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemPlantableSeed : Item
{
	private WorldInteraction[] interactions;

	public override void OnLoaded(ICoreAPI api)
	{
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		_ = api;
		interactions = ObjectCacheUtil.GetOrCreate(api, "seedInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (Block current in api.World.Blocks)
			{
				if (!(current.Code == null) && current.EntityClass != null && api.World.ClassRegistry.GetBlockEntity(current.EntityClass) == typeof(BlockEntityFarmland))
				{
					list.Add(new ItemStack(current));
				}
			}
			return new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "heldhelp-plant",
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray()
				}
			};
		});
	}

	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (blockSel == null)
		{
			return;
		}
		BlockPos pos = blockSel.Position;
		string lastCodePart = itemslot.Itemstack.Collectible.LastCodePart();
		if (lastCodePart == "bellpepper")
		{
			return;
		}
		BlockEntity be = byEntity.World.BlockAccessor.GetBlockEntity(pos);
		if (!(be is BlockEntityFarmland))
		{
			return;
		}
		Block cropBlock = byEntity.World.GetBlock(CodeWithPath("crop-" + lastCodePart + "-1"));
		if (cropBlock == null)
		{
			return;
		}
		IPlayer byPlayer = null;
		if (byEntity is EntityPlayer)
		{
			byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);
		}
		bool num = ((BlockEntityFarmland)be).TryPlant(cropBlock);
		if (num)
		{
			byEntity.World.PlaySoundAt(new AssetLocation("sounds/block/plant"), pos, 0.4375, byPlayer);
			((byEntity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			if (byPlayer == null || (byPlayer.WorldData?.CurrentGameMode).GetValueOrDefault() != EnumGameMode.Creative)
			{
				itemslot.TakeOut(1);
				itemslot.MarkDirty();
			}
		}
		if (num)
		{
			handHandling = EnumHandHandling.PreventDefault;
		}
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		Block cropBlock = world.GetBlock(CodeWithPath("crop-" + inSlot.Itemstack.Collectible.LastCodePart() + "-1"));
		if (cropBlock != null && cropBlock.CropProps != null)
		{
			dsc.AppendLine(Lang.Get("soil-nutrition-requirement") + cropBlock.CropProps.RequiredNutrient);
			dsc.AppendLine(Lang.Get("soil-nutrition-consumption") + cropBlock.CropProps.NutrientConsumption);
			double totalDays = cropBlock.CropProps.TotalGrowthDays;
			totalDays = ((!(totalDays > 0.0)) ? ((double)(cropBlock.CropProps.TotalGrowthMonths * (float)world.Calendar.DaysPerMonth)) : (totalDays / 12.0 * (double)world.Calendar.DaysPerMonth));
			totalDays /= api.World.Config.GetDecimal("cropGrowthRateMul", 1.0);
			dsc.AppendLine(Lang.Get("soil-growth-time") + " " + Lang.Get("count-days", Math.Round(totalDays, 1)));
			dsc.AppendLine(Lang.Get("crop-coldresistance", Math.Round(cropBlock.CropProps.ColdDamageBelow, 1)));
			dsc.AppendLine(Lang.Get("crop-heatresistance", Math.Round(cropBlock.CropProps.HeatDamageAbove, 1)));
		}
	}

	public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
	{
		return interactions.Append(base.GetHeldInteractionHelp(inSlot));
	}
}
