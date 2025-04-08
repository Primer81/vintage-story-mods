using System.Runtime.Serialization;
using Newtonsoft.Json;
using Vintagestory.API.Common;

namespace Vintagestory.ServerMods.NoObf;

[JsonObject(MemberSerialization.OptIn)]
public class TreeVariant
{
	[JsonProperty]
	public AssetLocation Generator;

	[JsonProperty]
	public float Weight;

	[JsonProperty]
	public EnumTreeHabitat Habitat;

	[JsonProperty]
	public float MinSize = 0.2f;

	[JsonProperty]
	public float MaxSize = 1f;

	[JsonProperty]
	public float SuitabilitySizeBonus = 0.5f;

	[JsonProperty]
	public float SaplingDropRate;

	[JsonProperty]
	public float GrowthSpeed;

	[JsonProperty]
	public float JankaHardness;

	[JsonProperty]
	public int MinTemp = -40;

	[JsonProperty]
	public int MaxTemp = 40;

	[JsonProperty]
	public int MinRain;

	[JsonProperty]
	public int MaxRain = 255;

	[JsonProperty]
	public int MinFert;

	[JsonProperty]
	public int MaxFert = 255;

	[JsonProperty]
	public int MinForest;

	[JsonProperty]
	public int MaxForest = 255;

	[JsonProperty]
	public float MinHeight;

	[JsonProperty]
	public float MaxHeight = 1f;

	public int TempMid;

	public float TempRange;

	public int RainMid;

	public float RainRange;

	public int FertMid;

	public float FertRange;

	public int ForestMid;

	public float ForestRange;

	public float HeightMid;

	public float HeightRange;

	[OnDeserialized]
	public void AfterDeserialization(StreamingContext context)
	{
		TempMid = (MinTemp + MaxTemp) / 2;
		TempRange = (MaxTemp - MinTemp) / 2;
		if (TempRange == 0f)
		{
			TempRange = 1f;
		}
		RainMid = (MinRain + MaxRain) / 2;
		RainRange = (MaxRain - MinRain) / 2;
		if (RainRange == 0f)
		{
			RainRange = 1f;
		}
		FertMid = (MinFert + MaxFert) / 2;
		FertRange = (MaxFert - MinFert) / 2;
		if (FertRange == 0f)
		{
			FertRange = 1f;
		}
		ForestMid = (MinForest + MaxForest) / 2;
		ForestRange = (MaxForest - MinForest) / 2;
		if (ForestRange == 0f)
		{
			ForestRange = 1f;
		}
		HeightMid = (MinHeight + MaxHeight) / 2f;
		HeightRange = (MaxHeight - MinHeight) / 2f;
		if (HeightRange == 0f)
		{
			HeightRange = 1f;
		}
	}
}
