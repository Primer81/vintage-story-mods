using System;
using System.Threading;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityParticleSystem : ModSystem, IRenderer, IDisposable
{
	protected MeshRef particleModelRef;

	protected MeshData[] updateBuffers;

	protected Vec3d[] cameraPos;

	protected float[] tickTimes;

	protected float[][] velocities;

	protected int writePosition = 1;

	protected int readPosition;

	protected object advanceCountLock = new object();

	protected int advanceCount;

	protected Random rand = new Random();

	protected ICoreClientAPI capi;

	protected float currentGamespeed;

	protected ParticlePhysics partPhysics;

	protected EnumParticleModel ModelType = EnumParticleModel.Cube;

	private int poolSize = 5000;

	private int quantityAlive;

	private int offthreadid;

	private EPCounter counter = new EPCounter();

	private bool isShuttingDown;

	public EntityParticle FirstAlive;

	public EntityParticle LastAlive;

	private float accumPhysics;

	public MeshRef Model => particleModelRef;

	public IBlockAccessor BlockAccess => partPhysics.BlockAccess;

	public double RenderOrder => 1.0;

	public int RenderRange => 50;

	public EPCounter Count => counter;

	public event Action<float> OnSimTick;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return forSide == EnumAppSide.Client;
	}

	public virtual MeshData LoadModel()
	{
		MeshData modeldata = CubeMeshUtil.GetCubeOnlyScaleXyz(1f / 32f, 1f / 32f, new Vec3f());
		modeldata.WithNormals();
		modeldata.Rgba = null;
		for (int i = 0; i < 24; i++)
		{
			BlockFacing face = BlockFacing.ALLFACES[i / 4];
			modeldata.AddNormal(face);
		}
		return modeldata;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "reeps-op");
		Thread thread = TyronThreadPool.CreateDedicatedThread(onThreadStart, "entityparticlesim");
		offthreadid = thread.ManagedThreadId;
		capi.Event.LeaveWorld += delegate
		{
			isShuttingDown = true;
		};
		thread.Start();
		partPhysics = new ParticlePhysics(api.World.GetLockFreeBlockAccessor());
		partPhysics.PhysicsTickTime = 0.125f;
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
		particleModelRef = api.Render.UploadMesh(particleModel);
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

	private void onThreadStart()
	{
		while (!isShuttingDown)
		{
			Thread.Sleep(10);
			if (!capi.IsGamePaused)
			{
				Vec3d cpos = capi.World.Player?.Entity?.CameraPos.Clone();
				if (cpos != null)
				{
					OnNewFrameOffThread(0.01f, cpos);
				}
			}
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

	public void SpawnParticle(EntityParticle eparticle)
	{
		if (Environment.CurrentManagedThreadId != offthreadid)
		{
			throw new InvalidOperationException("Only in the entityparticle thread");
		}
		eparticle.Prev = null;
		eparticle.Next = null;
		if (FirstAlive == null)
		{
			FirstAlive = eparticle;
			LastAlive = eparticle;
		}
		else
		{
			eparticle.Prev = LastAlive;
			LastAlive.Next = eparticle;
			LastAlive = eparticle;
		}
		eparticle.OnSpawned(partPhysics);
		counter.Inc(eparticle.Type);
		quantityAlive++;
	}

	protected void KillParticle(EntityParticle entityParticle)
	{
		if (Environment.CurrentManagedThreadId != offthreadid)
		{
			throw new InvalidOperationException("Only in the entityparticle thread");
		}
		ParticleBase prevParticle = entityParticle.Prev;
		ParticleBase nextParticle = entityParticle.Next;
		if (prevParticle != null)
		{
			prevParticle.Next = nextParticle;
		}
		if (nextParticle != null)
		{
			nextParticle.Prev = prevParticle;
		}
		if (FirstAlive == entityParticle)
		{
			FirstAlive = (EntityParticle)nextParticle;
		}
		if (LastAlive == entityParticle)
		{
			LastAlive = ((EntityParticle)prevParticle) ?? FirstAlive;
		}
		entityParticle.Prev = null;
		entityParticle.Next = null;
		quantityAlive--;
		counter.Dec(entityParticle.Type);
	}

	public void OnRenderFrame(float dt, EnumRenderStage stage)
	{
		IShaderProgram program = capi.Shader.GetProgram(2);
		program.Use();
		capi.Render.GlToggleBlend(blend: true);
		capi.Render.GlPushMatrix();
		capi.Render.GlLoadMatrix(capi.Render.CameraMatrixOrigin);
		program.Uniform("rgbaFogIn", capi.Ambient.BlendedFogColor);
		program.Uniform("rgbaAmbientIn", capi.Ambient.BlendedAmbientColor);
		program.Uniform("fogMinIn", capi.Ambient.BlendedFogMin);
		program.Uniform("fogDensityIn", capi.Ambient.BlendedFogDensity);
		program.UniformMatrix("projectionMatrix", capi.Render.CurrentProjectionMatrix);
		program.UniformMatrix("modelViewMatrix", capi.Render.CurrentModelviewMatrix);
		OnNewFrame(dt, capi.World.Player.Entity.CameraPos);
		capi.Render.RenderMeshInstanced(Model, quantityAlive);
		program.Stop();
		capi.Render.GlPopMatrix();
	}

	public void OnNewFrame(float dt, Vec3d cameraPos)
	{
		if (capi.IsGamePaused)
		{
			return;
		}
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
		int cnt = (quantityAlive = buffer.VerticesCount);
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
		capi.Render.UpdateMesh(particleModelRef, buffer);
	}

	public void OnNewFrameOffThread(float dt, Vec3d cameraPos)
	{
		if (capi.IsGamePaused)
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
		this.OnSimTick?.Invoke(dt);
		currentGamespeed = capi.World.Calendar.SpeedOfTime / 60f * 5f;
		ParticleBase particle = FirstAlive;
		int posPosition = 0;
		int rgbaPosition = 0;
		int flagPosition = 0;
		MeshData updateBuffer = updateBuffers[writePosition];
		float[] velocity = velocities[writePosition];
		Vec3d curCamPos = this.cameraPos[writePosition].Set(cameraPos);
		partPhysics.PhysicsTickTime = 1f / 64f;
		float pdt = Math.Max(partPhysics.PhysicsTickTime, dt);
		float spdt = pdt * currentGamespeed;
		int i = 0;
		while (particle != null)
		{
			double x = particle.Position.X;
			double y = particle.Position.Y;
			double z = particle.Position.Z;
			particle.TickNow(spdt, spdt, capi, partPhysics);
			if (!particle.Alive)
			{
				ParticleBase next = particle.Next;
				KillParticle((EntityParticle)particle);
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

	public override void Dispose()
	{
		particleModelRef?.Dispose();
	}

	public void Clear()
	{
		FirstAlive = null;
		LastAlive = null;
		quantityAlive = 0;
		counter.Clear();
	}
}
