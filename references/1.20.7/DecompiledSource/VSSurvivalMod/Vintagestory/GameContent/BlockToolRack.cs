using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class BlockToolRack : Block
{
	private static bool collectedToolTextures;

	private WorldInteraction[] interactions;

	public static Dictionary<Item, ToolTextures> ToolTextureSubIds(ICoreAPI api)
	{
		object obj;
		return (Dictionary<Item, ToolTextures>)(api.ObjectCache.TryGetValue("toolTextureSubIds", out obj) ? (obj as Dictionary<Item, ToolTextures>) : (api.ObjectCache["toolTextureSubIds"] = new Dictionary<Item, ToolTextures>()));
	}

	public override void OnLoaded(ICoreAPI api)
	{
		base.OnLoaded(api);
		collectedToolTextures = false;
		if (api.Side != EnumAppSide.Client)
		{
			return;
		}
		ICoreClientAPI capi = api as ICoreClientAPI;
		interactions = ObjectCacheUtil.GetOrCreate(api, "toolrackBlockInteractions", delegate
		{
			List<ItemStack> list = new List<ItemStack>();
			foreach (CollectibleObject current in api.World.Collectibles)
			{
				if (!current.Tool.HasValue)
				{
					JsonObject attributes = current.Attributes;
					if (attributes == null || !attributes["rackable"].AsBool())
					{
						continue;
					}
				}
				List<ItemStack> handBookStacks = current.GetHandBookStacks(capi);
				if (handBookStacks != null)
				{
					list.AddRange(handBookStacks);
				}
			}
			return new WorldInteraction[2]
			{
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-toolrack-place",
					HotKeyCode = null,
					MouseButton = EnumMouseButton.Right,
					Itemstacks = list.ToArray()
				},
				new WorldInteraction
				{
					ActionLangCode = "blockhelp-toolrack-take",
					HotKeyCode = null,
					MouseButton = EnumMouseButton.Right
				}
			};
		});
	}

	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		BlockEntity be = world.BlockAccessor.GetBlockEntity(blockSel.Position);
		if (be is BlockEntityToolrack)
		{
			return ((BlockEntityToolrack)be).OnPlayerInteract(byPlayer, blockSel.HitPosition);
		}
		return false;
	}

	public override void OnCollectTextures(ICoreAPI api, ITextureLocationDictionary textureDict)
	{
		base.OnCollectTextures(api, textureDict);
		if (collectedToolTextures)
		{
			return;
		}
		collectedToolTextures = true;
		Dictionary<Item, ToolTextures> toolTexturesDict = ToolTextureSubIds(api);
		toolTexturesDict.Clear();
		IList<Item> Items = api.World.Items;
		for (int i = 0; i < Items.Count; i++)
		{
			Item item = Items[i];
			if (!item.Tool.HasValue)
			{
				JsonObject attributes = item.Attributes;
				if (attributes == null || !attributes["rackable"].AsBool())
				{
					continue;
				}
			}
			ToolTextures tt = new ToolTextures();
			if (item.Shape != null)
			{
				Shape shape = (api as ICoreClientAPI).TesselatorManager.GetCachedShape(item.Shape.Base);
				if (shape != null)
				{
					foreach (KeyValuePair<string, AssetLocation> val2 in shape.Textures)
					{
						CompositeTexture ctex = new CompositeTexture(val2.Value.Clone());
						ctex.Bake(api.Assets);
						textureDict.GetOrAddTextureLocation(new AssetLocationAndSource(ctex.Baked.BakedName, "Shape code ", item.Shape.Base));
						tt.TextureSubIdsByCode[val2.Key] = textureDict[new AssetLocationAndSource(ctex.Baked.BakedName)];
					}
				}
			}
			foreach (KeyValuePair<string, CompositeTexture> val in item.Textures)
			{
				val.Value.Bake(api.Assets);
				textureDict.GetOrAddTextureLocation(new AssetLocationAndSource(val.Value.Baked.BakedName, "Item code ", item.Code));
				tt.TextureSubIdsByCode[val.Key] = textureDict[new AssetLocationAndSource(val.Value.Baked.BakedName)];
			}
			toolTexturesDict[item] = tt;
		}
	}

	public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
	{
		return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
	}
}
