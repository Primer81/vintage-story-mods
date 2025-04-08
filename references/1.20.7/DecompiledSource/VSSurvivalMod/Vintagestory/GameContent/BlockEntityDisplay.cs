using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public abstract class BlockEntityDisplay : BlockEntityContainer, ITexPositionSource
{
	protected CollectibleObject nowTesselatingObj;

	protected Shape nowTesselatingShape;

	protected ICoreClientAPI capi;

	protected float[][] tfMatrices;

	public virtual string ClassCode => InventoryClassName;

	public virtual int DisplayedItems => Inventory.Count;

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public virtual string AttributeTransformCode => "onDisplayTransform";

	public virtual TextureAtlasPosition this[string textureCode]
	{
		get
		{
			IDictionary<string, CompositeTexture> dictionary;
			if (!(nowTesselatingObj is Item item))
			{
				dictionary = (nowTesselatingObj as Block).Textures;
			}
			else
			{
				IDictionary<string, CompositeTexture> textures2 = item.Textures;
				dictionary = textures2;
			}
			IDictionary<string, CompositeTexture> textures = dictionary;
			AssetLocation texturePath = null;
			if (textures.TryGetValue(textureCode, out var tex))
			{
				texturePath = tex.Baked.BakedName;
			}
			if (texturePath == null && textures.TryGetValue("all", out tex))
			{
				texturePath = tex.Baked.BakedName;
			}
			if (texturePath == null)
			{
				nowTesselatingShape?.Textures.TryGetValue(textureCode, out texturePath);
			}
			if (texturePath == null)
			{
				texturePath = new AssetLocation(textureCode);
			}
			return getOrCreateTexPos(texturePath);
		}
	}

	protected Dictionary<string, MeshData> MeshCache => ObjectCacheUtil.GetOrCreate(Api, "meshesDisplay-" + ClassCode, () => new Dictionary<string, MeshData>());

	protected TextureAtlasPosition getOrCreateTexPos(AssetLocation texturePath)
	{
		TextureAtlasPosition texpos = capi.BlockTextureAtlas[texturePath];
		if (texpos == null && !capi.BlockTextureAtlas.GetOrInsertTexture(texturePath, out var _, out texpos))
		{
			capi.World.Logger.Warning(string.Concat("For render in block ", base.Block.Code, ", item {0} defined texture {1}, no such texture found."), nowTesselatingObj.Code, texturePath);
			return capi.BlockTextureAtlas.UnknownTexturePosition;
		}
		return texpos;
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		capi = api as ICoreClientAPI;
		if (capi != null)
		{
			updateMeshes();
			api.Event.RegisterEventBusListener(OnEventBusEvent);
		}
	}

	private void OnEventBusEvent(string eventname, ref EnumHandling handling, IAttribute data)
	{
		if ((eventname != "genjsontransform" && eventname != "oncloseedittransforms" && eventname != "onapplytransforms") || capi == null || Inventory.Empty || Pos.DistanceTo(capi.World.Player.Entity.Pos.X, capi.World.Player.Entity.Pos.Y, capi.World.Player.Entity.Pos.Z) > 20f)
		{
			return;
		}
		for (int i = 0; i < DisplayedItems; i++)
		{
			if (!Inventory[i].Empty)
			{
				string key = getMeshCacheKey(Inventory[i].Itemstack);
				MeshCache.Remove(key);
			}
		}
		updateMeshes();
		MarkDirty(redrawOnClient: true);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
	}

	protected virtual void RedrawAfterReceivingTreeAttributes(IWorldAccessor worldForResolving)
	{
		if (worldForResolving.Side == EnumAppSide.Client && Api != null)
		{
			updateMeshes();
			MarkDirty(redrawOnClient: true);
		}
	}

	public virtual void updateMeshes()
	{
		if (Api != null && Api.Side != EnumAppSide.Server && DisplayedItems != 0)
		{
			for (int i = 0; i < DisplayedItems; i++)
			{
				updateMesh(i);
			}
			tfMatrices = genTransformationMatrices();
		}
	}

	protected virtual void updateMesh(int index)
	{
		if (Api != null && Api.Side != EnumAppSide.Server && !Inventory[index].Empty)
		{
			getOrCreateMesh(Inventory[index].Itemstack, index);
		}
	}

	protected virtual string getMeshCacheKey(ItemStack stack)
	{
		IContainedMeshSource meshSource = stack.Collectible?.GetCollectibleInterface<IContainedMeshSource>();
		if (meshSource != null)
		{
			return meshSource.GetMeshCacheKey(stack);
		}
		return stack.Collectible.Code.ToString();
	}

	protected MeshData getMesh(ItemStack stack)
	{
		string key = getMeshCacheKey(stack);
		MeshCache.TryGetValue(key, out var meshdata);
		return meshdata;
	}

	protected virtual MeshData getOrCreateMesh(ItemStack stack, int index)
	{
		MeshData mesh = getMesh(stack);
		if (mesh != null)
		{
			return mesh;
		}
		IContainedMeshSource meshSource = stack.Collectible?.GetCollectibleInterface<IContainedMeshSource>();
		if (meshSource != null)
		{
			mesh = meshSource.GenMesh(stack, this.capi.BlockTextureAtlas, Pos);
		}
		if (mesh == null)
		{
			ICoreClientAPI capi = Api as ICoreClientAPI;
			if (stack.Class == EnumItemClass.Block)
			{
				mesh = capi.TesselatorManager.GetDefaultBlockMesh(stack.Block).Clone();
			}
			else
			{
				nowTesselatingObj = stack.Collectible;
				nowTesselatingShape = null;
				if (stack.Item.Shape?.Base != null)
				{
					nowTesselatingShape = capi.TesselatorManager.GetCachedShape(stack.Item.Shape.Base);
				}
				capi.Tesselator.TesselateItem(stack.Item, out mesh, this);
				mesh.RenderPassesAndExtraBits.Fill((short)2);
			}
		}
		JsonObject attributes = stack.Collectible.Attributes;
		if (attributes != null && attributes[AttributeTransformCode].Exists)
		{
			ModelTransform transform2 = stack.Collectible.Attributes?[AttributeTransformCode].AsObject<ModelTransform>();
			transform2.EnsureDefaultValues();
			mesh.ModelTransform(transform2);
		}
		else if (AttributeTransformCode == "onshelfTransform")
		{
			JsonObject attributes2 = stack.Collectible.Attributes;
			if (attributes2 != null && attributes2["onDisplayTransform"].Exists)
			{
				ModelTransform transform = stack.Collectible.Attributes?["onDisplayTransform"].AsObject<ModelTransform>();
				transform.EnsureDefaultValues();
				mesh.ModelTransform(transform);
			}
		}
		if (stack.Class == EnumItemClass.Item && (stack.Item.Shape == null || stack.Item.Shape.VoxelizeTexture))
		{
			mesh.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), (float)Math.PI / 2f, 0f, 0f);
			mesh.Scale(new Vec3f(0.5f, 0.5f, 0.5f), 0.33f, 0.33f, 0.33f);
			mesh.Translate(0f, -15f / 32f, 0f);
		}
		string key = getMeshCacheKey(stack);
		MeshCache[key] = mesh;
		return mesh;
	}

	protected abstract float[][] genTransformationMatrices();

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		for (int index = 0; index < DisplayedItems; index++)
		{
			ItemSlot slot = Inventory[index];
			if (!slot.Empty && tfMatrices != null)
			{
				mesher.AddMeshData(getMesh(slot.Itemstack), tfMatrices[index]);
			}
		}
		return base.OnTesselation(mesher, tessThreadTesselator);
	}
}
