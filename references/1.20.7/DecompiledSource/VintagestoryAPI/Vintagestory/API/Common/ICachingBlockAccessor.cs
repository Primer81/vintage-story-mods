namespace Vintagestory.API.Common;

public interface ICachingBlockAccessor : IBlockAccessor
{
	/// <summary>
	/// True if the most recent GetBlock or SetBlock had a laoded chunk
	/// </summary>
	bool LastChunkLoaded { get; }

	void Begin();

	void Dispose();
}
