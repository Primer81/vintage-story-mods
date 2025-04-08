using System.Runtime.Serialization;

namespace Vintagestory.API.Common.Entities;

/// <summary>
/// The spawn conditions assigned to various things.
/// </summary>
/// <example>
/// <code language="json">
///             "spawnconditions": {
///             	"worldgen": {
///             		"TriesPerChunk": {
///             			"avg": 0.1,
///             			"var": 0
///             		},
///             		"tryOnlySurface": true,
///             		"minLightLevel": 10,
///             		"groupSize": {
///             			"dist": "verynarrowgaussian",
///             			"avg": 3,
///             			"var": 4
///             		},
///             		"insideBlockCodes": [ "air", "tallgrass-*" ],
///             		"minTemp": 5,
///             		"maxTemp": 28,
///             		"minRain": 0.45,
///             		"minForest": 0.35,
///             		"companions": [ "pig-wild-female", "pig-wild-piglet" ]
///             	},
///             	"runtime": {
///             		"group": "neutral",
///             		"tryOnlySurface": true,
///             		"chance": 0.0006,
///             		"maxQuantity": 4,
///             		"minLightLevel": 10,
///             		"groupSize": {
///             			"dist": "verynarrowgaussian",
///             			"avg": 3,
///             			"var": 4
///             		},
///             		"insideBlockCodes": [ "air", "tallgrass-*" ],
///             		"minTemp": 5,
///             		"maxTemp": 28,
///             		"minRain": 0.45,
///             		"minForestOrShrubs": 0.35,
///             		"companions": [ "pig-wild-female", "pig-wild-piglet" ]
///             	}
///             }
/// </code>
/// </example>
[DocumentAsJson]
public class SpawnConditions
{
	/// <summary>
	/// <jsonoptional>Recommended</jsonoptional><jsondefault>None</jsondefault>
	/// Control specific spawn conditions based on climate.
	/// Note that this will override any climate values set in <see cref="F:Vintagestory.API.Common.Entities.SpawnConditions.Runtime" /> and <see cref="F:Vintagestory.API.Common.Entities.SpawnConditions.Worldgen" />.
	/// It is recommended to specify climate values here rather than setting them in the other spawn conditions.
	/// </summary>
	[DocumentAsJson]
	public ClimateSpawnCondition Climate;

	/// <summary>
	/// <jsonoptional>Recommended</jsonoptional><jsondefault>None</jsondefault>
	/// Runtime requirements for the object to spawn.
	/// </summary>
	[DocumentAsJson]
	public RuntimeSpawnConditions Runtime;

	/// <summary>
	/// <jsonoptional>Recommended</jsonoptional><jsondefault>None</jsondefault>
	/// Worldgen/region requirements for the object to spawn.
	/// </summary>
	[DocumentAsJson]
	public WorldGenSpawnConditions Worldgen;

	public SpawnConditions Clone()
	{
		return new SpawnConditions
		{
			Runtime = Runtime?.Clone(),
			Worldgen = Worldgen?.Clone()
		};
	}

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		if (Climate != null)
		{
			Runtime?.SetFrom(Climate);
			Worldgen?.SetFrom(Climate);
		}
	}
}
