using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityDeadCrop : BlockEntityContainer
{
	private InventoryGeneric inv;

	public EnumCropStressType deathReason;

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "deadcrop";

	public BlockEntityDeadCrop()
	{
		inv = new InventoryGeneric(1, "deadcrop-0", null);
	}

	public override void Initialize(ICoreAPI api)
	{
		Api = api;
		foreach (BlockEntityBehavior val in Behaviors)
		{
			val.Initialize(api, val.properties);
		}
		Inventory.LateInitialize(InventoryClassName + "-" + Pos, api);
		Inventory.Pos = Pos;
		Inventory.ResolveBlocksOrItems();
		container.Init(Api, () => Pos, delegate
		{
			MarkDirty(redrawOnClient: true);
		});
		container.LateInit();
	}

	public ItemStack[] GetDrops(IPlayer byPlayer, float dropQuantityMultiplier)
	{
		if (inv[0].Empty)
		{
			return new ItemStack[0];
		}
		ItemStack[] drops = inv[0].Itemstack.Block.GetDrops(Api.World, Pos, byPlayer, dropQuantityMultiplier);
		ItemStack seedStack = drops.FirstOrDefault((ItemStack stack) => stack.Collectible is ItemPlantableSeed);
		if (seedStack == null)
		{
			seedStack = inv[0].Itemstack.Block.Drops.FirstOrDefault((BlockDropItemStack bstack) => bstack.ResolvedItemstack.Collectible is ItemPlantableSeed)?.ResolvedItemstack.Clone();
			if (seedStack != null)
			{
				drops = drops.Append(seedStack);
			}
		}
		return drops;
	}

	public string GetPlacedBlockName()
	{
		if (inv[0].Empty)
		{
			return Lang.Get("Dead crop");
		}
		return Lang.Get("Dead {0}", inv[0].Itemstack.GetName());
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		deathReason = (EnumCropStressType)tree.GetInt("deathReason");
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetInt("deathReason", (int)deathReason);
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		switch (deathReason)
		{
		case EnumCropStressType.TooHot:
			dsc.AppendLine(Lang.Get("Died from too high temperatues."));
			break;
		case EnumCropStressType.TooCold:
			dsc.AppendLine(Lang.Get("Died from too low temperatures."));
			break;
		case EnumCropStressType.Eaten:
			dsc.AppendLine(Lang.Get("Eaten by wild animals."));
			break;
		}
		if (!inv[0].Empty)
		{
			dsc.Append(inv[0].Itemstack.Block.GetPlacedBlockInfo(Api.World, Pos, forPlayer));
		}
	}
}
