using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.MathTools;

public class CollisionTester
{
	public CachedCuboidListFaster CollisionBoxList = new CachedCuboidListFaster();

	public Cuboidd entityBox = new Cuboidd();

	public BlockPos tmpPos = new BlockPos();

	public Vec3d tmpPosDelta = new Vec3d();

	public BlockPos minPos = new BlockPos();

	public BlockPos maxPos = new BlockPos();

	public Vec3d pos = new Vec3d();

	private readonly Cuboidd tmpBox = new Cuboidd();

	private readonly BlockPos blockPos = new BlockPos();

	private readonly Vec3d blockPosVec = new Vec3d();

	private readonly BlockPos collBlockPos = new BlockPos();

	/// <summary>
	/// Takes the entity positiona and motion and adds them, respecting any colliding blocks. The resulting new position is put into outposition
	/// </summary>
	/// <param name="entity"></param>
	/// <param name="entityPos"></param>
	/// <param name="dtFactor"></param>
	/// <param name="newPosition"></param>
	/// <param name="stepHeight"></param>
	/// <param name="yExtra">Default 1 for the extra high collision boxes of fences</param>
	public void ApplyTerrainCollision(Entity entity, EntityPos entityPos, float dtFactor, ref Vec3d newPosition, float stepHeight = 1f, float yExtra = 1f)
	{
		minPos.dimension = entityPos.Dimension;
		IWorldAccessor worldAccessor = entity.World;
		Vec3d pos = this.pos;
		Cuboidd entityBox = this.entityBox;
		pos.X = entityPos.X;
		pos.Y = entityPos.Y;
		pos.Z = entityPos.Z;
		EnumPushDirection pushDirection = EnumPushDirection.None;
		entityBox.SetAndTranslate(entity.CollisionBox, pos.X, pos.Y, pos.Z);
		double motionX = entityPos.Motion.X * (double)dtFactor;
		double motionY = entityPos.Motion.Y * (double)dtFactor;
		double motionZ = entityPos.Motion.Z * (double)dtFactor;
		double epsilon = 0.0001;
		double motEpsX = 0.0;
		double motEpsY = 0.0;
		double motEpsZ = 0.0;
		if (motionX > epsilon)
		{
			motEpsX = epsilon;
		}
		if (motionX < 0.0 - epsilon)
		{
			motEpsX = 0.0 - epsilon;
		}
		if (motionY > epsilon)
		{
			motEpsY = epsilon;
		}
		if (motionY < 0.0 - epsilon)
		{
			motEpsY = 0.0 - epsilon;
		}
		if (motionZ > epsilon)
		{
			motEpsZ = epsilon;
		}
		if (motionZ < 0.0 - epsilon)
		{
			motEpsZ = 0.0 - epsilon;
		}
		motionX += motEpsX;
		motionY += motEpsY;
		motionZ += motEpsZ;
		GenerateCollisionBoxList(worldAccessor.BlockAccessor, motionX, motionY, motionZ, stepHeight, yExtra, entityPos.Dimension);
		bool collided = false;
		int collisionBoxListCount = CollisionBoxList.Count;
		Cuboidd[] CollisionBoxListCuboids = CollisionBoxList.cuboids;
		collBlockPos.dimension = entityPos.Dimension;
		for (int k = 0; k < CollisionBoxListCuboids.Length && k < collisionBoxListCount; k++)
		{
			motionY = CollisionBoxListCuboids[k].pushOutY(entityBox, motionY, ref pushDirection);
			if (pushDirection != 0)
			{
				collided = true;
				collBlockPos.Set(CollisionBoxList.positions[k]);
				CollisionBoxList.blocks[k].OnEntityCollide(worldAccessor, entity, collBlockPos, (pushDirection == EnumPushDirection.Negative) ? BlockFacing.UP : BlockFacing.DOWN, tmpPosDelta.Set(motionX, motionY, motionZ), !entity.CollidedVertically);
			}
		}
		entityBox.Translate(0.0, motionY, 0.0);
		entity.CollidedVertically = collided;
		bool horizontallyBlocked = false;
		entityBox.Translate(motionX, 0.0, motionZ);
		foreach (Cuboidd collisionBox in CollisionBoxList)
		{
			if (collisionBox.Intersects(entityBox))
			{
				horizontallyBlocked = true;
				break;
			}
		}
		entityBox.Translate(0.0 - motionX, 0.0, 0.0 - motionZ);
		collided = false;
		if (horizontallyBlocked)
		{
			for (int j = 0; j < CollisionBoxListCuboids.Length && j < collisionBoxListCount; j++)
			{
				motionX = CollisionBoxListCuboids[j].pushOutX(entityBox, motionX, ref pushDirection);
				if (pushDirection != 0)
				{
					collided = true;
					collBlockPos.Set(CollisionBoxList.positions[j]);
					CollisionBoxList.blocks[j].OnEntityCollide(worldAccessor, entity, collBlockPos, (pushDirection == EnumPushDirection.Negative) ? BlockFacing.EAST : BlockFacing.WEST, tmpPosDelta.Set(motionX, motionY, motionZ), !entity.CollidedHorizontally);
				}
			}
			entityBox.Translate(motionX, 0.0, 0.0);
			for (int i = 0; i < CollisionBoxListCuboids.Length && i < collisionBoxListCount; i++)
			{
				motionZ = CollisionBoxListCuboids[i].pushOutZ(entityBox, motionZ, ref pushDirection);
				if (pushDirection != 0)
				{
					collided = true;
					collBlockPos.Set(CollisionBoxList.positions[i]);
					CollisionBoxList.blocks[i].OnEntityCollide(worldAccessor, entity, collBlockPos, (pushDirection == EnumPushDirection.Negative) ? BlockFacing.SOUTH : BlockFacing.NORTH, tmpPosDelta.Set(motionX, motionY, motionZ), !entity.CollidedHorizontally);
				}
			}
		}
		entity.CollidedHorizontally = collided;
		if (motionY > 0.0 && entity.CollidedVertically)
		{
			motionY -= entity.LadderFixDelta;
		}
		motionX -= motEpsX;
		motionY -= motEpsY;
		motionZ -= motEpsZ;
		newPosition.Set(pos.X + motionX, pos.Y + motionY, pos.Z + motionZ);
	}

	protected virtual void GenerateCollisionBoxList(IBlockAccessor blockAccessor, double motionX, double motionY, double motionZ, float stepHeight, float yExtra, int dimension)
	{
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

	/// <summary>
	/// Tests given cuboidf collides with the terrain. By default also checks if the cuboid is merely touching the terrain, set alsoCheckTouch to disable that.
	/// </summary>
	/// <param name="blockAccessor"></param>
	/// <param name="entityBoxRel"></param>
	/// <param name="pos"></param>
	/// <param name="alsoCheckTouch"></param>
	/// <returns></returns>
	public bool IsColliding(IBlockAccessor blockAccessor, Cuboidf entityBoxRel, Vec3d pos, bool alsoCheckTouch = true)
	{
		return GetCollidingBlock(blockAccessor, entityBoxRel, pos, alsoCheckTouch) != null;
	}

	public Block GetCollidingBlock(IBlockAccessor blockAccessor, Cuboidf entityBoxRel, Vec3d pos, bool alsoCheckTouch = true)
	{
		Cuboidd entityBox = tmpBox.SetAndTranslate(entityBoxRel, pos);
		int minX = (int)entityBox.X1;
		int num = (int)entityBox.Y1 - 1;
		int minZ = (int)entityBox.Z1;
		int maxX = (int)entityBox.X2;
		int maxY = (int)entityBox.Y2;
		int maxZ = (int)entityBox.Z2;
		entityBox.Y1 = Math.Round(entityBox.Y1, 5);
		BlockPos blockPos = this.blockPos;
		Vec3d blockPosVec = this.blockPosVec;
		for (int y = num; y <= maxY; y++)
		{
			blockPos.SetAndCorrectDimension(minX, y, minZ);
			blockPosVec.Set(minX, y, minZ);
			for (int x = minX; x <= maxX; x++)
			{
				blockPos.X = x;
				blockPosVec.X = x;
				for (int z = minZ; z <= maxZ; z++)
				{
					blockPos.Z = z;
					Block block = blockAccessor.GetBlock(blockPos, 4);
					Cuboidf[] collisionBoxes = block.GetCollisionBoxes(blockAccessor, blockPos);
					if (collisionBoxes == null || collisionBoxes.Length == 0)
					{
						continue;
					}
					blockPosVec.Z = z;
					foreach (Cuboidf collBox in collisionBoxes)
					{
						if (collBox != null && (alsoCheckTouch ? entityBox.IntersectsOrTouches(collBox, blockPosVec) : entityBox.Intersects(collBox, blockPosVec)))
						{
							return block;
						}
					}
				}
			}
		}
		return null;
	}

	/// <summary>
	/// If given cuboidf collides with the terrain, returns the collision box it collides with. By default also checks if the cuboid is merely touching the terrain, set alsoCheckTouch to disable that.
	/// </summary>
	/// <param name="blockAccessor"></param>
	/// <param name="entityBoxRel"></param>
	/// <param name="pos"></param>
	/// <param name="alsoCheckTouch"></param>
	/// <returns></returns>
	public Cuboidd GetCollidingCollisionBox(IBlockAccessor blockAccessor, Cuboidf entityBoxRel, Vec3d pos, bool alsoCheckTouch = true)
	{
		BlockPos blockPos = new BlockPos();
		Vec3d blockPosVec = new Vec3d();
		Cuboidd entityBox = entityBoxRel.ToDouble().Translate(pos);
		entityBox.Y1 = Math.Round(entityBox.Y1, 5);
		int minX = (int)((double)entityBoxRel.X1 + pos.X);
		int num = (int)((double)entityBoxRel.Y1 + pos.Y - 1.0);
		int minZ = (int)((double)entityBoxRel.Z1 + pos.Z);
		int maxX = (int)Math.Ceiling((double)entityBoxRel.X2 + pos.X);
		int maxY = (int)Math.Ceiling((double)entityBoxRel.Y2 + pos.Y);
		int maxZ = (int)Math.Ceiling((double)entityBoxRel.Z2 + pos.Z);
		for (int y = num; y <= maxY; y++)
		{
			blockPos.Set(minX, y, minZ);
			blockPosVec.Set(minX, y, minZ);
			for (int x = minX; x <= maxX; x++)
			{
				blockPos.X = x;
				blockPosVec.X = x;
				for (int z = minZ; z <= maxZ; z++)
				{
					blockPos.Z = z;
					Cuboidf[] collisionBoxes = blockAccessor.GetMostSolidBlock(x, y, z).GetCollisionBoxes(blockAccessor, blockPos);
					if (collisionBoxes == null)
					{
						continue;
					}
					blockPosVec.Z = z;
					foreach (Cuboidf collBox in collisionBoxes)
					{
						if (collBox != null && (alsoCheckTouch ? entityBox.IntersectsOrTouches(collBox, blockPosVec) : entityBox.Intersects(collBox, blockPosVec)))
						{
							return collBox.ToDouble().Translate(blockPos);
						}
					}
				}
			}
		}
		return null;
	}

	/// <summary>
	/// Tests given cuboidf collides with the terrain. By default also checks if the cuboid is merely touching the terrain, set alsoCheckTouch to disable that.
	/// <br />NOTE: currently not dimension-aware unless the supplied Vec3d pos is dimension-aware
	/// </summary>
	/// <param name="blockAccessor"></param>
	/// <param name="entityBoxRel"></param>
	/// <param name="pos"></param>
	/// <param name="intoCubuid"></param>
	/// <param name="alsoCheckTouch"></param>
	/// <returns></returns>
	public bool GetCollidingCollisionBox(IBlockAccessor blockAccessor, Cuboidf entityBoxRel, Vec3d pos, ref Cuboidd intoCuboid, bool alsoCheckTouch = true)
	{
		BlockPos blockPos = new BlockPos();
		Vec3d blockPosVec = new Vec3d();
		Cuboidd entityBox = entityBoxRel.ToDouble().Translate(pos);
		entityBox.Y1 = Math.Round(entityBox.Y1, 5);
		int minX = (int)((double)entityBoxRel.X1 + pos.X);
		int num = (int)((double)entityBoxRel.Y1 + pos.Y - 1.0);
		int minZ = (int)((double)entityBoxRel.Z1 + pos.Z);
		int maxX = (int)Math.Ceiling((double)entityBoxRel.X2 + pos.X);
		int maxY = (int)Math.Ceiling((double)entityBoxRel.Y2 + pos.Y);
		int maxZ = (int)Math.Ceiling((double)entityBoxRel.Z2 + pos.Z);
		for (int y = num; y <= maxY; y++)
		{
			for (int x = minX; x <= maxX; x++)
			{
				blockPos.Set(x, y, minZ);
				blockPosVec.Set(x, y, minZ);
				for (int z = minZ; z <= maxZ; z++)
				{
					blockPos.Z = z;
					Cuboidf[] collisionBoxes = blockAccessor.GetBlock(x, y, z, 4).GetCollisionBoxes(blockAccessor, blockPos);
					if (collisionBoxes == null)
					{
						continue;
					}
					blockPosVec.Z = z;
					foreach (Cuboidf collBox in collisionBoxes)
					{
						if (collBox != null && (alsoCheckTouch ? entityBox.IntersectsOrTouches(collBox, blockPosVec) : entityBox.Intersects(collBox, blockPosVec)))
						{
							intoCuboid.Set(collBox).Translate(blockPos);
							return true;
						}
					}
				}
			}
		}
		return false;
	}

	public static bool AabbIntersect(Cuboidf aabb, double x, double y, double z, Cuboidf aabb2, Vec3d pos)
	{
		if (aabb2 == null)
		{
			return true;
		}
		if (x + (double)aabb.X1 < (double)aabb2.X2 + pos.X && x + (double)aabb.X2 > (double)aabb2.X1 + pos.X && z + (double)aabb.Z1 < (double)aabb2.Z2 + pos.Z && z + (double)aabb.Z2 > (double)aabb2.Z1 + pos.Z && y + (double)aabb.Y1 < (double)aabb2.Y2 + pos.Y)
		{
			return y + (double)aabb.Y2 > (double)aabb2.Y1 + pos.Y;
		}
		return false;
	}

	public static EnumIntersect AabbIntersect(Cuboidd aabb, Cuboidd aabb2, Vec3d motion)
	{
		if (aabb.Intersects(aabb2))
		{
			return EnumIntersect.Stuck;
		}
		if (aabb.X1 < aabb2.X2 + motion.X && aabb.X2 > aabb2.X1 + motion.X && aabb.Z1 < aabb2.Z2 && aabb.Z2 > aabb2.Z1 && aabb.Y1 < aabb2.Y2 && aabb.Y2 > aabb2.Y1)
		{
			return EnumIntersect.IntersectX;
		}
		if (aabb.X1 < aabb2.X2 && aabb.X2 > aabb2.X1 && aabb.Z1 < aabb2.Z2 && aabb.Z2 > aabb2.Z1 && aabb.Y1 < aabb2.Y2 + motion.Y && aabb.Y2 > aabb2.Y1 + motion.Y)
		{
			return EnumIntersect.IntersectY;
		}
		if (aabb.X1 < aabb2.X2 && aabb.X2 > aabb2.X1 && aabb.Z1 < aabb2.Z2 + motion.Z && aabb.Z2 > aabb2.Z1 + motion.Z && aabb.Y1 < aabb2.Y2 && aabb.Y2 > aabb2.Y1)
		{
			return EnumIntersect.IntersectZ;
		}
		return EnumIntersect.NoIntersect;
	}
}
