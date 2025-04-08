using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockClutterBookshelfWithLore : BlockClutterBookshelf
{
	private WorldInteraction[] interactions;

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		if (api.Side == EnumAppSide.Client)
		{
			interactions = ObjectCacheUtil.GetOrCreate(api, "bookshelfWithLoreInteractions", () => new WorldInteraction[1]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-takelorebook",
					MouseButton = EnumMouseButton.Right
				}
			});
		}
	}

	public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
	{
		MultiTextureMeshRef meshref = genCombinedMesh(itemstack);
		if (meshref != null)
		{
			renderinfo.ModelRef = meshref;
		}
		base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
	}

	private MultiTextureMeshRef genCombinedMesh(ItemStack itemstack)
	{
		Dictionary<string, MultiTextureMeshRef> cachedRefs = ObjectCacheUtil.GetOrCreate(api, "combinedBookShelfWithLoreMeshRef", () => new Dictionary<string, MultiTextureMeshRef>());
		string type = itemstack.Attributes.GetString("type", itemstack.Attributes.GetString("type1"));
		if (type == null)
		{
			return null;
		}
		if (cachedRefs.TryGetValue(type, out var meshref))
		{
			return meshref;
		}
		MeshData mesh = GetOrCreateMesh(GetTypeProps(type, itemstack, null));
		AssetLocation loc = new AssetLocation("shapes/block/clutter/" + type + "-book.json");
		Shape shape = api.Assets.TryGet(loc).ToObject<Shape>();
		(api as ICoreClientAPI).Tesselator.TesselateShape(this, shape, out var bookmesh);
		mesh.AddMeshData(bookmesh);
		return cachedRefs[type] = (api as ICoreClientAPI).Render.UploadMultiTextureMesh(mesh);
	}

	public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
	{
		BEBehaviorClutterBookshelfWithLore be = GetBEBehavior<BEBehaviorClutterBookshelfWithLore>(pos);
		ItemStack stack = base.OnPickBlock(world, pos);
		if (be != null)
		{
			stack.Attributes.SetString("loreCode", be.LoreCode);
		}
		return stack;
	}

	public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
	{
		base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
		string loreCode = inSlot.Itemstack.Attributes.GetString("loreCode");
		if (loreCode != null)
		{
			dsc.AppendLine("lore code:" + loreCode);
		}
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BEBehaviorClutterBookshelfWithLore be = GetBEBehavior<BEBehaviorClutterBookshelfWithLore>(blockSel.Position);
		if (be != null && be.OnInteract(byPlayer))
		{
			return true;
		}
		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}

	public override void OnUnloaded(ICoreAPI api)
	{
		base.OnUnloaded(api);
		Dictionary<string, MultiTextureMeshRef> cachedRefs = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, "combinedBookShelfWithLoreMeshRef");
		if (cachedRefs == null)
		{
			return;
		}
		foreach (KeyValuePair<string, MultiTextureMeshRef> item in cachedRefs)
		{
			item.Value.Dispose();
		}
	}
}
