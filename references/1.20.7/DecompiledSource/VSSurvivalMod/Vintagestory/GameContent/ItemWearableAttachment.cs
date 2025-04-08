using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ItemWearableAttachment : Item, IContainedMeshSource, ITexPositionSource
{
	private bool attachableToEntity;

	private ITextureAtlasAPI curAtlas;

	private Shape nowTesselatingShape;

	public Size2i AtlasSize => curAtlas.Size;

	public virtual TextureAtlasPosition this[string textureCode]
	{
		get
		{
			AssetLocation texturePath = null;
			if (Textures.TryGetValue(textureCode, out var tex))
			{
				texturePath = tex.Baked.BakedName;
			}
			if (texturePath == null && Textures.TryGetValue("all", out tex))
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

	public override void OnLoaded(ICoreAPI api)
	{
		attachableToEntity = IAttachableToEntity.FromCollectible(this) != null;
		base.OnLoaded(api);
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, "wearableAttachmentMeshRefs");
		if (meshRefs != null && meshRefs.Count > 0)
		{
			foreach (KeyValuePair<string, MultiTextureMeshRef> item in meshRefs)
			{
				item.Deconstruct(out var _, out var value);
				value.Dispose();
			}
			ObjectCacheUtil.Delete(api, "wearableAttachmentMeshRefs");
		}
		base.OnUnloaded(api);
	}

	protected TextureAtlasPosition getOrCreateTexPos(AssetLocation texturePath)
	{
		ICoreClientAPI capi = api as ICoreClientAPI;
		curAtlas.GetOrInsertTexture(texturePath, out var _, out var texpos, delegate
		{
			IAsset asset = capi.Assets.TryGet(texturePath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
			if (asset != null)
			{
				return asset.ToBitmap(capi);
			}
			capi.World.Logger.Warning("Item {0} defined texture {1}, not no such texture found.", Code, texturePath);
			return (IBitmap)null;
		}, 0.1f);
		return texpos;
	}

	public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos forBlockPos = null)
	{
		ICoreClientAPI capi = api as ICoreClientAPI;
		if (targetAtlas == capi.ItemTextureAtlas)
		{
			ITexPositionSource texSource = capi.Tesselator.GetTextureSource(itemstack.Item);
			return genMesh(capi, itemstack, texSource);
		}
		curAtlas = targetAtlas;
		MeshData meshData = genMesh(api as ICoreClientAPI, itemstack, this);
		meshData.RenderPassesAndExtraBits.Fill((short)1);
		return meshData;
	}

	public virtual string GetMeshCacheKey(ItemStack itemstack)
	{
		return "wearableAttachmentModelRef-" + itemstack.Collectible.Code.ToString();
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		if (attachableToEntity)
		{
			Dictionary<string, MultiTextureMeshRef> meshrefs = ObjectCacheUtil.GetOrCreate(capi, "wearableAttachmentMeshRefs", () => new Dictionary<string, MultiTextureMeshRef>());
			string key = GetMeshCacheKey(itemstack);
			if (!meshrefs.TryGetValue(key, out renderinfo.ModelRef))
			{
				ITexPositionSource texSource = capi.Tesselator.GetTextureSource(itemstack.Item);
				MeshData mesh = genMesh(capi, itemstack, texSource);
				ItemRenderInfo obj = renderinfo;
				MultiTextureMeshRef modelRef = (meshrefs[key] = ((mesh == null) ? renderinfo.ModelRef : capi.Render.UploadMultiTextureMesh(mesh)));
				obj.ModelRef = modelRef;
			}
			if (Attributes["visibleDamageEffect"].AsBool())
			{
				renderinfo.DamageEffect = Math.Max(0f, 1f - (float)GetRemainingDurability(itemstack) / (float)GetMaxDurability(itemstack) * 1.1f);
			}
		}
	}

	protected MeshData genMesh(ICoreClientAPI capi, ItemStack itemstack, ITexPositionSource texSource)
	{
		JsonObject attrObj = itemstack.Collectible.Attributes;
		EntityProperties entityType = capi.World.GetEntityType(new AssetLocation(attrObj?["wearerEntityCode"].ToString() ?? "player"));
		Shape entityShape = entityType.Client.LoadedShape;
		AssetLocation shapePathForLogging = entityType.Client.Shape.Base;
		Shape newShape = (attachableToEntity ? new Shape
		{
			Elements = entityShape.CloneElements(),
			Animations = entityShape.CloneAnimations(),
			AnimationsByCrc32 = entityShape.AnimationsByCrc32,
			JointsById = entityShape.JointsById,
			TextureWidth = entityShape.TextureWidth,
			TextureHeight = entityShape.TextureHeight,
			Textures = null
		} : entityShape);
		MeshData meshdata;
		if (attrObj["wearableInvShape"].Exists)
		{
			AssetLocation shapePath = new AssetLocation("shapes/" + attrObj["wearableInvShape"]?.ToString() + ".json");
			Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, shapePath);
			capi.Tesselator.TesselateShape(itemstack.Collectible, shape, out meshdata);
		}
		else
		{
			CompositeShape compArmorShape = (attrObj["attachShape"].Exists ? attrObj["attachShape"].AsObject<CompositeShape>(null, itemstack.Collectible.Code.Domain) : ((itemstack.Class == EnumItemClass.Item) ? itemstack.Item.Shape : itemstack.Block.Shape));
			if (compArmorShape == null)
			{
				capi.World.Logger.Warning("Wearable shape {0} {1} does not define a shape through either the shape property or the attachShape Attribute. Item will be invisible.", itemstack.Class, itemstack.Collectible.Code);
				return null;
			}
			AssetLocation shapePath2 = compArmorShape.Base.CopyWithPath("shapes/" + compArmorShape.Base.Path + ".json");
			Shape armorShape = Vintagestory.API.Common.Shape.TryGet(capi, shapePath2);
			if (armorShape == null)
			{
				capi.World.Logger.Warning("Wearable shape {0} defined in {1} {2} not found or errored, was supposed to be at {3}. Item will be invisible.", compArmorShape.Base, itemstack.Class, itemstack.Collectible.Code, shapePath2);
				return null;
			}
			newShape.StepParentShape(armorShape, shapePath2.ToShortString(), shapePathForLogging.ToShortString(), capi.Logger, delegate
			{
			});
			if (compArmorShape.Overlays != null)
			{
				CompositeShape[] overlays = compArmorShape.Overlays;
				foreach (CompositeShape overlay in overlays)
				{
					Shape oshape = Vintagestory.API.Common.Shape.TryGet(capi, overlay.Base.CopyWithPath("shapes/" + overlay.Base.Path + ".json"));
					if (oshape == null)
					{
						capi.World.Logger.Warning("Wearable shape {0} overlay {4} defined in {1} {2} not found or errored, was supposed to be at {3}. Item will be invisible.", compArmorShape.Base, itemstack.Class, itemstack.Collectible.Code, shapePath2, overlay.Base);
					}
					else
					{
						newShape.StepParentShape(oshape, overlay.Base.ToShortString(), shapePathForLogging.ToShortString(), capi.Logger, delegate
						{
						});
					}
				}
			}
			nowTesselatingShape = newShape;
			capi.Tesselator.TesselateShapeWithJointIds("entity", newShape, out meshdata, texSource, new Vec3f());
			nowTesselatingShape = null;
		}
		return meshdata;
	}
}
