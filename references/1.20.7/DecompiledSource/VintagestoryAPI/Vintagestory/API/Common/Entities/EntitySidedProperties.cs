using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Common.Entities;

public abstract class EntitySidedProperties
{
	/// <summary>
	/// The attributes of the entity type.
	/// </summary>
	public ITreeAttribute Attributes;

	/// <summary>
	/// Entity type behaviors
	/// </summary>
	public JsonObject[] BehaviorsAsJsonObj;

	/// <summary>
	/// When this property is attached to an entity - the behaviors attached of entity.
	/// </summary>
	public List<EntityBehavior> Behaviors = new List<EntityBehavior>();

	public EntitySidedProperties(JsonObject[] behaviors, Dictionary<string, JsonObject> commonConfigs)
	{
		BehaviorsAsJsonObj = new JsonObject[behaviors.Length];
		int count = 0;
		foreach (JsonObject jobj in behaviors)
		{
			if (!jobj["enabled"].AsBool(defaultValue: true))
			{
				continue;
			}
			string code = jobj["code"].AsString();
			if (code != null)
			{
				JsonObject mergedobj = jobj;
				if (commonConfigs != null && commonConfigs.ContainsKey(code))
				{
					JObject obj = commonConfigs[code].Token.DeepClone() as JObject;
					obj.Merge(jobj.Token as JObject);
					mergedobj = new JsonObject(obj);
				}
				BehaviorsAsJsonObj[count++] = new JsonObject_ReadOnly(mergedobj);
			}
		}
		if (count < behaviors.Length)
		{
			Array.Resize(ref BehaviorsAsJsonObj, count);
		}
	}

	public void loadBehaviors(Entity entity, EntityProperties properties, IWorldAccessor world)
	{
		if (BehaviorsAsJsonObj == null)
		{
			return;
		}
		Behaviors.Clear();
		for (int i = 0; i < BehaviorsAsJsonObj.Length; i++)
		{
			JsonObject jobj = BehaviorsAsJsonObj[i];
			string code = jobj["code"].AsString();
			if (world.ClassRegistry.GetEntityBehaviorClass(code) != null)
			{
				EntityBehavior behavior = world.ClassRegistry.CreateEntityBehavior(entity, code);
				Behaviors.Add(behavior);
				behavior.FromBytes(isSync: false);
				behavior.Initialize(properties, jobj);
			}
			else
			{
				world.Logger.Notification("Entity behavior {0} for entity {1} not found, will not load it.", code, properties.Code);
			}
		}
	}

	/// <summary>
	/// Use this to make a deep copy of these properties.
	/// </summary>
	/// <returns></returns>
	public abstract EntitySidedProperties Clone();
}
