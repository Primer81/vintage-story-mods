using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.Server;

public class WorldGenHandler : IWorldGenHandler
{
	public List<Action> OnInitWorldGen = new List<Action>();

	public List<MapRegionGeneratorDelegate> OnMapRegionGen = new List<MapRegionGeneratorDelegate>();

	public List<MapChunkGeneratorDelegate> OnMapChunkGen = new List<MapChunkGeneratorDelegate>();

	public List<ChunkColumnGenerationDelegate>[] OnChunkColumnGen = new List<ChunkColumnGenerationDelegate>[6];

	public Dictionary<string, WorldGenHookDelegate> SpecialHooks = new Dictionary<string, WorldGenHookDelegate>();

	List<MapRegionGeneratorDelegate> IWorldGenHandler.OnMapRegionGen => OnMapRegionGen;

	List<MapChunkGeneratorDelegate> IWorldGenHandler.OnMapChunkGen => OnMapChunkGen;

	List<ChunkColumnGenerationDelegate>[] IWorldGenHandler.OnChunkColumnGen => OnChunkColumnGen;

	public WorldGenHandler()
	{
		OnChunkColumnGen[1] = new List<ChunkColumnGenerationDelegate>();
		OnChunkColumnGen[2] = new List<ChunkColumnGenerationDelegate>();
		OnChunkColumnGen[3] = new List<ChunkColumnGenerationDelegate>();
		OnChunkColumnGen[4] = new List<ChunkColumnGenerationDelegate>();
		OnChunkColumnGen[5] = new List<ChunkColumnGenerationDelegate>();
	}

	public void WipeAllHandlers()
	{
		OnMapRegionGen.Clear();
		OnMapChunkGen.Clear();
		for (int i = 0; i < OnChunkColumnGen.Length; i++)
		{
			if (OnChunkColumnGen[i] != null)
			{
				OnChunkColumnGen[i].Clear();
			}
		}
	}
}
