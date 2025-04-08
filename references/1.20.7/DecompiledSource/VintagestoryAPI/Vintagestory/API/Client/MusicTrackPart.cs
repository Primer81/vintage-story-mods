using System.Collections.Generic;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

[JsonObject(MemberSerialization.OptIn)]
public class MusicTrackPart
{
	/// <summary>
	/// The minimum Suitability of the given track
	/// </summary>
	[JsonProperty]
	public float MinSuitability = 0.1f;

	/// <summary>
	/// The maximum Suitability of the given track
	/// </summary>
	[JsonProperty]
	public float MaxSuitability = 1f;

	/// <summary>
	/// The minimum volume of a given track.
	/// </summary>
	[JsonProperty]
	public float MinVolumne = 0.35f;

	/// <summary>
	/// The maximum volume of a given track.
	/// </summary>
	[JsonProperty]
	public float MaxVolumne = 1f;

	/// <summary>
	/// the Y position.
	/// </summary>
	[JsonProperty]
	public float[] PosY;

	[JsonProperty]
	public float[] Sunlight;

	/// <summary>
	/// The files for the part.
	/// </summary>
	[JsonProperty]
	public AssetLocation[] Files;

	/// <summary>
	/// The loaded sound
	/// </summary>
	public ILoadedSound Sound;

	/// <summary>
	/// Start time in Miliseconds
	/// </summary>
	public long StartedMs;

	/// <summary>
	/// Am I loading?
	/// </summary>
	public bool Loading;

	internal AssetLocation NowPlayingFile;

	/// <summary>
	/// Am I playing?
	/// </summary>
	public bool IsPlaying
	{
		get
		{
			if (Sound != null)
			{
				return Sound.IsPlaying;
			}
			return false;
		}
	}

	/// <summary>
	/// Am I applicable?
	/// </summary>
	/// <param name="world">world information</param>
	/// <param name="props">the properties of the current track.</param>
	/// <returns></returns>
	public bool Applicable(IWorldAccessor world, TrackedPlayerProperties props)
	{
		return CurrentSuitability(world, props) > MinSuitability;
	}

	/// <summary>
	/// The current volume of the track.
	/// </summary>
	/// <param name="world">world information</param>
	/// <param name="props">the properties of the current track.</param>
	/// <returns></returns>
	public float CurrentVolume(IWorldAccessor world, TrackedPlayerProperties props)
	{
		float x = CurrentSuitability(world, props);
		if (x == 1f)
		{
			return 1f;
		}
		float i = (MaxVolumne - MinVolumne) / (MaxSuitability - MinSuitability);
		float d = MinVolumne - i * MinSuitability;
		if (x < MinSuitability)
		{
			return 0f;
		}
		return GameMath.Min(i * x + d, MaxVolumne);
	}

	/// <summary>
	/// The current Suitability of the track.
	/// </summary>
	/// <param name="world">world information</param>
	/// <param name="props">the properties of the current track.</param>
	/// <returns></returns>
	public float CurrentSuitability(IWorldAccessor world, TrackedPlayerProperties props)
	{
		int applied = 0;
		float suitability = 0f;
		if (PosY != null)
		{
			suitability += GameMath.TriangleStep(props.posY, PosY[0], PosY[1]);
			applied++;
		}
		if (Sunlight != null)
		{
			suitability += GameMath.TriangleStep(props.sunSlight, Sunlight[0], Sunlight[1]);
			applied++;
		}
		if (applied == 0)
		{
			return 1f;
		}
		return suitability / (float)applied;
	}

	/// <summary>
	/// Expands the target files.
	/// </summary>
	/// <param name="assetManager">The current AssetManager instance.</param>
	public virtual void ExpandFiles(IAssetManager assetManager)
	{
		List<AssetLocation> expandedFiles = new List<AssetLocation>();
		for (int i = 0; i < Files.Length; i++)
		{
			AssetLocation fileLocation = Files[i];
			if (fileLocation.Path.EndsWith('*'))
			{
				foreach (AssetLocation location in assetManager.GetLocations("music/" + fileLocation.Path.Substring(0, fileLocation.Path.Length - 1), fileLocation.Domain))
				{
					expandedFiles.Add(location);
				}
			}
			else
			{
				expandedFiles.Add(fileLocation);
			}
		}
		Files = expandedFiles.ToArray();
	}
}
