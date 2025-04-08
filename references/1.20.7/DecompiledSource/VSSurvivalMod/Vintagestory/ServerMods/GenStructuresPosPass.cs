using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class GenStructuresPosPass : ModStdWorldGen
{
	public ChunkColumnGenerationDelegate handler;

	public override double ExecuteOrder()
	{
		return 0.5;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		if (TerraGenConfig.DoDecorationPass)
		{
			api.Event.ChunkColumnGeneration(handler, EnumWorldGenPass.TerrainFeatures, "standard");
		}
	}
}
