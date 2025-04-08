using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockLiquidContainerTopOpened : BlockLiquidContainerBase, IContainedMeshSource, IContainedCustomName
{
	private LiquidTopOpenContainerProps Props;

	private MeshData origcontainermesh;

	private Shape contentShape;

	private Shape liquidContentShape;

	protected virtual string meshRefsCacheKey => Code.ToShortString() + "meshRefs";

	protected virtual AssetLocation emptyShapeLoc => Props.EmptyShapeLoc;

	protected virtual AssetLocation contentShapeLoc => Props.OpaqueContentShapeLoc;

	protected virtual AssetLocation liquidContentShapeLoc => Props.LiquidContentShapeLoc;

	public override float TransferSizeLitres => Props.TransferSizeLitres;

	public override float CapacityLitres => Props.CapacityLitres;

	public override bool CanDrinkFrom => true;

	public override bool IsTopOpened => true;

	public override bool AllowHeldLiquidTransfer => true;

	protected virtual float liquidMaxYTranslate => Props.LiquidMaxYTranslate;

	protected virtual float liquidYTranslatePerLitre => liquidMaxYTranslate / CapacityLitres;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		Props = new LiquidTopOpenContainerProps();
		JsonObject attributes = Attributes;
		if (attributes != null && attributes["liquidContainerProps"].Exists)
		{
			Props = Attributes["liquidContainerProps"].AsObject<LiquidTopOpenContainerProps>(null, Code.Domain);
		}
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		object obj;
		Dictionary<int, MultiTextureMeshRef> meshrefs = (Dictionary<int, MultiTextureMeshRef>)(capi.ObjectCache.TryGetValue(meshRefsCacheKey, out obj) ? (obj as Dictionary<int, MultiTextureMeshRef>) : (capi.ObjectCache[meshRefsCacheKey] = new Dictionary<int, MultiTextureMeshRef>()));
		ItemStack contentStack = GetContent(itemstack);
		if (contentStack != null)
		{
			int hashcode = GetStackCacheHashCode(contentStack);
			if (!meshrefs.TryGetValue(hashcode, out var meshRef))
			{
				MeshData meshdata = GenMesh(capi, contentStack);
				meshRef = (meshrefs[hashcode] = capi.Render.UploadMultiTextureMesh(meshdata));
			}
			renderinfo.ModelRef = meshRef;
		}
	}

	protected int GetStackCacheHashCode(ItemStack contentStack)
	{
		return (contentStack.StackSize + "x" + contentStack.Collectible.Code.ToShortString()).GetHashCode();
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		if (!(api is ICoreClientAPI capi) || !capi.ObjectCache.TryGetValue(meshRefsCacheKey, out var obj))
		{
			return;
		}
		foreach (KeyValuePair<int, MultiTextureMeshRef> item in obj as Dictionary<int, MultiTextureMeshRef>)
		{
			item.Value.Dispose();
		}
		capi.ObjectCache.Remove(meshRefsCacheKey);
	}

	public MeshData GenMesh(ICoreClientAPI capi, ItemStack contentStack, BlockPos forBlockPos = null)
	{
		if (origcontainermesh == null)
		{
			Shape shape2 = Vintagestory.API.Common.Shape.TryGet(capi, emptyShapeLoc.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/"));
			if (shape2 == null)
			{
				capi.World.Logger.Error("Empty shape {0} not found. Liquid container {1} will be invisible.", emptyShapeLoc, Code);
				return new MeshData();
			}
			capi.Tesselator.TesselateShape(this, shape2, out origcontainermesh, new Vec3f(Shape.rotateX, Shape.rotateY, Shape.rotateZ));
		}
		MeshData containerMesh = origcontainermesh.Clone();
		if (contentStack != null)
		{
			WaterTightContainableProps props = BlockLiquidContainerBase.GetContainableProps(contentStack);
			if (props == null)
			{
				capi.World.Logger.Error("Contents ('{0}') has no liquid properties, contents of liquid container {1} will be invisible.", contentStack.GetName(), Code);
				return containerMesh;
			}
			ContainerTextureSource contentSource = new ContainerTextureSource(capi, contentStack, props.Texture);
			Shape shape = (props.IsOpaque ? contentShape : liquidContentShape);
			AssetLocation loc = (props.IsOpaque ? contentShapeLoc : liquidContentShapeLoc);
			if (shape == null)
			{
				shape = Vintagestory.API.Common.Shape.TryGet(capi, loc.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/"));
				if (props.IsOpaque)
				{
					contentShape = shape;
				}
				else
				{
					liquidContentShape = shape;
				}
			}
			if (shape == null)
			{
				capi.World.Logger.Error("Content shape {0} not found. Contents of liquid container {1} will be invisible.", loc, Code);
				return containerMesh;
			}
			capi.Tesselator.TesselateShape(GetType().Name, shape, out var contentMesh, contentSource, new Vec3f(Shape.rotateX, Shape.rotateY, Shape.rotateZ), props.GlowLevel, 0, 0);
			contentMesh.Translate(0f, GameMath.Min(liquidMaxYTranslate, (float)contentStack.StackSize / props.ItemsPerLitre * liquidYTranslatePerLitre), 0f);
			if (props.ClimateColorMap != null)
			{
				int col = ((!(forBlockPos != null)) ? capi.World.ApplyColorMapOnRgba(props.ClimateColorMap, null, -1, 196, 128, flipRb: false) : capi.World.ApplyColorMapOnRgba(props.ClimateColorMap, null, -1, forBlockPos.X, forBlockPos.Y, forBlockPos.Z, flipRb: false));
				byte[] rgba = ColorUtil.ToBGRABytes(col);
				for (int j = 0; j < contentMesh.Rgba.Length; j++)
				{
					contentMesh.Rgba[j] = (byte)(contentMesh.Rgba[j] * rgba[j % 4] / 255);
				}
			}
			for (int i = 0; i < contentMesh.FlagsCount; i++)
			{
				contentMesh.Flags[i] = contentMesh.Flags[i] & -4097;
			}
			containerMesh.AddMeshData(contentMesh);
			if (forBlockPos != null)
			{
				containerMesh.CustomInts = new CustomMeshDataPartInt(containerMesh.FlagsCount);
				containerMesh.CustomInts.Count = containerMesh.FlagsCount;
				containerMesh.CustomInts.Values.Fill(67108864);
				containerMesh.CustomFloats = new CustomMeshDataPartFloat(containerMesh.FlagsCount * 2);
				containerMesh.CustomFloats.Count = containerMesh.FlagsCount * 2;
			}
		}
		return containerMesh;
	}

	public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos forBlockPos = null)
	{
		ItemStack contentStack = GetContent(itemstack);
		return GenMesh(api as ICoreClientAPI, contentStack, forBlockPos);
	}

	public string GetMeshCacheKey(ItemStack itemstack)
	{
		ItemStack contentStack = GetContent(itemstack);
		return itemstack.Collectible.Code.ToShortString() + "-" + contentStack?.StackSize + "x" + contentStack?.Collectible.Code.ToShortString();
	}

	public string GetContainedInfo(ItemSlot inSlot)
	{
		float litres = GetCurrentLitres(inSlot.Itemstack);
		ItemStack contentStack = GetContent(inSlot.Itemstack);
		if (litres <= 0f)
		{
			return Lang.Get("{0} (Empty)", inSlot.Itemstack.GetName());
		}
		string incontainername = Lang.Get(contentStack.Collectible.Code.Domain + ":incontainer-" + contentStack.Class.ToString().ToLowerInvariant() + "-" + contentStack.Collectible.Code.Path);
		if (litres == 1f)
		{
			return Lang.Get("{0} ({1} litre of {2})", inSlot.Itemstack.GetName(), litres, incontainername);
		}
		return Lang.Get("{0} ({1} litres of {2})", inSlot.Itemstack.GetName(), litres, incontainername);
	}

	public string GetContainedName(ItemSlot inSlot, int quantity)
	{
		return inSlot.Itemstack.GetName();
	}
}
