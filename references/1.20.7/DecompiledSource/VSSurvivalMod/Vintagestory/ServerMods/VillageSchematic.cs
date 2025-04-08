using Newtonsoft.Json;

namespace Vintagestory.ServerMods;

public class VillageSchematic
{
	[JsonProperty]
	public int MinSpawnDistance;

	public string Path;

	public int OffsetY;

	public double Weight;

	public BlockSchematicStructure[] Structures;

	public int MinQuantity;

	public int MaxQuantity = 9999;

	public int NowQuantity;

	public bool ShouldGenerate => NowQuantity < MaxQuantity;
}
