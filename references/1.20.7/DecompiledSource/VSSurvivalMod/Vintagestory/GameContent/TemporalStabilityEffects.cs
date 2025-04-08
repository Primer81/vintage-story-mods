using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class TemporalStabilityEffects : ModSystem, IRenderer, IDisposable
{
	private bool rainAndFogActive;

	private bool slowmoModeActive;

	private bool glitchPresent;

	private float glitchActive;

	private float warp;

	private float secondsPassedRainFogMode;

	private float secondsPassedSlowMoMode;

	private float secondsPassedSlowGlitchMode;

	private ICoreAPI api;

	private ICoreClientAPI capi;

	private SimpleParticleProperties blackAirParticles;

	private IServerNetworkChannel serverChannel;

	private IClientNetworkChannel clientChannel;

	private AmbientModifier rainfogAmbient;

	private GearRenderer renderer;

	public double RenderOrder => 1.0;

	public int RenderRange => 9999;

	public override bool ShouldLoad(EnumAppSide forSide)
	{
		return true;
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		capi = api;
		this.api = api;
		clientChannel = api.Network.RegisterChannel("gameeffects").RegisterMessageType(typeof(GameEffectsPacket)).SetMessageHandler<GameEffectsPacket>(OnGameEffectToggle);
		blackAirParticles = new SimpleParticleProperties
		{
			Color = ColorUtil.ToRgba(150, 50, 25, 15),
			ParticleModel = EnumParticleModel.Quad,
			MinSize = 0.1f,
			MaxSize = 1f,
			GravityEffect = 0f,
			LifeLength = 1.2f,
			WithTerrainCollision = false,
			ShouldDieInLiquid = true,
			MinVelocity = new Vec3f(-5f, 10f, -3f),
			MinQuantity = 1f,
			AddQuantity = 0f
		};
		blackAirParticles.AddVelocity = new Vec3f(0f, 30f, 0f);
		blackAirParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -8f);
		api.Event.RegisterRenderer(this, EnumRenderStage.Before, "gameeffects");
		renderer = new GearRenderer(capi);
		api.ChatCommands.GetOrCreate("debug").BeginSubCommand("geartest").WithDescription("")
			.HandleWith(delegate
			{
				renderer.Init();
				return TextCommandResult.Success();
			})
			.EndSubCommand();
		api.Event.LevelFinalize += Event_LevelFinalize;
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		this.api = api;
		api.Event.RegisterGameTickListener(OnServerTick, 20);
		serverChannel = api.Network.RegisterChannel("gameeffects").RegisterMessageType(typeof(GameEffectsPacket));
	}

	private void OnGameEffectToggle(GameEffectsPacket msg)
	{
		if (rainAndFogActive != msg.RainAndFogActive)
		{
			ResetRainFog();
		}
		rainAndFogActive = msg.RainAndFogActive;
		if (slowmoModeActive != msg.SlomoActive)
		{
			ResetSlomo();
		}
		slowmoModeActive = msg.SlomoActive;
		if (glitchPresent != msg.GlitchPresent)
		{
			ResetGlitch();
		}
		glitchPresent = msg.GlitchPresent;
	}

	private void ResetRainFog()
	{
		if (capi != null)
		{
			float b = 0.5f;
			capi.Ambient.CurrentModifiers["brownrainandfog"] = (rainfogAmbient = new AmbientModifier
			{
				AmbientColor = new WeightedFloatArray(new float[4]
				{
					b * 132f / 255f,
					b * 115f / 255f,
					b * 112f / 255f,
					1f
				}, 0f),
				FogColor = new WeightedFloatArray(new float[4]
				{
					b * 132f / 255f,
					b * 115f / 255f,
					b * 112f / 255f,
					1f
				}, 0f),
				FogDensity = new WeightedFloat(0.035f, 0f)
			}.EnsurePopulated());
		}
		secondsPassedRainFogMode = 0f;
	}

	private void ResetSlomo()
	{
		GlobalConstants.OverallSpeedMultiplier = 1f;
		secondsPassedSlowMoMode = 0f;
		api.World.Calendar.RemoveTimeSpeedModifier("slomo");
	}

	private void ResetGlitch()
	{
		warp = 0f;
		if (capi != null)
		{
			capi.Render.ShaderUniforms.GlobalWorldWarp = 0f;
			capi.Ambient.CurrentModifiers.Remove("glitch");
		}
		secondsPassedSlowGlitchMode = 0f;
	}

	private void UpdateClients()
	{
		serverChannel.BroadcastPacket(new GameEffectsPacket
		{
			SlomoActive = slowmoModeActive,
			RainAndFogActive = rainAndFogActive,
			GlitchPresent = glitchPresent
		});
	}

	private void OnServerTick(float dt)
	{
		if (glitchPresent)
		{
			warp = GameMath.Clamp(warp + dt * 40f, 0f, 30f);
		}
		if (slowmoModeActive)
		{
			secondsPassedSlowMoMode += dt / 3f;
			GlobalConstants.OverallSpeedMultiplier = 1f - GameMath.SmoothStep(Math.Min(1f, secondsPassedSlowMoMode));
			api.World.Calendar.SetTimeSpeedModifier("slomo", -60f * (1f - GlobalConstants.OverallSpeedMultiplier));
		}
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		if (slowmoModeActive)
		{
			secondsPassedSlowMoMode += deltaTime / 3f;
			GlobalConstants.OverallSpeedMultiplier = 1f - GameMath.SmoothStep(Math.Min(1f, secondsPassedSlowMoMode));
			capi.World.Calendar.SetTimeSpeedModifier("slomo", -60f * (1f - GlobalConstants.OverallSpeedMultiplier));
		}
		if (rainAndFogActive)
		{
			secondsPassedRainFogMode += deltaTime;
			float strength = Math.Min(1f, secondsPassedRainFogMode / 3f);
			rainfogAmbient.AmbientColor.Weight = strength;
			rainfogAmbient.FogColor.Weight = strength;
			rainfogAmbient.FogDensity.Weight = strength;
			float tries = 40f * strength;
			while (tries-- > 0f)
			{
				float offX = (float)capi.World.Rand.NextDouble() * 64f - 32f;
				float offY = (float)capi.World.Rand.NextDouble() * 64f - 32f;
				float offZ = (float)capi.World.Rand.NextDouble() * 64f - 32f;
				Vec3d position = capi.World.Player.Entity.Pos.XYZ.OffsetCopy(offX, offY, offZ);
				BlockPos pos = new BlockPos((int)position.X, (int)position.Y, (int)position.Z);
				if (capi.World.BlockAccessor.IsValidPos(pos))
				{
					blackAirParticles.WithTerrainCollision = false;
					blackAirParticles.MinPos = position;
					blackAirParticles.SelfPropelled = true;
					capi.World.SpawnParticles(blackAirParticles);
				}
			}
		}
		if (!glitchPresent)
		{
			return;
		}
		warp = 0f;
		secondsPassedSlowGlitchMode += deltaTime;
		bool wasActive = glitchActive > 0f;
		if (capi.World.Rand.NextDouble() < 0.0001 + (double)secondsPassedSlowGlitchMode / 10000.0 && glitchActive <= 0f)
		{
			glitchActive = 0.1f + (float)capi.World.Rand.NextDouble() / 3.5f;
			capi.World.AddCameraShake(glitchActive * 1.2f);
		}
		if (glitchActive > 0f)
		{
			capi.Ambient.CurrentModifiers["glitch"] = new AmbientModifier
			{
				AmbientColor = new WeightedFloatArray(new float[4] { 0.458f, 0.223f, 0.129f, 1f }, 1f),
				FogColor = new WeightedFloatArray(new float[4] { 0.229f, 0.1115f, 0.0645f, 1f }, 1f),
				FogDensity = new WeightedFloat(0.04f, 1f)
			}.EnsurePopulated();
			glitchActive -= deltaTime;
			warp = 100f;
		}
		else
		{
			capi.Ambient.CurrentModifiers.Remove("glitch");
			if (wasActive)
			{
				capi.World.ReduceCameraShake(0.2f);
			}
		}
		capi.Render.ShaderUniforms.GlobalWorldWarp = warp;
	}

	private void Event_LevelFinalize()
	{
		renderer.Init();
		capi.Logger.VerboseDebug("Done init huge gears");
	}
}
