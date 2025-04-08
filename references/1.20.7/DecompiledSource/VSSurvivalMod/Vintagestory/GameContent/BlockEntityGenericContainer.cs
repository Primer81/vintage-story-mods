using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class BlockEntityGenericContainer : BlockEntityOpenableContainer
{
	internal InventoryGeneric inventory;

	public int quantitySlots = 16;

	public string inventoryClassName = "chest";

	public string dialogTitleLangCode = "chestcontents";

	public bool retrieveOnly;

	public override InventoryBase Inventory => inventory;

	public override string InventoryClassName => inventoryClassName;

	public override void Initialize(ICoreAPI api)
	{
		if (inventory == null)
		{
			InitInventory(base.Block);
		}
		base.Initialize(api);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		if (inventory == null)
		{
			if (tree.HasAttribute("forBlockId"))
			{
				InitInventory(worldForResolving.GetBlock((ushort)tree.GetInt("forBlockId")));
			}
			else
			{
				if (tree.GetTreeAttribute("inventory").GetInt("qslots") == 8)
				{
					quantitySlots = 8;
					inventoryClassName = "basket";
					dialogTitleLangCode = "basketcontents";
				}
				InitInventory(null);
			}
		}
		base.FromTreeAttributes(tree, worldForResolving);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		if (base.Block != null)
		{
			tree.SetInt("forBlockId", base.Block.BlockId);
		}
	}

	private void InitInventory(Block Block)
	{
		if (Block?.Attributes != null)
		{
			inventoryClassName = Block.Attributes["inventoryClassName"].AsString(inventoryClassName);
			dialogTitleLangCode = Block.Attributes["dialogTitleLangCode"].AsString(dialogTitleLangCode);
			quantitySlots = Block.Attributes["quantitySlots"].AsInt(quantitySlots);
			retrieveOnly = Block.Attributes["retrieveOnly"].AsBool();
		}
		inventory = new InventoryGeneric(quantitySlots, null, null);
		JsonObject attributes = Block.Attributes;
		if (attributes != null && attributes["spoilSpeedMulByFoodCat"].Exists)
		{
			inventory.PerishableFactorByFoodCategory = Block.Attributes["spoilSpeedMulByFoodCat"].AsObject<Dictionary<EnumFoodCategory, float>>();
		}
		JsonObject attributes2 = Block.Attributes;
		if (attributes2 != null && attributes2["transitionSpeedMul"].Exists)
		{
			inventory.TransitionableSpeedMulByType = Block.Attributes["transitionSpeedMul"].AsObject<Dictionary<EnumTransitionType, float>>();
		}
		inventory.OnInventoryClosed += OnInvClosed;
		inventory.OnInventoryOpened += OnInvOpened;
		inventory.SlotModified += OnSlotModifid;
	}

	private void OnSlotModifid(int slot)
	{
		Api.World.BlockAccessor.GetChunkAtBlockPos(Pos)?.MarkModified();
	}

	protected virtual void OnInvOpened(IPlayer player)
	{
		inventory.PutLocked = retrieveOnly && player.WorldData.CurrentGameMode != EnumGameMode.Creative;
	}

	protected virtual void OnInvClosed(IPlayer player)
	{
		inventory.PutLocked = retrieveOnly;
		invDialog?.Dispose();
		invDialog = null;
	}

	public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
	{
		if (Api.World is IServerWorldAccessor)
		{
			byte[] data = BlockEntityContainerOpen.ToBytes("BlockEntityInventory", Lang.Get(dialogTitleLangCode), 4, inventory);
			((ICoreServerAPI)Api).Network.SendBlockEntityPacket((IServerPlayer)byPlayer, Pos, 5000, data);
			byPlayer.InventoryManager.OpenInventory(inventory);
		}
		return true;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		if (retrieveOnly)
		{
			base.GetBlockInfo(forPlayer, dsc);
		}
	}
}
