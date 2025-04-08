using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class GenLightCreative : ModSystem
{
	private ICoreServerAPI api;

	private IWorldGenBlockAccessor blockAccessor;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return side == EnumAppSide.Server;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		this.api.Event.ChunkColumnGeneration(OnChunkColumnGeneration, EnumWorldGenPass.Vegetation, "superflat");
		this.api.Event.ChunkColumnGeneration(OnChunkColumnGenerationFlood, EnumWorldGenPass.NeighbourSunLightFlood, "superflat");
		this.api.Event.GetWorldgenBlockAccessor(OnWorldGenBlockAccessor);
	}

	private void OnWorldGenBlockAccessor(IChunkProviderThread chunkProvider)
	{
		blockAccessor = chunkProvider.GetBlockAccessor(updateHeightmap: false);
	}

	public override double ExecuteOrder()
	{
		return 0.95;
	}

	private void OnChunkColumnGeneration(IChunkColumnGenerateRequest request)
	{
		blockAccessor.BeginColumn();
		IWorldManagerAPI worldManager = api.WorldManager;
		IWorldChunk[] chunks = request.Chunks;
		worldManager.SunFloodChunkColumnForWorldGen(chunks, request.ChunkX, request.ChunkZ);
		blockAccessor.RunScheduledBlockLightUpdates(request.ChunkX, request.ChunkZ);
	}

	private void OnChunkColumnGenerationFlood(IChunkColumnGenerateRequest request)
	{
		IWorldManagerAPI worldManager = api.WorldManager;
		IWorldChunk[] chunks = request.Chunks;
		worldManager.SunFloodChunkColumnNeighboursForWorldGen(chunks, request.ChunkX, request.ChunkZ);
	}
}
