using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.API.MathTools;

public class MultiCollisionTester
{
	public CachedCuboidList CollisionBoxList = new CachedCuboidList();

	public Cuboidd[] entityBox = ArrayUtil.CreateFilled(10, (int i) => new Cuboidd());

	protected int count;

	private Cuboidf[] collBox = new Cuboidf[10];

	public BlockPos tmpPos = new BlockPos();

	public Vec3d tmpPosDelta = new Vec3d();

	public BlockPos minPos = new BlockPos();

	public BlockPos maxPos = new BlockPos();

	public Vec3d pos = new Vec3d();

	private readonly Cuboidd tmpBox = new Cuboidd();

	private readonly BlockPos blockPos = new BlockPos();

	private readonly Vec3d blockPosVec = new Vec3d();

	public void ApplyTerrainCollision(Cuboidf[] collisionBoxes, int collBoxCount, Entity entity, EntityPos entityPos, float dtFactor, ref Vec3d newPosition, float stepHeight = 1f, float yExtra = 1f)
	{
		count = collBoxCount;
		IWorldAccessor world = entity.World;
		pos.X = entityPos.X;
		pos.Y = entityPos.Y;
		pos.Z = entityPos.Z;
		EnumPushDirection pushDirection = EnumPushDirection.None;
		for (int l = 0; l < collBoxCount; l++)
		{
			entityBox[l].SetAndTranslate(collisionBoxes[l], pos.X, pos.Y, pos.Z);
		}
		double motionX = entityPos.Motion.X * (double)dtFactor;
		double motionY = entityPos.Motion.Y * (double)dtFactor;
		double motionZ = entityPos.Motion.Z * (double)dtFactor;
		GenerateCollisionBoxList(world.BlockAccessor, motionX, motionY, motionZ, stepHeight, yExtra);
		bool collided = false;
		int collisionBoxListCount = CollisionBoxList.Count;
		for (int k = 0; k < collisionBoxListCount; k++)
		{
			for (int j7 = 0; j7 < collBoxCount; j7++)
			{
				motionY = CollisionBoxList.cuboids[k].pushOutY(entityBox[j7], motionY, ref pushDirection);
				if (pushDirection != 0)
				{
					CollisionBoxList.blocks[k].OnEntityCollide(world, entity, CollisionBoxList.positions[k], (pushDirection == EnumPushDirection.Negative) ? BlockFacing.UP : BlockFacing.DOWN, tmpPosDelta.Set(motionX, motionY, motionZ), !entity.CollidedVertically);
					collided = true;
				}
			}
		}
		for (int j6 = 0; j6 < collBoxCount; j6++)
		{
			entityBox[j6].Translate(0.0, motionY, 0.0);
		}
		entity.CollidedVertically = collided;
		bool horizontallyBlocked = false;
		for (int j5 = 0; j5 < collBoxCount; j5++)
		{
			entityBox[j5].Translate(motionX, 0.0, motionZ);
		}
		foreach (Cuboidd cuboid in CollisionBoxList)
		{
			bool blocked = false;
			for (int j4 = 0; j4 < collBoxCount; j4++)
			{
				if (cuboid.Intersects(entityBox[j4]))
				{
					horizontallyBlocked = true;
					blocked = true;
					break;
				}
			}
			if (blocked)
			{
				break;
			}
		}
		for (int j3 = 0; j3 < collBoxCount; j3++)
		{
			entityBox[j3].Translate(0.0 - motionX, 0.0, 0.0 - motionZ);
		}
		collided = false;
		if (horizontallyBlocked)
		{
			for (int j = 0; j < collisionBoxListCount; j++)
			{
				bool pushed2 = false;
				for (int j2 = 0; j2 < collBoxCount; j2++)
				{
					motionX = CollisionBoxList.cuboids[j].pushOutX(entityBox[j2], motionX, ref pushDirection);
					if (pushDirection != 0)
					{
						CollisionBoxList.blocks[j].OnEntityCollide(world, entity, CollisionBoxList.positions[j], (pushDirection == EnumPushDirection.Negative) ? BlockFacing.EAST : BlockFacing.WEST, tmpPosDelta.Set(motionX, motionY, motionZ), !entity.CollidedHorizontally);
					}
					pushed2 = pushed2 || pushDirection != EnumPushDirection.None;
				}
				collided = pushed2;
			}
			for (int n = 0; n < collBoxCount; n++)
			{
				entityBox[n].Translate(motionX, 0.0, 0.0);
			}
			for (int i = 0; i < collisionBoxListCount; i++)
			{
				bool pushed = false;
				for (int m = 0; m < collBoxCount; m++)
				{
					motionZ = CollisionBoxList.cuboids[i].pushOutZ(entityBox[m], motionZ, ref pushDirection);
					if (pushDirection != 0)
					{
						CollisionBoxList.blocks[i].OnEntityCollide(world, entity, CollisionBoxList.positions[i], (pushDirection == EnumPushDirection.Negative) ? BlockFacing.SOUTH : BlockFacing.NORTH, tmpPosDelta.Set(motionX, motionY, motionZ), !entity.CollidedHorizontally);
					}
					pushed = pushed || pushDirection != EnumPushDirection.None;
				}
				collided = pushed;
			}
		}
		entity.CollidedHorizontally = collided;
		newPosition.Set(pos.X + motionX, pos.Y + motionY, pos.Z + motionZ);
	}

	protected virtual void GenerateCollisionBoxList(IBlockAccessor blockAccessor, double motionX, double motionY, double motionZ, float stepHeight, float yExtra)
	{
		double minx = double.MaxValue;
		double miny = double.MaxValue;
		double minz = double.MaxValue;
		double maxx = double.MinValue;
		double maxy = double.MinValue;
		double maxz = double.MinValue;
		for (int i = 0; i < count; i++)
		{
			Cuboidd ebox = entityBox[i];
			minx = Math.Min(minx, ebox.X1);
			miny = Math.Min(miny, ebox.Y1);
			minz = Math.Min(minz, ebox.Z1);
			maxx = Math.Max(maxx, ebox.X2);
			maxy = Math.Max(maxy, ebox.Y2);
			maxz = Math.Max(maxz, ebox.Z2);
		}
		minPos.Set((int)(minx + Math.Min(0.0, motionX)), (int)(miny + Math.Min(0.0, motionY) - (double)yExtra), (int)(minz + Math.Min(0.0, motionZ)));
		double y2 = Math.Max(miny + (double)stepHeight, maxy);
		maxPos.Set((int)(maxx + Math.Max(0.0, motionX)), (int)(y2 + Math.Max(0.0, motionY)), (int)(maxz + Math.Max(0.0, motionZ)));
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
	/// If given cuboidf collides with the terrain, returns the collision box it collides with. By default also checks if the cuboid is merely touching the terrain, set alsoCheckTouch to disable that.
	/// </summary>
	/// <param name="blockAccessor"></param>
	/// <param name="entityBoxRel"></param>
	/// <param name="pos"></param>
	/// <param name="alsoCheckTouch"></param>
	/// <returns></returns>
	public Cuboidd GetCollidingCollisionBox(IBlockAccessor blockAccessor, Cuboidf[] ecollisionBoxes, int collBoxCount, Vec3d pos, bool alsoCheckTouch = true)
	{
		for (int j = 0; j < collBoxCount; j++)
		{
			Cuboidf obj = ecollisionBoxes[j];
			BlockPos blockPos = new BlockPos();
			Vec3d blockPosVec = new Vec3d();
			Cuboidd entityBox = obj.ToDouble().Translate(pos);
			entityBox.Y1 = Math.Round(entityBox.Y1, 5);
			int minX = (int)((double)obj.X1 + pos.X);
			int minY = (int)((double)obj.Y1 + pos.Y - 1.0);
			int minZ = (int)((double)obj.Z1 + pos.Z);
			int maxX = (int)Math.Ceiling((double)obj.X2 + pos.X);
			int maxY = (int)Math.Ceiling((double)obj.Y2 + pos.Y);
			int maxZ = (int)Math.Ceiling((double)obj.Z2 + pos.Z);
			for (int y = minY; y <= maxY; y++)
			{
				for (int x = minX; x <= maxX; x++)
				{
					for (int z = minZ; z <= maxZ; z++)
					{
						Block mostSolidBlock = blockAccessor.GetMostSolidBlock(x, y, z);
						blockPos.Set(x, y, z);
						blockPosVec.Set(x, y, z);
						Cuboidf[] collisionBoxes = mostSolidBlock.GetCollisionBoxes(blockAccessor, blockPos);
						if (collisionBoxes == null)
						{
							continue;
						}
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
		}
		return null;
	}
}
