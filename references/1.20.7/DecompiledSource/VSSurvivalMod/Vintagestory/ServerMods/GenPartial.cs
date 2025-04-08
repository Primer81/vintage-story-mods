using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public abstract class GenPartial : ModStdWorldGen
{
	protected ICoreServerAPI api;

	protected int worldheight;

	public int airBlockId;

	protected LCGRandom chunkRand;

	protected abstract int chunkRange { get; }

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		api.Event.InitWorldGenerator(initWorldGen, "standard");
	}

	public virtual void initWorldGen()
	{
		LoadGlobalConfig(api);
		worldheight = api.WorldManager.MapSizeY;
		chunkRand = new LCGRandom(api.WorldManager.Seed);
	}

	protected virtual void GenChunkColumn(IChunkColumnGenerateRequest request)
	{
		IServerChunk[] chunks = request.Chunks;
		int chunkX = request.ChunkX;
		int chunkZ = request.ChunkZ;
		for (int dx = -chunkRange; dx <= chunkRange; dx++)
		{
			for (int dz = -chunkRange; dz <= chunkRange; dz++)
			{
				chunkRand.InitPositionSeed(chunkX + dx, chunkZ + dz);
				GeneratePartial(chunks, chunkX, chunkZ, dx, dz);
			}
		}
	}

	public virtual void GeneratePartial(IServerChunk[] chunks, int chunkX, int chunkZ, int basePosX, int basePosZ)
	{
	}
}
