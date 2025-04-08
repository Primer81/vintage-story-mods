using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Server;

public class ChunkPeekOptions
{
	/// <summary>
	/// Until which world gen pass to generate the chunk (default: Done)
	/// </summary>
	public EnumWorldGenPass UntilPass = EnumWorldGenPass.Done;

	/// <summary>
	/// Callback for when the chunks are ready and loaded
	/// </summary>
	public OnChunkPeekedDelegate OnGenerated;

	/// <summary>
	/// Additional config to pass onto the world generators
	/// </summary>
	public ITreeAttribute ChunkGenParams = new TreeAttribute();
}
