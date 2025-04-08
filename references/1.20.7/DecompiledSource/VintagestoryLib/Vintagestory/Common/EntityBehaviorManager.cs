using System;
using System.Collections.Generic;

namespace Vintagestory.Common;

public class EntityBehaviorManager
{
	internal Dictionary<Type, List<Type>> EntityBehaviors = new Dictionary<Type, List<Type>>();

	internal Type[] Get(Type entityType)
	{
		List<Type> types = null;
		EntityBehaviors.TryGetValue(entityType, out types);
		return types?.ToArray();
	}

	internal void Add(Type entityType, params Type[] propertyTypes)
	{
		List<Type> types = null;
		EntityBehaviors.TryGetValue(entityType, out types);
		if (types == null)
		{
			types = new List<Type>();
			EntityBehaviors.Add(entityType, types);
		}
		for (int i = 0; i < propertyTypes.Length; i++)
		{
			types.Add(propertyTypes[i]);
		}
	}
}
