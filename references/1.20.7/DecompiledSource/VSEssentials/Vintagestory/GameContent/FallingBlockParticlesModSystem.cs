using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class FallingBlockParticlesModSystem : ModSystem
{
	private static SimpleParticleProperties dustParticles;

	private static SimpleParticleProperties bitsParticles;

	private ICoreClientAPI capi;

	private HashSet<EntityBlockFalling> fallingBlocks = new HashSet<EntityBlockFalling>();

	private ConcurrentQueue<EntityBlockFalling> toRegister = new ConcurrentQueue<EntityBlockFalling>();

	private ConcurrentQueue<EntityBlockFalling> toRemove = new ConcurrentQueue<EntityBlockFalling>();

	public int ActiveFallingBlocks => fallingBlocks.Count;

	static FallingBlockParticlesModSystem()
	{
		dustParticles = new SimpleParticleProperties(1f, 3f, ColorUtil.ToRgba(40, 220, 220, 220), new Vec3d(), new Vec3d(), new Vec3f(-0.25f, -0.25f, -0.25f), new Vec3f(0.25f, 0.25f, 0.25f), 1f, 1f, 0.3f, 0.3f, EnumParticleModel.Quad);
		dustParticles.AddQuantity = 5f;
		dustParticles.MinVelocity.Set(-0.05f, -0.4f, -0.05f);
		dustParticles.AddVelocity.Set(0.1f, 0.2f, 0.1f);
		dustParticles.WithTerrainCollision = true;
		dustParticles.ParticleModel = EnumParticleModel.Quad;
		dustParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -16f);
		dustParticles.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, 3f);
		dustParticles.GravityEffect = 0f;
		dustParticles.MaxSize = 1.3f;
		dustParticles.LifeLength = 3f;
		dustParticles.SelfPropelled = true;
		dustParticles.AddPos.Set(1.4, 1.4, 1.4);
		bitsParticles = new SimpleParticleProperties(1f, 3f, ColorUtil.ToRgba(40, 220, 220, 220), new Vec3d(), new Vec3d(), new Vec3f(-0.25f, -0.25f, -0.25f), new Vec3f(0.25f, 0.25f, 0.25f), 1f, 1f, 0.1f, 0.3f, EnumParticleModel.Quad);
		bitsParticles.AddPos.Set(1.4, 1.4, 1.4);
		bitsParticles.AddQuantity = 20f;
		bitsParticles.MinVelocity.Set(-0.25f, 0f, -0.25f);
		bitsParticles.AddVelocity.Set(0.5f, 1f, 0.5f);
		bitsParticles.WithTerrainCollision = true;
		bitsParticles.ParticleModel = EnumParticleModel.Cube;
		bitsParticles.LifeLength = 1.5f;
		bitsParticles.SizeEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -0.5f);
		bitsParticles.GravityEffect = 2.5f;
		bitsParticles.MinSize = 0.5f;
		bitsParticles.MaxSize = 1.5f;
	}

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.Event.RegisterAsyncParticleSpawner(asyncParticleSpawn);
	}

	public void Register(EntityBlockFalling entity)
	{
		toRegister.Enqueue(entity);
	}

	public void Unregister(EntityBlockFalling entity)
	{
		toRemove.Enqueue(entity);
	}

	private bool asyncParticleSpawn(float dt, IAsyncParticleManager manager)
	{
		int alive = manager.ParticlesAlive(EnumParticleModel.Quad);
		float particlemul = Math.Max(0.05f, (float)Math.Pow(0.949999988079071, (float)alive / 200f));
		foreach (EntityBlockFalling bef3 in fallingBlocks)
		{
			float dustAdd = 0f;
			if (bef3.nowImpacted)
			{
				if (capi.World.BlockAccessor.GetBlock(bef3.Pos.AsBlockPos, 2).Id == 0)
				{
					dustAdd = 20f;
				}
				bef3.nowImpacted = false;
			}
			if (bef3.Block.Id != 0)
			{
				dustParticles.Color = bef3.stackForParticleColor.Collectible.GetRandomColor(capi, bef3.stackForParticleColor);
				dustParticles.Color &= 16777215;
				dustParticles.Color |= -1778384896;
				dustParticles.MinPos.Set(bef3.Pos.X - 0.2 - 0.5, bef3.Pos.Y, bef3.Pos.Z - 0.2 - 0.5);
				dustParticles.MinSize = 1f;
				float veloMul = dustAdd / 20f;
				dustParticles.AddPos.Y = bef3.maxSpawnHeightForParticles;
				dustParticles.MinVelocity.Set(-0.2f * veloMul, 1f * (float)bef3.Pos.Motion.Y, -0.2f * veloMul);
				dustParticles.AddVelocity.Set(0.4f * veloMul, 0.2f * (float)bef3.Pos.Motion.Y + (0f - veloMul), 0.4f * veloMul);
				dustParticles.MinQuantity = dustAdd * bef3.dustIntensity * particlemul / 2f;
				dustParticles.AddQuantity = (6f * Math.Abs((float)bef3.Pos.Motion.Y) + dustAdd) * bef3.dustIntensity * particlemul / 2f;
				manager.Spawn(dustParticles);
			}
			bitsParticles.MinPos.Set(bef3.Pos.X - 0.2 - 0.5, bef3.Pos.Y - 0.5, bef3.Pos.Z - 0.2 - 0.5);
			bitsParticles.MinVelocity.Set(-2f, 30f * (float)bef3.Pos.Motion.Y, -2f);
			bitsParticles.AddVelocity.Set(4f, 0.2f * (float)bef3.Pos.Motion.Y, 4f);
			bitsParticles.MinQuantity = particlemul;
			bitsParticles.AddQuantity = 6f * Math.Abs((float)bef3.Pos.Motion.Y) * particlemul;
			bitsParticles.Color = dustParticles.Color;
			bitsParticles.AddPos.Y = bef3.maxSpawnHeightForParticles;
			dustParticles.Color = bef3.Block.GetRandomColor(capi, bef3.stackForParticleColor);
			capi.World.SpawnParticles(bitsParticles);
		}
		int cnt = toRegister.Count;
		while (cnt-- > 0)
		{
			if (toRegister.TryDequeue(out var bef2))
			{
				fallingBlocks.Add(bef2);
			}
		}
		cnt = toRemove.Count;
		while (cnt-- > 0)
		{
			if (toRemove.TryDequeue(out var bef))
			{
				fallingBlocks.Remove(bef);
			}
		}
		return true;
	}
}
