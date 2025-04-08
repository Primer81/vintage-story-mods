using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class Camera
{
	internal float ZNear = 0.1f;

	internal float ZFar = 3000f;

	internal float Fov;

	public Vec3d CameraEyePos = new Vec3d();

	internal double[] CameraMatrix;

	internal double[] CameraMatrixOrigin;

	internal float[] CameraMatrixOriginf = Mat4f.Create();

	internal float Tppcameradistance;

	internal int TppCameraDistanceMin;

	internal int TppCameraDistanceMax;

	internal EnumCameraMode CameraMode;

	private double[] upVec3;

	private Vec3d camEyePosIn = new Vec3d();

	private Vec3d originPos = new Vec3d();

	public double PlayerHeight;

	public Vec3d forwardVec = new Vec3d();

	private Vec3d camTargetTmp = new Vec3d();

	private Vec3d camEyePosOutTmp = new Vec3d();

	public ModelTransform CameraOffset = new ModelTransform();

	private bool cameraStuck;

	private Vec3d to = new Vec3d();

	private Vec3d eyePosAbs = new Vec3d();

	public CachedCuboidList CollisionBoxList = new CachedCuboidList();

	public float MotionCap = 2f;

	private BlockPos minPos = new BlockPos();

	private BlockPos maxPos = new BlockPos();

	private Cuboidd cameraCollBox = new Cuboidd();

	private Cuboidd blockCollBox = new Cuboidd();

	private BlockPos tmpPos = new BlockPos();

	public Vec3d CamSourcePosition
	{
		get
		{
			return camEyePosIn;
		}
		set
		{
			camEyePosIn = value;
		}
	}

	public Vec3d OriginPosition
	{
		get
		{
			return originPos;
		}
		set
		{
			originPos = value;
		}
	}

	public double Yaw { get; set; }

	public double Pitch { get; set; }

	public double Roll { get; set; }

	public Camera()
	{
		CameraMode = EnumCameraMode.FirstPerson;
		CameraOffset.EnsureDefaultValues();
		Tppcameradistance = 3f;
		TppCameraDistanceMin = 1;
		TppCameraDistanceMax = 10;
		CameraMatrix = Mat4d.Create();
		upVec3 = Vec3Utilsd.FromValues(0.0, 1.0, 0.0);
	}

	internal virtual void SetMode(EnumCameraMode type)
	{
		CameraMode = type;
	}

	internal void Update(float deltaTime, AABBIntersectionTest intersectionTester)
	{
		CameraMatrix = GetCameraMatrix(camEyePosIn, camEyePosIn, Yaw, Pitch, intersectionTester);
		CameraEyePos.Set(camEyePosOutTmp);
		CameraMatrixOrigin = GetCameraMatrix(originPos, camEyePosIn, Yaw, Pitch, intersectionTester);
		Mat4d.Rotate(CameraMatrixOrigin, CameraMatrixOrigin, Roll, new double[3] { 1.0, 0.0, 0.0 });
		for (int i = 0; i < 16; i++)
		{
			CameraMatrixOriginf[i] = (float)CameraMatrixOrigin[i];
		}
	}

	internal double[] GetCameraMatrix(Vec3d camEyePosIn, Vec3d worldPos, double yaw, double pitch, AABBIntersectionTest intersectionTester)
	{
		VectorTool.ToVectorInFixedSystem(CameraOffset.Translation.X, CameraOffset.Translation.Y, CameraOffset.Translation.Z + 1f, (double)CameraOffset.Rotation.X + pitch, (double)CameraOffset.Rotation.Y - yaw + 3.1415927410125732, forwardVec);
		IClientWorldAccessor cworld = intersectionTester.bsTester as IClientWorldAccessor;
		EntityPlayer plr = cworld.Player.Entity;
		(cworld.Player as ClientPlayer).OverrideCameraMode = null;
		EnumCameraMode cameraMode = CameraMode;
		if ((uint)(cameraMode - 1) <= 1u)
		{
			float camDist = ((CameraMode == EnumCameraMode.FirstPerson) ? 0f : Tppcameradistance);
			camTargetTmp.X = worldPos.X + plr.LocalEyePos.X;
			camTargetTmp.Y = worldPos.Y + plr.LocalEyePos.Y;
			camTargetTmp.Z = worldPos.Z + plr.LocalEyePos.Z;
			camEyePosOutTmp.X = camTargetTmp.X + forwardVec.X * (double)(0f - camDist);
			camEyePosOutTmp.Y = camTargetTmp.Y + forwardVec.Y * (double)(0f - camDist);
			camEyePosOutTmp.Z = camTargetTmp.Z + forwardVec.Z * (double)(0f - camDist);
			FloatRef currentCameradistance = FloatRef.Create(camDist);
			if (camDist > 0f && !LimitThirdPersonCameraToWalls(intersectionTester, yaw, camEyePosOutTmp, camTargetTmp, currentCameradistance))
			{
				(cworld.Player as ClientPlayer).OverrideCameraMode = EnumCameraMode.FirstPerson;
				return lookatFp(plr, camEyePosIn);
			}
			if ((double)currentCameradistance.value > 0.5)
			{
				camTargetTmp.X = camEyePosIn.X + plr.LocalEyePos.X;
				camTargetTmp.Y = camEyePosIn.Y + plr.LocalEyePos.Y;
				camTargetTmp.Z = camEyePosIn.Z + plr.LocalEyePos.Z;
				camEyePosOutTmp.X = camTargetTmp.X + forwardVec.X * (double)(0f - currentCameradistance.value);
				camEyePosOutTmp.Y = camTargetTmp.Y + forwardVec.Y * (double)(0f - currentCameradistance.value);
				camEyePosOutTmp.Z = camTargetTmp.Z + forwardVec.Z * (double)(0f - currentCameradistance.value);
				return lookAt(camEyePosOutTmp, camTargetTmp);
			}
			camEyePosOutTmp.X = camEyePosIn.X + plr.LocalEyePos.X + forwardVec.X * 0.2;
			camEyePosOutTmp.Y = camEyePosIn.Y + plr.LocalEyePos.Y + forwardVec.Y * 0.2;
			camEyePosOutTmp.Z = camEyePosIn.Z + plr.LocalEyePos.Z + forwardVec.Z * 0.2;
			camTargetTmp.X = camEyePosOutTmp.X + forwardVec.X;
			camTargetTmp.Y = camEyePosOutTmp.Y + forwardVec.Y;
			camTargetTmp.Z = camEyePosOutTmp.Z + forwardVec.Z;
			return lookAt(camEyePosOutTmp, camTargetTmp);
		}
		_ = cworld.Api;
		camTargetTmp.X = camEyePosIn.X + plr.LocalEyePos.X;
		camTargetTmp.Y = camEyePosIn.Y + plr.LocalEyePos.Y;
		camTargetTmp.Z = camEyePosIn.Z + plr.LocalEyePos.Z;
		if (camEyePosIn == OriginPosition || !cworld.Player.ImmersiveFpMode)
		{
			return lookatFp(plr, camEyePosIn);
		}
		float cameraSize = 0.5f;
		RenderAPIGame rpi = (cworld as ClientMain).api.renderapi;
		if (cworld.Player.WorldData.NoClip || cameraStuck)
		{
			eyePosAbs.Set(camTargetTmp);
			rpi.CameraStuck = cworld.CollisionTester.IsColliding(cworld.BlockAccessor, new Cuboidf(cameraSize), eyePosAbs, alsoCheckTouch: false);
			return lookatFp(plr, camEyePosIn);
		}
		if (camTargetTmp.DistanceTo(eyePosAbs) > 1f)
		{
			eyePosAbs.Set(camTargetTmp);
		}
		else
		{
			Vec3d cameraMotion = camTargetTmp - eyePosAbs;
			EnumCollideFlags flags = UpdateCameraMotion(cworld, eyePosAbs, cameraMotion.Mul(1.01), cameraSize);
			eyePosAbs.Add(cameraMotion.Mul(0.99));
			plr.LocalEyePos.Set(eyePosAbs.X - camEyePosIn.X, eyePosAbs.Y - camEyePosIn.Y, eyePosAbs.Z - camEyePosIn.Z);
			rpi.CameraStuck = flags != (EnumCollideFlags)0;
			if (flags != 0)
			{
				if ((double)cworld.Player.CameraPitch > 3.769911289215088)
				{
					plr.LocalEyePos.Y += ((double)cworld.Player.CameraPitch - 3.769911289215088) / 8.0;
				}
				cameraStuck = cworld.CollisionTester.IsColliding(cworld.BlockAccessor, new Cuboidf(cameraSize * 0.99f), eyePosAbs, alsoCheckTouch: false);
			}
		}
		camEyePosOutTmp.X = eyePosAbs.X;
		camEyePosOutTmp.Y = eyePosAbs.Y;
		camEyePosOutTmp.Z = eyePosAbs.Z;
		to.Set(camEyePosOutTmp.X + forwardVec.X, camEyePosOutTmp.Y + forwardVec.Y, camEyePosOutTmp.Z + forwardVec.Z);
		return lookAt(camTargetTmp, to);
	}

	private double[] lookatFp(EntityPlayer plr, Vec3d camEyePosIn)
	{
		camEyePosOutTmp.X = camEyePosIn.X + plr.LocalEyePos.X;
		camEyePosOutTmp.Y = camEyePosIn.Y + plr.LocalEyePos.Y;
		camEyePosOutTmp.Z = camEyePosIn.Z + plr.LocalEyePos.Z;
		camTargetTmp.X = camEyePosOutTmp.X + forwardVec.X;
		camTargetTmp.Y = camEyePosOutTmp.Y + forwardVec.Y;
		camTargetTmp.Z = camEyePosOutTmp.Z + forwardVec.Z;
		return lookAt(camEyePosOutTmp, camTargetTmp);
	}

	private double[] lookAt(Vec3d from, Vec3d to)
	{
		double[] array = new double[16];
		Mat4d.LookAt(array, from.ToDoubleArray(), to.ToDoubleArray(), upVec3);
		return array;
	}

	internal bool LimitThirdPersonCameraToWalls(AABBIntersectionTest intersectionTester, double yaw, Vec3d eye, Vec3d target, FloatRef curtppcameradistance)
	{
		float centerDistance = GetIntersectionDistance(intersectionTester, eye, target);
		float leftDistance = GetIntersectionDistance(intersectionTester, eye.AheadCopy(0.15000000596046448, 0.0, yaw), target.AheadCopy(0.15000000596046448, 0.0, yaw));
		float rightDistance = GetIntersectionDistance(intersectionTester, eye.AheadCopy(-0.15000000596046448, 0.0, yaw), target.AheadCopy(-0.15000000596046448, 0.0, yaw));
		float distance = GameMath.Min(centerDistance, leftDistance, rightDistance);
		if ((double)distance < 0.35)
		{
			return false;
		}
		curtppcameradistance.value = Math.Min(curtppcameradistance.value, distance);
		double raydirX = eye.X - target.X;
		double raydirY = eye.Y - target.Y;
		double raydirZ = eye.Z - target.Z;
		float raydirLength2 = (float)Math.Sqrt(raydirX * raydirX + raydirY * raydirY + raydirZ * raydirZ);
		raydirX /= (double)raydirLength2;
		raydirY /= (double)raydirLength2;
		raydirZ /= (double)raydirLength2;
		raydirX *= (double)(Tppcameradistance + 1f);
		raydirY *= (double)(Tppcameradistance + 1f);
		raydirZ *= (double)(Tppcameradistance + 1f);
		float raydirLength = (float)Math.Sqrt(raydirX * raydirX + raydirY * raydirY + raydirZ * raydirZ);
		raydirX /= (double)raydirLength;
		raydirY /= (double)raydirLength;
		raydirZ /= (double)raydirLength;
		eye.X = target.X + raydirX * (double)curtppcameradistance.value;
		eye.Y = target.Y + raydirY * (double)curtppcameradistance.value;
		eye.Z = target.Z + raydirZ * (double)curtppcameradistance.value;
		return true;
	}

	private float GetIntersectionDistance(AABBIntersectionTest intersectionTester, Vec3d eye, Vec3d target)
	{
		Line3D pick = new Line3D();
		double raydirX = eye.X - target.X;
		double raydirY = eye.Y - target.Y;
		double raydirZ = eye.Z - target.Z;
		float raydirLength1 = (float)Math.Sqrt(raydirX * raydirX + raydirY * raydirY + raydirZ * raydirZ);
		raydirX /= (double)raydirLength1;
		raydirY /= (double)raydirLength1;
		raydirZ /= (double)raydirLength1;
		raydirX *= (double)(Tppcameradistance + 1f);
		raydirY *= (double)(Tppcameradistance + 1f);
		raydirZ *= (double)(Tppcameradistance + 1f);
		pick.Start = target.ToDoubleArray();
		pick.End = new double[3];
		pick.End[0] = target.X + raydirX;
		pick.End[1] = target.Y + raydirY;
		pick.End[2] = target.Z + raydirZ;
		intersectionTester.LoadRayAndPos(pick);
		BlockSelection selection = intersectionTester.GetSelectedBlock(TppCameraDistanceMax, (BlockPos pos, Block block) => block.CollisionBoxes != null && block.CollisionBoxes.Length != 0 && block.RenderPass != EnumChunkRenderPass.Transparent && block.RenderPass != EnumChunkRenderPass.Meta);
		if (selection != null)
		{
			float pickX = (float)((double)selection.Position.X + selection.HitPosition.X - target.X);
			float pickY = (float)((double)selection.Position.InternalY + selection.HitPosition.Y - target.Y);
			float pickZ = (float)((double)selection.Position.Z + selection.HitPosition.Z - target.Z);
			float pickdistance = Length(pickX, pickY, pickZ);
			return GameMath.Max(0.3f, pickdistance - 1f);
		}
		return 999f;
	}

	internal float Length(float x, float y, float z)
	{
		return GameMath.Sqrt(x * x + y * y + z * z);
	}

	public EnumCollideFlags UpdateCameraMotion(IWorldAccessor world, Vec3d pos, Vec3d motion, float size)
	{
		cameraCollBox.Set(pos.X - (double)(size / 2f), pos.Y - (double)(size / 2f), pos.Z - (double)(size / 2f), pos.X + (double)(size / 2f), pos.Y + (double)(size / 2f), pos.Z + (double)(size / 2f));
		motion.X = GameMath.Clamp(motion.X, 0f - MotionCap, MotionCap);
		motion.Y = GameMath.Clamp(motion.Y, 0f - MotionCap, MotionCap);
		motion.Z = GameMath.Clamp(motion.Z, 0f - MotionCap, MotionCap);
		EnumCollideFlags flags = (EnumCollideFlags)0;
		minPos.SetAndCorrectDimension((int)(cameraCollBox.X1 + Math.Min(0.0, motion.X)), (int)(cameraCollBox.Y1 + Math.Min(0.0, motion.Y) - 1.0), (int)(cameraCollBox.Z1 + Math.Min(0.0, motion.Z)));
		maxPos.SetAndCorrectDimension((int)(cameraCollBox.X2 + Math.Max(0.0, motion.X)), (int)(cameraCollBox.Y2 + Math.Max(0.0, motion.Y)), (int)(cameraCollBox.Z2 + Math.Max(0.0, motion.Z)));
		tmpPos.dimension = minPos.dimension;
		cameraCollBox.Y1 %= 32768.0;
		cameraCollBox.Y2 %= 32768.0;
		CollisionBoxList.Clear();
		world.BlockAccessor.WalkBlocks(minPos, maxPos, delegate(Block cblock, int x, int y, int z)
		{
			Cuboidf[] collisionBoxes = cblock.GetCollisionBoxes(world.BlockAccessor, tmpPos.Set(x, y, z));
			if (collisionBoxes != null)
			{
				CollisionBoxList.Add(collisionBoxes, x, y, z, cblock);
			}
		});
		EnumPushDirection pushDirection = EnumPushDirection.None;
		for (int k = 0; k < CollisionBoxList.Count; k++)
		{
			blockCollBox = CollisionBoxList.cuboids[k];
			motion.Y = (float)blockCollBox.pushOutY(cameraCollBox, motion.Y, ref pushDirection);
			if (pushDirection != 0)
			{
				flags |= EnumCollideFlags.CollideY;
			}
		}
		cameraCollBox.Translate(0.0, motion.Y, 0.0);
		for (int j = 0; j < CollisionBoxList.Count; j++)
		{
			blockCollBox = CollisionBoxList.cuboids[j];
			motion.X = (float)blockCollBox.pushOutX(cameraCollBox, motion.X, ref pushDirection);
			if (pushDirection != 0)
			{
				flags |= EnumCollideFlags.CollideX;
			}
		}
		cameraCollBox.Translate(motion.X, 0.0, 0.0);
		for (int i = 0; i < CollisionBoxList.Count; i++)
		{
			blockCollBox = CollisionBoxList.cuboids[i];
			motion.Z = (float)blockCollBox.pushOutZ(cameraCollBox, motion.Z, ref pushDirection);
			if (pushDirection != 0)
			{
				flags |= EnumCollideFlags.CollideZ;
			}
		}
		return flags;
	}
}
