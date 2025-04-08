using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class ParticlePoolQuads : IParticlePool
{
	public FastParticlePool ParticlesPool;

	protected MeshRef particleModelRef;

	protected MeshData updateBuffer;

	protected MeshData[] updateBuffers;

	protected Vec3d[] cameraPos;

	protected float[] tickTimes;

	protected float[][] velocities;

	private int writePosition = 1;

	private int readPosition;

	private object advanceCountLock = new object();

	private int advanceCount;

	protected int poolSize;

	protected ClientMain game;

	protected Random rand = new Random();

	private float currentGamespeed;

	private ParticlePhysics partPhysics;

	private bool offthread;

	protected EnumParticleModel ModelType;

	private float accumPhysics;

	public MeshRef Model => particleModelRef;

	public int QuantityAlive { get; set; }

	internal virtual float ParticleHeight => 0.5f;

	public IBlockAccessor BlockAccess => partPhysics.BlockAccess;

	public virtual MeshData LoadModel()
	{
		return QuadMeshUtilExt.GetCustomQuadModelData(0f, 0f, 0f, 0.25f, 0.25f, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
	}

	public ParticlePoolQuads(int poolSize, ClientMain game, bool offthread)
	{
		this.offthread = offthread;
		this.poolSize = poolSize;
		this.game = game;
		partPhysics = new ParticlePhysics(new BlockAccessorReadLockfree(game.WorldMap, game));
		if (offthread)
		{
			partPhysics.PhysicsTickTime = 0.125f;
		}
		ParticlesPool = new FastParticlePool(poolSize, () => new ParticleGeneric());
		MeshData particleModel = LoadModel();
		particleModel.CustomFloats = new CustomMeshDataPartFloat
		{
			Instanced = true,
			StaticDraw = false,
			Values = new float[poolSize * 4],
			InterleaveSizes = new int[2] { 3, 1 },
			InterleaveStride = 16,
			InterleaveOffsets = new int[2] { 0, 12 },
			Count = poolSize * 4
		};
		particleModel.CustomBytes = new CustomMeshDataPartByte
		{
			Conversion = DataConversion.NormalizedFloat,
			Instanced = true,
			StaticDraw = false,
			Values = new byte[poolSize * 12],
			InterleaveSizes = new int[3] { 4, 4, 4 },
			InterleaveStride = 12,
			InterleaveOffsets = new int[3] { 0, 4, 8 },
			Count = poolSize * 12
		};
		particleModel.Flags = new int[poolSize];
		particleModel.FlagsInstanced = true;
		particleModelRef = game.Platform.UploadMesh(particleModel);
		if (offthread)
		{
			updateBuffers = new MeshData[5];
			cameraPos = new Vec3d[5];
			tickTimes = new float[5];
			velocities = new float[5][];
			for (int i = 0; i < 5; i++)
			{
				tickTimes[i] = partPhysics.PhysicsTickTime;
				velocities[i] = new float[3 * poolSize];
				cameraPos[i] = new Vec3d();
				updateBuffers[i] = genUpdateBuffer();
			}
		}
		else
		{
			updateBuffer = genUpdateBuffer();
		}
	}

	private MeshData genUpdateBuffer()
	{
		return new MeshData
		{
			CustomFloats = new CustomMeshDataPartFloat
			{
				Values = new float[poolSize * 4],
				Count = poolSize * 4
			},
			CustomBytes = new CustomMeshDataPartByte
			{
				Values = new byte[poolSize * 12],
				Count = poolSize * 12
			},
			Flags = new int[poolSize],
			FlagsInstanced = true
		};
	}

	public int SpawnParticles(IParticlePropertiesProvider particleProperties)
	{
		float speed = 5f / GameMath.Sqrt(currentGamespeed);
		int spawned = 0;
		if (QuantityAlive * 100 >= game.particleLevel * poolSize)
		{
			return 0;
		}
		for (float quantity = particleProperties.Quantity * currentGamespeed; (float)spawned < quantity; spawned++)
		{
			if (ParticlesPool.FirstDead == null)
			{
				break;
			}
			if (rand.NextDouble() > (double)(quantity - (float)spawned))
			{
				break;
			}
			ParticleGeneric particle = (ParticleGeneric)ParticlesPool.ReviveOne();
			particle.SecondaryParticles = particleProperties.SecondaryParticles;
			particle.DeathParticles = particleProperties.DeathParticles;
			particleProperties.BeginParticle();
			particle.Position.Set(particleProperties.Pos);
			particle.Velocity.Set(particleProperties.GetVelocity(particle.Position));
			particle.ParentVelocity = particleProperties.ParentVelocity;
			particle.ParentVelocityWeight = particleProperties.ParentVelocityWeight;
			particle.Bounciness = particleProperties.Bounciness;
			particle.StartingVelocity.Set(particle.Velocity);
			particle.SizeMultiplier = particleProperties.Size;
			particle.ParticleHeight = ParticleHeight;
			int color = particleProperties.GetRgbaColor(game.api);
			particle.ColorRed = (byte)color;
			particle.ColorGreen = (byte)(color >> 8);
			particle.ColorBlue = (byte)(color >> 16);
			particle.ColorAlpha = (byte)(color >> 24);
			particle.LightEmission = particleProperties.LightEmission;
			particle.VertexFlags = particleProperties.VertexFlags;
			particle.SelfPropelled = particleProperties.SelfPropelled;
			particle.LifeLength = particleProperties.LifeLength * speed;
			particle.TerrainCollision = particleProperties.TerrainCollision;
			particle.GravityStrength = particleProperties.GravityEffect * GlobalConstants.GravityStrengthParticle * 40f;
			particle.SwimOnLiquid = particleProperties.SwimOnLiquid;
			particle.DieInLiquid = particleProperties.DieInLiquid;
			particle.DieInAir = particleProperties.DieInAir;
			particle.DieOnRainHeightmap = particleProperties.DieOnRainHeightmap;
			particle.OpacityEvolve = particleProperties.OpacityEvolve;
			particle.RedEvolve = particleProperties.RedEvolve;
			particle.GreenEvolve = particleProperties.GreenEvolve;
			particle.BlueEvolve = particleProperties.BlueEvolve;
			particle.SizeEvolve = particleProperties.SizeEvolve;
			particle.VelocityEvolve = particleProperties.VelocityEvolve;
			particle.RandomVelocityChange = particleProperties.RandomVelocityChange;
			particle.Spawned(partPhysics);
		}
		return spawned;
	}

	public bool ShouldRender()
	{
		return ParticlesPool.AliveCount > 0;
	}

	public void OnNewFrame(float dt, Vec3d cameraPos)
	{
		if (game.IsPaused)
		{
			return;
		}
		if (offthread)
		{
			ProcessParticlesFromOffThread(dt, cameraPos);
			return;
		}
		currentGamespeed = game.Calendar.SpeedOfTime / 60f;
		dt *= currentGamespeed;
		ParticleBase particle = ParticlesPool.FirstAlive;
		int posPosition = 0;
		int rgbaPosition = 0;
		int flagPosition = 0;
		while (particle != null)
		{
			particle.TickFixedStep(dt, game.api, partPhysics);
			if (!particle.Alive)
			{
				ParticleBase next = particle.Next;
				ParticlesPool.Kill(particle);
				particle = next;
			}
			else
			{
				particle.UpdateBuffers(updateBuffer, cameraPos, ref posPosition, ref rgbaPosition, ref flagPosition);
				particle = particle.Next;
			}
		}
		((IWorldAccessor)game).FrameProfiler.Mark("particles-tick");
		updateBuffer.CustomFloats.Count = ParticlesPool.AliveCount * 4;
		updateBuffer.CustomBytes.Count = ParticlesPool.AliveCount * 12;
		updateBuffer.VerticesCount = ParticlesPool.AliveCount;
		QuantityAlive = ParticlesPool.AliveCount;
		UpdateDebugScreen();
		game.Platform.UpdateMesh(particleModelRef, updateBuffer);
		((IWorldAccessor)game).FrameProfiler.Mark("particles-updatemesh");
	}

	private void ProcessParticlesFromOffThread(float dt, Vec3d cameraPos)
	{
		accumPhysics += dt;
		float ticktime = tickTimes[readPosition];
		if (accumPhysics >= ticktime)
		{
			lock (advanceCountLock)
			{
				if (advanceCount > 0)
				{
					readPosition = (readPosition + 1) % updateBuffers.Length;
					advanceCount--;
					accumPhysics -= ticktime;
					ticktime = tickTimes[readPosition];
				}
			}
			if (accumPhysics > 1f)
			{
				accumPhysics = 0f;
			}
		}
		float step = dt / ticktime;
		MeshData buffer = updateBuffers[readPosition];
		float[] velocity = velocities[readPosition];
		int num = (QuantityAlive = buffer.VerticesCount);
		int cnt = num;
		float camdX = (float)(this.cameraPos[readPosition].X - cameraPos.X);
		float camdY = (float)(this.cameraPos[readPosition].Y - cameraPos.Y);
		float camdZ = (float)(this.cameraPos[readPosition].Z - cameraPos.Z);
		this.cameraPos[readPosition].X -= camdX;
		this.cameraPos[readPosition].Y -= camdY;
		this.cameraPos[readPosition].Z -= camdZ;
		float[] nowFloats = buffer.CustomFloats.Values;
		for (int i = 0; i < cnt; i++)
		{
			int a = i * 4;
			nowFloats[a] += camdX + velocity[i * 3] * step;
			a++;
			nowFloats[a] += camdY + velocity[i * 3 + 1] * step;
			a++;
			nowFloats[a] += camdZ + velocity[i * 3 + 2] * step;
		}
		game.Platform.UpdateMesh(particleModelRef, buffer);
		if (ModelType == EnumParticleModel.Quad)
		{
			if (game.extendedDebugInfo)
			{
				game.DebugScreenInfo["asyncquadparticlepool"] = "Async Quad Particle pool: " + ParticlesPool.AliveCount + " / " + (int)((float)poolSize * (float)game.particleLevel / 100f);
			}
			else
			{
				game.DebugScreenInfo["asyncquadparticlepool"] = "";
			}
		}
		else if (game.extendedDebugInfo)
		{
			game.DebugScreenInfo["asynccubeparticlepool"] = "Async Cube Particle pool: " + ParticlesPool.AliveCount + " / " + (int)((float)poolSize * (float)game.particleLevel / 100f);
		}
		else
		{
			game.DebugScreenInfo["asynccubeparticlepool"] = "";
		}
		((IWorldAccessor)game).FrameProfiler.Mark("otparticles-tick");
	}

	public void OnNewFrameOffThread(float dt, Vec3d cameraPos)
	{
		if (game.IsPaused || !offthread)
		{
			return;
		}
		lock (advanceCountLock)
		{
			if (advanceCount >= updateBuffers.Length - 1)
			{
				return;
			}
		}
		currentGamespeed = game.Calendar.SpeedOfTime / 60f;
		ParticleBase particle = ParticlesPool.FirstAlive;
		int posPosition = 0;
		int rgbaPosition = 0;
		int flagPosition = 0;
		MeshData updateBuffer = updateBuffers[writePosition];
		float[] velocity = velocities[writePosition];
		Vec3d curCamPos = this.cameraPos[writePosition].Set(cameraPos);
		if (ParticlesPool.AliveCount < 20000)
		{
			partPhysics.PhysicsTickTime = 0.0625f;
		}
		else
		{
			partPhysics.PhysicsTickTime = 0.125f;
		}
		float pdt = Math.Max(partPhysics.PhysicsTickTime, dt);
		float spdt = pdt * currentGamespeed;
		int i = 0;
		while (particle != null)
		{
			double x = particle.Position.X;
			double y = particle.Position.Y;
			double z = particle.Position.Z;
			particle.TickNow(spdt, spdt, game.api, partPhysics);
			if (!particle.Alive)
			{
				ParticleBase next = particle.Next;
				ParticlesPool.Kill(particle);
				particle = next;
				continue;
			}
			velocity[i * 3] = (particle.prevPosDeltaX = (float)(particle.Position.X - x));
			velocity[i * 3 + 1] = (particle.prevPosDeltaY = (float)(particle.Position.Y - y));
			velocity[i * 3 + 2] = (particle.prevPosDeltaZ = (float)(particle.Position.Z - z));
			i++;
			particle.UpdateBuffers(updateBuffer, curCamPos, ref posPosition, ref rgbaPosition, ref flagPosition);
			particle = particle.Next;
		}
		updateBuffer.CustomFloats.Count = i * 4;
		updateBuffer.CustomBytes.Count = i * 12;
		updateBuffer.VerticesCount = i;
		tickTimes[writePosition] = Math.Min(pdt, 1f);
		writePosition = (writePosition + 1) % updateBuffers.Length;
		lock (advanceCountLock)
		{
			advanceCount++;
		}
	}

	internal virtual void UpdateDebugScreen()
	{
		if (game.extendedDebugInfo)
		{
			game.DebugScreenInfo["quadparticlepool"] = "Quad Particle pool: " + ParticlesPool.AliveCount + " / " + (int)((float)poolSize * (float)game.particleLevel / 100f);
		}
		else
		{
			game.DebugScreenInfo["quadparticlepool"] = "";
		}
	}

	public void Dipose()
	{
		particleModelRef.Dispose();
	}
}
