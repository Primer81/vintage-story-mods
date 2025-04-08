namespace Vintagestory.API.Common;

public interface IClientChunk : IWorldChunk
{
	/// <summary>
	/// True if fully initialized
	/// </summary>
	bool LoadedFromServer { get; }

	/// <summary>
	/// Can be used to set a chunk as invisible, probably temporarily (e.g. for the Timeswitch system)
	/// </summary>
	/// <param name="visible"></param>
	void SetVisibility(bool visible);
}
