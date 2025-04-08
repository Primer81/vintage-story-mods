using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityGroundStorage : BlockEntityDisplay, IBlockEntityContainer, IRotatable, IHeatSource, ITemperatureSensitive
{
	private static SimpleParticleProperties smokeParticles;

	public object inventoryLock = new object();

	protected InventoryGeneric inventory;

	public bool forceStorageProps;

	protected EnumGroundStorageLayout? overrideLayout;

	protected Cuboidf[] colBoxes;

	protected Cuboidf[] selBoxes;

	private ItemSlot isUsingSlot;

	public bool clientsideFirstPlacement;

	private GroundStorageRenderer renderer;

	public bool UseRenderer;

	public bool NeedsRetesselation;

	public MultiTextureMeshRef[] MeshRefs = new MultiTextureMeshRef[4];

	public ModelTransform[] ModelTransformsRenderer = new ModelTransform[4];

	private bool burning;

	private double burnStartTotalHours;

	private ILoadedSound ambientSound;

	private long listenerId;

	private float burnHoursPerItem;

	private BlockFacing[] facings = (BlockFacing[])BlockFacing.ALLFACES.Clone();

	public GroundStorageProperties StorageProps { get; protected set; }

	public int TransferQuantity => StorageProps?.TransferQuantity ?? 1;

	public int BulkTransferQuantity
	{
		get
		{
			if (StorageProps.Layout != EnumGroundStorageLayout.Stacking)
			{
				return 1;
			}
			return StorageProps.BulkTransferQuantity;
		}
	}

	protected virtual int invSlotCount => 4;

	private Dictionary<string, MultiTextureMeshRef> UploadedMeshCache => ObjectCacheUtil.GetOrCreate(Api, "groundStorageUMC", () => new Dictionary<string, MultiTextureMeshRef>());

	public bool CanIgnite
	{
		get
		{
			if (burnHoursPerItem > 0f)
			{
				ItemStack itemstack = inventory[0].Itemstack;
				if (itemstack == null)
				{
					return false;
				}
				return itemstack.Collectible.CombustibleProps?.BurnTemperature > 200;
			}
			return false;
		}
	}

	public int Layers
	{
		get
		{
			if (inventory[0].StackSize != 1)
			{
				return (int)((float)inventory[0].StackSize * StorageProps.ModelItemsToStackSizeRatio);
			}
			return 1;
		}
	}

	public bool IsBurning => burning;

	public bool IsHot => burning;

	public override int DisplayedItems
	{
		get
		{
			if (StorageProps == null)
			{
				return 0;
			}
			return StorageProps.Layout switch
			{
				EnumGroundStorageLayout.SingleCenter => 1, 
				EnumGroundStorageLayout.Halves => 2, 
				EnumGroundStorageLayout.WallHalves => 2, 
				EnumGroundStorageLayout.Quadrants => 4, 
				EnumGroundStorageLayout.Stacking => 1, 
				_ => 0, 
			};
		}
	}

	public int TotalStackSize
	{
		get
		{
			int sum = 0;
			foreach (ItemSlot slot in inventory)
			{
				sum += slot.StackSize;
			}
			return sum;
		}
	}

	public int Capacity => StorageProps.Layout switch
	{
		EnumGroundStorageLayout.SingleCenter => 1, 
		EnumGroundStorageLayout.Halves => 2, 
		EnumGroundStorageLayout.WallHalves => 2, 
		EnumGroundStorageLayout.Quadrants => 4, 
		EnumGroundStorageLayout.Stacking => StorageProps.StackingCapacity, 
		_ => 1, 
	};

	public override InventoryBase Inventory => inventory;

	public override string InventoryClassName => "groundstorage";

	public override string AttributeTransformCode => "groundStorageTransform";

	public float MeshAngle { get; set; }

	public BlockFacing AttachFace { get; set; }

	public override TextureAtlasPosition this[string textureCode]
	{
		get
		{
			if (StorageProps.Layout == EnumGroundStorageLayout.Stacking && StorageProps.StackingTextures != null && StorageProps.StackingTextures.TryGetValue(textureCode, out var texturePath))
			{
				return getOrCreateTexPos(texturePath);
			}
			return base[textureCode];
		}
	}

	static BlockEntityGroundStorage()
	{
		smokeParticles = new SimpleParticleProperties(1f, 1f, ColorUtil.ToRgba(150, 40, 40, 40), new Vec3d(), new Vec3d(1.0, 0.0, 1.0), new Vec3f(-1f / 32f, 0.1f, -1f / 32f), new Vec3f(1f / 32f, 0.1f, 1f / 32f), 2f, -1f / 160f, 0.2f, 1f, EnumParticleModel.Quad);
		smokeParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.25f);
		smokeParticles.SelfPropelled = true;
		smokeParticles.AddPos.Set(1.0, 0.0, 1.0);
	}

	public bool CanAttachBlockAt(BlockFacing blockFace, Cuboidi attachmentArea)
	{
		if (StorageProps == null)
		{
			return false;
		}
		if (blockFace == BlockFacing.UP && StorageProps.Layout == EnumGroundStorageLayout.Stacking && inventory[0].StackSize == Capacity)
		{
			return StorageProps.UpSolid;
		}
		return false;
	}

	public BlockEntityGroundStorage()
	{
		inventory = new InventoryGeneric(invSlotCount, null, null, (int slotId, InventoryGeneric inv) => new ItemSlot(inv));
		foreach (ItemSlot item in inventory)
		{
			item.StorageType |= EnumItemStorageFlags.Backpack;
		}
		inventory.OnGetAutoPushIntoSlot = GetAutoPushIntoSlot;
		inventory.OnGetAutoPullFromSlot = GetAutoPullFromSlot;
		colBoxes = new Cuboidf[1]
		{
			new Cuboidf(0f, 0f, 0f, 1f, 0.25f, 1f)
		};
		selBoxes = new Cuboidf[1]
		{
			new Cuboidf(0f, 0f, 0f, 1f, 0.25f, 1f)
		};
	}

	private ItemSlot GetAutoPullFromSlot(BlockFacing atBlockFace)
	{
		return null;
	}

	private ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
	{
		return null;
	}

	public void ForceStorageProps(GroundStorageProperties storageProps)
	{
		StorageProps = storageProps;
		forceStorageProps = true;
	}

	public override void Initialize(ICoreAPI api)
	{
		capi = api as ICoreClientAPI;
		base.Initialize(api);
		BEBehaviorBurning bh = GetBehavior<BEBehaviorBurning>();
		if (bh != null)
		{
			bh.FirePos = Pos.Copy();
			bh.FuelPos = Pos.Copy();
			bh.OnFireDeath = delegate
			{
				Extinguish();
			};
		}
		UpdateIgnitable();
		DetermineStorageProperties(null);
		if (capi != null)
		{
			float temp = 0f;
			if (!Inventory.Empty)
			{
				foreach (ItemSlot slot in Inventory)
				{
					temp = Math.Max(temp, slot.Itemstack?.Collectible.GetTemperature(capi.World, slot.Itemstack) ?? 0f);
				}
			}
			if (temp >= 450f)
			{
				renderer = new GroundStorageRenderer(capi, this);
			}
			updateMeshes();
		}
		UpdateBurningState();
	}

	public void CoolNow(float amountRel)
	{
		if (Inventory.Empty)
		{
			return;
		}
		for (int index = 0; index < Inventory.Count; index++)
		{
			ItemSlot slot = Inventory[index];
			ItemStack itemStack = slot.Itemstack;
			if (itemStack?.Collectible == null)
			{
				continue;
			}
			float temperature = itemStack.Collectible.GetTemperature(Api.World, itemStack);
			float breakChance = Math.Max(0f, amountRel - 0.6f) * Math.Max(temperature - 250f, 0f) / 5000f;
			if ((itemStack.Collectible.Code.Path.Contains("burn") || itemStack.Collectible.Code.Path.Contains("fired")) && Api.World.Rand.NextDouble() < (double)breakChance)
			{
				Api.World.PlaySoundAt(new AssetLocation("sounds/block/ceramicbreak"), Pos, -0.4);
				base.Block.SpawnBlockBrokenParticles(Pos);
				base.Block.SpawnBlockBrokenParticles(Pos);
				int size = itemStack.StackSize;
				slot.Itemstack = GetShatteredStack(itemStack);
				StorageProps = null;
				DetermineStorageProperties(slot);
				forceStorageProps = true;
				slot.Itemstack.StackSize = size;
				slot.MarkDirty();
			}
			else
			{
				if (temperature > 120f)
				{
					Api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish"), Pos, -0.5, null, randomizePitch: false, 16f);
				}
				itemStack.Collectible.SetTemperature(Api.World, itemStack, Math.Max(20f, temperature - amountRel * 20f), delayCooldown: false);
			}
		}
		MarkDirty(redrawOnClient: true);
		if (Inventory.Empty)
		{
			Api.World.BlockAccessor.SetBlock(0, Pos);
		}
	}

	private void UpdateIgnitable()
	{
		CollectibleBehaviorGroundStorable cbhgs = Inventory[0].Itemstack?.Collectible.GetBehavior<CollectibleBehaviorGroundStorable>();
		if (cbhgs != null)
		{
			burnHoursPerItem = JsonObject.FromJson(cbhgs.propertiesAtString)["burnHoursPerItem"].AsFloat(burnHoursPerItem);
		}
	}

	protected ItemStack GetShatteredStack(ItemStack contents)
	{
		JsonItemStack shatteredStack = contents.Collectible.Attributes?["shatteredStack"].AsObject<JsonItemStack>();
		if (shatteredStack != null)
		{
			shatteredStack.Resolve(Api.World, "shatteredStack for" + contents.Collectible.Code);
			if (shatteredStack.ResolvedItemstack != null)
			{
				return shatteredStack.ResolvedItemstack;
			}
		}
		shatteredStack = base.Block.Attributes?["shatteredStack"].AsObject<JsonItemStack>();
		if (shatteredStack != null)
		{
			shatteredStack.Resolve(Api.World, "shatteredStack for" + contents.Collectible.Code);
			if (shatteredStack.ResolvedItemstack != null)
			{
				return shatteredStack.ResolvedItemstack;
			}
		}
		return null;
	}

	public Cuboidf[] GetSelectionBoxes()
	{
		return selBoxes;
	}

	public Cuboidf[] GetCollisionBoxes()
	{
		return colBoxes;
	}

	public virtual bool OnPlayerInteractStart(IPlayer player, BlockSelection bs)
	{
		ItemSlot hotbarSlot = player.InventoryManager.ActiveHotbarSlot;
		if (!hotbarSlot.Empty && !hotbarSlot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorGroundStorable>())
		{
			return false;
		}
		if (!BlockBehaviorReinforcable.AllowRightClickPickup(Api.World, Pos, player))
		{
			return false;
		}
		DetermineStorageProperties(hotbarSlot);
		bool ok = false;
		if (StorageProps != null)
		{
			if (!hotbarSlot.Empty && StorageProps.CtrlKey && !player.Entity.Controls.CtrlKey)
			{
				return false;
			}
			Vec3f hitPos = rotatedOffset(bs.HitPosition.ToVec3f(), 0f - MeshAngle);
			if (StorageProps.Layout == EnumGroundStorageLayout.Quadrants && inventory.Empty)
			{
				double num = Math.Abs((double)hitPos.X - 0.5);
				double dz = Math.Abs((double)hitPos.Z - 0.5);
				if (num < 0.125 && dz < 0.125)
				{
					overrideLayout = EnumGroundStorageLayout.SingleCenter;
					DetermineStorageProperties(hotbarSlot);
				}
			}
			switch (StorageProps.Layout)
			{
			case EnumGroundStorageLayout.SingleCenter:
				ok = putOrGetItemSingle(inventory[0], player, bs);
				break;
			case EnumGroundStorageLayout.Halves:
			case EnumGroundStorageLayout.WallHalves:
				ok = ((!((double)hitPos.X < 0.5)) ? putOrGetItemSingle(inventory[1], player, bs) : putOrGetItemSingle(inventory[0], player, bs));
				break;
			case EnumGroundStorageLayout.Quadrants:
			{
				int pos = (((double)hitPos.X > 0.5) ? 2 : 0) + (((double)hitPos.Z > 0.5) ? 1 : 0);
				ok = putOrGetItemSingle(inventory[pos], player, bs);
				break;
			}
			case EnumGroundStorageLayout.Stacking:
				ok = putOrGetItemStacking(player, bs);
				break;
			}
		}
		UpdateIgnitable();
		renderer?.UpdateTemps();
		if (ok)
		{
			MarkDirty();
		}
		if (inventory.Empty && !clientsideFirstPlacement)
		{
			Api.World.BlockAccessor.SetBlock(0, Pos);
			Api.World.BlockAccessor.TriggerNeighbourBlockUpdate(Pos);
		}
		return ok;
	}

	public bool OnPlayerInteractStep(float secondsUsed, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (isUsingSlot?.Itemstack?.Collectible is IContainedInteractable collIci)
		{
			return collIci.OnContainedInteractStep(secondsUsed, this, isUsingSlot, byPlayer, blockSel);
		}
		isUsingSlot = null;
		return false;
	}

	public void OnPlayerInteractStop(float secondsUsed, IPlayer byPlayer, BlockSelection blockSel)
	{
		if (isUsingSlot?.Itemstack.Collectible is IContainedInteractable collIci)
		{
			collIci.OnContainedInteractStop(secondsUsed, this, isUsingSlot, byPlayer, blockSel);
		}
		isUsingSlot = null;
	}

	public ItemSlot GetSlotAt(BlockSelection bs)
	{
		if (StorageProps == null)
		{
			return null;
		}
		switch (StorageProps.Layout)
		{
		case EnumGroundStorageLayout.SingleCenter:
			return inventory[0];
		case EnumGroundStorageLayout.Halves:
		case EnumGroundStorageLayout.WallHalves:
			if (bs.HitPosition.X < 0.5)
			{
				return inventory[0];
			}
			return inventory[1];
		case EnumGroundStorageLayout.Quadrants:
		{
			Vec3f hitPos = rotatedOffset(bs.HitPosition.ToVec3f(), 0f - MeshAngle);
			int pos = (((double)hitPos.X > 0.5) ? 2 : 0) + (((double)hitPos.Z > 0.5) ? 1 : 0);
			return inventory[pos];
		}
		case EnumGroundStorageLayout.Stacking:
			return inventory[0];
		default:
			return null;
		}
	}

	public bool OnTryCreateKiln()
	{
		ItemStack stack = inventory.FirstNonEmptySlot.Itemstack;
		if (stack == null)
		{
			return false;
		}
		if (stack.StackSize > StorageProps.MaxFireable)
		{
			capi?.TriggerIngameError(this, "overfull", Lang.Get("Can only fire up to {0} at once.", StorageProps.MaxFireable));
			return false;
		}
		if (stack.Collectible.CombustibleProps == null || stack.Collectible.CombustibleProps.SmeltingType != EnumSmeltType.Fire)
		{
			capi?.TriggerIngameError(this, "notfireable", Lang.Get("This is not a fireable block or item", StorageProps.MaxFireable));
			return false;
		}
		return true;
	}

	public virtual void DetermineStorageProperties(ItemSlot sourceSlot)
	{
		ItemStack sourceStack = inventory.FirstNonEmptySlot?.Itemstack ?? sourceSlot?.Itemstack;
		if (!forceStorageProps && StorageProps == null)
		{
			if (sourceStack == null)
			{
				return;
			}
			StorageProps = sourceStack.Collectible?.GetBehavior<CollectibleBehaviorGroundStorable>()?.StorageProps;
		}
		if (StorageProps != null)
		{
			if (StorageProps.CollisionBox != null)
			{
				colBoxes[0] = (selBoxes[0] = StorageProps.CollisionBox.Clone());
			}
			else if (sourceStack?.Block != null)
			{
				colBoxes[0] = (selBoxes[0] = sourceStack.Block.CollisionBoxes[0].Clone());
			}
			if (StorageProps.SelectionBox != null)
			{
				selBoxes[0] = StorageProps.SelectionBox.Clone();
			}
			if (StorageProps.CbScaleYByLayer != 0f)
			{
				colBoxes[0] = colBoxes[0].Clone();
				colBoxes[0].Y2 *= (int)Math.Ceiling(StorageProps.CbScaleYByLayer * (float)inventory[0].StackSize) * 8 / 8;
				selBoxes[0] = colBoxes[0];
			}
			if (overrideLayout.HasValue)
			{
				StorageProps = StorageProps.Clone();
				StorageProps.Layout = overrideLayout.Value;
			}
		}
	}

	protected bool putOrGetItemStacking(IPlayer byPlayer, BlockSelection bs)
	{
		if (Api.Side == EnumAppSide.Client)
		{
			(byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			return true;
		}
		BlockPos abovePos = Pos.UpCopy();
		if (Api.World.BlockAccessor.GetBlockEntity(abovePos) is BlockEntityGroundStorage beg)
		{
			return beg.OnPlayerInteractStart(byPlayer, bs);
		}
		bool sneaking = byPlayer.Entity.Controls.ShiftKey;
		ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (sneaking && hotbarSlot.Empty)
		{
			return false;
		}
		if (sneaking && TotalStackSize >= Capacity)
		{
			Block pileblock = Api.World.BlockAccessor.GetBlock(Pos);
			if (Api.World.BlockAccessor.GetBlock(abovePos).IsReplacableBy(pileblock))
			{
				BlockGroundStorage obj = pileblock as BlockGroundStorage;
				BlockSelection bsc = bs.Clone();
				bsc.Position.Up();
				bsc.Face = null;
				return obj.CreateStorage(Api.World, bsc, byPlayer);
			}
			return false;
		}
		bool equalStack = inventory[0].Empty || (hotbarSlot.Itemstack != null && hotbarSlot.Itemstack.Equals(Api.World, inventory[0].Itemstack, GlobalConstants.IgnoredStackAttributes));
		if (sneaking && !equalStack)
		{
			return false;
		}
		lock (inventoryLock)
		{
			if (sneaking)
			{
				return TryPutItem(byPlayer);
			}
			return TryTakeItem(byPlayer);
		}
	}

	public virtual bool TryPutItem(IPlayer player)
	{
		if (TotalStackSize >= Capacity)
		{
			return false;
		}
		ItemSlot hotbarSlot = player.InventoryManager.ActiveHotbarSlot;
		if (hotbarSlot.Itemstack == null)
		{
			return false;
		}
		ItemSlot invSlot = inventory[0];
		if (invSlot.Empty)
		{
			bool putBulk = player.Entity.Controls.CtrlKey;
			if (hotbarSlot.TryPutInto(Api.World, invSlot, putBulk ? BulkTransferQuantity : TransferQuantity) > 0)
			{
				Api.World.PlaySoundAt(StorageProps.PlaceRemoveSound.WithPathPrefixOnce("sounds/"), (double)Pos.X + 0.5, Pos.InternalY, (double)Pos.Z + 0.5, null, 0.88f + (float)Api.World.Rand.NextDouble() * 0.24f, 16f);
			}
			Api.World.Logger.Audit("{0} Put {1}x{2} into new Ground storage at {3}.", player.PlayerName, TransferQuantity, invSlot.Itemstack.Collectible.Code, Pos);
			return true;
		}
		if (invSlot.Itemstack.Equals(Api.World, hotbarSlot.Itemstack, GlobalConstants.IgnoredStackAttributes))
		{
			bool putBulk2 = player.Entity.Controls.CtrlKey;
			int q = GameMath.Min(hotbarSlot.StackSize, putBulk2 ? BulkTransferQuantity : TransferQuantity, Capacity - TotalStackSize);
			int oldSize = invSlot.Itemstack.StackSize;
			invSlot.Itemstack.StackSize += q;
			if (oldSize + q > 0)
			{
				float tempPile = invSlot.Itemstack.Collectible.GetTemperature(Api.World, invSlot.Itemstack);
				float tempAdded = hotbarSlot.Itemstack.Collectible.GetTemperature(Api.World, hotbarSlot.Itemstack);
				invSlot.Itemstack.Collectible.SetTemperature(Api.World, invSlot.Itemstack, (tempPile * (float)oldSize + tempAdded * (float)q) / (float)(oldSize + q), delayCooldown: false);
			}
			if (player.WorldData.CurrentGameMode != EnumGameMode.Creative)
			{
				hotbarSlot.TakeOut(q);
				hotbarSlot.OnItemSlotModified(null);
			}
			Api.World.PlaySoundAt(StorageProps.PlaceRemoveSound.WithPathPrefixOnce("sounds/"), (double)Pos.X + 0.5, Pos.InternalY, (double)Pos.Z + 0.5, null, 0.88f + (float)Api.World.Rand.NextDouble() * 0.24f, 16f);
			Api.World.Logger.Audit("{0} Put {1}x{2} into Ground storage at {3}.", player.PlayerName, q, invSlot.Itemstack.Collectible.Code, Pos);
			MarkDirty();
			Cuboidf[] collBoxes = Api.World.BlockAccessor.GetBlock(Pos).GetCollisionBoxes(Api.World.BlockAccessor, Pos);
			if (collBoxes != null && collBoxes.Length != 0 && CollisionTester.AabbIntersect(collBoxes[0], Pos.X, Pos.Y, Pos.Z, player.Entity.SelectionBox, player.Entity.SidedPos.XYZ))
			{
				player.Entity.SidedPos.Y += (double)collBoxes[0].Y2 - (player.Entity.SidedPos.Y - (double)(int)player.Entity.SidedPos.Y);
			}
			return true;
		}
		return false;
	}

	public bool TryTakeItem(IPlayer player)
	{
		bool takeBulk = player.Entity.Controls.CtrlKey;
		int q = GameMath.Min(takeBulk ? BulkTransferQuantity : TransferQuantity, TotalStackSize);
		if (inventory[0]?.Itemstack != null)
		{
			ItemStack stack = inventory[0].TakeOut(q);
			player.InventoryManager.TryGiveItemstack(stack);
			if (stack.StackSize > 0)
			{
				Api.World.SpawnItemEntity(stack, Pos);
			}
			Api.World.Logger.Audit("{0} Took {1}x{2} from Ground storage at {3}.", player.PlayerName, q, stack.Collectible.Code, Pos);
		}
		if (TotalStackSize == 0)
		{
			Api.World.BlockAccessor.SetBlock(0, Pos);
		}
		Api.World.PlaySoundAt(StorageProps.PlaceRemoveSound, (double)Pos.X + 0.5, Pos.InternalY, (double)Pos.Z + 0.5, null, 0.88f + (float)Api.World.Rand.NextDouble() * 0.24f, 16f);
		MarkDirty();
		(player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
		return true;
	}

	public bool putOrGetItemSingle(ItemSlot ourSlot, IPlayer player, BlockSelection bs)
	{
		isUsingSlot = null;
		if (!ourSlot.Empty && ourSlot.Itemstack.Collectible is IContainedInteractable collIci && collIci.OnContainedInteractStart(this, ourSlot, player, bs))
		{
			BlockGroundStorage.IsUsingContainedBlock = true;
			isUsingSlot = ourSlot;
			return true;
		}
		ItemSlot hotbarSlot = player.InventoryManager.ActiveHotbarSlot;
		if (!hotbarSlot.Empty && !inventory.Empty && StorageProps.Layout != hotbarSlot.Itemstack.Collectible.GetBehavior<CollectibleBehaviorGroundStorable>()?.StorageProps.Layout)
		{
			return false;
		}
		lock (inventoryLock)
		{
			if (ourSlot.Empty)
			{
				if (hotbarSlot.Empty)
				{
					return false;
				}
				if (player.WorldData.CurrentGameMode == EnumGameMode.Creative)
				{
					ItemStack itemStack = hotbarSlot.Itemstack.Clone();
					itemStack.StackSize = 1;
					if (new DummySlot(itemStack).TryPutInto(Api.World, ourSlot, TransferQuantity) > 0)
					{
						Api.World.PlaySoundAt(StorageProps.PlaceRemoveSound, (double)Pos.X + 0.5, Pos.InternalY, (double)Pos.Z + 0.5, player, 0.88f + (float)Api.World.Rand.NextDouble() * 0.24f, 16f);
						Api.World.Logger.Audit("{0} Put 1x{1} into Ground storage at {2}.", player.PlayerName, ourSlot.Itemstack.Collectible.Code, Pos);
					}
				}
				else if (hotbarSlot.TryPutInto(Api.World, ourSlot, TransferQuantity) > 0)
				{
					Api.World.PlaySoundAt(StorageProps.PlaceRemoveSound, (double)Pos.X + 0.5, Pos.InternalY, (double)Pos.Z + 0.5, player, 0.88f + (float)Api.World.Rand.NextDouble() * 0.24f, 16f);
					Api.World.Logger.Audit("{0} Put 1x{1} into Ground storage at {2}.", player.PlayerName, ourSlot.Itemstack.Collectible.Code, Pos);
				}
			}
			else
			{
				if (!player.InventoryManager.TryGiveItemstack(ourSlot.Itemstack, slotNotifyEffect: true))
				{
					Api.World.SpawnItemEntity(ourSlot.Itemstack, Pos);
				}
				Api.World.PlaySoundAt(StorageProps.PlaceRemoveSound, (double)Pos.X + 0.5, Pos.InternalY, (double)Pos.Z + 0.5, player, 0.88f + (float)Api.World.Rand.NextDouble() * 0.24f, 16f);
				Api.World.Logger.Audit("{0} Took 1x{1} from Ground storage at {2}.", player.PlayerName, ourSlot.Itemstack?.Collectible.Code, Pos);
				ourSlot.Itemstack = null;
				ourSlot.MarkDirty();
			}
		}
		return true;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		clientsideFirstPlacement = false;
		forceStorageProps = tree.GetBool("forceStorageProps");
		if (forceStorageProps)
		{
			StorageProps = JsonUtil.FromString<GroundStorageProperties>(tree.GetString("storageProps"));
		}
		overrideLayout = null;
		if (tree.HasAttribute("overrideLayout"))
		{
			overrideLayout = (EnumGroundStorageLayout)tree.GetInt("overrideLayout");
		}
		if (Api != null)
		{
			DetermineStorageProperties(null);
		}
		MeshAngle = tree.GetFloat("meshAngle");
		AttachFace = BlockFacing.ALLFACES[tree.GetInt("attachFace")];
		bool wasBurning = burning;
		burning = tree.GetBool("burning");
		burnStartTotalHours = tree.GetDouble("lastTickTotalHours");
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Client && !wasBurning && burning)
		{
			UpdateBurningState();
		}
		if (!burning)
		{
			if (listenerId != 0L)
			{
				UnregisterGameTickListener(listenerId);
				listenerId = 0L;
			}
			ambientSound?.Stop();
			listenerId = 0L;
		}
		RedrawAfterReceivingTreeAttributes(worldForResolving);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("forceStorageProps", forceStorageProps);
		if (forceStorageProps)
		{
			tree.SetString("storageProps", JsonUtil.ToString(StorageProps));
		}
		if (overrideLayout.HasValue)
		{
			tree.SetInt("overrideLayout", (int)overrideLayout.Value);
		}
		tree.SetBool("burning", burning);
		tree.SetDouble("lastTickTotalHours", burnStartTotalHours);
		tree.SetFloat("meshAngle", MeshAngle);
		tree.SetInt("attachFace", AttachFace?.Index ?? 0);
	}

	public override void OnBlockBroken(IPlayer byPlayer = null)
	{
	}

	public virtual string GetBlockName()
	{
		if (StorageProps == null || inventory.Empty)
		{
			return Lang.Get("Empty pile");
		}
		string[] contentSummary = getContentSummary();
		if (contentSummary.Length == 1)
		{
			ItemSlot firstSlot = inventory.FirstNonEmptySlot;
			ItemStack stack = firstSlot.Itemstack;
			int sumQ = inventory.Sum((ItemSlot s) => s.StackSize);
			if (firstSlot.Itemstack.Collectible is IContainedCustomName ccn)
			{
				string name = ccn.GetContainedName(firstSlot, sumQ);
				if (name != null)
				{
					return name;
				}
			}
			if (sumQ == 1)
			{
				return stack.GetName();
			}
			return contentSummary[0];
		}
		return Lang.Get("Ground Storage");
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		if (inventory.Empty)
		{
			return;
		}
		string[] contentSummary = getContentSummary();
		ItemStack stack = inventory.FirstNonEmptySlot.Itemstack;
		if (contentSummary.Length == 1 && !(stack.Collectible is IContainedCustomName) && stack.Class == EnumItemClass.Block && ((Block)stack.Collectible).EntityClass == null)
		{
			string detailedInfo = stack.Block.GetPlacedBlockInfo(Api.World, Pos, forPlayer);
			if (detailedInfo != null && detailedInfo.Length > 0)
			{
				dsc.Append(detailedInfo);
			}
		}
		else
		{
			string[] array = contentSummary;
			foreach (string line in array)
			{
				dsc.AppendLine(line);
			}
		}
		if (inventory.Empty)
		{
			return;
		}
		foreach (ItemSlot slot in inventory)
		{
			float temperature = slot.Itemstack?.Collectible.GetTemperature(Api.World, slot.Itemstack) ?? 0f;
			if (temperature > 20f)
			{
				float f = slot.Itemstack?.Attributes.GetFloat("hoursHeatReceived") ?? 0f;
				dsc.AppendLine(Lang.Get("temperature-precise", temperature));
				if (f > 0f)
				{
					dsc.AppendLine(Lang.Get("Fired for {0:0.##} hours", f));
				}
			}
		}
	}

	public virtual string[] getContentSummary()
	{
		OrderedDictionary<string, int> dict = new OrderedDictionary<string, int>();
		foreach (ItemSlot slot in inventory)
		{
			if (!slot.Empty)
			{
				string stackName = slot.Itemstack.GetName();
				if (slot.Itemstack.Collectible is IContainedCustomName ccn)
				{
					stackName = ccn.GetContainedInfo(slot);
				}
				if (!dict.TryGetValue(stackName, out var cnt))
				{
					cnt = 0;
				}
				dict[stackName] = cnt + slot.StackSize;
			}
		}
		return dict.Select((KeyValuePair<string, int> elem) => Lang.Get("{0}x {1}", elem.Value, elem.Key)).ToArray();
	}

	public override bool OnTesselation(ITerrainMeshPool meshdata, ITesselatorAPI tesselator)
	{
		float temp = 0f;
		if (!Inventory.Empty)
		{
			foreach (ItemSlot slot in Inventory)
			{
				temp = Math.Max(temp, slot.Itemstack?.Collectible.GetTemperature(capi.World, slot.Itemstack) ?? 0f);
			}
		}
		UseRenderer = temp >= 500f;
		if (renderer == null && temp >= 450f)
		{
			capi.Event.EnqueueMainThreadTask(delegate
			{
				if (renderer == null)
				{
					renderer = new GroundStorageRenderer(capi, this);
				}
			}, "groundStorageRendererE");
		}
		if (UseRenderer)
		{
			return true;
		}
		if (renderer != null && temp < 450f)
		{
			capi.Event.EnqueueMainThreadTask(delegate
			{
				if (renderer != null)
				{
					renderer.Dispose();
					renderer = null;
				}
			}, "groundStorageRendererD");
		}
		NeedsRetesselation = false;
		lock (inventoryLock)
		{
			return base.OnTesselation(meshdata, tesselator);
		}
	}

	private Vec3f rotatedOffset(Vec3f offset, float radY)
	{
		Matrixf matrixf = new Matrixf();
		matrixf.Translate(0.5f, 0.5f, 0.5f).RotateY(radY).Translate(-0.5f, -0.5f, -0.5f);
		return matrixf.TransformVector(new Vec4f(offset.X, offset.Y, offset.Z, 1f)).XYZ;
	}

	protected override float[][] genTransformationMatrices()
	{
		float[][] tfMatrices = new float[DisplayedItems][];
		Vec3f[] offs = new Vec3f[DisplayedItems];
		lock (inventoryLock)
		{
			GetLayoutOffset(offs);
		}
		for (int i = 0; i < tfMatrices.Length; i++)
		{
			Vec3f off = offs[i];
			off = new Matrixf().RotateY(MeshAngle).TransformVector(off.ToVec4f(0f)).XYZ;
			tfMatrices[i] = new Matrixf().Translate(off.X, off.Y, off.Z).Translate(0.5f, 0f, 0.5f).RotateY(MeshAngle)
				.Translate(-0.5f, 0f, -0.5f)
				.Values;
		}
		return tfMatrices;
	}

	public void GetLayoutOffset(Vec3f[] offs)
	{
		switch (StorageProps.Layout)
		{
		case EnumGroundStorageLayout.SingleCenter:
			offs[0] = new Vec3f();
			break;
		case EnumGroundStorageLayout.Halves:
		case EnumGroundStorageLayout.WallHalves:
			offs[0] = new Vec3f(-0.25f, 0f, 0f);
			offs[1] = new Vec3f(0.25f, 0f, 0f);
			break;
		case EnumGroundStorageLayout.Quadrants:
			offs[0] = new Vec3f(-0.25f, 0f, -0.25f);
			offs[1] = new Vec3f(-0.25f, 0f, 0.25f);
			offs[2] = new Vec3f(0.25f, 0f, -0.25f);
			offs[3] = new Vec3f(0.25f, 0f, 0.25f);
			break;
		case EnumGroundStorageLayout.Stacking:
			offs[0] = new Vec3f();
			break;
		}
	}

	protected override string getMeshCacheKey(ItemStack stack)
	{
		return ((!(StorageProps.ModelItemsToStackSizeRatio > 0f)) ? 1 : stack.StackSize) + "x" + base.getMeshCacheKey(stack);
	}

	protected override MeshData getOrCreateMesh(ItemStack stack, int index)
	{
		if (stack.Class == EnumItemClass.Block)
		{
			MeshRefs[index] = capi.TesselatorManager.GetDefaultBlockMeshRef(stack.Block);
		}
		else if (stack.Class == EnumItemClass.Item && StorageProps.Layout != EnumGroundStorageLayout.Stacking)
		{
			MeshRefs[index] = capi.TesselatorManager.GetDefaultItemMeshRef(stack.Item);
		}
		if (StorageProps.Layout == EnumGroundStorageLayout.Stacking)
		{
			string key = getMeshCacheKey(stack);
			MeshData mesh = getMesh(stack);
			if (mesh != null)
			{
				UploadedMeshCache.TryGetValue(key, out MeshRefs[index]);
				return mesh;
			}
			AssetLocation loc = StorageProps.StackingModel.Clone().WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json");
			nowTesselatingShape = Shape.TryGet(capi, loc);
			nowTesselatingObj = stack.Collectible;
			if (nowTesselatingShape == null)
			{
				capi.Logger.Error(string.Concat("Stacking model shape for collectible ", stack.Collectible.Code, " not found. Block will be invisible!"));
				return null;
			}
			capi.Tesselator.TesselateShape("storagePile", nowTesselatingShape, out mesh, this, null, 0, 0, 0, (int)Math.Ceiling(StorageProps.ModelItemsToStackSizeRatio * (float)stack.StackSize));
			base.MeshCache[key] = mesh;
			if (UploadedMeshCache.TryGetValue(key, out var mr))
			{
				mr.Dispose();
			}
			UploadedMeshCache[key] = capi.Render.UploadMultiTextureMesh(mesh);
			MeshRefs[index] = UploadedMeshCache[key];
			return mesh;
		}
		MeshData orCreateMesh = base.getOrCreateMesh(stack, index);
		JsonObject attributes = stack.Collectible.Attributes;
		if (attributes != null && attributes[AttributeTransformCode].Exists)
		{
			ModelTransform transform = stack.Collectible.Attributes?[AttributeTransformCode].AsObject<ModelTransform>();
			ModelTransformsRenderer[index] = transform;
			return orCreateMesh;
		}
		ModelTransformsRenderer[index] = null;
		return orCreateMesh;
	}

	public void TryIgnite()
	{
		if (!burning && CanIgnite)
		{
			burning = true;
			burnStartTotalHours = Api.World.Calendar.TotalHours;
			MarkDirty();
			UpdateBurningState();
		}
	}

	public void Extinguish()
	{
		if (burning)
		{
			burning = false;
			UnregisterGameTickListener(listenerId);
			listenerId = 0L;
			MarkDirty(redrawOnClient: true);
			Api.World.PlaySoundAt(new AssetLocation("sounds/effect/extinguish"), Pos, 0.0, null, randomizePitch: false, 16f);
		}
	}

	public float GetHoursLeft(double startTotalHours)
	{
		double totalHoursPassed = startTotalHours - burnStartTotalHours;
		return (float)((double)((float)inventory[0].StackSize / 2f * burnHoursPerItem) - totalHoursPassed);
	}

	private void UpdateBurningState()
	{
		if (!burning)
		{
			return;
		}
		if (Api.World.Side == EnumAppSide.Client)
		{
			if (ambientSound == null || !ambientSound.IsPlaying)
			{
				ambientSound = ((IClientWorldAccessor)Api.World).LoadSound(new SoundParams
				{
					Location = new AssetLocation("sounds/held/torch-idle.ogg"),
					ShouldLoop = true,
					Position = Pos.ToVec3f().Add(0.5f, 0.25f, 0.5f),
					DisposeOnFinish = false,
					Volume = 1f
				});
				if (ambientSound != null)
				{
					ambientSound.PlaybackPosition = ambientSound.SoundLengthSeconds * (float)Api.World.Rand.NextDouble();
					ambientSound.Start();
				}
			}
			listenerId = RegisterGameTickListener(OnBurningTickClient, 100);
		}
		else
		{
			listenerId = RegisterGameTickListener(OnBurningTickServer, 10000);
		}
	}

	private void OnBurningTickClient(float dt)
	{
		if (!burning || !(Api.World.Rand.NextDouble() < 0.93))
		{
			return;
		}
		float yOffset = (float)Layers / ((float)StorageProps.StackingCapacity * StorageProps.ModelItemsToStackSizeRatio);
		Vec3d pos = Pos.ToVec3d().Add(0.0, yOffset, 0.0);
		Random rnd = Api.World.Rand;
		for (int i = 0; i < Entity.FireParticleProps.Length; i++)
		{
			AdvancedParticleProperties particles = Entity.FireParticleProps[i];
			particles.Velocity[1].avg = (float)(rnd.NextDouble() - 0.5);
			particles.basePos.Set(pos.X + 0.5, pos.Y, pos.Z + 0.5);
			if (i == 0)
			{
				particles.Quantity.avg = 1f;
			}
			if (i == 1)
			{
				particles.Quantity.avg = 2f;
				particles.PosOffset[0].var = 0.39f;
				particles.PosOffset[1].var = 0.39f;
				particles.PosOffset[2].var = 0.39f;
			}
			Api.World.SpawnParticles(particles);
		}
	}

	private void OnBurningTickServer(float dt)
	{
		facings.Shuffle(Api.World.Rand);
		BEBehaviorBurning bh = GetBehavior<BEBehaviorBurning>();
		BlockFacing[] array = facings;
		foreach (BlockFacing val in array)
		{
			BlockPos blockPos = Pos.AddCopy(val);
			BlockEntity blockEntity = Api.World.BlockAccessor.GetBlockEntity(blockPos);
			if (blockEntity is BlockEntityCoalPile becp)
			{
				becp.TryIgnite();
				if (Api.World.Rand.NextDouble() < 0.75)
				{
					break;
				}
			}
			else if (blockEntity is BlockEntityGroundStorage besg)
			{
				besg.TryIgnite();
				if (Api.World.Rand.NextDouble() < 0.75)
				{
					break;
				}
			}
			else if (((ICoreServerAPI)Api).Server.Config.AllowFireSpread && 0.5 > Api.World.Rand.NextDouble() && bh.TrySpreadTo(blockPos) && Api.World.Rand.NextDouble() < 0.75)
			{
				break;
			}
		}
		bool changed = false;
		while (Api.World.Calendar.TotalHours - burnStartTotalHours > (double)burnHoursPerItem)
		{
			burnStartTotalHours += burnHoursPerItem;
			inventory[0].TakeOut(1);
			if (inventory[0].Empty)
			{
				Api.World.BlockAccessor.SetBlock(0, Pos);
				break;
			}
			changed = true;
		}
		if (changed)
		{
			MarkDirty(redrawOnClient: true);
		}
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
	{
		MeshAngle = tree.GetFloat("meshAngle");
		MeshAngle -= (float)degreeRotation * ((float)Math.PI / 180f);
		tree.SetFloat("meshAngle", MeshAngle);
		AttachFace = BlockFacing.ALLFACES[tree.GetInt("attachFace")];
		AttachFace = AttachFace.FaceWhenRotatedBy(0f, (float)(-degreeRotation) * ((float)Math.PI / 180f), 0f);
		tree.SetInt("attachFace", AttachFace?.Index ?? 0);
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		renderer?.Dispose();
		ambientSound?.Stop();
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		renderer?.Dispose();
		ambientSound?.Stop();
	}

	public float GetHeatStrength(IWorldAccessor world, BlockPos heatSourcePos, BlockPos heatReceiverPos)
	{
		return IsBurning ? 10 : 0;
	}
}
