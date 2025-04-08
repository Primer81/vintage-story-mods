using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.Common.Database;

namespace Vintagestory.Client.NoObf;

public class TerrainIlluminator : IChunkProvider
{
	private ChunkIlluminator chunkIlluminator;

	private ClientMain game;

	public ILogger Logger => game.Logger;

	public TerrainIlluminator(ClientMain game)
	{
		this.game = game;
		chunkIlluminator = new ChunkIlluminator(this, new BlockAccessorRelaxed(game.WorldMap, game, synchronize: false, relight: false), game.WorldMap.ClientChunkSize);
	}

	public void OnBlockTexturesLoaded()
	{
		chunkIlluminator.InitForWorld(game.Blocks, (ushort)game.WorldMap.SunBrightness, game.WorldMap.MapSizeX, game.WorldMap.MapSizeY, game.WorldMap.MapSizeZ);
	}

	internal void SunRelightChunk(ClientChunk chunk, long index3d)
	{
		ChunkPos pos = game.WorldMap.ChunkPosFromChunkIndex3D(index3d);
		SunRelightChunk(chunk, pos.X, pos.Y, pos.Z);
	}

	public void SunRelightChunk(ClientChunk chunk, int chunkX, int chunkY, int chunkZ)
	{
		ClientChunk[] chunks = new ClientChunk[game.WorldMap.ChunkMapSizeY];
		for (int y = 0; y < game.WorldMap.ChunkMapSizeY; y++)
		{
			chunks[y] = game.WorldMap.GetClientChunk(chunkX, y, chunkZ);
			chunks[y].shouldSunRelight = false;
			chunks[y].quantityRelit++;
			chunks[y].Unpack();
		}
		chunk.Lighting.ClearAllSunlight();
		ChunkIlluminator obj = chunkIlluminator;
		IWorldChunk[] chunks2 = chunks;
		obj.Sunlight(chunks2, chunkX, chunkY, chunkZ, 0);
		ChunkIlluminator obj2 = chunkIlluminator;
		chunks2 = chunks;
		obj2.SunlightFlood(chunks2, chunkX, chunkY, chunkZ);
		ChunkIlluminator obj3 = chunkIlluminator;
		chunks2 = chunks;
		byte spreadFaces = obj3.SunLightFloodNeighbourChunks(chunks2, chunkX, chunkY, chunkZ, 0);
		BlockFacing[] aLLFACES = BlockFacing.ALLFACES;
		foreach (BlockFacing face in aLLFACES)
		{
			if ((face.Flag & spreadFaces) > 0)
			{
				int neibCx = chunkX + face.Normali.X;
				int neibCy = chunkY + face.Normali.Y;
				int neibCz = chunkZ + face.Normali.Z;
				game.WorldMap.MarkChunkDirty(neibCx, neibCy, neibCz, priority: true);
			}
		}
	}

	public IWorldChunk GetChunk(int chunkX, int chunkY, int chunkZ)
	{
		ClientChunk chunk = game.WorldMap.GetClientChunk(chunkX, chunkY, chunkZ);
		chunk?.Unpack();
		return chunk;
	}

	public IWorldChunk GetUnpackedChunkFast(int chunkX, int chunkY, int chunkZ, bool notRecentlyAccessed = false)
	{
		return ((IChunkProvider)game.WorldMap).GetUnpackedChunkFast(chunkX, chunkY, chunkZ, notRecentlyAccessed);
	}

	public long ChunkIndex3D(int chunkX, int chunkY, int chunkZ)
	{
		return ((long)chunkY * (long)game.WorldMap.index3dMulZ + chunkZ) * game.WorldMap.index3dMulX + chunkX;
	}

	public long ChunkIndex3D(EntityPos pos)
	{
		return game.WorldMap.ChunkIndex3D(pos);
	}
}
