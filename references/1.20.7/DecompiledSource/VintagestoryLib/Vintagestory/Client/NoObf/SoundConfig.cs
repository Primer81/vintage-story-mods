using System.Collections.Generic;
using System.Runtime.Serialization;
using Vintagestory.API.Common;

namespace Vintagestory.Client.NoObf;

public class SoundConfig
{
	public Dictionary<AssetLocation, AssetLocation[]> Soundsets = new Dictionary<AssetLocation, AssetLocation[]>();

	public BlockSounds defaultBlockSounds = new BlockSounds();

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		Dictionary<AssetLocation, AssetLocation[]> fixedSoundSets = new Dictionary<AssetLocation, AssetLocation[]>();
		foreach (KeyValuePair<AssetLocation, AssetLocation[]> val in Soundsets)
		{
			val.Key.WithPathPrefix("sounds/");
			for (int i = 0; i < val.Value.Length; i++)
			{
				val.Value[i].WithPathPrefix("sounds/");
			}
			fixedSoundSets[val.Key] = val.Value;
		}
		Soundsets = fixedSoundSets;
	}
}
