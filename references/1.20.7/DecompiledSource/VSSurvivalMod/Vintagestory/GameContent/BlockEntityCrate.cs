using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityCrate : BlockEntityContainer, IRotatable
{
	private InventoryGeneric inventory;

	private BlockCrate ownBlock;

	public string type = "wood-aged";

	public string label;

	public string preferredLidState = "closed";

	public int quantitySlots = 16;

	public bool retrieveOnly;

	private float rotAngleY;

	private MeshData ownMesh;

	private MeshData labelMesh;

	private ICoreClientAPI capi;

	private Cuboidf selBoxCrate;

	private Cuboidf selBoxLabel;

	private int labelColor;

	private ItemStack labelStack;

	private ModSystemLabelMeshCache labelCacheSys;

	private bool requested;

	private static Vec3f origin = new Vec3f(0.5f, 0f, 0.5f);

	public virtual float MeshAngle
	{
		get
		{
			return rotAngleY;
		}
		set
		{
			rotAngleY = value;
		}
	}

	public override InventoryBase Inventory => inventory;

	public override string InventoryClassName => "crate";

	public LabelProps LabelProps
	{
		get
		{
			if (label == null)
			{
				return null;
			}
			ownBlock.Props.Labels.TryGetValue(label, out var prop);
			return prop;
		}
	}

	public string LidState
	{
		get
		{
			if (preferredLidState == "closed")
			{
				return preferredLidState;
			}
			if (inventory.Empty)
			{
				return preferredLidState;
			}
			ItemStack stack = inventory.FirstNonEmptySlot.Itemstack;
			if (stack?.Collectible == null || (stack.ItemAttributes != null && stack.ItemAttributes["inContainerTexture"].Exists))
			{
				return preferredLidState;
			}
			JsonObject itemAttributes = stack.ItemAttributes;
			bool? displayInsideCrate = ((itemAttributes == null || !itemAttributes["displayInsideCrate"].Exists) ? null : stack.ItemAttributes?["displayInsideCrate"].AsBool(defaultValue: true));
			if ((stack.Block == null || stack.Block.DrawType != EnumDrawType.Cube || displayInsideCrate == false) && !displayInsideCrate.GetValueOrDefault())
			{
				return "closed";
			}
			return preferredLidState;
		}
	}

	private float rndScale => 1f + (float)(GameMath.MurmurHash3Mod(Pos.X, Pos.Y, Pos.Z, 100) - 50) / 1000f;

	public override void Initialize(ICoreAPI api)
	{
		ownBlock = base.Block as BlockCrate;
		capi = api as ICoreClientAPI;
		bool isNewlyplaced = inventory == null;
		if (isNewlyplaced)
		{
			InitInventory(base.Block, api);
		}
		base.Initialize(api);
		if (api.Side == EnumAppSide.Client && !isNewlyplaced)
		{
			labelCacheSys = api.ModLoader.GetModSystem<ModSystemLabelMeshCache>();
			loadOrCreateMesh();
		}
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		if (byItemStack?.Attributes != null)
		{
			string nowType = byItemStack.Attributes.GetString("type", ownBlock.Props.DefaultType);
			string nowLabel = byItemStack.Attributes.GetString("label");
			string nowLidState = byItemStack.Attributes.GetString("lidState", "closed");
			if (nowType != type || nowLabel != label || nowLidState != preferredLidState)
			{
				label = nowLabel;
				type = nowType;
				preferredLidState = nowLidState;
				InitInventory(base.Block, Api);
				Inventory.LateInitialize(InventoryClassName + "-" + Pos, Api);
				Inventory.ResolveBlocksOrItems();
				container.LateInit();
				MarkDirty();
			}
		}
		base.OnBlockPlaced((ItemStack)null);
	}

	public bool OnBlockInteractStart(IPlayer byPlayer, BlockSelection blockSel)
	{
		bool put = byPlayer.Entity.Controls.ShiftKey;
		bool take = !put;
		bool bulk = byPlayer.Entity.Controls.CtrlKey;
		ItemSlot ownSlot = inventory.FirstNonEmptySlot;
		ItemSlot hotbarslot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (put && hotbarslot != null && (hotbarslot.Itemstack?.ItemAttributes?["pigment"]?["color"].Exists).GetValueOrDefault() && blockSel.SelectionBoxIndex == 1)
		{
			if (!inventory.Empty)
			{
				JsonObject jsonObject = hotbarslot.Itemstack.ItemAttributes["pigment"]["color"];
				int r = jsonObject["red"].AsInt();
				int g = jsonObject["green"].AsInt();
				int b = jsonObject["blue"].AsInt();
				FreeAtlasSpace();
				labelColor = ColorUtil.ToRgba(255, (int)GameMath.Clamp((float)r * 1.2f, 0f, 255f), (int)GameMath.Clamp((float)g * 1.2f, 0f, 255f), (int)GameMath.Clamp((float)b * 1.2f, 0f, 255f));
				labelStack = inventory.FirstNonEmptySlot.Itemstack.Clone();
				labelMesh = null;
				byPlayer.Entity.World.PlaySoundAt(new AssetLocation("sounds/player/chalkdraw"), (double)blockSel.Position.X + blockSel.HitPosition.X, (double)blockSel.Position.InternalY + blockSel.HitPosition.Y, (double)blockSel.Position.Z + blockSel.HitPosition.Z, byPlayer, randomizePitch: true, 8f);
				MarkDirty(redrawOnClient: true);
			}
			else
			{
				(Api as ICoreClientAPI)?.TriggerIngameError(this, "empty", Lang.Get("Can't draw item symbol on an empty crate. Put something inside the crate first"));
			}
			return true;
		}
		if (take && ownSlot != null)
		{
			ItemStack stack = (bulk ? ownSlot.TakeOutWhole() : ownSlot.TakeOut(1));
			int quantity2 = ((!bulk) ? 1 : stack.StackSize);
			if (!byPlayer.InventoryManager.TryGiveItemstack(stack, slotNotifyEffect: true))
			{
				Api.World.SpawnItemEntity(stack, Pos.ToVec3d().Add(0.5f + blockSel.Face.Normalf.X, 0.5f + blockSel.Face.Normalf.Y, 0.5f + blockSel.Face.Normalf.Z));
			}
			else
			{
				didMoveItems(stack, byPlayer);
			}
			Api.World.Logger.Audit("{0} Took {1}x{2} from Crate at {3}.", byPlayer.PlayerName, quantity2, stack?.Collectible.Code, Pos);
			if (inventory.Empty)
			{
				FreeAtlasSpace();
				labelStack = null;
				labelMesh = null;
			}
			ownSlot.MarkDirty();
			MarkDirty();
		}
		if (put && !hotbarslot.Empty)
		{
			int quantity = ((!bulk) ? 1 : hotbarslot.StackSize);
			if (ownSlot == null)
			{
				if (hotbarslot.TryPutInto(Api.World, inventory[0], quantity) > 0)
				{
					didMoveItems(inventory[0].Itemstack, byPlayer);
					Api.World.Logger.Audit("{0} Put {1}x{2} into Crate at {3}.", byPlayer.PlayerName, quantity, inventory[0].Itemstack?.Collectible.Code, Pos);
				}
			}
			else if (hotbarslot.Itemstack.Equals(Api.World, ownSlot.Itemstack, GlobalConstants.IgnoredStackAttributes))
			{
				List<ItemSlot> skipSlots = new List<ItemSlot>();
				while (hotbarslot.StackSize > 0 && skipSlots.Count < inventory.Count)
				{
					WeightedSlot wslot = inventory.GetBestSuitedSlot(hotbarslot, null, skipSlots);
					if (wslot.slot == null)
					{
						break;
					}
					if (hotbarslot.TryPutInto(Api.World, wslot.slot, quantity) > 0)
					{
						didMoveItems(wslot.slot.Itemstack, byPlayer);
						Api.World.Logger.Audit("{0} Put {1}x{2} into Crate at {3}.", byPlayer.PlayerName, quantity, wslot.slot.Itemstack?.Collectible.Code, Pos);
						if (!bulk)
						{
							break;
						}
					}
					skipSlots.Add(wslot.slot);
				}
			}
			hotbarslot.MarkDirty();
			MarkDirty();
		}
		return true;
	}

	protected void didMoveItems(ItemStack stack, IPlayer byPlayer)
	{
		if (Api.Side == EnumAppSide.Client)
		{
			loadOrCreateMesh();
		}
		capi?.World.Player.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
		AssetLocation sound = stack?.Block?.Sounds?.Place;
		Api.World.PlaySoundAt((sound != null) ? sound : new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
	}

	protected virtual void InitInventory(Block block, ICoreAPI api)
	{
		if (block?.Attributes != null)
		{
			JsonObject props = block.Attributes["properties"][type];
			if (!props.Exists)
			{
				props = block.Attributes["properties"]["*"];
			}
			quantitySlots = props["quantitySlots"].AsInt(quantitySlots);
			retrieveOnly = props["retrieveOnly"].AsBool();
		}
		inventory = new InventoryGeneric(quantitySlots, null, null);
		inventory.BaseWeight = 1f;
		inventory.OnGetSuitability = (ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge) => (isMerge ? (inventory.BaseWeight + 3f) : (inventory.BaseWeight + 1f)) + (float)((sourceSlot.Inventory is InventoryBasePlayer) ? 1 : 0);
		inventory.OnGetAutoPullFromSlot = GetAutoPullFromSlot;
		inventory.OnGetAutoPushIntoSlot = GetAutoPushIntoSlot;
		if (block?.Attributes != null)
		{
			if (block.Attributes["spoilSpeedMulByFoodCat"][type].Exists)
			{
				inventory.PerishableFactorByFoodCategory = block.Attributes["spoilSpeedMulByFoodCat"][type].AsObject<Dictionary<EnumFoodCategory, float>>();
			}
			if (block.Attributes["transitionSpeedMul"][type].Exists)
			{
				inventory.TransitionableSpeedMulByType = block.Attributes["transitionSpeedMul"][type].AsObject<Dictionary<EnumTransitionType, float>>();
			}
		}
		inventory.PutLocked = retrieveOnly;
		inventory.OnInventoryClosed += OnInvClosed;
		inventory.OnInventoryOpened += OnInvOpened;
		if (api.Side == EnumAppSide.Server)
		{
			inventory.SlotModified += Inventory_SlotModified;
		}
		container.Reset();
	}

	private void Inventory_SlotModified(int obj)
	{
		MarkDirty();
	}

	public Cuboidf[] GetSelectionBoxes()
	{
		if (selBoxCrate == null)
		{
			selBoxCrate = ownBlock.SelectionBoxes[0].RotatedCopy(0f, (int)Math.Round(rotAngleY * (180f / (float)Math.PI) / 90f) * 90, 0f, new Vec3d(0.5, 0.0, 0.5));
			selBoxLabel = ownBlock.SelectionBoxes[1].RotatedCopy(0f, rotAngleY * (180f / (float)Math.PI), 0f, new Vec3d(0.5, 0.0, 0.5));
		}
		if (Api.Side == EnumAppSide.Client)
		{
			ItemSlot activeHotbarSlot = (Api as ICoreClientAPI).World.Player.InventoryManager.ActiveHotbarSlot;
			if (activeHotbarSlot != null && (activeHotbarSlot.Itemstack?.ItemAttributes?["pigment"]?["color"].Exists).GetValueOrDefault())
			{
				return new Cuboidf[2] { selBoxCrate, selBoxLabel };
			}
		}
		return new Cuboidf[1] { selBoxCrate };
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		BlockCrate block = worldForResolving.GetBlock(new AssetLocation(tree.GetString("blockCode"))) as BlockCrate;
		type = tree.GetString("type", block?.Props.DefaultType);
		label = tree.GetString("label");
		MeshAngle = tree.GetFloat("meshAngle", MeshAngle);
		labelColor = tree.GetInt("labelColor");
		labelStack = tree.GetItemstack("labelStack");
		preferredLidState = tree.GetString("lidState");
		if (labelStack != null && !labelStack.ResolveBlockOrItem(worldForResolving))
		{
			labelStack = null;
		}
		if (inventory == null)
		{
			if (tree.HasAttribute("blockCode"))
			{
				InitInventory(block, worldForResolving.Api);
			}
			else
			{
				InitInventory(null, worldForResolving.Api);
			}
		}
		if (Api != null && Api.Side == EnumAppSide.Client)
		{
			loadOrCreateMesh();
			MarkDirty(redrawOnClient: true);
		}
		base.FromTreeAttributes(tree, worldForResolving);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		if (base.Block != null)
		{
			tree.SetString("forBlockCode", base.Block.Code.ToShortString());
		}
		if (type == null)
		{
			type = ownBlock.Props.DefaultType;
		}
		tree.SetString("label", label);
		tree.SetString("type", type);
		tree.SetFloat("meshAngle", MeshAngle);
		tree.SetInt("labelColor", labelColor);
		tree.SetString("lidState", preferredLidState);
		tree.SetItemstack("labelStack", labelStack);
	}

	public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
	{
		base.OnLoadCollectibleMappings(worldForResolve, oldBlockIdMapping, oldItemIdMapping, schematicSeed, resolveImports);
		if (labelStack != null && !labelStack.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
		{
			labelStack = null;
		}
	}

	public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
	{
		base.OnStoreCollectibleMappings(blockIdMapping, itemIdMapping);
		labelStack?.Collectible.OnStoreCollectibleMappings(Api.World, new DummySlot(labelStack), blockIdMapping, itemIdMapping);
	}

	private ItemSlot GetAutoPullFromSlot(BlockFacing atBlockFace)
	{
		if (atBlockFace == BlockFacing.DOWN)
		{
			return inventory.FirstNonEmptySlot;
		}
		return null;
	}

	private ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
	{
		ItemSlot slotNonEmpty = inventory.FirstNonEmptySlot;
		if (slotNonEmpty == null)
		{
			return inventory[0];
		}
		if (slotNonEmpty.Itemstack.Equals(Api.World, fromSlot.Itemstack, GlobalConstants.IgnoredStackAttributes))
		{
			foreach (ItemSlot slot in inventory)
			{
				if (slot.Itemstack == null || slot.StackSize < slot.Itemstack.Collectible.MaxStackSize)
				{
					return slot;
				}
			}
			return null;
		}
		return null;
	}

	protected virtual void OnInvOpened(IPlayer player)
	{
		inventory.PutLocked = retrieveOnly && player.WorldData.CurrentGameMode != EnumGameMode.Creative;
	}

	protected virtual void OnInvClosed(IPlayer player)
	{
		inventory.PutLocked = retrieveOnly;
	}

	private void loadOrCreateMesh()
	{
		BlockCrate block = base.Block as BlockCrate;
		if (base.Block == null)
		{
			block = (BlockCrate)(base.Block = Api.World.BlockAccessor.GetBlock(Pos) as BlockCrate);
		}
		if (block == null)
		{
			return;
		}
		string cacheKey = "crateMeshes" + block.FirstCodePart();
		Dictionary<string, MeshData> meshes = ObjectCacheUtil.GetOrCreate(Api, cacheKey, () => new Dictionary<string, MeshData>());
		CompositeShape cshape = ownBlock.Props[type].Shape;
		if (!(cshape?.Base == null))
		{
			ItemStack firstStack = inventory.FirstNonEmptySlot?.Itemstack;
			string meshKey = type + block.Subtype + "-" + label + "-" + LidState + "-" + ((LidState == "closed") ? null : (firstStack?.StackSize + "-" + firstStack?.GetHashCode()));
			if (!meshes.TryGetValue(meshKey, out var mesh))
			{
				mesh = (meshes[meshKey] = block.GenMesh(Api as ICoreClientAPI, firstStack, type, label, LidState, cshape, new Vec3f(cshape.rotateX, cshape.rotateY, cshape.rotateZ)));
			}
			ownMesh = mesh.Clone().Rotate(origin, 0f, MeshAngle, 0f).Scale(origin, rndScale, rndScale, rndScale);
		}
	}

	private void genLabelMesh()
	{
		if (LabelProps?.EditableShape != null && labelStack != null && !requested)
		{
			if (labelCacheSys == null)
			{
				labelCacheSys = Api.ModLoader.GetModSystem<ModSystemLabelMeshCache>();
			}
			requested = true;
			labelCacheSys.RequestLabelTexture(labelColor, Pos, labelStack, delegate(int texSubId)
			{
				GenLabelMeshWithItemStack(texSubId);
				MarkDirty(redrawOnClient: true);
				requested = false;
			});
		}
	}

	private void GenLabelMeshWithItemStack(int textureSubId)
	{
		TextureAtlasPosition texPos = capi.BlockTextureAtlas.Positions[textureSubId];
		labelMesh = ownBlock.GenLabelMesh(capi, label, texPos, editableVariant: true);
		labelMesh.Rotate(origin, 0f, rotAngleY + (float)Math.PI, 0f).Scale(origin, rndScale, rndScale, rndScale);
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		if (base.OnTesselation(mesher, tesselator))
		{
			return true;
		}
		if (ownMesh == null)
		{
			return true;
		}
		if (labelMesh == null)
		{
			genLabelMesh();
		}
		mesher.AddMeshData(ownMesh);
		mesher.AddMeshData(labelMesh);
		return true;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		int stacksize = 0;
		foreach (ItemSlot slot in inventory)
		{
			stacksize += slot.StackSize;
		}
		if (stacksize > 0)
		{
			dsc.AppendLine(Lang.Get("Contents: {0}x{1}", stacksize, inventory.FirstNonEmptySlot.GetStackName()));
		}
		else
		{
			dsc.AppendLine(Lang.Get("Empty"));
		}
		base.GetBlockInfo(forPlayer, dsc);
	}

	public override void OnBlockUnloaded()
	{
		FreeAtlasSpace();
		base.OnBlockUnloaded();
	}

	public override void OnBlockRemoved()
	{
		FreeAtlasSpace();
		base.OnBlockRemoved();
	}

	private void FreeAtlasSpace()
	{
		if (labelStack != null)
		{
			labelCacheSys?.FreeLabelTexture(labelStack, labelColor, Pos);
		}
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
	{
		ownMesh = null;
		MeshAngle = tree.GetFloat("meshAngle");
		MeshAngle -= (float)degreeRotation * ((float)Math.PI / 180f);
		tree.SetFloat("meshAngle", MeshAngle);
	}
}
