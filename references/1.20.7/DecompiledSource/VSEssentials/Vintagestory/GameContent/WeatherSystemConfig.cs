using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace Vintagestory.GameContent;

[JsonObject(MemberSerialization.OptIn)]
public class WeatherSystemConfig
{
	[JsonProperty]
	public AssetLocation[] SnowLayerBlockCodes;

	[JsonProperty]
	public WeatherPatternConfig RainOverlayPattern;

	public OrderedDictionary<Block, int> SnowLayerBlocks;

	internal void Init(IWorldAccessor world)
	{
		SnowLayerBlocks = new OrderedDictionary<Block, int>();
		int i = 0;
		AssetLocation[] snowLayerBlockCodes = SnowLayerBlockCodes;
		foreach (AssetLocation loc in snowLayerBlockCodes)
		{
			Block block = world.GetBlock(loc);
			if (block == null)
			{
				world.Logger.Error("config/weather.json: No such block found: '{0}', will ignore.", loc);
			}
			else
			{
				SnowLayerBlocks[block] = i++;
			}
		}
	}
}
