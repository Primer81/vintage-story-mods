using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class LightningFlash : IDisposable
{
	private MeshRef quadRef;

	private Vec4f color;

	private float linewidth;

	public List<Vec3d> points;

	public LightiningPointLight[] pointLights = new LightiningPointLight[2];

	public Vec3d origin;

	public float secondsAlive;

	public bool Alive = true;

	private ICoreAPI api;

	private ICoreClientAPI capi;

	public float flashAccum;

	public float rndVal;

	public float advanceWaitSec;

	private bool soundPlayed;

	private WeatherSystemBase weatherSys;

	private Random rand;

	public LightningFlash(WeatherSystemBase weatherSys, ICoreAPI api, int? seed, Vec3d startpoint)
	{
		this.weatherSys = weatherSys;
		capi = api as ICoreClientAPI;
		this.api = api;
		rand = new Random((!seed.HasValue) ? capi.World.Rand.Next() : seed.Value);
		color = new Vec4f(1f, 1f, 1f, 1f);
		linewidth = 0.33f;
		origin = startpoint.Clone();
		origin.Y = api.World.BlockAccessor.GetRainMapHeightAt((int)origin.X, (int)origin.Z) + 1;
	}

	public void ClientInit()
	{
		genPoints(weatherSys);
		genMesh(points);
		float b = 200f;
		pointLights[0] = new LightiningPointLight(new Vec3f(b, b, b), points[0].AddCopy(origin));
		pointLights[1] = new LightiningPointLight(new Vec3f(0f, 0f, 0f), points[points.Count - 1].AddCopy(origin));
		capi.Render.AddPointLight(pointLights[0]);
		capi.Render.AddPointLight(pointLights[1]);
		Vec3d lp = points[points.Count - 1];
		Vec3d pos = origin + lp;
		float dist = (float)capi.World.Player.Entity.Pos.DistanceTo(pos);
		if (dist < 150f)
		{
			AssetLocation loc = new AssetLocation("sounds/weather/lightning-verynear.ogg");
			capi.World.PlaySoundAt(loc, 0.0, 0.0, 0.0, null, EnumSoundType.Weather, 1f, 32f, 1f - dist / 180f);
		}
		else if (dist < 200f)
		{
			AssetLocation loc2 = new AssetLocation("sounds/weather/lightning-near.ogg");
			capi.World.PlaySoundAt(loc2, 0.0, 0.0, 0.0, null, EnumSoundType.Weather, 1f, 32f, 1f - dist / 250f);
		}
		else if (dist < 320f)
		{
			AssetLocation loc3 = new AssetLocation("sounds/weather/lightning-distant.ogg");
			capi.World.PlaySoundAt(loc3, 0.0, 0.0, 0.0, null, EnumSoundType.Weather, 1f, 32f, 1f - dist / 500f);
		}
	}

	protected void genPoints(WeatherSystemBase weatherSys)
	{
		Vec3d pos = new Vec3d();
		points = new List<Vec3d>();
		pos.Y = 0.0;
		float startY = (float)((double)(weatherSys.CloudLevelRel * (float)capi.World.BlockAccessor.MapSizeY + 2f) - origin.Y);
		while (pos.Y < (double)startY)
		{
			points.Add(pos.Clone());
			pos.Y += rand.NextDouble();
			pos.X += rand.NextDouble() * 2.0 - 1.0;
			pos.Z += rand.NextDouble() * 2.0 - 1.0;
		}
		if (points.Count == 0)
		{
			points.Add(pos.Clone());
		}
		points.Reverse();
	}

	protected void genMesh(List<Vec3d> points)
	{
		float[] data = new float[points.Count * 3];
		for (int i = 0; i < points.Count; i++)
		{
			Vec3d point = points[i];
			data[i * 3] = (float)point.X;
			data[i * 3 + 1] = (float)point.Y;
			data[i * 3 + 2] = (float)point.Z;
		}
		quadRef?.Dispose();
		MeshData quadMesh = CubeMeshUtil.GetCube(0.5f, 0.5f, 0.5f, new Vec3f(0f, 0f, 0f));
		quadMesh.Flags = null;
		quadMesh.Rgba = null;
		quadMesh.CustomFloats = new CustomMeshDataPartFloat
		{
			Instanced = true,
			InterleaveOffsets = new int[2] { 0, 12 },
			InterleaveSizes = new int[2] { 3, 3 },
			InterleaveStride = 12,
			StaticDraw = false,
			Values = data,
			Count = data.Length
		};
		MeshData updateMesh = new MeshData(initialiseArrays: false);
		updateMesh.CustomFloats = quadMesh.CustomFloats;
		quadRef = capi.Render.UploadMesh(quadMesh);
		capi.Render.UpdateMesh(quadRef, updateMesh);
	}

	public void GameTick(float dt)
	{
		dt *= 3f;
		if (rand.NextDouble() < 0.4 && (double)(secondsAlive * 10f) < 0.6 && advanceWaitSec <= 0f)
		{
			advanceWaitSec = 0.05f + (float)rand.NextDouble() / 10f;
		}
		secondsAlive += Math.Max(0f, dt - advanceWaitSec);
		advanceWaitSec = Math.Max(0f, advanceWaitSec - dt);
		if ((double)secondsAlive > 0.7)
		{
			Alive = false;
		}
		if (api.Side != EnumAppSide.Server || !(secondsAlive > 0f))
		{
			return;
		}
		weatherSys.TriggerOnLightningImpactEnd(origin, out var handling);
		if (handling != 0 || !api.World.Config.GetBool("lightningDamage", defaultValue: true))
		{
			return;
		}
		DamageSource dmgSrc = new DamageSource
		{
			KnockbackStrength = 2f,
			Source = EnumDamageSource.Weather,
			Type = EnumDamageType.Electricity,
			SourcePos = origin,
			HitPosition = new Vec3d()
		};
		api.ModLoader.GetModSystem<EntityPartitioning>().WalkEntities(origin, 8.0, delegate(Entity entity)
		{
			if (!entity.IsInteractable)
			{
				return true;
			}
			float damage = 6f;
			entity.ReceiveDamage(dmgSrc, damage);
			return true;
		}, EnumEntitySearchType.Creatures);
	}

	public void Render(float dt)
	{
		GameTick(dt);
		capi.Render.CurrentActiveShader.Uniform("color", this.color);
		capi.Render.CurrentActiveShader.Uniform("lineWidth", linewidth);
		IClientPlayer plr = capi.World.Player;
		Vec3d camPos = plr.Entity.CameraPos;
		capi.Render.CurrentActiveShader.Uniform("origin", new Vec3f((float)(origin.X - camPos.X), (float)(origin.Y - camPos.Y), (float)(origin.Z - camPos.Z)));
		double cntRel = GameMath.Clamp(secondsAlive * 10f, 0f, 1f);
		int instanceCount = (int)(cntRel * (double)points.Count) - 1;
		if (instanceCount > 0)
		{
			capi.Render.RenderMeshInstanced(quadRef, instanceCount);
		}
		if (cntRel >= 0.9 && !soundPlayed)
		{
			soundPlayed = true;
			Vec3d lp = points[points.Count - 1];
			Vec3d pos = origin + lp;
			float dist = (float)plr.Entity.Pos.DistanceTo(pos);
			if (dist < 150f)
			{
				AssetLocation loc = new AssetLocation("sounds/weather/lightning-nodistance.ogg");
				capi.World.PlaySoundAt(loc, 0.0, 0.0, 0.0, null, EnumSoundType.Weather, 1f, 32f, Math.Max(0.1f, 1f - dist / 70f));
			}
			if (dist < 100f)
			{
				(weatherSys as WeatherSystemClient).simLightning.lightningTime = 0.3f + (float)rand.NextDouble() * 0.17f;
				(weatherSys as WeatherSystemClient).simLightning.lightningIntensity = 1.5f + (float)rand.NextDouble() * 0.4f;
				int sub = Math.Max(0, (int)dist - 5) * 3;
				int color = ColorUtil.ToRgba(255, 255, 255, 255);
				SimpleParticleProperties props = new SimpleParticleProperties(250 - sub, 300 - sub, color, pos.AddCopy(-0.5f, 0f, -0.5f), pos.AddCopy(0.5f, 1f, 0.5f), new Vec3f(-5f, 0f, -5f), new Vec3f(5f, 10f, 5f), 3f, 0.3f, 0.4f, 2f);
				props.VertexFlags = 255;
				props.LightEmission = int.MaxValue;
				props.ShouldDieInLiquid = true;
				props.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.LINEARREDUCE, 1f);
				capi.World.SpawnParticles(props);
				props.ParticleModel = EnumParticleModel.Quad;
				props.MinSize /= 2f;
				props.MaxSize /= 2f;
				capi.World.SpawnParticles(props);
			}
		}
		flashAccum += dt;
		if (flashAccum > rndVal)
		{
			rndVal = (float)rand.NextDouble() / 10f;
			flashAccum = 0f;
			float bnorm = (float)rand.NextDouble();
			float b = 50f + bnorm * 150f;
			pointLights[0].Color.Set(b, b, b);
			linewidth = (0.4f + 0.6f * bnorm) / 3f;
			if (cntRel < 1.0)
			{
				b = 0f;
			}
			pointLights[1].Color.Set(b, b, b);
		}
	}

	public void Dispose()
	{
		quadRef?.Dispose();
		capi?.Render.RemovePointLight(pointLights[0]);
		capi?.Render.RemovePointLight(pointLights[1]);
	}
}
