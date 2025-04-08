using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client;

public class ParticlePool2D
{
	public FastParticlePool ParticlesPool;

	protected MeshRef particleModelRef;

	protected MeshData particleData;

	protected int poolSize;

	private ICoreClientAPI capi;

	protected Random rand = new Random();

	private Matrixf mat;

	private Vec4d tmpVec = new Vec4d();

	public virtual bool RenderTransparent => true;

	public MeshRef Model => particleModelRef;

	public int QuantityAlive => ParticlesPool.AliveCount;

	internal virtual float ParticleHeight => 0.5f;

	public virtual MeshData LoadModel()
	{
		MeshData customQuadModelData = QuadMeshUtilExt.GetCustomQuadModelData(0f, 0f, 0f, 10f, 10f, byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
		customQuadModelData.Flags = null;
		return customQuadModelData;
	}

	public ParticlePool2D(ICoreClientAPI capi, int poolSize)
	{
		this.capi = capi;
		this.poolSize = poolSize;
		ParticlesPool = new FastParticlePool(poolSize, () => new Particle2D());
		MeshData particleModel = LoadModel();
		particleModel.CustomFloats = new CustomMeshDataPartFloat
		{
			Instanced = true,
			StaticDraw = false,
			Values = new float[poolSize * 9],
			InterleaveSizes = new int[4] { 4, 3, 1, 1 },
			InterleaveStride = 36,
			InterleaveOffsets = new int[4] { 0, 16, 28, 32 },
			Count = poolSize * 9
		};
		particleModelRef = ScreenManager.Platform.UploadMesh(particleModel);
		particleData = new MeshData();
		particleData.CustomFloats = new CustomMeshDataPartFloat
		{
			Values = new float[poolSize * 9],
			Count = poolSize * 9
		};
	}

	public int Spawn(IParticlePropertiesProvider particleProperties)
	{
		float speed = 5f;
		int spawned = 0;
		if (QuantityAlive >= poolSize)
		{
			return 0;
		}
		for (float quantity = particleProperties.Quantity; (float)spawned < quantity; spawned++)
		{
			if (ParticlesPool.FirstDead == null)
			{
				break;
			}
			if (rand.NextDouble() > (double)(quantity - (float)spawned))
			{
				break;
			}
			Particle2D particle = ParticlesPool.ReviveOne() as Particle2D;
			particle.ParentVelocityWeight = particleProperties.ParentVelocityWeight;
			particle.ParentVelocity = particleProperties.ParentVelocity?.Clone();
			particleProperties.BeginParticle();
			particle.Position.Set(particleProperties.Pos);
			particle.Velocity.Set(particleProperties.GetVelocity(particle.Position));
			particle.StartingVelocity = particle.Velocity.Clone();
			particle.SizeMultiplier = particleProperties.Size;
			particle.ParticleHeight = ParticleHeight;
			particle.Color = ColorUtil.ToRGBABytes(particleProperties.GetRgbaColor(null));
			particle.VertexFlags = particleProperties.VertexFlags;
			particle.LifeLength = particleProperties.LifeLength * speed;
			particle.SetAlive(particleProperties.GravityEffect);
			particle.OpacityEvolve = particleProperties.OpacityEvolve;
			particle.SizeEvolve = particleProperties.SizeEvolve;
			particle.VelocityEvolve = particleProperties.VelocityEvolve;
		}
		return spawned;
	}

	internal void TransformNextUpdate(Matrixf mat)
	{
		this.mat = mat;
	}

	public bool ShouldRender()
	{
		return ParticlesPool.AliveCount > 0;
	}

	public void OnNewFrame(float dt)
	{
		ParticleBase particle = ParticlesPool.FirstAlive;
		int posPosition = 0;
		int unused1 = 0;
		int unused2 = 0;
		while (particle != null)
		{
			particle.TickFixedStep(dt, capi, null);
			if (mat != null)
			{
				tmpVec.Set(particle.Position.X, particle.Position.Y, particle.Position.Z, 1.0);
				tmpVec = mat.TransformVector(tmpVec);
				particle.Position.X = tmpVec.X;
				particle.Position.Y = tmpVec.Y;
				particle.Position.Z = tmpVec.Z;
			}
			if (!particle.Alive)
			{
				ParticleBase next = particle.Next;
				ParticlesPool.Kill(particle);
				particle = next;
			}
			else
			{
				particle.UpdateBuffers(particleData, null, ref posPosition, ref unused1, ref unused2);
				particle = particle.Next;
			}
		}
		particleData.CustomFloats.Count = ParticlesPool.AliveCount * 9;
		particleData.VerticesCount = ParticlesPool.AliveCount;
		ScreenManager.Platform.UpdateMesh(particleModelRef, particleData);
		mat = null;
	}

	public void Dispose()
	{
		particleModelRef.Dispose();
	}
}
