using System.Collections.Generic;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public abstract class MarkerMapLayer : MapLayer
{
	public Dictionary<string, int> IconTextures = new Dictionary<string, int>();

	public MarkerMapLayer(ICoreAPI api, IWorldMapManager mapSink)
		: base(api, mapSink)
	{
	}
}
