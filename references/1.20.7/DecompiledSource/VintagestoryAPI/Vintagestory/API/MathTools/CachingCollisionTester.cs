using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace Vintagestory.API.MathTools;

/// <summary>
/// Originally intended to be a special version of CollisionTester for BehaviorControlledPhysics, which does not re-do the WalkBlocks() call and re-generate the CollisionBoxList more than once in the same entity tick
/// <br />Currently in 1.20 the caching is not very useful when we loop through all entities sequentially - but empirical testing shows it is actually faster not to cache
/// </summary>
public class CachingCollisionTester : CollisionTester
{
	public void NewTick(EntityPos entityPos)
	{
		minPos.Set(int.MinValue, int.MinValue, int.MinValue);
		minPos.dimension = entityPos.Dimension;
		tmpPos.dimension = entityPos.Dimension;
	}

	public void AssignToEntity(PhysicsBehaviorBase entityPhysics, int dimension)
	{
		minPos.dimension = dimension;
		tmpPos.dimension = dimension;
	}

	protected override void GenerateCollisionBoxList(IBlockAccessor blockAccessor, double motionX, double motionY, double motionZ, float stepHeight, float yExtra, int dimension)
	{
		Cuboidd entityBox = base.entityBox;
		bool num = minPos.SetAndEquals((int)(entityBox.X1 + Math.Min(0.0, motionX)), (int)(entityBox.Y1 + Math.Min(0.0, motionY) - (double)yExtra), (int)(entityBox.Z1 + Math.Min(0.0, motionZ)));
		double y2 = Math.Max(entityBox.Y1 + (double)stepHeight, entityBox.Y2);
		bool maxPosIsUnchanged = maxPos.SetAndEquals((int)(entityBox.X2 + Math.Max(0.0, motionX)), (int)(y2 + Math.Max(0.0, motionY)), (int)(entityBox.Z2 + Math.Max(0.0, motionZ)));
		if (num && maxPosIsUnchanged)
		{
			return;
		}
		CollisionBoxList.Clear();
		blockAccessor.WalkBlocks(minPos, maxPos, delegate(Block block, int x, int y, int z)
		{
			Cuboidf[] collisionBoxes = block.GetCollisionBoxes(blockAccessor, tmpPos.Set(x, y, z));
			if (collisionBoxes != null)
			{
				CollisionBoxList.Add(collisionBoxes, x, y, z, block);
			}
		}, centerOrder: true);
	}

	public void PushOutFromBlocks(IBlockAccessor blockAccessor, Entity entity, Vec3d tmpVec, float clippingLimit)
	{
		if (!IsColliding(blockAccessor, entity.CollisionBox, tmpVec, alsoCheckTouch: false))
		{
			return;
		}
		Vec3d pos = entity.SidedPos.XYZ;
		entityBox.SetAndTranslate(entity.CollisionBox, pos.X, pos.Y, pos.Z);
		GenerateCollisionBoxList(blockAccessor, 0.0, 0.0, 0.0, 0.5f, 0f, entity.SidedPos.Dimension);
		int collisionBoxListCount = CollisionBoxList.Count;
		if (collisionBoxListCount == 0)
		{
			return;
		}
		Cuboidd[] CollisionBoxListCuboids = CollisionBoxList.cuboids;
		double deltaX = 0.0;
		double deltaZ = 0.0;
		EnumPushDirection pushDirection = EnumPushDirection.None;
		Cuboidd reducedBox = entity.CollisionBox.ToDouble();
		reducedBox.Translate(pos.X, pos.Y, pos.Z);
		reducedBox.GrowBy(0f - clippingLimit, 0.0, 0f - clippingLimit);
		for (int l = 0; l < CollisionBoxListCuboids.Length && l < collisionBoxListCount; l++)
		{
			deltaX = CollisionBoxListCuboids[l].pushOutX(reducedBox, clippingLimit, ref pushDirection);
		}
		if (deltaX == (double)clippingLimit)
		{
			for (int k = 0; k < CollisionBoxListCuboids.Length && k < collisionBoxListCount; k++)
			{
				deltaX = CollisionBoxListCuboids[k].pushOutX(reducedBox, 0f - clippingLimit, ref pushDirection);
			}
			deltaX += (double)clippingLimit;
		}
		else
		{
			deltaX -= (double)clippingLimit;
		}
		for (int j = 0; j < CollisionBoxListCuboids.Length && j < collisionBoxListCount; j++)
		{
			deltaZ = CollisionBoxListCuboids[j].pushOutZ(reducedBox, clippingLimit, ref pushDirection);
		}
		if (deltaZ == (double)clippingLimit)
		{
			for (int i = 0; i < CollisionBoxListCuboids.Length && i < collisionBoxListCount; i++)
			{
				deltaZ = CollisionBoxListCuboids[i].pushOutZ(reducedBox, 0f - clippingLimit, ref pushDirection);
			}
			deltaZ += (double)clippingLimit;
		}
		else
		{
			deltaZ -= (double)clippingLimit;
		}
		entity.SidedPos.X = pos.X + deltaX;
		entity.SidedPos.Z = pos.Z + deltaZ;
	}
}
