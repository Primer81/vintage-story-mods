using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class WeatherSimulationParticles
{
	private WeatherSystemClient ws;

	private ICoreClientAPI capi;

	private Random rand;

	private static int[,] lowResRainHeightMap;

	private static BlockPos centerPos;

	public static int waterColor;

	public static int lowStabColor;

	public int rainParticleColor;

	public static SimpleParticleProperties splashParticles;

	public static WeatherParticleProps stormDustParticles;

	public static SimpleParticleProperties stormWaterParticles;

	public static WeatherParticleProps rainParticle;

	public static WeatherParticleProps hailParticle;

	private static WeatherParticleProps snowParticle;

	private Block lblock;

	private Vec3f parentVeloSnow = new Vec3f();

	private BlockPos tmpPos = new BlockPos();

	private Vec3d particlePos = new Vec3d();

	private AmbientModifier desertStormAmbient;

	private int spawnCount;

	private float sandFinds;

	private int dustParticlesPerTick = 30;

	private float[] sandCountByBlock;

	private float[] targetFogColor = new float[3];

	private float targetFogDensity;

	private Dictionary<int, int> indicesBySandBlockId = new Dictionary<int, int>();

	private float accum;

	private Vec3f windSpeed;

	private float windSpeedIntensity;

	static WeatherSimulationParticles()
	{
		lowResRainHeightMap = new int[16, 16];
		centerPos = new BlockPos();
		waterColor = ColorUtil.ToRgba(230, 128, 178, 255);
		lowStabColor = ColorUtil.ToRgba(230, 207, 53, 10);
		splashParticles = new SimpleParticleProperties
		{
			MinPos = new Vec3d(),
			AddPos = new Vec3d(1.0, 0.25, 0.0),
			MinQuantity = 0f,
			AddQuantity = 3f,
			Color = ColorUtil.ToRgba(230, 128, 178, 200),
			GravityEffect = 1f,
			WithTerrainCollision = true,
			ParticleModel = EnumParticleModel.Quad,
			LifeLength = 0.5f,
			MinVelocity = new Vec3f(-1f, 2f, -1f),
			AddVelocity = new Vec3f(2f, 0f, 2f),
			MinSize = 0.07f,
			MaxSize = 0.2f,
			VertexFlags = 32
		};
		stormDustParticles = new WeatherParticleProps
		{
			MinPos = new Vec3d(),
			AddPos = new Vec3d(),
			MinQuantity = 0f,
			AddQuantity = 3f,
			Color = ColorUtil.ToRgba(100, 200, 200, 200),
			GravityEffect = 1f,
			WithTerrainCollision = true,
			ParticleModel = EnumParticleModel.Quad,
			LifeLength = 0.5f,
			MinVelocity = new Vec3f(-1f, 2f, -1f),
			AddVelocity = new Vec3f(2f, 0f, 2f),
			MinSize = 0.07f,
			MaxSize = 0.1f
		};
		stormWaterParticles = new SimpleParticleProperties
		{
			MinPos = new Vec3d(),
			AddPos = new Vec3d(),
			MinQuantity = 0f,
			AddQuantity = 3f,
			Color = ColorUtil.ToRgba(230, 128, 178, 200),
			GravityEffect = 1f,
			WithTerrainCollision = true,
			ParticleModel = EnumParticleModel.Quad,
			LifeLength = 0.5f,
			MinVelocity = new Vec3f(-1f, 2f, -1f),
			AddVelocity = new Vec3f(2f, 0f, 2f),
			MinSize = 0.07f,
			MaxSize = 0.2f,
			VertexFlags = 0
		};
		rainParticle = new WeatherParticleProps
		{
			MinPos = new Vec3d(),
			AddPos = new Vec3d(60.0, 9.0, 60.0),
			MinQuantity = 300f,
			AddQuantity = 25f,
			Color = waterColor,
			GravityEffect = 1f,
			WithTerrainCollision = false,
			DieOnRainHeightmap = true,
			ShouldDieInLiquid = true,
			ParticleModel = EnumParticleModel.Quad,
			LifeLength = 1.5f,
			MinVelocity = new Vec3f(-0.25f, -0.25f, -0.25f),
			AddVelocity = new Vec3f(0.5f, 0f, 0.5f),
			MinSize = 0.15f,
			MaxSize = 0.22f,
			VertexFlags = -2147483616
		};
		hailParticle = new HailParticleProps
		{
			MinPos = new Vec3d(),
			AddPos = new Vec3d(60.0, 0.0, 60.0),
			MinQuantity = 50f,
			AddQuantity = 25f,
			Color = ColorUtil.ToRgba(255, 255, 255, 255),
			GravityEffect = 1f,
			WithTerrainCollision = true,
			DieOnRainHeightmap = false,
			ShouldDieInLiquid = false,
			ShouldSwimOnLiquid = true,
			ParticleModel = EnumParticleModel.Cube,
			LifeLength = 3f,
			MinVelocity = new Vec3f(-1f, -2f, -1f),
			AddVelocity = new Vec3f(2f, 0f, 2f),
			MinSize = 0.1f,
			MaxSize = 0.14f,
			WindAffectednes = 0f,
			ParentVelocity = null,
			Bounciness = 0.3f
		};
		snowParticle = new WeatherParticleProps
		{
			MinPos = new Vec3d(),
			AddPos = new Vec3d(60.0, 0.0, 60.0),
			MinQuantity = 80f,
			AddQuantity = 15f,
			Color = ColorUtil.ToRgba(200, 255, 255, 255),
			GravityEffect = 0.003f,
			WithTerrainCollision = true,
			DieOnRainHeightmap = false,
			ShouldDieInLiquid = false,
			ParticleModel = EnumParticleModel.Quad,
			LifeLength = 5f,
			MinVelocity = new Vec3f(-3.5f, -1.25f, -0.5f),
			AddVelocity = new Vec3f(1f, 0.05f, 1f),
			MinSize = 0.1f,
			MaxSize = 0.2f
		};
		stormDustParticles.lowResRainHeightMap = lowResRainHeightMap;
		hailParticle.lowResRainHeightMap = lowResRainHeightMap;
		snowParticle.lowResRainHeightMap = lowResRainHeightMap;
		rainParticle.lowResRainHeightMap = lowResRainHeightMap;
		stormDustParticles.centerPos = centerPos;
		hailParticle.centerPos = centerPos;
		snowParticle.centerPos = centerPos;
		rainParticle.centerPos = centerPos;
	}

	public WeatherSimulationParticles(ICoreClientAPI capi, WeatherSystemClient ws)
	{
		this.capi = capi;
		this.ws = ws;
		rand = new Random(capi.World.Seed + 223123123);
		rainParticleColor = waterColor;
		desertStormAmbient = new AmbientModifier().EnsurePopulated();
		desertStormAmbient.FogDensity = new WeightedFloat();
		desertStormAmbient.FogColor = new WeightedFloatArray
		{
			Value = new float[3]
		};
		desertStormAmbient.FogMin = new WeightedFloat();
		capi.Ambient.CurrentModifiers["desertstorm"] = desertStormAmbient;
	}

	public void Initialize()
	{
		lblock = capi.World.GetBlock(new AssetLocation("water-still-7"));
		if (lblock == null)
		{
			return;
		}
		capi.Event.RegisterAsyncParticleSpawner(asyncParticleSpawn);
		capi.Event.RegisterRenderer(new DummyRenderer
		{
			action = desertStormSim
		}, EnumRenderStage.Before);
		int i = 0;
		foreach (Block block in capi.World.Blocks)
		{
			if (block.BlockMaterial == EnumBlockMaterial.Sand)
			{
				indicesBySandBlockId[block.Id] = i++;
			}
		}
		sandCountByBlock = new float[indicesBySandBlockId.Count];
	}

	private void desertStormSim(float dt)
	{
		accum += dt;
		if (accum > 2f)
		{
			int cnt = spawnCount;
			float sum = sandFinds;
			float[] sandBlocks = sandCountByBlock;
			if (cnt > 10 && sum > 0f)
			{
				sandCountByBlock = new float[indicesBySandBlockId.Count];
				spawnCount = 0;
				sandFinds = 0f;
				_ = ws.BlendedWeatherData;
				BlockPos bpos = capi.World.Player.Entity.Pos.AsBlockPos;
				ClimateCondition climate = capi.World.BlockAccessor.GetClimateAt(bpos);
				float sunlightrel = (float)capi.World.BlockAccessor.GetLightLevel(bpos, EnumLightLevelType.OnlySunLight) / 22f;
				float climateWeight = 2f * Math.Max(0f, windSpeedIntensity - 0.65f) * (1f - climate.WorldgenRainfall) * (1f - climate.Rainfall);
				BlockPos pos = capi.World.Player.Entity.Pos.AsBlockPos;
				targetFogColor[0] = (targetFogColor[1] = (targetFogColor[2] = 0f));
				foreach (KeyValuePair<int, int> val in indicesBySandBlockId)
				{
					float weight = sandBlocks[val.Value] / sum;
					double[] colparts = ColorUtil.ToRGBADoubles(capi.World.GetBlock(val.Key).GetColor(capi, pos));
					targetFogColor[0] += (float)colparts[2] * weight;
					targetFogColor[1] += (float)colparts[1] * weight;
					targetFogColor[2] += (float)colparts[0] * weight;
				}
				float sandRatio = (float)((double)sum / 30.0 / (double)cnt) * climateWeight * sunlightrel;
				targetFogDensity = sandRatio;
			}
			accum = 0f;
		}
		float dtf = dt / 3f;
		targetFogDensity = Math.Max(0f, targetFogDensity - 2f * WeatherSystemClient.CurrentEnvironmentWetness4h);
		desertStormAmbient.FogColor.Value[0] += (targetFogColor[0] - desertStormAmbient.FogColor.Value[0]) * dtf;
		desertStormAmbient.FogColor.Value[1] += (targetFogColor[1] - desertStormAmbient.FogColor.Value[1]) * dtf;
		desertStormAmbient.FogColor.Value[2] += (targetFogColor[2] - desertStormAmbient.FogColor.Value[2]) * dtf;
		desertStormAmbient.FogDensity.Value += ((float)Math.Pow(targetFogDensity, 1.2000000476837158) - desertStormAmbient.FogDensity.Value) * dtf;
		desertStormAmbient.FogDensity.Weight += (targetFogDensity - desertStormAmbient.FogDensity.Weight) * dtf;
		desertStormAmbient.FogColor.Weight += (Math.Min(1f, 2f * targetFogDensity) - desertStormAmbient.FogColor.Weight) * dtf;
	}

	private bool asyncParticleSpawn(float dt, IAsyncParticleManager manager)
	{
		WeatherDataSnapshot weatherData = ws.BlendedWeatherData;
		ClimateCondition conds = ws.clientClimateCond;
		if (conds == null || !ws.playerChunkLoaded)
		{
			return true;
		}
		EntityPos plrPos = capi.World.Player.Entity.Pos;
		float precIntensity = conds.Rainfall;
		float plevel = precIntensity * (float)capi.Settings.Int["particleLevel"] / 100f;
		float dryness = GameMath.Clamp(1f - precIntensity, 0f, 1f);
		tmpPos.Set((int)plrPos.X, (int)plrPos.Y, (int)plrPos.Z);
		precIntensity = Math.Max(0f, precIntensity - (float)Math.Max(0.0, (plrPos.Y - (double)capi.World.SeaLevel - 5000.0) / 10000.0));
		EnumPrecipitationType precType = weatherData.BlendedPrecType;
		if (precType == EnumPrecipitationType.Auto)
		{
			precType = ((conds.Temperature < weatherData.snowThresholdTemp) ? EnumPrecipitationType.Snow : EnumPrecipitationType.Rain);
		}
		int rainYPos = capi.World.BlockAccessor.GetRainMapHeightAt((int)particlePos.X, (int)particlePos.Z);
		particlePos.Set(capi.World.Player.Entity.Pos.X, rainYPos, capi.World.Player.Entity.Pos.Z);
		int onwaterSplashParticleColor = capi.World.ApplyColorMapOnRgba(lblock.ClimateColorMapResolved, lblock.SeasonColorMapResolved, -1, (int)particlePos.X, (int)particlePos.Y, (int)particlePos.Z, flipRb: false);
		byte[] col = ColorUtil.ToBGRABytes(onwaterSplashParticleColor);
		onwaterSplashParticleColor = ColorUtil.ToRgba(94, col[0], col[1], col[2]);
		centerPos.Set((int)particlePos.X, 0, (int)particlePos.Z);
		for (int lx = 0; lx < 16; lx++)
		{
			int dx = (lx - 8) * 4;
			for (int lz = 0; lz < 16; lz++)
			{
				int dz = (lz - 8) * 4;
				lowResRainHeightMap[lx, lz] = capi.World.BlockAccessor.GetRainMapHeightAt(centerPos.X + dx, centerPos.Z + dz);
			}
		}
		windSpeed = capi.World.BlockAccessor.GetWindSpeedAt(plrPos.XYZ).ToVec3f();
		windSpeedIntensity = windSpeed.Length();
		parentVeloSnow.X = (0f - Math.Max(0f, windSpeed.X / 2f - 0.15f)) * 2f;
		parentVeloSnow.Y = 0f;
		parentVeloSnow.Z = (0f - Math.Max(0f, windSpeed.Z / 2f - 0.15f)) * 2f;
		if (windSpeedIntensity > 0.5f)
		{
			SpawnDustParticles(manager, weatherData, plrPos, dryness, onwaterSplashParticleColor);
		}
		particlePos.Y = capi.World.Player.Entity.Pos.Y;
		if ((double)precIntensity <= 0.02)
		{
			return true;
		}
		switch (precType)
		{
		case EnumPrecipitationType.Hail:
			SpawnHailParticles(manager, weatherData, conds, plrPos, plevel);
			return true;
		case EnumPrecipitationType.Rain:
			SpawnRainParticles(manager, weatherData, conds, plrPos, plevel, onwaterSplashParticleColor);
			break;
		}
		if (precType == EnumPrecipitationType.Snow)
		{
			SpawnSnowParticles(manager, weatherData, conds, plrPos, plevel);
		}
		return true;
	}

	private void SpawnDustParticles(IAsyncParticleManager manager, WeatherDataSnapshot weatherData, EntityPos plrPos, float dryness, int onwaterSplashParticleColor)
	{
		float dx = (float)(plrPos.Motion.X * 40.0) - 50f * windSpeed.X;
		float dy = (float)(plrPos.Motion.Y * 40.0);
		float dz = (float)(plrPos.Motion.Z * 40.0) - 50f * windSpeed.Z;
		double range = 40.0;
		float rangReduction = 1f - targetFogDensity;
		range *= (double)rangReduction;
		float intensity = windSpeed.Length();
		stormDustParticles.MinPos.Set(particlePos.X - range + (double)dx, particlePos.Y + 20.0 + (double)(5f * intensity) + (double)dy, particlePos.Z - range + (double)dz);
		stormDustParticles.AddPos.Set(2.0 * range, -30.0, 2.0 * range);
		stormDustParticles.GravityEffect = 0.1f;
		stormDustParticles.ParticleModel = EnumParticleModel.Quad;
		stormDustParticles.LifeLength = 1f;
		stormDustParticles.DieOnRainHeightmap = true;
		stormDustParticles.WindAffectednes = 8f;
		stormDustParticles.MinQuantity = 0f;
		stormDustParticles.AddQuantity = 8f * (intensity - 0.5f) * dryness;
		stormDustParticles.MinSize = 0.2f;
		stormDustParticles.MaxSize = 0.7f;
		stormDustParticles.MinVelocity.Set(-0.025f + 12f * windSpeed.X, 0f, -0.025f + 12f * windSpeed.Z).Mul(3f);
		stormDustParticles.AddVelocity.Set(0.05f + 6f * windSpeed.X, -0.25f, 0.05f + 6f * windSpeed.Z).Mul(3f);
		float extra = Math.Max(1f, intensity * 3f);
		int cnt = (int)((float)dustParticlesPerTick * extra);
		for (int j = 0; j < cnt; j++)
		{
			double px2 = particlePos.X + (double)dx + rand.NextDouble() * rand.NextDouble() * 60.0 * (double)(1 - 2 * rand.Next(2));
			double pz2 = particlePos.Z + (double)dz + rand.NextDouble() * rand.NextDouble() * 60.0 * (double)(1 - 2 * rand.Next(2));
			int py2 = capi.World.BlockAccessor.GetRainMapHeightAt((int)px2, (int)pz2);
			Block block2 = capi.World.BlockAccessor.GetBlock((int)px2, py2, (int)pz2);
			if (block2.Id != 0 && capi.World.BlockAccessor.GetBlock((int)px2, py2, (int)pz2, 2).Id == 0 && (block2.BlockMaterial == EnumBlockMaterial.Sand || block2.BlockMaterial == EnumBlockMaterial.Snow || (!(rand.NextDouble() < 0.699999988079071) && block2.RenderPass != EnumChunkRenderPass.TopSoil)))
			{
				if (block2.BlockMaterial == EnumBlockMaterial.Sand)
				{
					sandFinds += 1f / extra;
					sandCountByBlock[indicesBySandBlockId[block2.Id]] += 1f / extra;
				}
				if (!(Math.Abs((double)py2 - particlePos.Y) > 15.0))
				{
					tmpPos.Set((int)px2, py2, (int)pz2);
					stormDustParticles.Color = ColorUtil.ReverseColorBytes(block2.GetColor(capi, tmpPos));
					stormDustParticles.Color |= -16777216;
					manager.Spawn(stormDustParticles);
				}
			}
		}
		spawnCount++;
		if (!(intensity > 0.85f))
		{
			return;
		}
		stormWaterParticles.AddVelocity.Y = 1.5f;
		stormWaterParticles.LifeLength = 0.17f;
		stormWaterParticles.WindAffected = true;
		stormWaterParticles.WindAffectednes = 1f;
		stormWaterParticles.GravityEffect = 0.4f;
		stormWaterParticles.MinVelocity.Set(-0.025f + 4f * windSpeed.X, 1.5f, -0.025f + 4f * windSpeed.Z);
		stormWaterParticles.Color = onwaterSplashParticleColor;
		stormWaterParticles.MinQuantity = 1f;
		stormWaterParticles.AddQuantity = 5f;
		stormWaterParticles.ShouldDieInLiquid = false;
		splashParticles.WindAffected = true;
		splashParticles.WindAffectednes = 1f;
		for (int i = 0; i < 20; i++)
		{
			double px = particlePos.X + rand.NextDouble() * rand.NextDouble() * 40.0 * (double)(1 - 2 * rand.Next(2));
			double pz = particlePos.Z + rand.NextDouble() * rand.NextDouble() * 40.0 * (double)(1 - 2 * rand.Next(2));
			int py = capi.World.BlockAccessor.GetRainMapHeightAt((int)px, (int)pz);
			Block block = capi.World.BlockAccessor.GetBlock((int)px, py, (int)pz, 2);
			if (block.IsLiquid())
			{
				stormWaterParticles.MinPos.Set(px, (float)py + block.TopMiddlePos.Y, pz);
				stormWaterParticles.ParticleModel = EnumParticleModel.Cube;
				stormWaterParticles.MinSize = 0.4f;
				manager.Spawn(stormWaterParticles);
				splashParticles.MinPos.Set(px, (float)py + block.TopMiddlePos.Y - 0.125f, pz);
				splashParticles.MinVelocity.X = windSpeed.X * 1.5f;
				splashParticles.AddVelocity.Y = 1.5f;
				splashParticles.MinVelocity.Z = windSpeed.Z * 1.5f;
				splashParticles.LifeLength = 0.17f;
				splashParticles.Color = onwaterSplashParticleColor;
				manager.Spawn(splashParticles);
			}
		}
	}

	private void SpawnHailParticles(IAsyncParticleManager manager, WeatherDataSnapshot weatherData, ClimateCondition conds, EntityPos plrPos, float plevel)
	{
		float dx = (float)(plrPos.Motion.X * 40.0) - 4f * windSpeed.X;
		float dy = (float)(plrPos.Motion.Y * 40.0);
		float dz = (float)(plrPos.Motion.Z * 40.0) - 4f * windSpeed.Z;
		hailParticle.MinPos.Set(particlePos.X + (double)dx, particlePos.Y + 15.0 + (double)dy, particlePos.Z + (double)dz);
		hailParticle.MinSize = 0.3f * (0.5f + conds.Rainfall);
		hailParticle.MaxSize = 1f * (0.5f + conds.Rainfall);
		hailParticle.Color = ColorUtil.ToRgba(220, 210, 230, 255);
		hailParticle.MinQuantity = 100f * plevel;
		hailParticle.AddQuantity = 25f * plevel;
		hailParticle.MinVelocity.Set(-0.025f + 7.5f * windSpeed.X, -5f, -0.025f + 7.5f * windSpeed.Z);
		hailParticle.AddVelocity.Set(0.05f + 7.5f * windSpeed.X, 0.05f, 0.05f + 7.5f * windSpeed.Z);
		manager.Spawn(hailParticle);
	}

	private void SpawnRainParticles(IAsyncParticleManager manager, WeatherDataSnapshot weatherData, ClimateCondition conds, EntityPos plrPos, float plevel, int onwaterSplashParticleColor)
	{
		float dx = (float)(plrPos.Motion.X * 80.0);
		float dy = (float)(plrPos.Motion.Y * 80.0);
		float dz = (float)(plrPos.Motion.Z * 80.0);
		rainParticle.MinPos.Set(particlePos.X - 30.0 + (double)dx, particlePos.Y + 15.0 + (double)dy, particlePos.Z - 30.0 + (double)dz);
		rainParticle.WithTerrainCollision = false;
		rainParticle.MinQuantity = 1000f * plevel;
		rainParticle.LifeLength = 1f;
		rainParticle.AddQuantity = 25f * plevel;
		rainParticle.MinSize = 0.15f * (0.5f + conds.Rainfall);
		rainParticle.MaxSize = 0.22f * (0.5f + conds.Rainfall);
		rainParticle.Color = rainParticleColor;
		rainParticle.MinVelocity.Set(-0.025f + 8f * windSpeed.X, -10f, -0.025f + 8f * windSpeed.Z);
		rainParticle.AddVelocity.Set(0.05f + 8f * windSpeed.X, 0.05f, 0.05f + 8f * windSpeed.Z);
		manager.Spawn(rainParticle);
		splashParticles.MinVelocity = new Vec3f(-1f, 3f, -1f);
		splashParticles.AddVelocity = new Vec3f(2f, 0f, 2f);
		splashParticles.LifeLength = 0.1f;
		splashParticles.MinSize = 0.07f * (0.5f + 0.65f * conds.Rainfall);
		splashParticles.MaxSize = 0.2f * (0.5f + 0.65f * conds.Rainfall);
		splashParticles.ShouldSwimOnLiquid = true;
		splashParticles.Color = rainParticleColor;
		float cnt = 100f * plevel;
		for (int i = 0; (float)i < cnt; i++)
		{
			double px = particlePos.X + rand.NextDouble() * rand.NextDouble() * 60.0 * (double)(1 - 2 * rand.Next(2));
			double pz = particlePos.Z + rand.NextDouble() * rand.NextDouble() * 60.0 * (double)(1 - 2 * rand.Next(2));
			int py = capi.World.BlockAccessor.GetRainMapHeightAt((int)px, (int)pz);
			Block block = capi.World.BlockAccessor.GetBlock((int)px, py, (int)pz, 2);
			if (block.IsLiquid())
			{
				splashParticles.MinPos.Set(px, (float)py + block.TopMiddlePos.Y - 0.125f, pz);
				splashParticles.AddVelocity.Y = 1.5f;
				splashParticles.LifeLength = 0.17f;
				splashParticles.Color = onwaterSplashParticleColor;
			}
			else
			{
				if (block.BlockId == 0)
				{
					block = capi.World.BlockAccessor.GetBlock((int)px, py, (int)pz);
				}
				double b = 0.75 + 0.25 * rand.NextDouble();
				int ca = 230 - rand.Next(100);
				int cr = (int)((double)((rainParticleColor >> 16) & 0xFF) * b);
				int cg = (int)((double)((rainParticleColor >> 8) & 0xFF) * b);
				int cb = (int)((double)(rainParticleColor & 0xFF) * b);
				splashParticles.Color = (ca << 24) | (cr << 16) | (cg << 8) | cb;
				splashParticles.AddVelocity.Y = 0f;
				splashParticles.LifeLength = 0.1f;
				splashParticles.MinPos.Set(px, (double)((float)py + block.TopMiddlePos.Y) + 0.05, pz);
			}
			manager.Spawn(splashParticles);
		}
	}

	private void SpawnSnowParticles(IAsyncParticleManager manager, WeatherDataSnapshot weatherData, ClimateCondition conds, EntityPos plrPos, float plevel)
	{
		snowParticle.WindAffected = true;
		snowParticle.WindAffectednes = 1f;
		float wetness = 2.5f * GameMath.Clamp(ws.clientClimateCond.Temperature + 1f, 0f, 4f) / 4f;
		float num = (float)plrPos.Motion.X * 60f;
		float mz = (float)plrPos.Motion.Z * 60f;
		float horSpeedSqrt = (float)Math.Pow(num * num + mz * mz, 0.25);
		float dx = num - Math.Max(0f, (30f - 9f * wetness) * windSpeed.X - 5f * horSpeedSqrt);
		float dy = (float)(plrPos.Motion.Y * 60.0);
		float dz = mz - Math.Max(0f, (30f - 9f * wetness) * windSpeed.Z - 5f * horSpeedSqrt);
		snowParticle.MinVelocity.Set(-0.5f + 10f * windSpeed.X, -1f, -0.5f + 10f * windSpeed.Z);
		snowParticle.AddVelocity.Set(1f + 10f * windSpeed.X, 0.05f, 1f + 10f * windSpeed.Z);
		snowParticle.Color = ColorUtil.ToRgba(255, 255, 255, 255);
		snowParticle.MinQuantity = 100f * plevel * (1f + wetness / 3f);
		snowParticle.AddQuantity = 25f * plevel * (1f + wetness / 3f);
		snowParticle.ParentVelocity = parentVeloSnow;
		snowParticle.ShouldDieInLiquid = true;
		snowParticle.LifeLength = Math.Max(1f, 4f - wetness - windSpeedIntensity);
		snowParticle.Color = ColorUtil.ColorOverlay(ColorUtil.ToRgba(255, 255, 255, 255), rainParticle.Color, wetness / 4f);
		snowParticle.GravityEffect = 0.005f * (1f + 20f * wetness);
		snowParticle.MinSize = 0.1f * conds.Rainfall;
		snowParticle.MaxSize = 0.3f * conds.Rainfall / (1f + wetness);
		float hrange = 20f;
		float vrange = 23f + windSpeedIntensity * 5f;
		dy -= Math.Min(10f, horSpeedSqrt) + windSpeedIntensity * 5f;
		snowParticle.MinVelocity.Y = -2f;
		snowParticle.MinPos.Set(particlePos.X - (double)hrange + (double)dx, particlePos.Y + (double)vrange + (double)dy, particlePos.Z - (double)hrange + (double)dz);
		snowParticle.AddPos.Set(2f * hrange + dx, -0.66f * vrange + dy, 2f * hrange + dz);
		manager.Spawn(snowParticle);
	}
}
