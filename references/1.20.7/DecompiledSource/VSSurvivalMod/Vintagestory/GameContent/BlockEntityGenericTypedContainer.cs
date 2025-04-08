using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockEntityGenericTypedContainer : BlockEntityOpenableContainer, IRotatable
{
	internal InventoryGeneric inventory;

	public string type = "normal-generic";

	public string defaultType;

	public int quantitySlots = 16;

	public int quantityColumns = 4;

	public string inventoryClassName = "chest";

	public string dialogTitleLangCode = "chestcontents";

	public bool retrieveOnly;

	private float meshangle;

	private MeshData ownMesh;

	public Cuboidf[] collisionSelectionBoxes;

	private Vec3f rendererRot = new Vec3f();

	public virtual float MeshAngle
	{
		get
		{
			return meshangle;
		}
		set
		{
			meshangle = value;
			rendererRot.Y = value * (180f / (float)Math.PI);
		}
	}

	public virtual string DialogTitle => Lang.Get(dialogTitleLangCode);

	public override InventoryBase Inventory => inventory;

	public override string InventoryClassName => inventoryClassName;

	private BlockEntityAnimationUtil animUtil => GetBehavior<BEBehaviorAnimatable>()?.animUtil;

	public override void Initialize(ICoreAPI api)
	{
		defaultType = base.Block.Attributes?["defaultType"]?.AsString("normal-generic");
		if (defaultType == null)
		{
			defaultType = "normal-generic";
		}
		if (inventory == null)
		{
			InitInventory(base.Block);
		}
		base.Initialize(api);
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		if (byItemStack?.Attributes != null)
		{
			string nowType = byItemStack.Attributes.GetString("type", defaultType);
			if (nowType != type)
			{
				type = nowType;
				InitInventory(base.Block);
				LateInitInventory();
			}
		}
		base.OnBlockPlaced((ItemStack)null);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		string prevType = type;
		type = tree.GetString("type", defaultType);
		MeshAngle = tree.GetFloat("meshAngle", MeshAngle);
		if (inventory == null)
		{
			if (tree.HasAttribute("forBlockId"))
			{
				InitInventory(worldForResolving.GetBlock((ushort)tree.GetInt("forBlockId")));
			}
			else if (tree.HasAttribute("forBlockCode"))
			{
				InitInventory(worldForResolving.GetBlock(new AssetLocation(tree.GetString("forBlockCode"))));
			}
			else
			{
				if (tree.GetTreeAttribute("inventory").GetInt("qslots") == 8)
				{
					quantitySlots = 8;
					inventoryClassName = "basket";
					dialogTitleLangCode = "basketcontents";
					if (type == null)
					{
						type = "reed";
					}
				}
				InitInventory(null);
			}
		}
		else if (type != prevType)
		{
			InitInventory(base.Block);
			if (Api == null)
			{
				Api = worldForResolving.Api;
			}
			LateInitInventory();
		}
		if (Api != null && Api.Side == EnumAppSide.Client)
		{
			ownMesh = null;
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
			type = defaultType;
		}
		tree.SetString("type", type);
		tree.SetFloat("meshAngle", MeshAngle);
	}

	protected virtual void InitInventory(Block block)
	{
		if (block?.Attributes != null)
		{
			collisionSelectionBoxes = block.Attributes["collisionSelectionBoxes"]?[type]?.AsObject<Cuboidf[]>();
			inventoryClassName = block.Attributes["inventoryClassName"].AsString(inventoryClassName);
			dialogTitleLangCode = block.Attributes["dialogTitleLangCode"][type].AsString(dialogTitleLangCode);
			quantitySlots = block.Attributes["quantitySlots"][type].AsInt(quantitySlots);
			quantityColumns = block.Attributes["quantityColumns"][type].AsInt(4);
			retrieveOnly = block.Attributes["retrieveOnly"][type].AsBool();
			if (block.Attributes["typedOpenSound"][type].Exists)
			{
				OpenSound = AssetLocation.Create(block.Attributes["typedOpenSound"][type].AsString(OpenSound.ToShortString()), block.Code.Domain);
			}
			if (block.Attributes["typedCloseSound"][type].Exists)
			{
				CloseSound = AssetLocation.Create(block.Attributes["typedCloseSound"][type].AsString(CloseSound.ToShortString()), block.Code.Domain);
			}
		}
		inventory = new InventoryGeneric(quantitySlots, null, null);
		inventory.BaseWeight = 1f;
		inventory.OnGetSuitability = (ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge) => (isMerge ? (inventory.BaseWeight + 3f) : (inventory.BaseWeight + 1f)) + (float)((sourceSlot.Inventory is InventoryBasePlayer) ? 1 : 0);
		inventory.OnGetAutoPullFromSlot = GetAutoPullFromSlot;
		container.Reset();
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
	}

	public virtual void LateInitInventory()
	{
		Inventory.LateInitialize(InventoryClassName + "-" + Pos, Api);
		Inventory.ResolveBlocksOrItems();
		container.LateInit();
		MarkDirty();
	}

	private ItemSlot GetAutoPullFromSlot(BlockFacing atBlockFace)
	{
		if (atBlockFace == BlockFacing.DOWN)
		{
			return inventory.FirstOrDefault((ItemSlot slot) => !slot.Empty);
		}
		return null;
	}

	protected virtual void OnInvOpened(IPlayer player)
	{
		inventory.PutLocked = retrieveOnly && player.WorldData.CurrentGameMode != EnumGameMode.Creative;
		if (Api.Side == EnumAppSide.Client)
		{
			OpenLid();
		}
	}

	public void OpenLid()
	{
		BlockEntityAnimationUtil blockEntityAnimationUtil = animUtil;
		if (blockEntityAnimationUtil != null && !blockEntityAnimationUtil.activeAnimationsByAnimCode.ContainsKey("lidopen"))
		{
			animUtil?.StartAnimation(new AnimationMetaData
			{
				Animation = "lidopen",
				Code = "lidopen",
				AnimationSpeed = 1.8f,
				EaseOutSpeed = 6f,
				EaseInSpeed = 15f
			});
		}
	}

	public void CloseLid()
	{
		BlockEntityAnimationUtil blockEntityAnimationUtil = animUtil;
		if (blockEntityAnimationUtil != null && blockEntityAnimationUtil.activeAnimationsByAnimCode.ContainsKey("lidopen"))
		{
			animUtil?.StopAnimation("lidopen");
		}
	}

	protected virtual void OnInvClosed(IPlayer player)
	{
		if (LidOpenEntityId.Count == 0)
		{
			CloseLid();
		}
		inventory.PutLocked = retrieveOnly;
		GuiDialogBlockEntity inv = invDialog;
		invDialog = null;
		if (inv != null && inv.IsOpened())
		{
			inv?.TryClose();
		}
		inv?.Dispose();
	}

	public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
	{
		if (byPlayer.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			inventory.PutLocked = false;
		}
		if (inventory.PutLocked && inventory.Empty)
		{
			return false;
		}
		if (Api.World is IServerWorldAccessor)
		{
			byte[] data = BlockEntityContainerOpen.ToBytes("BlockEntityInventory", Lang.Get(dialogTitleLangCode), (byte)quantityColumns, inventory);
			((ICoreServerAPI)Api).Network.SendBlockEntityPacket((IServerPlayer)byPlayer, Pos, 5000, data);
			byPlayer.InventoryManager.OpenInventory(inventory);
			data = SerializerUtil.Serialize(new OpenContainerLidPacket(byPlayer.Entity.EntityId, LidOpenEntityId.Count > 0));
			((ICoreServerAPI)Api).Network.BroadcastBlockEntityPacket(Pos, 5001, data, (IServerPlayer)byPlayer);
		}
		return true;
	}

	private MeshData GenMesh(ITesselatorAPI tesselator)
	{
		BlockGenericTypedContainer block = base.Block as BlockGenericTypedContainer;
		if (base.Block == null)
		{
			block = (BlockGenericTypedContainer)(base.Block = Api.World.BlockAccessor.GetBlock(Pos) as BlockGenericTypedContainer);
		}
		if (block == null)
		{
			return null;
		}
		int rndTexNum = (base.Block.Attributes?["rndTexNum"][type]?.AsInt()).GetValueOrDefault();
		string key = "typedContainerMeshes" + base.Block.Code.ToShortString();
		Dictionary<string, MeshData> meshes = ObjectCacheUtil.GetOrCreate(Api, key, () => new Dictionary<string, MeshData>());
		string shapename = base.Block.Attributes?["shape"][type].AsString();
		if (shapename == null)
		{
			return null;
		}
		Shape shape = null;
		if (animUtil != null)
		{
			string skeydict = "typedContainerShapes";
			Dictionary<string, Shape> shapes = ObjectCacheUtil.GetOrCreate(Api, skeydict, () => new Dictionary<string, Shape>());
			string skey = base.Block.FirstCodePart() + type + block.Subtype + "--" + shapename + "-" + rndTexNum;
			if (!shapes.TryGetValue(skey, out shape))
			{
				shape = (shapes[skey] = block.GetShape(Api as ICoreClientAPI, shapename));
			}
		}
		string meshKey = type + block.Subtype + "-" + rndTexNum;
		if (meshes.TryGetValue(meshKey, out var mesh))
		{
			if (animUtil != null && animUtil.renderer == null)
			{
				animUtil.InitializeAnimator(type + "-" + key + "-" + block.Subtype, mesh, shape, rendererRot);
			}
			return mesh;
		}
		if (rndTexNum > 0)
		{
			rndTexNum = GameMath.MurmurHash3Mod(Pos.X, Pos.Y, Pos.Z, rndTexNum);
		}
		if (animUtil != null)
		{
			if (animUtil.renderer == null)
			{
				GenericContainerTextureSource texSource = new GenericContainerTextureSource
				{
					blockTextureSource = tesselator.GetTextureSource(base.Block, rndTexNum),
					curType = type
				};
				mesh = animUtil.InitializeAnimator(type + "-" + key + "-" + block.Subtype, shape, texSource, rendererRot);
			}
			return meshes[meshKey] = mesh;
		}
		mesh = block.GenMesh(Api as ICoreClientAPI, type, shapename, tesselator, new Vec3f(), rndTexNum);
		return meshes[meshKey] = mesh;
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		if (!base.OnTesselation(mesher, tesselator))
		{
			if (ownMesh == null)
			{
				ownMesh = GenMesh(tesselator);
				if (ownMesh == null)
				{
					return false;
				}
			}
			mesher.AddMeshData(ownMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, MeshAngle, 0f));
		}
		return true;
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
	{
		MeshAngle = tree.GetFloat("meshAngle");
		MeshAngle -= (float)degreeRotation * ((float)Math.PI / 180f);
		tree.SetFloat("meshAngle", MeshAngle);
	}
}
