using System;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

public class FurnaceSection : BlockEntityOpenableContainer
{
	public int MaxTemperature = 900;

	internal InventorySmelting inventory;

	private int state;

	private int burntime;

	private int receivedAirBlows;

	private int airblowtimer;

	public override InventoryBase Inventory => inventory;

	public override string InventoryClassName => "furnacesection";

	public FurnaceSection()
	{
		inventory = new InventorySmelting(null, null);
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		RegisterGameTickListener(OnSlowTick, 100);
	}

	private void OnSlowTick(float dt)
	{
		if (airblowtimer > 0)
		{
			airblowtimer--;
		}
		if (burntime <= 0)
		{
			return;
		}
		burntime--;
		if (state != 1)
		{
			return;
		}
		if (Api is ICoreClientAPI)
		{
			Api.World.Rand.Next(100);
			float quantitysmoke = 2.5f * (float)burntime * (float)burntime / (float)(getTotalBurnTime() * getTotalBurnTime());
			quantitysmoke += (float)Math.Min(4, airblowtimer / 2);
			while (quantitysmoke > 0.001f && (!(quantitysmoke < 1f) || !(Api.World.Rand.NextDouble() > (double)quantitysmoke)))
			{
				_ = Pos.X;
				Api.World.Rand.NextDouble();
				_ = Pos.Y;
				Api.World.Rand.NextDouble();
				_ = Pos.Z;
				Api.World.Rand.NextDouble();
				quantitysmoke -= 1f;
			}
		}
		else if (burntime - receivedAirBlows * 20 <= 1)
		{
			finishMelt();
		}
	}

	public void finishMelt()
	{
		state = 2;
		Slot(0).Itemstack = null;
		Slot(2).Itemstack = getSmeltedOre(Slot(1));
		if (Slot(2).Itemstack != null)
		{
			Slot(2).Itemstack.StackSize = Slot(1).Itemstack.StackSize / getSmeltedRatio(Slot(1).Itemstack);
		}
		Slot(1).Itemstack.StackSize = 0;
		if (Slot(2).Itemstack.StackSize == 0)
		{
			state = 0;
		}
		burntime = 0;
		receivedAirBlows = 0;
		Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
	}

	public ItemSlot Slot(int i)
	{
		return inventory[i];
	}

	public ItemStack getSmeltedOre(ItemSlot oreSlot)
	{
		if (oreSlot.Itemstack == null)
		{
			return null;
		}
		CombustibleProperties compustibleOpts = oreSlot.Itemstack.Collectible.CombustibleProps;
		if (compustibleOpts == null)
		{
			return null;
		}
		ItemStack smelted = compustibleOpts.SmeltedStack.ResolvedItemstack.Clone();
		if (compustibleOpts.MeltingPoint <= MaxTemperature)
		{
			return smelted;
		}
		return null;
	}

	public int getSmeltedRatio(IItemStack oreStack)
	{
		return oreStack?.Collectible.CombustibleProps.SmeltedStack.ResolvedItemstack.StackSize ?? 0;
	}

	public int getTotalBurnTime()
	{
		return 6000;
	}

	public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
	{
		return true;
	}

	public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
	{
		if (packetid < 1000)
		{
			Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);
			Api.World.BlockAccessor.GetChunkAtBlockPos(Pos).MarkModified();
		}
		else if (packetid == 1001 && player.InventoryManager != null)
		{
			player.InventoryManager.CloseInventory(Inventory);
		}
	}

	public override void OnReceivedServerPacket(int packetid, byte[] data)
	{
		if (packetid == 1000)
		{
			using MemoryStream ms = new MemoryStream(data);
			BinaryReader reader = new BinaryReader(ms);
			reader.ReadString();
			reader.ReadString();
			TreeAttribute tree = new TreeAttribute();
			tree.FromBytes(reader);
			Inventory.FromTreeAttributes(tree);
			Inventory.ResolveBlocksOrItems();
			_ = (IClientWorldAccessor)Api.World;
		}
		if (packetid == 1001)
		{
			((IClientWorldAccessor)Api.World).Player.InventoryManager.CloseInventory(Inventory);
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		ITreeAttribute invtree = new TreeAttribute();
		Inventory.ToTreeAttributes(invtree);
		tree["inventory"] = invtree;
	}
}
