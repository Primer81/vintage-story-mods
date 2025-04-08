using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public abstract class RGBMapLayer : MapLayer
{
	public Dictionary<Vec2i, int> ChunkTextures = new Dictionary<Vec2i, int>();

	public bool Visible;

	public abstract MapLegendItem[] LegendItems { get; }

	public abstract EnumMinMagFilter MinFilter { get; }

	public abstract EnumMinMagFilter MagFilter { get; }

	public RGBMapLayer(ICoreAPI api, IWorldMapManager mapSink)
		: base(api, mapSink)
	{
	}
}
