using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockCropProp : Block, ITexPositionSource
{
	private ICoreClientAPI capi;

	private string nowTesselatingType;

	public Size2i AtlasSize => capi.BlockTextureAtlas.Size;

	public TextureAtlasPosition this[string textureCode]
	{
		get
		{
			capi.BlockTextureAtlas.GetOrInsertTexture(new AssetLocation("block/meta/cropprop/" + nowTesselatingType), out var _, out var texPos);
			return texPos;
		}
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		capi = api as ICoreClientAPI;
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, "croprop-meshes");
		if (meshRefs != null && meshRefs.Count > 0)
		{
			foreach (KeyValuePair<string, MultiTextureMeshRef> item in meshRefs)
			{
				item.Deconstruct(out var _, out var value);
				value.Dispose();
			}
			ObjectCacheUtil.Delete(api, "croprop-meshes");
		}
		base.OnUnloaded(api);
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		Dictionary<string, MultiTextureMeshRef> dict = ObjectCacheUtil.GetOrCreate(capi, "croprop-meshes", () => new Dictionary<string, MultiTextureMeshRef>());
		string type = itemstack.Attributes.GetString("type", "unknown");
		if (dict.TryGetValue(type, out var meshref))
		{
			renderinfo.ModelRef = meshref;
		}
		else
		{
			nowTesselatingType = type;
			capi.Tesselator.TesselateShape("croppropinv", Code, Shape, out var meshdata, this, 0, 0, 0);
			dict[type] = (renderinfo.ModelRef = capi.Render.UploadMultiTextureMesh(meshdata));
		}
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
	}

	public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
	{
		BEBehaviorCropProp beh = GetBEBehavior<BEBehaviorCropProp>(pos);
		string type = beh?.Type;
		if (type == null)
		{
			return base.GetPlacedBlockName(world, pos);
		}
		return Lang.GetMatching("block-crop-" + type + "-" + beh.Stage);
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		dsc.AppendLine(string.Format(Lang.Get("Type: {0}", Lang.Get("cropprop-type-" + inSlot.Itemstack.Attributes.GetString("type")))));
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
	}
}
