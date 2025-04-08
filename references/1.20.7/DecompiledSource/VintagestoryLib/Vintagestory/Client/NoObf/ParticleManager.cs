using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ParticleManager
{
	protected SystemRenderParticles particleSystem;

	protected BlockBreakingParticleProps blockBreakingProps;

	protected object asyncParticleQueueLock = new object();

	protected Queue<IParticlePropertiesProvider> asyncParticleQueue = new Queue<IParticlePropertiesProvider>();

	private Dictionary<long, Entity> loadedEntities = new Dictionary<long, Entity>();

	public void Init(SystemRenderParticles particleSystem)
	{
		this.particleSystem = particleSystem;
		blockBreakingProps = new BlockBreakingParticleProps();
		particleSystem.game.eventManager.OnPlayerBreakingBlock.Add(SpawnBlockBreakingParticles);
		particleSystem.game.api.eventapi.RegisterAsyncParticleSpawner(AsyncParticleSpawnTick);
		particleSystem.game.api.eventapi.RegisterGameTickListener(copyLoadedEntitiesList2sec, 2000, 123);
	}

	private void copyLoadedEntitiesList2sec(float dt)
	{
		Dictionary<long, Entity> dict = new Dictionary<long, Entity>();
		foreach (KeyValuePair<long, Entity> val in particleSystem.game.LoadedEntities)
		{
			dict[val.Key] = val.Value;
		}
		loadedEntities = dict;
	}

	private bool AsyncParticleSpawnTick(float dt, IAsyncParticleManager manager)
	{
		foreach (KeyValuePair<long, Entity> loadedEntity in loadedEntities)
		{
			loadedEntity.Value.OnAsyncParticleTick(dt, manager);
		}
		if (asyncParticleQueue.Count == 0)
		{
			return true;
		}
		lock (asyncParticleQueueLock)
		{
			while (asyncParticleQueue.Count > 0)
			{
				IParticlePropertiesProvider p = asyncParticleQueue.Dequeue();
				particleSystem.offthreadpools[(int)p.ParticleModel].SpawnParticles(p);
			}
		}
		return true;
	}

	public void SpawnParticles(float quantity, int color, Vec3d minPos, Vec3d maxPos, Vec3f minVelocity, Vec3f maxVelocity, float lifeLength, float gravityEffect, float scale, EnumParticleModel model)
	{
		SimpleParticleProperties props = new SimpleParticleProperties(quantity, quantity, color, minPos, maxPos, minVelocity, maxVelocity, lifeLength, gravityEffect);
		props.Init(particleSystem.game.api);
		props.ParticleModel = model;
		props.MinSize = (props.MaxSize = scale);
		particleSystem.mainthreadpools[(int)props.ParticleModel].SpawnParticles(props);
	}

	public void EnqueueAsyncParticles(IParticlePropertiesProvider props)
	{
		lock (asyncParticleQueueLock)
		{
			asyncParticleQueue.Enqueue(props);
		}
	}

	public void SpawnParticles(IParticlePropertiesProvider properties)
	{
		bool isMainThread = Environment.CurrentManagedThreadId == RuntimeEnv.MainThreadId;
		if (isMainThread && properties.Async)
		{
			EnqueueAsyncParticles(properties);
			return;
		}
		properties.Init(particleSystem.game.api);
		if (!isMainThread)
		{
			particleSystem.offthreadpools[(int)properties.ParticleModel].SpawnParticles(properties);
		}
		else
		{
			particleSystem.mainthreadpools[(int)properties.ParticleModel].SpawnParticles(properties);
		}
	}

	public void SpawnBlockBreakingParticles(BlockDamage blockdamage)
	{
		blockBreakingProps.Init(particleSystem.game.api);
		blockBreakingProps.blockdamage = blockdamage;
		blockBreakingProps.boyant = blockdamage.Block.MaterialDensity < 1000;
		particleSystem.mainthreadpools[(int)blockBreakingProps.ParticleModel].SpawnParticles(blockBreakingProps);
	}

	public int SpawnParticlesOffThread(IParticlePropertiesProvider particleProperties)
	{
		return particleSystem.offthreadpools[(int)particleProperties.ParticleModel].SpawnParticles(particleProperties);
	}

	public int ParticlesAlive(EnumParticleModel model)
	{
		return particleSystem.offthreadpools[(int)model].QuantityAlive;
	}
}
