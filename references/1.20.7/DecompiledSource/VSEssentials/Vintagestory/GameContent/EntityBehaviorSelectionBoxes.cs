using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace Vintagestory.GameContent;

public class EntityBehaviorSelectionBoxes : EntityBehavior, IRenderer, IDisposable
{
	private ICoreClientAPI capi;

	private Matrixf mvmat = new Matrixf();

	private bool debug;

	private bool rendererRegistered;

	public AttachmentPointAndPose[] selectionBoxes = new AttachmentPointAndPose[0];

	private string[] selectionBoxCodes;

	public WireframeCube BoxWireframe;

	private float accum;

	private static Cuboidd standardbox = new Cuboidd(0.0, 0.0, 0.0, 1.0, 1.0, 1.0);

	private Vec3d hitPositionOBBSpace;

	private Vec3d hitPositionAABBSpace;

	public double RenderOrder => 1.0;

	public int RenderRange => 24;

	public EntityBehaviorSelectionBoxes(Entity entity)
		: base(entity)
	{
	}

	public void Dispose()
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		capi = entity.Api as ICoreClientAPI;
		if (capi != null)
		{
			debug = capi.Settings.Bool["debugEntitySelectionBoxes"];
		}
		setupWireframe();
		entity.trickleDownRayIntersects = true;
		entity.requirePosesOnServer = true;
		selectionBoxCodes = attributes["selectionBoxes"].AsStringArray(new string[0]);
		if (selectionBoxCodes.Length == 0)
		{
			capi.World.Logger.Warning("EntityBehaviorSelectionBoxes, missing selectionBoxes property. Will ignore.");
		}
	}

	public override void OnTesselated()
	{
		loadSelectionBoxes();
	}

	private void loadSelectionBoxes()
	{
		List<AttachmentPointAndPose> list = new List<AttachmentPointAndPose>();
		string[] array = selectionBoxCodes;
		foreach (string code in array)
		{
			AttachmentPointAndPose apap = entity.AnimManager?.Animator?.GetAttachmentPointPose(code);
			if (apap != null)
			{
				AttachmentPointAndPose dapap = new AttachmentPointAndPose
				{
					AnimModelMatrix = apap.AnimModelMatrix,
					AttachPoint = apap.AttachPoint,
					CachedPose = apap.CachedPose
				};
				list.Add(dapap);
			}
		}
		selectionBoxes = list.ToArray();
	}

	public override void OnGameTick(float deltaTime)
	{
		if (capi != null && (accum += deltaTime) >= 1f)
		{
			accum = 0f;
			debug = capi.Settings.Bool["debugEntitySelectionBoxes"];
			setupWireframe();
		}
		base.OnGameTick(deltaTime);
	}

	private void setupWireframe()
	{
		if (!rendererRegistered)
		{
			if (capi != null)
			{
				capi.Event.RegisterRenderer(this, EnumRenderStage.AfterFinalComposition, "selectionboxesbhdebug");
				BoxWireframe = WireframeCube.CreateUnitCube(capi, -1);
			}
			rendererRegistered = true;
		}
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (capi.HideGuis)
		{
			return;
		}
		int hitindex = getHitIndex();
		if (hitindex < 0 && capi.World.Player.CurrentEntitySelection?.Entity != entity)
		{
			return;
		}
		EntityPlayer eplr = capi.World.Player.Entity;
		if (debug)
		{
			for (int i = 0; i < selectionBoxes.Length; i++)
			{
				if (hitindex != i)
				{
					Render(eplr, i, ColorUtil.WhiteArgbVec);
				}
			}
			if (hitindex >= 0)
			{
				Render(eplr, hitindex, new Vec4f(1f, 0f, 0f, 1f));
			}
		}
		else if (hitindex >= 0)
		{
			Render(eplr, hitindex, new Vec4f(0f, 0f, 0f, 0.5f));
		}
	}

	private void Render(EntityPlayer eplr, int i, Vec4f color)
	{
		AttachmentPointAndPose apap = selectionBoxes[i];
		EntityPos pos = entity.Pos;
		mvmat.Identity();
		mvmat.Set(capi.Render.CameraMatrixOrigin);
		IMountable ims = entity.GetInterface<IMountable>();
		IMountableSeat seat;
		if (ims != null && (seat = ims.GetSeatOfMountedEntity(eplr)) != null)
		{
			Vec3d offset = seat.SeatPosition.XYZ - seat.MountSupplier.Position.XYZ;
			mvmat.Translate(0f - (float)offset.X, 0f - (float)offset.Y, 0f - (float)offset.Z);
		}
		else
		{
			mvmat.Translate(pos.X - eplr.CameraPos.X, pos.InternalY - eplr.CameraPos.Y, pos.Z - eplr.CameraPos.Z);
		}
		applyBoxTransform(mvmat, apap);
		BoxWireframe.Render(capi, mvmat, 1.6f, color);
	}

	private int getHitIndex()
	{
		EntityPlayer eplr = capi.World.Player.Entity;
		Ray pickingray = Ray.FromAngles(eplr.SidedPos.XYZ + eplr.LocalEyePos - entity.SidedPos.XYZ, eplr.SidedPos.Pitch, eplr.SidedPos.Yaw, capi.World.Player.WorldData.PickingRange);
		return getHitIndex(pickingray);
	}

	private void applyBoxTransform(Matrixf mvmat, AttachmentPointAndPose apap)
	{
		EntityShapeRenderer esr = entity.Properties.Client.Renderer as EntityShapeRenderer;
		mvmat.RotateY((float)Math.PI / 2f + entity.SidedPos.Yaw);
		if (esr != null)
		{
			mvmat.Translate(0f, entity.SelectionBox.Y2 / 2f, 0f);
			mvmat.RotateX(esr.xangle);
			mvmat.RotateY(esr.yangle);
			mvmat.RotateZ(esr.zangle);
			mvmat.Translate(0f, (0f - entity.SelectionBox.Y2) / 2f, 0f);
		}
		mvmat.Translate(0f, 0.7f, 0f);
		mvmat.RotateX(esr?.nowSwivelRad ?? 0f);
		mvmat.Translate(0f, -0.7f, 0f);
		float s = entity.Properties.Client.Size;
		mvmat.Scale(s, s, s);
		mvmat.Translate(-0.5f, 0f, -0.5f);
		mvmat.Mul(apap.AnimModelMatrix);
		ShapeElement selem = apap.AttachPoint.ParentElement;
		float sizex = (float)(selem.To[0] - selem.From[0]) / 16f;
		float sizey = (float)(selem.To[1] - selem.From[1]) / 16f;
		float sizez = (float)(selem.To[2] - selem.From[2]) / 16f;
		mvmat.Scale(sizex, sizey, sizez);
	}

	public override bool IntersectsRay(Ray ray, AABBIntersectionTest intersectionTester, out double intersectionDistance, ref int selectionBoxIndex, ref EnumHandling handled)
	{
		Ray pickingray = new Ray(ray.origin - entity.SidedPos.XYZ, ray.dir);
		int index = getHitIndex(pickingray);
		if (index >= 0)
		{
			intersectionDistance = hitPositionAABBSpace.Length();
			intersectionTester.hitPosition = hitPositionAABBSpace.AddCopy(entity.SidedPos.XYZ);
			selectionBoxIndex = 1 + index;
			handled = EnumHandling.PreventDefault;
			return true;
		}
		intersectionDistance = double.MaxValue;
		return false;
	}

	private int getHitIndex(Ray pickingray)
	{
		int foundIndex = -1;
		double foundDistance = double.MaxValue;
		for (int i = 0; i < selectionBoxes.Length; i++)
		{
			AttachmentPointAndPose apap = selectionBoxes[i];
			mvmat.Identity();
			applyBoxTransform(mvmat, apap);
			Matrixf matrixf = mvmat.Clone().Invert();
			Vec4d obbSpaceOrigin = matrixf.TransformVector(new Vec4d(pickingray.origin.X, pickingray.origin.Y, pickingray.origin.Z, 1.0));
			Vec4d obbSpaceDirection = matrixf.TransformVector(new Vec4d(pickingray.dir.X, pickingray.dir.Y, pickingray.dir.Z, 0.0));
			Ray obbSpaceRay = new Ray(obbSpaceOrigin.XYZ, obbSpaceDirection.XYZ);
			if (Testintersection(standardbox, obbSpaceRay))
			{
				Vec4d tf = mvmat.TransformVector(new Vec4d(hitPositionOBBSpace.X, hitPositionOBBSpace.Y, hitPositionOBBSpace.Z, 1.0));
				double dist = (tf.XYZ - pickingray.origin).LengthSq();
				if (foundIndex < 0 || !(foundDistance < dist))
				{
					hitPositionAABBSpace = tf.XYZ;
					foundDistance = dist;
					foundIndex = i;
				}
			}
		}
		return foundIndex;
	}

	public Vec3d GetCenterPosOfBox(int selectionBoxIndex)
	{
		if (selectionBoxIndex >= selectionBoxes.Length)
		{
			return null;
		}
		AttachmentPointAndPose apap = selectionBoxes[selectionBoxIndex];
		mvmat.Identity();
		applyBoxTransform(mvmat, apap);
		ShapeElement pe = apap.AttachPoint.ParentElement;
		Vec4d centerPos = new Vec4d((pe.To[0] - pe.From[0]) / 2.0 / 16.0, (pe.To[1] - pe.From[1]) / 2.0 / 16.0, (pe.To[2] - pe.From[2]) / 2.0 / 16.0, 1.0);
		Vec3d basePos = entity.Pos.XYZ;
		EntityPlayer eplr = capi.World.Player.Entity;
		if (eplr?.MountedOn?.Entity == entity)
		{
			Vec3d mpos = eplr.MountedOn.SeatPosition.XYZ - entity.Pos.XYZ;
			basePos = new Vec3d(eplr.CameraPos.X - mpos.X, eplr.CameraPos.Y - mpos.Y, eplr.CameraPos.Z - mpos.Z);
		}
		return mvmat.TransformVector(centerPos).XYZ.Add(basePos);
	}

	public bool Testintersection(Cuboidd b, Ray r)
	{
		double w = b.X2 - b.X1;
		double h = b.Y2 - b.Y1;
		double j = b.Z2 - b.Z1;
		for (int i = 0; i < 6; i++)
		{
			BlockFacing blockSideFacing = BlockFacing.ALLFACES[i];
			Vec3i planeNormal = blockSideFacing.Normali;
			double demon = (double)planeNormal.X * r.dir.X + (double)planeNormal.Y * r.dir.Y + (double)planeNormal.Z * r.dir.Z;
			if (!(demon < -1E-05))
			{
				continue;
			}
			Vec3d planeCenterPosition = blockSideFacing.PlaneCenter.ToVec3d().Mul(w, h, j).Add(b.X1, b.Y1, b.Z1);
			Vec3d pt = Vec3d.Sub(planeCenterPosition, r.origin);
			double t = (pt.X * (double)planeNormal.X + pt.Y * (double)planeNormal.Y + pt.Z * (double)planeNormal.Z) / demon;
			if (t >= 0.0)
			{
				hitPositionOBBSpace = new Vec3d(r.origin.X + r.dir.X * t, r.origin.Y + r.dir.Y * t, r.origin.Z + r.dir.Z * t);
				Vec3d lastExitedBlockFacePos = Vec3d.Sub(hitPositionOBBSpace, planeCenterPosition);
				if (Math.Abs(lastExitedBlockFacePos.X) <= w / 2.0 && Math.Abs(lastExitedBlockFacePos.Y) <= h / 2.0 && Math.Abs(lastExitedBlockFacePos.Z) <= j / 2.0)
				{
					return true;
				}
			}
		}
		return false;
	}

	public override void GetInfoText(StringBuilder infotext)
	{
		int hitindex = getHitIndex();
		if (hitindex >= 0)
		{
			if (capi.Settings.Bool["extendedDebugInfo"])
			{
				infotext.AppendLine("<font color=\"#bbbbbb\">looking at AP " + selectionBoxes[hitindex].AttachPoint.Code + "</font>");
			}
			infotext.AppendLine(Lang.GetMatching("creature-" + entity.Code.ToShortString() + "-selectionbox-" + selectionBoxes[hitindex].AttachPoint.Code));
		}
		base.GetInfoText(infotext);
	}

	public override void OnEntityDespawn(EntityDespawnData despawn)
	{
		base.OnEntityDespawn(despawn);
		capi?.Event.UnregisterRenderer(this, EnumRenderStage.AfterFinalComposition);
		BoxWireframe?.Dispose();
	}

	public override string PropertyName()
	{
		return "selectionboxes";
	}
}
