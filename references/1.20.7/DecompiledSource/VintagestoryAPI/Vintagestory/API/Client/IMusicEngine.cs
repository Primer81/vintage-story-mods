using System;
using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

public interface IMusicEngine
{
	/// <summary>
	/// The currently playing track
	/// </summary>
	IMusicTrack CurrentTrack { get; }

	/// <summary>
	/// The track that played before
	/// </summary>
	IMusicTrack LastPlayedTrack { get; }

	/// <summary>
	/// The total passed milliseconds since game start at the point where the last track stopped playing
	/// </summary>
	long MillisecondsSinceLastTrack { get; }

	/// <summary>
	/// Loads the sound into memory and plays the track. Slow call. Encapsulate it into ThreadPool.QueueUserWorkItem() to not block the main thread
	/// </summary>
	/// <param name="location"></param>
	/// <param name="onLoaded"></param>
	/// <param name="volume"></param>
	/// <param name="pitch"></param>
	void LoadTrack(AssetLocation location, Action<ILoadedSound> onLoaded, float volume = 1f, float pitch = 1f);

	void StopTrack(IMusicTrack musicTrack);
}
