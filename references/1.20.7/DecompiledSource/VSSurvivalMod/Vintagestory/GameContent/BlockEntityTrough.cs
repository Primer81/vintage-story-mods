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

namespace Vintagestory.GameContent;

public class BlockEntityTrough : BlockEntityContainer, ITexPositionSource, IAnimalFoodSource, IPointOfInterest
{
	internal InventoryGeneric inventory;

	private ITexPositionSource blockTexPosSource;

	private MeshData currentMesh;

	private string contentCode = "";

	private DoubleTroughPoiDummy dummypoi;

	public override InventoryBase Inventory => inventory;

	public override string InventoryClassName => "trough";

	public Size2i AtlasSize => (Api as ICoreClientAPI).BlockTextureAtlas.Size;

	public Vec3d Position => Pos.ToVec3d().Add(0.5, 0.5, 0.5);

	public string Type => "food";

	public ContentConfig[] contentConfigs => Api.ObjectCache["troughContentConfigs-" + base.Block.Code] as ContentConfig[];

	public bool IsFull
	{
		get
		{
			ItemStack[] stacks = GetNonEmptyContentStacks();
			ContentConfig config = contentConfigs.FirstOrDefault((ContentConfig c) => c.Code == contentCode);
			if (config == null)
			{
				return false;
			}
			if (stacks.Length != 0)
			{
				return stacks[0].StackSize >= config.QuantityPerFillLevel * config.MaxFillLevels;
			}
			return false;
		}
	}

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			if (textureCode != "contents")
			{
				return blockTexPosSource[textureCode];
			}
			string configTextureCode = contentConfigs.FirstOrDefault((ContentConfig c) => c.Code == contentCode)?.TextureCode;
			if (configTextureCode != null && configTextureCode.Equals("*"))
			{
				configTextureCode = "contents-" + Inventory.FirstNonEmptySlot.Itemstack.Collectible.Code.ToShortString();
			}
			if (configTextureCode == null)
			{
				return blockTexPosSource[textureCode];
			}
			return blockTexPosSource[configTextureCode];
		}
	}

	public BlockEntityTrough()
	{
		inventory = new InventoryGeneric(4, null, null, (int id, InventoryGeneric inv) => new ItemSlotTrough(this, inv));
		inventory.OnGetAutoPushIntoSlot = (BlockFacing face, ItemSlot slot) => IsFull ? null : inventory.GetBestSuitedSlot(slot).slot;
	}

	public bool IsSuitableFor(Entity entity, CreatureDiet diet)
	{
		if (inventory.Empty || diet == null)
		{
			return false;
		}
		ContentConfig config = contentConfigs.FirstOrDefault((ContentConfig c) => c.Code == contentCode);
		ItemStack contentResolvedItemstack = config?.Content?.ResolvedItemstack ?? ResolveWildcardContent(config, entity.World);
		if (contentResolvedItemstack == null)
		{
			return false;
		}
		if (diet.Matches(contentResolvedItemstack) && inventory[0].StackSize >= config.QuantityPerFillLevel && base.Block is BlockTroughBase trough)
		{
			return !trough.UnsuitableForEntity(entity.Code.Path);
		}
		return false;
	}

	private ItemStack ResolveWildcardContent(ContentConfig config, IWorldAccessor worldAccessor)
	{
		if (config?.Content?.Code == null)
		{
			return null;
		}
		List<CollectibleObject> searchObjects = new List<CollectibleObject>();
		switch (config.Content.Type)
		{
		case EnumItemClass.Block:
			searchObjects.AddRange(worldAccessor.SearchBlocks(config.Content.Code));
			break;
		case EnumItemClass.Item:
			searchObjects.AddRange(worldAccessor.SearchItems(config.Content.Code));
			break;
		default:
			throw new ArgumentOutOfRangeException("Type");
		}
		foreach (CollectibleObject item in searchObjects)
		{
			if (item.Code.Equals(Inventory.FirstNonEmptySlot?.Itemstack?.Item?.Code))
			{
				return new ItemStack(item);
			}
		}
		return null;
	}

	public float ConsumeOnePortion(Entity entity)
	{
		ContentConfig config = contentConfigs.FirstOrDefault((ContentConfig c) => c.Code == contentCode);
		if (config == null || inventory.Empty)
		{
			return 0f;
		}
		inventory[0].TakeOut(config.QuantityPerFillLevel);
		if (inventory[0].Empty)
		{
			contentCode = "";
		}
		inventory[0].MarkDirty();
		MarkDirty(redrawOnClient: true);
		return 1f;
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		if (Api.Side == EnumAppSide.Client)
		{
			_ = (ICoreClientAPI)api;
			if (currentMesh == null)
			{
				currentMesh = GenMesh();
			}
		}
		else
		{
			Api.ModLoader.GetModSystem<POIRegistry>().AddPOI(this);
			if (base.Block is BlockTroughDoubleBlock doubleblock)
			{
				dummypoi = new DoubleTroughPoiDummy(this)
				{
					Position = doubleblock.OtherPartPos(Pos).ToVec3d().Add(0.5, 0.5, 0.5)
				};
				Api.ModLoader.GetModSystem<POIRegistry>().AddPOI(dummypoi);
			}
		}
		inventory.SlotModified += Inventory_SlotModified;
	}

	private void Inventory_SlotModified(int id)
	{
		contentCode = ItemSlotTrough.getContentConfig(Api.World, contentConfigs, inventory[id])?.Code;
		if (Api.Side == EnumAppSide.Client)
		{
			currentMesh = GenMesh();
		}
		MarkDirty(redrawOnClient: true);
	}

	public override void OnBlockPlaced(ItemStack byItemStack = null)
	{
		base.OnBlockPlaced(byItemStack);
		if (Api.Side == EnumAppSide.Client)
		{
			currentMesh = GenMesh();
			MarkDirty(redrawOnClient: true);
		}
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		if (Api.Side == EnumAppSide.Server)
		{
			Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);
			if (dummypoi != null)
			{
				Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(dummypoi);
			}
		}
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Server)
		{
			Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(this);
			if (dummypoi != null)
			{
				Api.ModLoader.GetModSystem<POIRegistry>().RemovePOI(dummypoi);
			}
		}
	}

	internal MeshData GenMesh()
	{
		if (base.Block == null)
		{
			return null;
		}
		ItemStack firstStack = inventory[0].Itemstack;
		if (firstStack == null)
		{
			return null;
		}
		string shapeLoc = "";
		ICoreClientAPI capi = Api as ICoreClientAPI;
		if (contentCode == "" || contentConfigs == null)
		{
			if (!(firstStack.Collectible.Code.Path == "rot"))
			{
				return null;
			}
			shapeLoc = "block/wood/trough/" + ((base.Block.Variant["part"] == "small") ? "small" : "large") + "/rotfill" + GameMath.Clamp(firstStack.StackSize / 4, 1, 4);
		}
		else
		{
			ContentConfig config = contentConfigs.FirstOrDefault((ContentConfig c) => c.Code == contentCode);
			if (config == null)
			{
				return null;
			}
			int fillLevel = Math.Max(0, firstStack.StackSize / config.QuantityPerFillLevel - 1);
			shapeLoc = config.ShapesPerFillLevel[Math.Min(config.ShapesPerFillLevel.Length - 1, fillLevel)];
		}
		Vec3f rotation = new Vec3f(base.Block.Shape.rotateX, base.Block.Shape.rotateY, base.Block.Shape.rotateZ);
		blockTexPosSource = capi.Tesselator.GetTextureSource(base.Block);
		Shape shape = Shape.TryGet(Api, "shapes/" + shapeLoc + ".json");
		capi.Tesselator.TesselateShape("betroughcontentsleft", shape, out var meshbase, this, rotation, 0, 0, 0);
		if (base.Block is BlockTroughDoubleBlock doubleblock)
		{
			capi.Tesselator.TesselateShape("betroughcontentsright", shape, out var meshadd, this, rotation.Add(0f, 180f, 0f), 0, 0, 0);
			BlockFacing facing = doubleblock.OtherPartFacing();
			meshadd.Translate(facing.Normalf);
			meshbase.AddMeshData(meshadd);
		}
		return meshbase;
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
	{
		mesher.AddMeshData(currentMesh);
		return false;
	}

	internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
	{
		ItemSlot handSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (handSlot.Empty)
		{
			return false;
		}
		ItemStack[] stacks = GetNonEmptyContentStacks();
		ContentConfig contentConf = ItemSlotTrough.getContentConfig(Api.World, contentConfigs, handSlot);
		if (contentConf == null)
		{
			return false;
		}
		if (stacks.Length == 0)
		{
			if (handSlot.StackSize >= contentConf.QuantityPerFillLevel)
			{
				inventory[0].Itemstack = handSlot.TakeOut(contentConf.QuantityPerFillLevel);
				inventory[0].MarkDirty();
				return true;
			}
			return false;
		}
		if (handSlot.Itemstack.Equals(Api.World, stacks[0], GlobalConstants.IgnoredStackAttributes) && handSlot.StackSize >= contentConf.QuantityPerFillLevel && stacks[0].StackSize < contentConf.QuantityPerFillLevel * contentConf.MaxFillLevels)
		{
			handSlot.TakeOut(contentConf.QuantityPerFillLevel);
			inventory[0].Itemstack.StackSize += contentConf.QuantityPerFillLevel;
			inventory[0].MarkDirty();
			return true;
		}
		return false;
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetString("contentCode", contentCode);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		contentCode = tree.GetString("contentCode");
		ICoreAPI api = Api;
		if (api != null && api.Side == EnumAppSide.Client)
		{
			currentMesh = GenMesh();
			MarkDirty(redrawOnClient: true);
		}
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		ItemStack firstStack = inventory[0].Itemstack;
		if (contentConfigs == null)
		{
			return;
		}
		ContentConfig config = contentConfigs.FirstOrDefault((ContentConfig c) => c.Code == contentCode);
		if (config == null && firstStack != null)
		{
			dsc.AppendLine(firstStack.StackSize + "x " + firstStack.GetName());
		}
		if (config == null || firstStack == null)
		{
			return;
		}
		int fillLevel = firstStack.StackSize / config.QuantityPerFillLevel;
		dsc.AppendLine(Lang.Get("Portions: {0}", fillLevel));
		ItemStack contentsStack = config.Content.ResolvedItemstack ?? ResolveWildcardContent(config, forPlayer.Entity.World);
		if (contentsStack == null)
		{
			return;
		}
		CollectibleObject collectible = contentsStack.Collectible;
		EnumFoodCategory foodCat = collectible.NutritionProps?.FoodCategory ?? EnumFoodCategory.NoNutrition;
		string[] foodTags = collectible.Attributes?["foodTags"].AsArray<string>();
		dsc.AppendLine(Lang.Get(contentsStack.GetName()));
		HashSet<string> creatureNames = new HashSet<string>();
		foreach (EntityProperties entityType in Api.World.EntityTypes)
		{
			JsonObject attr = entityType.Attributes;
			if (attr != null && attr["creatureDiet"].Exists && attr["creatureDiet"].AsObject<CreatureDiet>().Matches(foodCat, foodTags))
			{
				string code = attr?["creatureDietGroup"].AsString() ?? attr?["handbook"]["groupcode"].AsString() ?? ("item-creature-" + entityType.Code);
				creatureNames.Add(Lang.Get(code));
			}
		}
		if (creatureNames.Count > 0)
		{
			dsc.AppendLine(Lang.Get("trough-suitable", string.Join(", ", creatureNames)));
		}
		else
		{
			dsc.AppendLine(Lang.Get("trough-unsuitable"));
		}
	}
}
