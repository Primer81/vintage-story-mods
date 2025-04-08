using System;
using Vintagestory.API.Common;

namespace Vintagestory.API.MathTools;

public class AABBIntersectionTest
{
	private BlockFacing hitOnBlockFaceTmp = BlockFacing.DOWN;

	private Vec3d hitPositionTmp = new Vec3d();

	private Vec3d lastExitedBlockFacePos = new Vec3d();

	public IWorldIntersectionSupplier bsTester;

	private Cuboidd tmpCuboidd = new Cuboidd();

	public Vec3d hitPosition = new Vec3d();

	public Ray ray = new Ray();

	public BlockPos pos = new BlockPos();

	public BlockFacing hitOnBlockFace = BlockFacing.DOWN;

	public int hitOnSelectionBox;

	private Block blockIntersected;

	public AABBIntersectionTest(IWorldIntersectionSupplier blockSelectionTester)
	{
		bsTester = blockSelectionTester;
	}

	public void LoadRayAndPos(Line3D line3d)
	{
		ray.origin.Set(line3d.Start[0], line3d.Start[1], line3d.Start[2]);
		ray.dir.Set(line3d.End[0] - line3d.Start[0], line3d.End[1] - line3d.Start[1], line3d.End[2] - line3d.Start[2]);
		pos.SetAndCorrectDimension((int)line3d.Start[0], (int)line3d.Start[1], (int)line3d.Start[2]);
	}

	public void LoadRayAndPos(Ray ray)
	{
		this.ray = ray;
		pos.SetAndCorrectDimension(ray.origin);
	}

	public BlockSelection GetSelectedBlock(Vec3d from, Vec3d to, BlockFilter filter = null)
	{
		LoadRayAndPos(new Line3D
		{
			Start = new double[3] { from.X, from.Y, from.Z },
			End = new double[3] { to.X, to.Y, to.Z }
		});
		float maxDistance = from.DistanceTo(to);
		return GetSelectedBlock(maxDistance, filter);
	}

	public BlockSelection GetSelectedBlock(float maxDistance, BlockFilter filter = null, bool testCollide = false)
	{
		float distanceSq = 0f;
		BlockFacing lastExitedBlockFace = GetExitingFullBlockFace(pos, ref lastExitedBlockFacePos);
		if (lastExitedBlockFace == null)
		{
			return null;
		}
		float maxDistanceSq = (maxDistance + 1f) * (maxDistance + 1f);
		while (!RayIntersectsBlockSelectionBox(pos, filter, testCollide))
		{
			if (distanceSq >= maxDistanceSq)
			{
				return null;
			}
			pos.Offset(lastExitedBlockFace);
			lastExitedBlockFace = GetExitingFullBlockFace(pos, ref lastExitedBlockFacePos);
			if (lastExitedBlockFace == null)
			{
				return null;
			}
			distanceSq = pos.DistanceSqTo(ray.origin.X - 0.5, ray.origin.Y - 0.5, ray.origin.Z - 0.5);
		}
		if (hitPosition.SquareDistanceTo(ray.origin) > maxDistance * maxDistance)
		{
			return null;
		}
		return new BlockSelection
		{
			Face = hitOnBlockFace,
			Position = pos.CopyAndCorrectDimension(),
			HitPosition = hitPosition.SubCopy(pos.X, pos.InternalY, pos.Z),
			SelectionBoxIndex = hitOnSelectionBox,
			Block = blockIntersected
		};
	}

	public bool RayIntersectsBlockSelectionBox(BlockPos pos, BlockFilter filter, bool testCollide = false)
	{
		Block block = bsTester.blockAccessor.GetBlock(pos, 2);
		Cuboidf[] hitboxes;
		if (block.SideSolid.Any)
		{
			hitboxes = (testCollide ? block.GetCollisionBoxes(bsTester.blockAccessor, pos) : block.GetSelectionBoxes(bsTester.blockAccessor, pos));
		}
		else
		{
			block = bsTester.GetBlock(pos);
			hitboxes = (testCollide ? block.GetCollisionBoxes(bsTester.blockAccessor, pos) : bsTester.GetBlockIntersectionBoxes(pos));
		}
		if (hitboxes == null)
		{
			return false;
		}
		if (filter != null && !filter(pos, block))
		{
			return false;
		}
		bool intersects = false;
		bool wasDecor = false;
		for (int i = 0; i < hitboxes.Length; i++)
		{
			tmpCuboidd.Set(hitboxes[i]).Translate(pos.X, pos.InternalY, pos.Z);
			if (RayIntersectsWithCuboid(tmpCuboidd, ref hitOnBlockFaceTmp, ref hitPositionTmp))
			{
				bool isDecor = hitboxes[i] is DecorSelectionBox;
				if (!intersects || !(!wasDecor || isDecor) || !(hitPosition.SquareDistanceTo(ray.origin) <= hitPositionTmp.SquareDistanceTo(ray.origin)))
				{
					hitOnSelectionBox = i;
					intersects = true;
					wasDecor = isDecor;
					hitOnBlockFace = hitOnBlockFaceTmp;
					hitPosition.Set(hitPositionTmp);
				}
			}
		}
		if (intersects && hitboxes[hitOnSelectionBox] is DecorSelectionBox { PosAdjust: var posAdjust } && posAdjust != null)
		{
			pos.Add(posAdjust);
			block = bsTester.GetBlock(pos);
		}
		if (intersects)
		{
			blockIntersected = block;
		}
		return intersects;
	}

	public bool RayIntersectsWithCuboid(Cuboidd selectionBox)
	{
		if (selectionBox == null)
		{
			return false;
		}
		return RayIntersectsWithCuboid(tmpCuboidd, ref hitOnBlockFace, ref hitPosition);
	}

	public bool RayIntersectsWithCuboid(Cuboidf selectionBox, double posX, double posY, double posZ)
	{
		if (selectionBox == null)
		{
			return false;
		}
		tmpCuboidd.Set(selectionBox).Translate(posX, posY, posZ);
		return RayIntersectsWithCuboid(tmpCuboidd, ref hitOnBlockFace, ref hitPosition);
	}

	public bool RayIntersectsWithCuboid(Cuboidd selectionBox, ref BlockFacing hitOnBlockFace, ref Vec3d hitPosition)
	{
		if (selectionBox == null)
		{
			return false;
		}
		double w = selectionBox.X2 - selectionBox.X1;
		double h = selectionBox.Y2 - selectionBox.Y1;
		double j = selectionBox.Z2 - selectionBox.Z1;
		for (int i = 0; i < 6; i++)
		{
			BlockFacing blockSideFacing = BlockFacing.ALLFACES[i];
			Vec3i planeNormal = blockSideFacing.Normali;
			double demon = (double)planeNormal.X * ray.dir.X + (double)planeNormal.Y * ray.dir.Y + (double)planeNormal.Z * ray.dir.Z;
			if (!(demon < -1E-05))
			{
				continue;
			}
			Vec3d planeCenterPosition = blockSideFacing.PlaneCenter.ToVec3d().Mul(w, h, j).Add(selectionBox.X1, selectionBox.Y1, selectionBox.Z1);
			Vec3d pt = Vec3d.Sub(planeCenterPosition, ray.origin);
			double t = (pt.X * (double)planeNormal.X + pt.Y * (double)planeNormal.Y + pt.Z * (double)planeNormal.Z) / demon;
			if (t >= 0.0)
			{
				hitPosition = new Vec3d(ray.origin.X + ray.dir.X * t, ray.origin.Y + ray.dir.Y * t, ray.origin.Z + ray.dir.Z * t);
				lastExitedBlockFacePos = Vec3d.Sub(hitPosition, planeCenterPosition);
				if (Math.Abs(lastExitedBlockFacePos.X) <= w / 2.0 && Math.Abs(lastExitedBlockFacePos.Y) <= h / 2.0 && Math.Abs(lastExitedBlockFacePos.Z) <= j / 2.0)
				{
					hitOnBlockFace = blockSideFacing;
					return true;
				}
			}
		}
		return false;
	}

	public static bool RayInteresectWithCuboidSlabMethod(Cuboidd b, Ray r)
	{
		double val = (b.X1 - r.dir.X) / r.dir.X;
		double tx2 = (b.X2 - r.dir.X) / r.dir.X;
		double tmin = Math.Min(val, tx2);
		double val2 = Math.Max(val, tx2);
		double ty1 = (b.Y1 - r.dir.Y) / r.dir.Y;
		double ty2 = (b.Y2 - r.dir.Y) / r.dir.Y;
		tmin = Math.Max(tmin, Math.Min(ty1, ty2));
		double val3 = Math.Min(val2, Math.Max(ty1, ty2));
		double tz1 = (b.Z1 - r.dir.Z) / r.dir.Z;
		double tz2 = (b.Z2 - r.dir.Z) / r.dir.Z;
		tmin = Math.Max(tmin, Math.Min(tz1, tz2));
		return Math.Min(val3, Math.Max(tz1, tz2)) >= tmin;
	}

	private BlockFacing GetExitingFullBlockFace(BlockPos pos, ref Vec3d exitPos)
	{
		for (int i = 0; i < 6; i++)
		{
			BlockFacing blockSideFacing = BlockFacing.ALLFACES[i];
			Vec3i planeNormal = blockSideFacing.Normali;
			double demon = (double)planeNormal.X * ray.dir.X + (double)planeNormal.Y * ray.dir.Y + (double)planeNormal.Z * ray.dir.Z;
			if (!(demon > 1E-05))
			{
				continue;
			}
			Vec3d planePosition = pos.ToVec3d().Add(blockSideFacing.PlaneCenter);
			Vec3d pt = Vec3d.Sub(planePosition, ray.origin);
			double t = (pt.X * (double)planeNormal.X + pt.Y * (double)planeNormal.Y + pt.Z * (double)planeNormal.Z) / demon;
			if (t >= 0.0)
			{
				Vec3d pHit = new Vec3d(ray.origin.X + ray.dir.X * t, ray.origin.Y + ray.dir.Y * t, ray.origin.Z + ray.dir.Z * t);
				exitPos = Vec3d.Sub(pHit, planePosition);
				if (Math.Abs(exitPos.X) <= 0.5 && Math.Abs(exitPos.Y) <= 0.5 && Math.Abs(exitPos.Z) <= 0.5)
				{
					return blockSideFacing;
				}
			}
		}
		return null;
	}
}
