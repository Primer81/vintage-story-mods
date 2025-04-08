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

public class BlockEntityPlantContainer : BlockEntityContainer, ITexPositionSource, IRotatable
{
	private InventoryGeneric inv;

	private MeshData potMesh;

	private MeshData contentMesh;

	private RoomRegistry roomReg;

	private ICoreClientAPI capi;

	private ITexPositionSource contentTexSource;

	private PlantContainerProps curContProps;

	private Dictionary<string, AssetLocation> shapeTextures;

	public override InventoryBase Inventory => inv;

	public override string InventoryClassName => "pottedplant";

	public virtual float MeshAngle { get; set; }

	public string ContainerSize => base.Block.Attributes?["plantContainerSize"].AsString();

	private bool hasSoil => !inv[0].Empty;

	private PlantContainerProps PlantContProps => GetProps(inv[0].Itemstack);

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			AssetLocation textureLoc = null;
			if (curContProps.Textures != null && curContProps.Textures.TryGetValue(textureCode, out var compTex))
			{
				textureLoc = compTex.Base;
			}
			if (textureLoc == null && shapeTextures != null)
			{
				shapeTextures.TryGetValue(textureCode, out textureLoc);
			}
			int textureSubId;
			if (textureLoc != null)
			{
				TextureAtlasPosition texPos = capi.BlockTextureAtlas[textureLoc];
				if (texPos == null)
				{
					BitmapRef bmp2 = capi.Assets.TryGet(textureLoc.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"))?.ToBitmap(capi);
					if (bmp2 != null)
					{
						capi.BlockTextureAtlas.GetOrInsertTexture(textureLoc, out textureSubId, out texPos, () => bmp2);
						bmp2.Dispose();
					}
				}
				return texPos;
			}
			ItemStack content = GetContents();
			if (content.Class == EnumItemClass.Item)
			{
				textureLoc = content.Item.Textures[textureCode].Base;
				BitmapRef bmp = capi.Assets.TryGet(textureLoc.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"))?.ToBitmap(capi);
				if (bmp != null)
				{
					capi.BlockTextureAtlas.GetOrInsertTexture(textureLoc, out textureSubId, out var texPos2, () => bmp);
					bmp.Dispose();
					return texPos2;
				}
			}
			return contentTexSource[textureCode];
		}
	}

	public BlockEntityPlantContainer()
	{
		inv = new InventoryGeneric(1, null, null);
		inv.OnAcquireTransitionSpeed += slotTransitionSpeed;
	}

	private float slotTransitionSpeed(EnumTransitionType transType, ItemStack stack, float mulByConfig)
	{
		return 0f;
	}

	protected override void OnTick(float dt)
	{
	}

	public ItemStack GetContents()
	{
		return inv[0].Itemstack;
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		capi = api as ICoreClientAPI;
		if (api.Side == EnumAppSide.Client && potMesh == null)
		{
			genMeshes();
			MarkDirty(redrawOnClient: true);
			roomReg = api.ModLoader.GetModSystem<RoomRegistry>();
		}
	}

	public bool TryPutContents(ItemSlot fromSlot, IPlayer player)
	{
		if (!inv[0].Empty || fromSlot.Empty)
		{
			return false;
		}
		ItemStack stack = fromSlot.Itemstack;
		if (GetProps(stack) == null)
		{
			return false;
		}
		if (fromSlot.TryPutInto(Api.World, inv[0]) > 0)
		{
			if (Api.Side == EnumAppSide.Server)
			{
				Api.World.PlaySoundAt(new AssetLocation("sounds/block/plant"), Pos, 0.0);
			}
			(player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
			fromSlot.MarkDirty();
			MarkDirty(redrawOnClient: true);
			return true;
		}
		return false;
	}

	public bool TrySetContents(ItemStack stack)
	{
		if (GetProps(stack) == null)
		{
			return false;
		}
		inv[0].Itemstack = stack;
		MarkDirty(redrawOnClient: true);
		return true;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		MeshAngle = tree.GetFloat("meshAngle", MeshAngle);
		if (capi != null)
		{
			genMeshes();
			MarkDirty(redrawOnClient: true);
		}
	}

	private void genMeshes()
	{
		if (base.Block.Code == null)
		{
			return;
		}
		potMesh = GenPotMesh(capi.Tesselator);
		if (potMesh != null)
		{
			potMesh = potMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, MeshAngle, 0f);
		}
		MeshData[] meshes = GenContentMeshes(capi.Tesselator);
		if (meshes != null && meshes.Length != 0)
		{
			contentMesh = meshes[GameMath.MurmurHash3Mod(Pos.X, Pos.Y, Pos.Z, meshes.Length)];
			if (PlantContProps.RandomRotate)
			{
				float radY = (float)GameMath.MurmurHash3Mod(Pos.X, Pos.Y, Pos.Z, 16) * 22.5f * ((float)Math.PI / 180f);
				contentMesh = contentMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, radY, 0f);
			}
			else
			{
				contentMesh = contentMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, MeshAngle, 0f);
			}
		}
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetFloat("meshAngle", MeshAngle);
	}

	private MeshData GenPotMesh(ITesselatorAPI tesselator)
	{
		Dictionary<string, MeshData> meshes = ObjectCacheUtil.GetOrCreate(Api, "plantContainerMeshes", () => new Dictionary<string, MeshData>());
		string key = base.Block.Code.ToString() + (hasSoil ? "soil" : "empty");
		if (meshes.TryGetValue(key, out var mesh))
		{
			return mesh;
		}
		if (hasSoil && base.Block.Attributes != null)
		{
			CompositeShape compshape = base.Block.Attributes["filledShape"].AsObject<CompositeShape>(null, base.Block.Code.Domain);
			Shape shape = null;
			if (compshape != null)
			{
				shape = Shape.TryGet(Api, compshape.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json"));
			}
			if (shape == null)
			{
				Api.World.Logger.Error("Plant container, asset {0} not found,", compshape?.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json"));
				return mesh;
			}
			tesselator.TesselateShape(base.Block, shape, out mesh);
		}
		else
		{
			mesh = capi.TesselatorManager.GetDefaultBlockMesh(base.Block);
		}
		return meshes[key] = mesh;
	}

	private MeshData[] GenContentMeshes(ITesselatorAPI tesselator)
	{
		ItemStack content = GetContents();
		if (content == null)
		{
			return null;
		}
		Dictionary<string, MeshData[]> meshes = ObjectCacheUtil.GetOrCreate(Api, "plantContainerContentMeshes", () => new Dictionary<string, MeshData[]>());
		float fillHeight = ((base.Block.Attributes == null) ? 0.4f : base.Block.Attributes["fillHeight"].AsFloat(0.4f));
		string containersize = ContainerSize;
		string key = content?.ToString() + "-" + containersize + "f" + fillHeight;
		if (meshes.TryGetValue(key, out var meshwithVariants))
		{
			return meshwithVariants;
		}
		curContProps = PlantContProps;
		if (curContProps == null)
		{
			return null;
		}
		CompositeShape compoShape = curContProps.Shape;
		if (compoShape == null)
		{
			compoShape = ((content.Class == EnumItemClass.Block) ? content.Block.Shape : content.Item.Shape);
		}
		ModelTransform transform = curContProps.Transform;
		if (transform == null)
		{
			transform = new ModelTransform().EnsureDefaultValues();
			transform.Translation.Y = fillHeight;
		}
		contentTexSource = ((content.Class == EnumItemClass.Block) ? capi.Tesselator.GetTextureSource(content.Block) : capi.Tesselator.GetTextureSource(content.Item));
		List<IAsset> assets;
		if (compoShape.Base.Path.EndsWith('*'))
		{
			assets = Api.Assets.GetManyInCategory("shapes", compoShape.Base.Path.Substring(0, compoShape.Base.Path.Length - 1), compoShape.Base.Domain);
		}
		else
		{
			assets = new List<IAsset>();
			assets.Add(Api.Assets.TryGet(compoShape.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json")));
		}
		if (assets != null && assets.Count > 0)
		{
			ShapeElement.locationForLogging = compoShape.Base;
			meshwithVariants = new MeshData[assets.Count];
			for (int i = 0; i < assets.Count; i++)
			{
				Shape shape = assets[i].ToObject<Shape>();
				shapeTextures = shape.Textures;
				MeshData mesh;
				try
				{
					byte climateColorMapId = (byte)((content.Block?.ClimateColorMapResolved != null) ? ((byte)(content.Block.ClimateColorMapResolved.RectIndex + 1)) : 0);
					byte seasonColorMapId = (byte)((content.Block?.SeasonColorMapResolved != null) ? ((byte)(content.Block.SeasonColorMapResolved.RectIndex + 1)) : 0);
					tesselator.TesselateShape("plant container content shape", shape, out mesh, this, null, 0, climateColorMapId, seasonColorMapId);
				}
				catch (Exception e)
				{
					Api.Logger.Error(string.Concat(e.Message, " (when tesselating ", compoShape.Base.WithPathPrefixOnce("shapes/"), ")"));
					Api.Logger.Error(e);
					meshwithVariants = null;
					break;
				}
				mesh.ModelTransform(transform);
				meshwithVariants[i] = mesh;
			}
		}
		else
		{
			Api.World.Logger.Error("Plant container, content asset {0} not found,", compoShape.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json"));
		}
		return meshes[key] = meshwithVariants;
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		if (potMesh == null)
		{
			return false;
		}
		mesher.AddMeshData(potMesh);
		if (contentMesh != null)
		{
			if (Api.World.BlockAccessor.GetDistanceToRainFall(Pos, 6, 2) >= 20)
			{
				MeshData cloned = contentMesh.Clone();
				cloned.ClearWindFlags();
				mesher.AddMeshData(cloned);
			}
			else
			{
				mesher.AddMeshData(contentMesh);
			}
		}
		return true;
	}

	public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
	{
		ItemStack contents = GetContents();
		if (contents != null)
		{
			dsc.Append(Lang.Get("Planted: {0}", contents.GetName()));
		}
	}

	public PlantContainerProps GetProps(ItemStack stack)
	{
		return stack?.Collectible.Attributes?["plantContainable"]?[ContainerSize + "Container"]?.AsObject<PlantContainerProps>(null, stack.Collectible.Code.Domain);
	}

	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
	{
		MeshAngle = tree.GetFloat("meshAngle");
		MeshAngle -= (float)degreeRotation * ((float)Math.PI / 180f);
		tree.SetFloat("meshAngle", MeshAngle);
	}
}
