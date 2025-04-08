using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class ModSystemLabelMeshCache : ModSystem
{
	private ICoreClientAPI capi;

	protected ConcurrentDictionary<int, ItemStackRenderCacheItem> itemStackRenders = new ConcurrentDictionary<int, ItemStackRenderCacheItem>();

	private ConcurrentDictionary<int, bool> mipmapRegenQueued = new ConcurrentDictionary<int, bool>();

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
	}

	public void RequestLabelTexture(int labelColor, BlockPos pos, ItemStack labelStack, Action<int> onLabelTextureReady)
	{
		int hashCode = labelStack.GetHashCode(GlobalConstants.IgnoredStackAttributes) + 23 * labelColor.GetHashCode();
		if (itemStackRenders.TryGetValue(hashCode, out var val))
		{
			val.UsedCounter.Add(pos.GetHashCode());
			if (val.TextureSubId != 0)
			{
				onLabelTextureReady(val.TextureSubId);
			}
			else
			{
				val.onLabelTextureReady.Add(onLabelTextureReady);
			}
			return;
		}
		ConcurrentDictionary<int, ItemStackRenderCacheItem> concurrentDictionary = itemStackRenders;
		int key = hashCode;
		ItemStackRenderCacheItem obj = new ItemStackRenderCacheItem
		{
			UsedCounter = new HashSet<int>()
		};
		ItemStackRenderCacheItem itemStackRenderCacheItem = obj;
		concurrentDictionary[key] = obj;
		itemStackRenderCacheItem.UsedCounter.Add(pos.GetHashCode());
		itemStackRenderCacheItem.onLabelTextureReady.Add(onLabelTextureReady);
		capi.Render.RenderItemStackToAtlas(labelStack, capi.BlockTextureAtlas, 52, delegate(int texSubid)
		{
			if (itemStackRenders.TryGetValue(hashCode, out var value))
			{
				value.TextureSubId = texSubid;
				foreach (Action<int> item in value.onLabelTextureReady)
				{
					item(texSubid);
				}
				value.onLabelTextureReady.Clear();
				regenMipMapsOnce(capi.BlockTextureAtlas.Positions[texSubid].atlasNumber);
			}
			else
			{
				capi.BlockTextureAtlas.FreeTextureSpace(texSubid);
			}
		}, ColorUtil.ColorOverlay(labelColor, -1, 0.65f), 0.5f);
	}

	private void regenMipMapsOnce(int atlasNumber)
	{
		if (mipmapRegenQueued.ContainsKey(atlasNumber))
		{
			return;
		}
		mipmapRegenQueued[atlasNumber] = true;
		capi.Event.EnqueueMainThreadTask(delegate
		{
			capi.Event.EnqueueMainThreadTask(delegate
			{
				capi.BlockTextureAtlas.RegenMipMaps(atlasNumber);
				mipmapRegenQueued.Remove(atlasNumber);
			}, "genmipmaps");
		}, "genmipmaps");
	}

	public void FreeLabelTexture(ItemStack labelStack, int labelColor, BlockPos pos)
	{
		if (labelStack == null)
		{
			return;
		}
		int hashCode = labelStack.GetHashCode(GlobalConstants.IgnoredStackAttributes) + 23 * labelColor.GetHashCode();
		if (itemStackRenders.TryGetValue(hashCode, out var val))
		{
			val.UsedCounter.Remove(pos.GetHashCode());
			if (val.UsedCounter.Count == 0)
			{
				capi.BlockTextureAtlas.FreeTextureSpace(val.TextureSubId);
				val.TextureSubId = 0;
				itemStackRenders.Remove(hashCode);
			}
		}
	}
}
