using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class SystemRenderParticles : ClientSystem, IAsyncParticleManager
{
	internal IParticlePool[] mainthreadpools;

	internal IParticlePool[] offthreadpools;

	private bool renderParticles = true;

	private bool ready;

	private SimpleParticleProperties fountainParticle = new SimpleParticleProperties
	{
		AddPos = new Vec3d(),
		AddQuantity = 0f,
		Color = ColorUtil.ToRgba(255, 0, 200, 50),
		GravityEffect = 1f,
		LifeLength = 1f,
		MinVelocity = new Vec3f(-4f, 10f, -4f),
		AddVelocity = new Vec3f(8f, 15f, 8f),
		MinSize = 0.1f,
		MaxSize = 1f
	};

	private bool asyncfountain;

	private float accumSpawn;

	public override string Name => "rep";

	public IBlockAccessor BlockAccess { get; set; }

	public SystemRenderParticles(ClientMain game)
		: base(game)
	{
		InitAtlasAndModelPool();
		int div = ((ClientSettings.OptimizeRamMode < 2) ? 1 : 2);
		mainthreadpools = new IParticlePool[2]
		{
			new ParticlePoolQuads(ClientSettings.MaxQuadParticles / div, game, offthread: false),
			new ParticlePoolCubes(ClientSettings.MaxCubeParticles / div, game, offthread: false)
		};
		offthreadpools = new IParticlePool[2]
		{
			new ParticlePoolQuads(ClientSettings.MaxAsyncQuadParticles / div, game, offthread: true),
			new ParticlePoolCubes(ClientSettings.MaxAsyncCubeParticles / div, game, offthread: true)
		};
		game.particleManager.Init(this);
		CommandArgumentParsers parsers = game.api.ChatCommands.Parsers;
		game.api.ChatCommands.GetOrCreate("debug").BeginSubCommand("fountain").WithDescription("Toggle Particle fountain")
			.WithArgs(parsers.WordRange("type", "quad", "cube"), parsers.OptionalInt("quantity", 1))
			.HandleWith(OnToggleParticleFountain)
			.EndSubCommand()
			.BeginSubCommand("asyncfountain")
			.WithDescription("Toggle async Particle fountain")
			.WithArgs(parsers.WordRange("type", "quad", "cube"), parsers.OptionalInt("quantity", 1))
			.HandleWith(OnToggleAsyncParticleFountain)
			.EndSubCommand();
		renderParticles = ClientSettings.RenderParticles;
		game.eventManager.RegisterRenderer(OnRenderFrame3D, EnumRenderStage.Opaque, "rep-opa", 0.6);
		game.eventManager.RegisterRenderer(OnRenderFrame3DOIT, EnumRenderStage.OIT, "rep-oit", 0.6);
	}

	internal override void OnLevelFinalize()
	{
		base.OnLevelFinalize();
		ready = true;
		BlockAccess = new BlockAccessorCaching(game.WorldMap, game, synchronize: false, relight: false);
	}

	private void InitAtlasAndModelPool()
	{
	}

	private TextCommandResult OnToggleParticleFountain(TextCommandCallingArgs args)
	{
		int quantityFountainParticles = (int)args[1];
		fountainParticle.MinPos = game.EntityPlayer.Pos.XYZ;
		fountainParticle.MinQuantity = quantityFountainParticles;
		if (args[0] as string == "quad")
		{
			fountainParticle.ParticleModel = EnumParticleModel.Quad;
		}
		else
		{
			fountainParticle.ParticleModel = EnumParticleModel.Cube;
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult OnToggleAsyncParticleFountain(TextCommandCallingArgs args)
	{
		OnToggleParticleFountain(args);
		if (!asyncfountain)
		{
			asyncfountain = true;
			game.api.eventapi.RegisterAsyncParticleSpawner(delegate(float dt, IAsyncParticleManager mgr)
			{
				mgr.Spawn(fountainParticle);
				return asyncfountain;
			});
		}
		else
		{
			asyncfountain = false;
			fountainParticle.MinQuantity = 0f;
		}
		return TextCommandResult.Success();
	}

	private void UpdateParticleFountain()
	{
		if (!game.IsPaused && !asyncfountain && fountainParticle.MinQuantity > 0f)
		{
			mainthreadpools[(int)fountainParticle.ParticleModel].SpawnParticles(fountainParticle);
		}
	}

	public void OnRenderFrame3D(float deltaTime)
	{
		UpdateParticleFountain();
		ShaderProgramParticlescube particlescube = ShaderPrograms.Particlescube;
		particlescube.Use();
		game.Platform.GlToggleBlend(on: true);
		Render(1, deltaTime);
		particlescube.Stop();
	}

	public void OnRenderFrame3DOIT(float deltaTime)
	{
		ShaderProgramParticlesquad particlesquad = ShaderPrograms.Particlesquad;
		particlesquad.Use();
		Render(0, deltaTime);
		particlesquad.Stop();
	}

	private void Render(int poolindex, float dt)
	{
		if (renderParticles)
		{
			game.GlPushMatrix();
			game.GlLoadMatrix(game.MainCamera.CameraMatrixOrigin);
			ShaderProgramBase currentShaderProgram = ShaderProgramBase.CurrentShaderProgram;
			((IShaderProgram)currentShaderProgram).Uniform("rgbaFogIn", game.AmbientManager.BlendedFogColor);
			((IShaderProgram)currentShaderProgram).Uniform("rgbaAmbientIn", game.AmbientManager.BlendedAmbientColor);
			((IShaderProgram)currentShaderProgram).Uniform("fogMinIn", game.AmbientManager.BlendedFogMin);
			((IShaderProgram)currentShaderProgram).Uniform("fogDensityIn", game.AmbientManager.BlendedFogDensity);
			((IShaderProgram)currentShaderProgram).UniformMatrix("projectionMatrix", game.CurrentProjectionMatrix);
			((IShaderProgram)currentShaderProgram).UniformMatrix("modelViewMatrix", game.CurrentModelViewMatrix);
			IParticlePool poolm = mainthreadpools[poolindex];
			IParticlePool poolo = offthreadpools[poolindex];
			poolm.OnNewFrame(dt, game.EntityPlayer.CameraPos);
			poolo.OnNewFrame(dt, game.EntityPlayer.CameraPos);
			game.Platform.RenderMeshInstanced(poolm.Model, poolm.QuantityAlive);
			game.Platform.RenderMeshInstanced(poolo.Model, poolo.QuantityAlive);
			((IShaderProgram)currentShaderProgram).Stop();
			game.GlPopMatrix();
		}
	}

	public override int SeperateThreadTickIntervalMs()
	{
		return 20;
	}

	public override void OnSeperateThreadGameTick(float dt)
	{
		if (!ready || game.IsPaused)
		{
			return;
		}
		dt = Math.Min(1f, dt);
		accumSpawn += dt;
		for (accumSpawn = Math.Min(accumSpawn, 1f); accumSpawn >= 0.033f; accumSpawn -= 0.033f)
		{
			List<ContinousParticleSpawnTaskDelegate> list = game.asyncParticleSpawners;
			int count = list.Count;
			for (int i = 0; i < count; i++)
			{
				ContinousParticleSpawnTaskDelegate handler;
				lock (game.asyncParticleSpawnersLock)
				{
					handler = list[i];
				}
				if (!handler(dt, this))
				{
					lock (game.asyncParticleSpawnersLock)
					{
						list.RemoveAt(i);
						i--;
						count--;
					}
				}
			}
		}
		offthreadpools[0].OnNewFrameOffThread(dt, game.EntityPlayer.CameraPos);
		offthreadpools[1].OnNewFrameOffThread(dt, game.EntityPlayer.CameraPos);
	}

	public int Spawn(IParticlePropertiesProvider particleProperties)
	{
		return game.particleManager.SpawnParticlesOffThread(particleProperties);
	}

	public int ParticlesAlive(EnumParticleModel model)
	{
		return game.particleManager.ParticlesAlive(model);
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}

	public override void Dispose(ClientMain game)
	{
		ready = false;
		for (int j = 0; j < mainthreadpools.Length; j++)
		{
			mainthreadpools[j].Dipose();
		}
		for (int i = 0; i < offthreadpools.Length; i++)
		{
			offthreadpools[i].Dipose();
		}
		(BlockAccess as ICachingBlockAccessor)?.Dispose();
	}
}
