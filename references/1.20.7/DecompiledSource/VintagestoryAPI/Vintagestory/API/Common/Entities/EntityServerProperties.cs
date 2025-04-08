using System.Collections.Generic;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common.Entities;

public class EntityServerProperties : EntitySidedProperties
{
	/// <summary>
	/// The conditions for spawning the entity.
	/// </summary>
	public SpawnConditions SpawnConditions;

	public EntityServerProperties(JsonObject[] behaviors, Dictionary<string, JsonObject> commonConfigs)
		: base(behaviors, commonConfigs)
	{
	}

	/// <summary>
	/// Makes a copy of this EntiyServerProperties type
	/// </summary>
	/// <returns></returns>
	public override EntitySidedProperties Clone()
	{
		return new EntityServerProperties(BehaviorsAsJsonObj, null)
		{
			Attributes = Attributes,
			SpawnConditions = SpawnConditions
		};
	}
}
