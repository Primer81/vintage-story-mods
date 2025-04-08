using System.Collections.Generic;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.GameContent;

public class EntityPartitionChunk
{
	public List<Entity>[] Entities;

	public List<Entity>[] InanimateEntities;

	public EntityPartitionChunk()
	{
		Entities = new List<Entity>[64];
		InanimateEntities = new List<Entity>[64];
	}

	public List<Entity> Add(Entity e, int gridIndex)
	{
		List<Entity> obj = (e.IsCreature ? FetchOrCreateList(ref Entities[gridIndex]) : FetchOrCreateList(ref InanimateEntities[gridIndex]));
		obj.Add(e);
		return obj;
	}

	private List<Entity> FetchOrCreateList(ref List<Entity> list)
	{
		if (list == null)
		{
			list = new List<Entity>(4);
		}
		return list;
	}
}
