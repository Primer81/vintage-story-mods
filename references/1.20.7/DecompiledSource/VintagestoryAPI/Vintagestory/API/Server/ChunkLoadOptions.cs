using System;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Server;

public class ChunkLoadOptions
{
	/// <summary>
	/// If true, the chunk will never get unloaded unless UnloadChunkColumn() is called
	/// </summary>
	public bool KeepLoaded;

	/// <summary>
	/// Callback for when the chunks are ready and loaded
	/// </summary>
	public Action OnLoaded;

	/// <summary>
	/// Additional config to pass onto the world generators
	/// </summary>
	public ITreeAttribute ChunkGenParams = new TreeAttribute();
}
