using System;
using System.Collections.Generic;

namespace Vintagestory.GameContent;

public class ItemStackRenderCacheItem
{
	public int TextureSubId;

	public HashSet<int> UsedCounter;

	public List<Action<int>> onLabelTextureReady = new List<Action<int>>();
}
