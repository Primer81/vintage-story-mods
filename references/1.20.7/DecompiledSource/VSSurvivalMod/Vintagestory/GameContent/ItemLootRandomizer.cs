using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemLootRandomizer : Item, IResolvableCollectible
{
	private Random rand;

	public override void OnLoaded(ICoreAPI api)
	{
		rand = new Random();
		base.OnLoaded(api);
	}

	public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if ((byEntity as EntityPlayer).Player != null)
		{
			TreeAttribute tree = new TreeAttribute();
			tree.SetString("inventoryId", slot.Inventory.InventoryID);
			tree.SetInt("slotId", slot.Inventory.GetSlotId(slot));
			api.Event.PushEvent("OpenLootRandomizerDialog", tree);
		}
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		int i = 0;
		foreach (KeyValuePair<string, IAttribute> val in inSlot.Itemstack.Attributes)
		{
			if (val.Key.StartsWithOrdinal("stack") && val.Value is TreeAttribute)
			{
				TreeAttribute subtree = val.Value as TreeAttribute;
				if (i == 0)
				{
					dsc.AppendLine("Contents: ");
				}
				ItemStack cstack = subtree.GetItemstack("stack");
				cstack.ResolveBlockOrItem(world);
				dsc.AppendLine(cstack.StackSize + "x " + cstack.GetName() + ": " + subtree.GetFloat("chance") + "%");
				i++;
			}
		}
	}

	public void Resolve(ItemSlot slot, IWorldAccessor worldForResolve, bool resolveImports)
	{
		if (!resolveImports)
		{
			return;
		}
		double diceRoll = rand.NextDouble();
		ItemStack itemstack = slot.Itemstack;
		slot.Itemstack = null;
		IAttribute[] values = itemstack.Attributes.Values;
		values.Shuffle(rand);
		IAttribute[] array = values;
		foreach (IAttribute val in array)
		{
			if (!(val is TreeAttribute))
			{
				continue;
			}
			TreeAttribute subtree = val as TreeAttribute;
			float chance = subtree.GetFloat("chance") / 100f;
			if ((double)chance > diceRoll)
			{
				ItemStack cstack = subtree.GetItemstack("stack")?.Clone();
				if (cstack?.Collectible != null)
				{
					cstack.ResolveBlockOrItem(worldForResolve);
					slot.Itemstack = cstack;
				}
				else
				{
					slot.Itemstack = null;
				}
				break;
			}
			diceRoll -= (double)chance;
		}
	}

	public BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
	{
		IAttribute[] values = handbookStack.Attributes.Values;
		List<BlockDropItemStack> resolvedDrops = new List<BlockDropItemStack>();
		IAttribute[] array = values;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i] is TreeAttribute subtree)
			{
				ItemStack dropsStack = subtree.GetItemstack("stack")?.Clone();
				if (dropsStack?.Collectible != null)
				{
					dropsStack.ResolveBlockOrItem(forPlayer.Entity.World);
					resolvedDrops.Add(new BlockDropItemStack(dropsStack));
				}
			}
		}
		return resolvedDrops.ToArray();
	}

	public override void OnStoreCollectibleMappings(IWorldAccessor world, ItemSlot inSlot, Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
		base.OnStoreCollectibleMappings(world, inSlot, blockIdMapping, itemIdMapping);
		foreach (KeyValuePair<string, IAttribute> val in inSlot.Itemstack.Attributes)
		{
			if (val.Key.StartsWithOrdinal("stack") && val.Value is TreeAttribute)
			{
				ItemStack cstack = (val.Value as TreeAttribute).GetItemstack("stack");
				cstack.ResolveBlockOrItem(world);
				if (cstack.Class == EnumItemClass.Block)
				{
					blockIdMapping[cstack.Id] = cstack.Collectible.Code;
				}
				else
				{
					itemIdMapping[cstack.Id] = cstack.Collectible.Code;
				}
			}
		}
	}
}
