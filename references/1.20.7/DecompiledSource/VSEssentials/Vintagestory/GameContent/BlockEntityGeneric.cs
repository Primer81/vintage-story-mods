using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class BlockEntityGeneric : BlockEntity, IRotatable
{
	public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
	{
		foreach (BlockEntityBehavior behavior in Behaviors)
		{
			if (behavior is IRotatable bhrot)
			{
				bhrot.OnTransformed(worldAccessor, tree, degreeRotation, oldBlockIdMapping, oldItemIdMapping, flipAxis);
			}
		}
	}
}
