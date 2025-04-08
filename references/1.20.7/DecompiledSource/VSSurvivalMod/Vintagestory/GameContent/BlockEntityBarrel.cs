using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockEntityBarrel : BlockEntityLiquidContainer
{
	private GuiDialogBarrel invDialog;

	private MeshData currentMesh;

	private BlockBarrel ownBlock;

	public bool Sealed;

	public double SealedSinceTotalHours;

	public BarrelRecipe CurrentRecipe;

	public int CurrentOutSize;

	private bool ignoreChange;

	public int CapacityLitres { get; set; } = 50;


	public override string InventoryClassName => "barrel";

	public bool CanSeal
	{
		get
		{
			FindMatchingRecipe();
			if (CurrentRecipe != null && CurrentRecipe.SealHours > 0.0)
			{
				return true;
			}
			return false;
		}
	}

	public BlockEntityBarrel()
	{
		inventory = new InventoryGeneric(2, null, null, (int id, InventoryGeneric self) => (id == 0) ? ((ItemSlot)new ItemSlotBarrelInput(self)) : ((ItemSlot)new ItemSlotLiquidOnly(self, 50f)));
		inventory.BaseWeight = 1f;
		inventory.OnGetSuitability = GetSuitability;
		inventory.SlotModified += Inventory_SlotModified;
		inventory.OnAcquireTransitionSpeed += Inventory_OnAcquireTransitionSpeed1;
	}

	private float Inventory_OnAcquireTransitionSpeed1(EnumTransitionType transType, ItemStack stack, float mul)
	{
		if (Sealed && CurrentRecipe != null && CurrentRecipe.SealHours > 0.0)
		{
			return 0f;
		}
		return mul;
	}

	private float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
	{
		if (targetSlot == inventory[1] && inventory[0].StackSize > 0)
		{
			ItemStack currentStack = inventory[0].Itemstack;
			ItemStack testStack = sourceSlot.Itemstack;
			if (currentStack.Collectible.Equals(currentStack, testStack, GlobalConstants.IgnoredStackAttributes))
			{
				return -1f;
			}
		}
		return (isMerge ? (inventory.BaseWeight + 3f) : (inventory.BaseWeight + 1f)) + (float)((sourceSlot.Inventory is InventoryBasePlayer) ? 1 : 0);
	}

	protected override ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
	{
		if (atBlockFace == BlockFacing.UP)
		{
			return inventory[0];
		}
		return null;
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		ownBlock = base.Block as BlockBarrel;
		BlockBarrel blockBarrel = ownBlock;
		if (blockBarrel != null && (blockBarrel.Attributes?["capacityLitres"].Exists).GetValueOrDefault())
		{
			CapacityLitres = ownBlock.Attributes["capacityLitres"].AsInt(50);
			(inventory[1] as ItemSlotLiquidOnly).CapacityLitres = CapacityLitres;
		}
		if (api.Side == EnumAppSide.Client && currentMesh == null)
		{
			currentMesh = GenMesh();
			MarkDirty(redrawOnClient: true);
		}
		if (api.Side == EnumAppSide.Server)
		{
			RegisterGameTickListener(OnEvery3Second, 3000);
		}
		FindMatchingRecipe();
	}

	private void Inventory_SlotModified(int slotId)
	{
		if (!ignoreChange && (slotId == 0 || slotId == 1))
		{
			invDialog?.UpdateContents();
			ICoreAPI api = Api;
			if (api != null && api.Side == EnumAppSide.Client)
			{
				currentMesh = GenMesh();
			}
			MarkDirty(redrawOnClient: true);
			FindMatchingRecipe();
		}
	}

	private void FindMatchingRecipe()
	{
		ItemSlot[] inputSlots = new ItemSlot[2]
		{
			inventory[0],
			inventory[1]
		};
		CurrentRecipe = null;
		foreach (BarrelRecipe recipe in Api.GetBarrelRecipes())
		{
			if (!recipe.Matches(inputSlots, out var outsize))
			{
				continue;
			}
			ignoreChange = true;
			if (recipe.SealHours > 0.0)
			{
				CurrentRecipe = recipe;
				CurrentOutSize = outsize;
			}
			else
			{
				ICoreAPI api = Api;
				if (api != null && api.Side == EnumAppSide.Server)
				{
					recipe.TryCraftNow(Api, 0.0, inputSlots);
					MarkDirty(redrawOnClient: true);
					Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
				}
			}
			invDialog?.UpdateContents();
			ICoreAPI api2 = Api;
			if (api2 != null && api2.Side == EnumAppSide.Client)
			{
				currentMesh = GenMesh();
				MarkDirty(redrawOnClient: true);
			}
			ignoreChange = false;
			break;
		}
	}

	private void OnEvery3Second(float dt)
	{
		if (!inventory[0].Empty && CurrentRecipe == null)
		{
			FindMatchingRecipe();
		}
		if (CurrentRecipe != null)
		{
			if (Sealed && CurrentRecipe.TryCraftNow(Api, Api.World.Calendar.TotalHours - SealedSinceTotalHours, new ItemSlot[2]
			{
				inventory[0],
				inventory[1]
			}))
			{
				MarkDirty(redrawOnClient: true);
				Api.World.BlockAccessor.MarkBlockEntityDirty(Pos);
				Sealed = false;
			}
		}
		else if (Sealed)
		{
			Sealed = false;
			MarkDirty(redrawOnClient: true);
		}
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(byItemStack);
		ItemSlot inputSlot = Inventory[0];
		ItemSlot liquidSlot = Inventory[1];
		if (!inputSlot.Empty && liquidSlot.Empty && BlockLiquidContainerBase.GetContainableProps(inputSlot.Itemstack) != null)
		{
			Inventory.TryFlipItems(1, inputSlot);
		}
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
		if (!Sealed)
		{
			base.OnBlockBroken(byPlayer);
		}
		invDialog?.TryClose();
		invDialog = null;
	}

	public void SealBarrel()
	{
		if (!Sealed)
		{
			Sealed = true;
			SealedSinceTotalHours = Api.World.Calendar.TotalHours;
			MarkDirty(redrawOnClient: true);
		}
	}

	public void OnPlayerRightClick(IPlayer byPlayer)
	{
		if (!Sealed)
		{
			FindMatchingRecipe();
			if (Api.Side == EnumAppSide.Client)
			{
				toggleInventoryDialogClient(byPlayer);
			}
		}
	}

	protected void toggleInventoryDialogClient(IPlayer byPlayer)
	{
		if (invDialog == null)
		{
			ICoreClientAPI capi = Api as ICoreClientAPI;
			invDialog = new GuiDialogBarrel(Lang.Get("Barrel"), Inventory, Pos, Api as ICoreClientAPI);
			invDialog.OnClosed += delegate
			{
				invDialog = null;
				capi.Network.SendBlockEntityPacket(Pos, 1001);
				capi.Network.SendPacketClient(Inventory.Close(byPlayer));
			};
			invDialog.OpenSound = AssetLocation.Create("sounds/block/barrelopen", base.Block.Code.Domain);
			invDialog.CloseSound = AssetLocation.Create("sounds/block/barrelclose", base.Block.Code.Domain);
			invDialog.TryOpen();
			capi.Network.SendPacketClient(Inventory.Open(byPlayer));
			capi.Network.SendBlockEntityPacket(Pos, 1000);
		}
		else
		{
			invDialog.TryClose();
		}
	}

	public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
	{
		base.OnReceivedClientPacket(player, packetid, data);
		if (packetid < 1000)
		{
			Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);
			Api.World.BlockAccessor.GetChunkAtBlockPos(Pos).MarkModified();
			return;
		}
		if (packetid == 1001)
		{
			player.InventoryManager?.CloseInventory(Inventory);
		}
		if (packetid == 1000)
		{
			player.InventoryManager?.OpenInventory(Inventory);
		}
		if (packetid == 1337)
		{
			SealBarrel();
		}
	}

	public override void OnReceivedServerPacket(int packetid, byte[] data)
	{
		base.OnReceivedServerPacket(packetid, data);
		if (packetid == 1001)
		{
			(Api.World as IClientWorldAccessor).Player.InventoryManager.CloseInventory(Inventory);
			invDialog?.TryClose();
			invDialog?.Dispose();
			invDialog = null;
		}
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		Sealed = tree.GetBool("sealed");
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Client)
		{
			currentMesh = GenMesh();
			MarkDirty(redrawOnClient: true);
			invDialog?.UpdateContents();
		}
		SealedSinceTotalHours = tree.GetDouble("sealedSinceTotalHours");
		if (Api != null)
		{
			FindMatchingRecipe();
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("sealed", Sealed);
		tree.SetDouble("sealedSinceTotalHours", SealedSinceTotalHours);
	}

	internal MeshData GenMesh()
	{
		if (ownBlock == null)
		{
			return null;
		}
		MeshData mesh = ownBlock.GenMesh(inventory[0].Itemstack, inventory[1].Itemstack, Sealed, Pos);
		if (mesh.CustomInts != null)
		{
			for (int i = 0; i < mesh.CustomInts.Count; i++)
			{
				mesh.CustomInts.Values[i] |= 134217728;
				mesh.CustomInts.Values[i] |= 67108864;
			}
		}
		return mesh;
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		invDialog?.Dispose();
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		mesher.AddMeshData(currentMesh);
		return true;
	}
}
