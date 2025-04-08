using System;
using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[ProtoContract]
public class ClothSystem
{
	[ProtoMember(1)]
	public int ClothId;

	[ProtoMember(2)]
	private EnumClothType clothType;

	[ProtoMember(3)]
	private List<PointList> Points2d = new List<PointList>();

	[ProtoMember(4)]
	private List<ClothConstraint> Constraints = new List<ClothConstraint>();

	public static float Resolution = 2f;

	public float StretchWarn = 0.6f;

	public float StretchRip = 0.75f;

	public bool LineDebug;

	public bool boyant;

	protected ICoreClientAPI capi;

	public ICoreAPI api;

	public Vec3d windSpeed = new Vec3d();

	public ParticlePhysics pp;

	protected NormalizedSimplexNoise noiseGen;

	protected float[] tmpMat = new float[16];

	protected Vec3f distToCam = new Vec3f();

	protected AssetLocation ropeSectionModel;

	protected MeshData debugUpdateMesh;

	protected MeshRef debugMeshRef;

	public float secondsOverStretched;

	private double minLen = 1.5;

	private double maxLen = 10.0;

	private Matrixf mat = new Matrixf();

	private float accum;

	[ProtoMember(5)]
	public bool Active { get; set; }

	public bool PinnedAnywhere
	{
		get
		{
			foreach (PointList item in Points2d)
			{
				foreach (ClothPoint point in item.Points)
				{
					if (point.Pinned)
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	public double MaxExtension
	{
		get
		{
			if (Constraints.Count != 0)
			{
				return Constraints.Max((ClothConstraint c) => c.Extension);
			}
			return 0.0;
		}
	}

	public Vec3d CenterPosition
	{
		get
		{
			Vec3d pos = new Vec3d();
			int cnt = 0;
			foreach (PointList item in Points2d)
			{
				foreach (ClothPoint point2 in item.Points)
				{
					_ = point2;
					cnt++;
				}
			}
			foreach (PointList item2 in Points2d)
			{
				foreach (ClothPoint point in item2.Points)
				{
					pos.Add(point.Pos.X / (double)cnt, point.Pos.Y / (double)cnt, point.Pos.Z / (double)cnt);
				}
			}
			return pos;
		}
	}

	public ClothPoint FirstPoint => Points2d[0].Points[0];

	public ClothPoint LastPoint
	{
		get
		{
			List<ClothPoint> points = Points2d[Points2d.Count - 1].Points;
			return points[points.Count - 1];
		}
	}

	public ClothPoint[] Ends => new ClothPoint[2] { FirstPoint, LastPoint };

	public static ClothSystem CreateCloth(ICoreAPI api, ClothManager cm, Vec3d start, Vec3d end)
	{
		return new ClothSystem(api, cm, start, end, EnumClothType.Cloth);
	}

	public static ClothSystem CreateRope(ICoreAPI api, ClothManager cm, Vec3d start, Vec3d end, AssetLocation clothSectionModel)
	{
		return new ClothSystem(api, cm, start, end, EnumClothType.Rope, clothSectionModel);
	}

	private ClothSystem()
	{
	}

	public bool ChangeRopeLength(double len)
	{
		PointList plist = Points2d[0];
		double currentLength = (float)plist.Points.Count / Resolution;
		bool isAdd = len > 0.0;
		if (isAdd && len + currentLength > maxLen)
		{
			return false;
		}
		if (!isAdd && len + currentLength < minLen)
		{
			return false;
		}
		int pointIndex = plist.Points.Max((ClothPoint p) => p.PointIndex) + 1;
		ClothPoint fp = FirstPoint;
		Entity pine = fp.PinnedToEntity;
		BlockPos pinb = fp.PinnedToBlockPos;
		Vec3f pino = fp.pinnedToOffset;
		fp.UnPin();
		float step = 1f / Resolution;
		int totalPoints = Math.Abs((int)(len * (double)Resolution));
		if (isAdd)
		{
			for (int i = 0; i <= totalPoints; i++)
			{
				plist.Points.Insert(0, new ClothPoint(this, pointIndex++, fp.Pos.X + (double)(step * (float)(i + 1)), fp.Pos.Y, fp.Pos.Z));
				ClothPoint p2 = plist.Points[0];
				ClothPoint p3 = plist.Points[1];
				ClothConstraint constraint = new ClothConstraint(p2, p3);
				Constraints.Add(constraint);
			}
		}
		else
		{
			for (int j = 0; j <= totalPoints; j++)
			{
				ClothPoint point = plist.Points[0];
				plist.Points.RemoveAt(0);
				for (int k = 0; k < Constraints.Count; k++)
				{
					ClothConstraint c = Constraints[k];
					if (c.Point1 == point || c.Point2 == point)
					{
						Constraints.RemoveAt(k);
						k--;
					}
				}
			}
		}
		if (pine != null)
		{
			FirstPoint.PinTo(pine, pino);
		}
		if (pinb != null)
		{
			FirstPoint.PinTo(pinb, pino);
		}
		genDebugMesh();
		return true;
	}

	private ClothSystem(ICoreAPI api, ClothManager cm, Vec3d start, Vec3d end, EnumClothType clothType, AssetLocation ropeSectionModel = null)
	{
		this.clothType = clothType;
		this.ropeSectionModel = ropeSectionModel;
		Init(api, cm);
		_ = 1f / Resolution;
		Vec3d dir = end - start;
		if (clothType == EnumClothType.Rope)
		{
			double num = dir.Length();
			PointList plist = new PointList();
			Points2d.Add(plist);
			int totalPoints = (int)(num * (double)Resolution);
			for (int i = 0; i <= totalPoints; i++)
			{
				float t = (float)i / (float)totalPoints;
				plist.Points.Add(new ClothPoint(this, i, start.X + dir.X * (double)t, start.Y + dir.Y * (double)t, start.Z + dir.Z * (double)t));
				if (i > 0)
				{
					ClothPoint p5 = plist.Points[i - 1];
					ClothPoint p4 = plist.Points[i];
					ClothConstraint constraint3 = new ClothConstraint(p5, p4);
					Constraints.Add(constraint3);
				}
			}
		}
		if (clothType != EnumClothType.Cloth)
		{
			return;
		}
		double hlen = (end - start).HorLength();
		double vlen = Math.Abs(end.Y - start.Y);
		int hleni = (int)(hlen * (double)Resolution);
		int vleni = (int)(vlen * (double)Resolution);
		int index = 0;
		for (int a = 0; a < hleni; a++)
		{
			Points2d.Add(new PointList());
			for (int y = 0; y < vleni; y++)
			{
				double th = (double)a / hlen;
				double tv = (double)y / vlen;
				Points2d[a].Points.Add(new ClothPoint(this, index++, start.X + dir.X * th, start.Y + dir.Y * tv, start.Z + dir.Z * th));
				if (a > 0)
				{
					ClothPoint p6 = Points2d[a - 1].Points[y];
					ClothPoint p3 = Points2d[a].Points[y];
					ClothConstraint constraint2 = new ClothConstraint(p6, p3);
					Constraints.Add(constraint2);
				}
				if (y > 0)
				{
					ClothPoint p7 = Points2d[a].Points[y - 1];
					ClothPoint p2 = Points2d[a].Points[y];
					ClothConstraint constraint = new ClothConstraint(p7, p2);
					Constraints.Add(constraint);
				}
			}
		}
	}

	public void genDebugMesh()
	{
		if (capi != null)
		{
			debugMeshRef?.Dispose();
			debugUpdateMesh = new MeshData(20, 15, withNormals: false, withUv: false);
			int vertexIndex = 0;
			for (int i = 0; i < Constraints.Count; i++)
			{
				_ = Constraints[i];
				int color = ((i % 2 > 0) ? (-1) : ColorUtil.BlackArgb);
				debugUpdateMesh.AddVertexSkipTex(0f, 0f, 0f, color);
				debugUpdateMesh.AddVertexSkipTex(0f, 0f, 0f, color);
				debugUpdateMesh.AddIndex(vertexIndex++);
				debugUpdateMesh.AddIndex(vertexIndex++);
			}
			debugUpdateMesh.mode = EnumDrawMode.Lines;
			debugMeshRef = capi.Render.UploadMesh(debugUpdateMesh);
			debugUpdateMesh.Indices = null;
			debugUpdateMesh.Rgba = null;
		}
	}

	public void Init(ICoreAPI api, ClothManager cm)
	{
		this.api = api;
		capi = api as ICoreClientAPI;
		pp = cm.partPhysics;
		noiseGen = NormalizedSimplexNoise.FromDefaultOctaves(4, 100.0, 0.9, api.World.Seed + CenterPosition.GetHashCode());
	}

	public void WalkPoints(Action<ClothPoint> onPoint)
	{
		foreach (PointList item in Points2d)
		{
			foreach (ClothPoint point in item.Points)
			{
				onPoint(point);
			}
		}
	}

	public int UpdateMesh(MeshData updateMesh, float dt)
	{
		CustomMeshDataPartFloat cfloats = updateMesh.CustomFloats;
		Vec3d campos = capi.World.Player.Entity.CameraPos;
		int basep = cfloats.Count;
		Vec4f lightRgba = new Vec4f();
		if (Constraints.Count > 0)
		{
			lightRgba = api.World.BlockAccessor.GetLightRGBs(Constraints[Constraints.Count / 2].Point1.Pos.AsBlockPos);
		}
		for (int i = 0; i < Constraints.Count; i++)
		{
			ClothConstraint clothConstraint = Constraints[i];
			Vec3d p1 = clothConstraint.Point1.Pos;
			Vec3d p2 = clothConstraint.Point2.Pos;
			double dX = p1.X - p2.X;
			double dY = p1.Y - p2.Y;
			double dZ = p1.Z - p2.Z;
			float yaw = (float)Math.Atan2(dX, dZ) + (float)Math.PI / 2f;
			float pitch = (float)Math.Atan2(Math.Sqrt(dZ * dZ + dX * dX), dY) + (float)Math.PI / 2f;
			double nowx = p1.X + (p1.X - p2.X) / 2.0;
			double nowy = p1.Y + (p1.Y - p2.Y) / 2.0;
			double nowz = p1.Z + (p1.Z - p2.Z) / 2.0;
			distToCam.Set((float)(nowx - campos.X), (float)(nowy - campos.Y), (float)(nowz - campos.Z));
			Mat4f.Identity(tmpMat);
			Mat4f.Translate(tmpMat, tmpMat, 0f, 1f / 32f, 0f);
			Mat4f.Translate(tmpMat, tmpMat, distToCam.X, distToCam.Y, distToCam.Z);
			Mat4f.RotateY(tmpMat, tmpMat, yaw);
			Mat4f.RotateZ(tmpMat, tmpMat, pitch);
			float roll = (float)i / 5f;
			Mat4f.RotateX(tmpMat, tmpMat, roll);
			float length = GameMath.Sqrt(dX * dX + dY * dY + dZ * dZ);
			Mat4f.Scale(tmpMat, tmpMat, new float[3] { length, 1f, 1f });
			Mat4f.Translate(tmpMat, tmpMat, -1.5f, -1f / 32f, -0.5f);
			int j = basep + i * 20;
			cfloats.Values[j++] = lightRgba.R;
			cfloats.Values[j++] = lightRgba.G;
			cfloats.Values[j++] = lightRgba.B;
			cfloats.Values[j++] = lightRgba.A;
			for (int k = 0; k < 16; k++)
			{
				cfloats.Values[j + k] = tmpMat[k];
			}
		}
		return Constraints.Count;
	}

	public void setRenderCenterPos()
	{
		for (int i = 0; i < Constraints.Count; i++)
		{
			ClothConstraint clothConstraint = Constraints[i];
			Vec3d start = clothConstraint.Point1.Pos;
			Vec3d end = clothConstraint.Point2.Pos;
			double nowx = start.X + (start.X - end.X) / 2.0;
			double nowy = start.Y + (start.Y - end.Y) / 2.0;
			double nowz = start.Z + (start.Z - end.Z) / 2.0;
			clothConstraint.renderCenterPos.X = nowx;
			clothConstraint.renderCenterPos.Y = nowy;
			clothConstraint.renderCenterPos.Z = nowz;
		}
	}

	public void CustomRender(float dt)
	{
		if (LineDebug && capi != null)
		{
			if (debugMeshRef == null)
			{
				genDebugMesh();
			}
			BlockPos originPos = CenterPosition.AsBlockPos;
			for (int i = 0; i < Constraints.Count; i++)
			{
				ClothConstraint clothConstraint = Constraints[i];
				Vec3d p1 = clothConstraint.Point1.Pos;
				Vec3d p2 = clothConstraint.Point2.Pos;
				debugUpdateMesh.xyz[i * 6] = (float)(p1.X - (double)originPos.X);
				debugUpdateMesh.xyz[i * 6 + 1] = (float)(p1.Y - (double)originPos.Y) + 0.005f;
				debugUpdateMesh.xyz[i * 6 + 2] = (float)(p1.Z - (double)originPos.Z);
				debugUpdateMesh.xyz[i * 6 + 3] = (float)(p2.X - (double)originPos.X);
				debugUpdateMesh.xyz[i * 6 + 4] = (float)(p2.Y - (double)originPos.Y) + 0.005f;
				debugUpdateMesh.xyz[i * 6 + 5] = (float)(p2.Z - (double)originPos.Z);
			}
			capi.Render.UpdateMesh(debugMeshRef, debugUpdateMesh);
			IShaderProgram program = capi.Shader.GetProgram(23);
			program.Use();
			capi.Render.LineWidth = 6f;
			capi.Render.BindTexture2d(0);
			capi.Render.GLDisableDepthTest();
			Vec3d cameraPos = capi.World.Player.Entity.CameraPos;
			mat.Set(capi.Render.CameraMatrixOrigin);
			mat.Translate((float)((double)originPos.X - cameraPos.X), (float)((double)originPos.Y - cameraPos.Y), (float)((double)originPos.Z - cameraPos.Z));
			program.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
			program.UniformMatrix("modelViewMatrix", mat.Values);
			capi.Render.RenderMesh(debugMeshRef);
			program.Stop();
			capi.Render.GLEnableDepthTest();
		}
	}

	public void updateFixedStep(float dt)
	{
		accum += dt;
		if (accum > 1f)
		{
			accum = 0.25f;
		}
		float step = pp.PhysicsTickTime;
		while (accum >= step)
		{
			accum -= step;
			tickNow(step);
		}
	}

	private void tickNow(float pdt)
	{
		for (int j = Constraints.Count - 1; j >= 0; j--)
		{
			Constraints[j].satisfy(pdt);
		}
		for (int i = Points2d.Count - 1; i >= 0; i--)
		{
			for (int k = Points2d[i].Points.Count - 1; k >= 0; k--)
			{
				Points2d[i].Points[k].update(pdt, api.World);
			}
		}
	}

	public void slowTick3s()
	{
		if (!double.IsNaN(CenterPosition.X))
		{
			windSpeed = api.World.BlockAccessor.GetWindSpeedAt(CenterPosition) * (0.2 + noiseGen.Noise(0.0, api.World.Calendar.TotalHours * 50.0 % 2000.0) * 0.8);
		}
	}

	public void restoreReferences()
	{
		if (!Active)
		{
			return;
		}
		Dictionary<int, ClothPoint> pointsByIndex = new Dictionary<int, ClothPoint>();
		WalkPoints(delegate(ClothPoint p)
		{
			pointsByIndex[p.PointIndex] = p;
			p.restoreReferences(this, api.World);
		});
		foreach (ClothConstraint constraint in Constraints)
		{
			constraint.RestorePoints(pointsByIndex);
		}
	}

	public void updateActiveState(EnumActiveStateChange stateChange)
	{
		if ((!Active || stateChange != EnumActiveStateChange.RegionNowLoaded) && (Active || stateChange != EnumActiveStateChange.RegionNowUnloaded))
		{
			bool active = Active;
			Active = true;
			WalkPoints(delegate(ClothPoint p)
			{
				Active &= api.World.BlockAccessor.GetChunkAtBlockPos((int)p.Pos.X, (int)p.Pos.Y, (int)p.Pos.Z) != null;
			});
			if (!active && Active)
			{
				restoreReferences();
			}
		}
	}

	public void CollectDirtyPoints(List<ClothPointPacket> packets)
	{
		for (int i = 0; i < Points2d.Count; i++)
		{
			for (int j = 0; j < Points2d[i].Points.Count; j++)
			{
				ClothPoint point = Points2d[i].Points[j];
				if (point.Dirty)
				{
					packets.Add(new ClothPointPacket
					{
						ClothId = ClothId,
						PointX = i,
						PointY = j,
						Point = point
					});
					point.Dirty = false;
				}
			}
		}
	}

	public void updatePoint(ClothPointPacket msg)
	{
		if (msg.PointX >= Points2d.Count)
		{
			api.Logger.Error($"ClothSystem: {ClothId} got invalid Points2d update index for {msg.PointX}/{Points2d.Count}");
		}
		else if (msg.PointY >= Points2d[msg.PointX].Points.Count)
		{
			api.Logger.Error($"ClothSystem: {ClothId} got invalid Points2d[{msg.PointX}] update index for {msg.PointY}/{Points2d[msg.PointX].Points.Count}");
		}
		else
		{
			Points2d[msg.PointX].Points[msg.PointY].updateFromPoint(msg.Point, api.World);
		}
	}

	public void OnPinnnedEntityLoaded(Entity entity)
	{
		if (FirstPoint.pinnedToEntityId == entity.EntityId)
		{
			FirstPoint.restoreReferences(entity);
		}
		if (LastPoint.pinnedToEntityId == entity.EntityId)
		{
			LastPoint.restoreReferences(entity);
		}
	}
}
