using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class ModSystemAmbientParticles : ModSystem
{
	private SimpleParticleProperties liquidParticles;

	private SimpleParticleProperties summerAirParticles;

	private SimpleParticleProperties fireflyParticles;

	private ClampedSimplexNoise fireflyLocationNoise;

	private ClampedSimplexNoise fireflyrateNoise;

	private ICoreClientAPI capi;

	private ClimateCondition climate = new ClimateCondition();

	private bool spawnParticles;

	private Vec3d position = new Vec3d();

	private BlockPos blockPos = new BlockPos();

	public event ActionBoolReturn ShouldSpawnAmbientParticles;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		capi.Event.RegisterGameTickListener(OnSlowTick, 1000);
		capi.Event.RegisterAsyncParticleSpawner(AsyncParticleSpawnTick);
		liquidParticles = new SimpleParticleProperties
		{
			MinSize = 0.1f,
			MaxSize = 0.1f,
			MinQuantity = 1f,
			GravityEffect = 0f,
			LifeLength = 2f,
			ParticleModel = EnumParticleModel.Quad,
			ShouldDieInAir = true,
			VertexFlags = 512
		};
		summerAirParticles = new SimpleParticleProperties
		{
			Color = ColorUtil.ToRgba(35, 230, 230, 150),
			ParticleModel = EnumParticleModel.Quad,
			MinSize = 0.05f,
			MaxSize = 0.1f,
			GravityEffect = 0f,
			LifeLength = 2f,
			WithTerrainCollision = false,
			ShouldDieInLiquid = true,
			MinVelocity = new Vec3f(-0.125f, -0.125f, -0.125f),
			MinQuantity = 1f,
			AddQuantity = 0f
		};
		summerAirParticles.AddVelocity = new Vec3f(0.25f, 0.25f, 0.25f);
		summerAirParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.CLAMPEDPOSITIVESINUS, (float)Math.PI);
		summerAirParticles.MinPos = new Vec3d();
		fireflyParticles = new SimpleParticleProperties
		{
			Color = ColorUtil.ToRgba(150, 150, 250, 139),
			ParticleModel = EnumParticleModel.Quad,
			MinSize = 0.1f,
			MaxSize = 0.1f,
			GravityEffect = 0f,
			LifeLength = 2f,
			ShouldDieInLiquid = true,
			MinVelocity = new Vec3f(-0.25f, -0.0625f, -0.25f),
			MinQuantity = 2f,
			AddQuantity = 0f,
			LightEmission = ColorUtil.ToRgba(255, 77, 250, 139)
		};
		fireflyParticles.AddVelocity = new Vec3f(0.5f, 0.125f, 0.5f);
		fireflyParticles.VertexFlags = 255;
		fireflyParticles.AddPos.Set(1.0, 1.0, 1.0);
		fireflyParticles.AddQuantity = 8f;
		fireflyParticles.addLifeLength = 1f;
		fireflyParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.CLAMPEDPOSITIVESINUS, (float)Math.PI);
		fireflyParticles.RandomVelocityChange = true;
		fireflyLocationNoise = new ClampedSimplexNoise(new double[1] { 1.0 }, new double[1] { 5.0 }, capi.World.Rand.Next());
		fireflyrateNoise = new ClampedSimplexNoise(new double[1] { 1.0 }, new double[1] { 5.0 }, capi.World.Rand.Next());
	}

	private void OnSlowTick(float dt)
	{
		climate = capi.World.BlockAccessor.GetClimateAt(capi.World.Player.Entity.Pos.AsBlockPos);
		if (climate == null)
		{
			climate = new ClimateCondition();
		}
		spawnParticles = capi.Settings.Bool["ambientParticles"];
	}

	private bool AsyncParticleSpawnTick(float dt, IAsyncParticleManager manager)
	{
		if (!spawnParticles)
		{
			return true;
		}
		if (this.ShouldSpawnAmbientParticles != null && !this.ShouldSpawnAmbientParticles())
		{
			return true;
		}
		int particleLevel = capi.Settings.Int["particleLevel"];
		IClientWorldAccessor world = capi.World;
		EntityPlayer eplr = world.Player.Entity;
		ClimateCondition conds = world.BlockAccessor.GetClimateAt(blockPos.Set((int)eplr.Pos.X, (int)eplr.Pos.Y, (int)eplr.Pos.Z));
		float tries = 0.5f * (float)particleLevel;
		while (tries-- > 0f)
		{
			double offX2 = world.Rand.NextDouble() * 32.0 - 16.0;
			double offY2 = world.Rand.NextDouble() * 20.0 - 10.0;
			double offZ2 = world.Rand.NextDouble() * 32.0 - 16.0;
			position.Set(eplr.Pos.X, eplr.Pos.Y, eplr.Pos.Z).Add(offX2, offY2, offZ2);
			blockPos.Set(position);
			if (!world.BlockAccessor.IsValidPos(blockPos))
			{
				continue;
			}
			double chance2 = 0.05 + (double)Math.Max(0f, world.Calendar.DayLightStrength) * 0.4;
			if ((double)conds.Rainfall <= 0.01 && GlobalConstants.CurrentWindSpeedClient.X < 0.2f && world.Rand.NextDouble() < chance2 && climate.Temperature >= 14f && climate.WorldgenRainfall >= 0.4f && blockPos.Y > world.SeaLevel && manager.BlockAccess.GetBlock(blockPos).Id == 0)
			{
				IMapChunk mapchunk = manager.BlockAccess.GetMapChunk(blockPos.X / 32, blockPos.Z / 32);
				if (mapchunk != null && blockPos.Y > mapchunk.RainHeightMap[blockPos.Z % 32 * 32 + blockPos.X % 32])
				{
					summerAirParticles.MinPos.Set(position);
					summerAirParticles.RandomVelocityChange = true;
					manager.Spawn(summerAirParticles);
				}
				continue;
			}
			Block block2 = manager.BlockAccess.GetBlock(blockPos, 2);
			if (block2.IsLiquid() && block2.LiquidLevel >= 7)
			{
				liquidParticles.MinVelocity = new Vec3f((float)world.Rand.NextDouble() / 16f - 1f / 32f, (float)world.Rand.NextDouble() / 16f - 1f / 32f, (float)world.Rand.NextDouble() / 16f - 1f / 32f);
				liquidParticles.MinPos = position;
				if (world.Rand.Next(3) > 0)
				{
					liquidParticles.RandomVelocityChange = false;
					liquidParticles.Color = ColorUtil.HsvToRgba(110, 40 + world.Rand.Next(50), 200 + world.Rand.Next(30), 50 + world.Rand.Next(40));
				}
				else
				{
					liquidParticles.RandomVelocityChange = true;
					liquidParticles.Color = ColorUtil.HsvToRgba(110, 20 + world.Rand.Next(25), 100 + world.Rand.Next(15), 100 + world.Rand.Next(40));
				}
				manager.Spawn(liquidParticles);
			}
		}
		if ((double)conds.Rainfall < 0.15 && conds.Temperature > 5f)
		{
			double noise = (fireflyrateNoise.Noise(world.Calendar.TotalDays / 3.0, 0.0) - 0.4000000059604645) * 4.0;
			float f = Math.Max(0f, (float)(noise - (double)(Math.Abs(GlobalConstants.CurrentWindSpeedClient.X) * 2f)));
			int itries = GameMath.RoundRandom(world.Rand, f * 0.01f * (float)particleLevel);
			while (itries-- > 0)
			{
				double offX = world.Rand.NextDouble() * 80.0 - 40.0;
				double offY = world.Rand.NextDouble() * 80.0 - 40.0;
				double offZ = world.Rand.NextDouble() * 80.0 - 40.0;
				position.Set(eplr.Pos.X, eplr.Pos.Y, eplr.Pos.Z).Add(offX, offY, offZ);
				blockPos.Set(position);
				if (!world.BlockAccessor.IsValidPos(blockPos))
				{
					continue;
				}
				double posrnd = Math.Max(0.0, fireflyLocationNoise.Noise((double)blockPos.X / 60.0, (double)blockPos.Z / 60.0, world.Calendar.TotalDays / 5.0 - 0.800000011920929) - 0.5) * 2.0;
				double chance = (double)Math.Max(0f, 1f - world.Calendar.DayLightStrength * 2f) * posrnd;
				int prevY = blockPos.Y;
				blockPos.Y = manager.BlockAccess.GetTerrainMapheightAt(blockPos);
				Block block = manager.BlockAccess.GetBlock(blockPos);
				if (world.Rand.NextDouble() <= chance && climate.Temperature >= 8f && climate.Temperature <= 29f && climate.WorldgenRainfall >= 0.4f && block.Fertility > 30 && blockPos.Y > world.SeaLevel)
				{
					blockPos.Y += world.Rand.Next(4);
					position.Y += blockPos.Y - prevY;
					block = manager.BlockAccess.GetBlock(blockPos);
					Cuboidf[] collboxes = block.GetCollisionBoxes(manager.BlockAccess, blockPos);
					if (collboxes == null || collboxes.Length == 0)
					{
						fireflyParticles.AddVelocity.X = 0.5f + GlobalConstants.CurrentWindSpeedClient.X;
						fireflyParticles.MinPos = position;
						manager.Spawn(fireflyParticles);
					}
				}
			}
		}
		return true;
	}
}
