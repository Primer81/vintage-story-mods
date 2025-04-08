using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class WeatherSimulationLightning : IRenderer, IDisposable
{
	private WeatherSystemBase weatherSys;

	private WeatherSystemClient weatherSysc;

	private ICoreClientAPI capi;

	private IShaderProgram prog;

	public float lightningTime;

	public float lightningIntensity;

	public AmbientModifier LightningAmbient;

	public AmbientModifier actualSunGlowAmb = new AmbientModifier().EnsurePopulated();

	private float nearLightningCoolDown;

	public List<LightningFlash> lightningFlashes = new List<LightningFlash>();

	public double RenderOrder => 0.35;

	public int RenderRange => 9999;

	public WeatherSimulationLightning(ICoreAPI api, WeatherSystemBase weatherSys)
	{
		this.weatherSys = weatherSys;
		weatherSysc = weatherSys as WeatherSystemClient;
		capi = api as ICoreClientAPI;
		if (api.Side == EnumAppSide.Client)
		{
			LightningAmbient = new AmbientModifier().EnsurePopulated();
			capi.Ambient.CurrentModifiers["lightningambient"] = LightningAmbient;
			capi.Event.ReloadShader += LoadShader;
			LoadShader();
			capi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "lightning");
		}
		else
		{
			api.Event.RegisterGameTickListener(OnServerTick, 40, 3);
			api.ChatCommands.GetOrCreate("debug").BeginSubCommand("lntest").BeginSubCommand("spawn")
				.WithDescription("Lightning test")
				.WithArgs(api.ChatCommands.Parsers.OptionalInt("range", 10))
				.RequiresPlayer()
				.RequiresPrivilege(Privilege.controlserver)
				.HandleWith(OnCmdLineTestServer)
				.EndSubCommand()
				.BeginSubCommand("clear")
				.WithDescription("Clear all lightning flashes")
				.RequiresPrivilege(Privilege.controlserver)
				.HandleWith(OnCmdLineTestServerClear)
				.EndSubCommand()
				.EndSubCommand();
		}
	}

	private TextCommandResult OnCmdLineTestServerClear(TextCommandCallingArgs args)
	{
		foreach (LightningFlash lightningFlash in lightningFlashes)
		{
			lightningFlash.Dispose();
		}
		lightningFlashes.Clear();
		return TextCommandResult.Success("Cleared all lightning flashes");
	}

	private TextCommandResult OnCmdLineTestServer(TextCommandCallingArgs args)
	{
		int range = (int)args.Parsers[0].GetValue();
		Vec3d pos = args.Caller.Entity.Pos.AheadCopy(range).XYZ;
		weatherSys.SpawnLightningFlash(pos);
		return TextCommandResult.Success($"Spawned lightning {range} block ahead");
	}

	public void ClientTick(float dt)
	{
		WeatherDataSnapshot weatherData = weatherSysc.BlendedWeatherData;
		if (!(weatherSysc.clientClimateCond.Temperature >= weatherData.lightningMinTemp))
		{
			return;
		}
		float deepnessSub = GameMath.Clamp(1f - (float)Math.Pow(capi.World.Player.Entity.Pos.Y / (double)capi.World.SeaLevel * 1.5 - 0.5, 1.5) - WeatherSimulationSound.roomVolumePitchLoss * 0.5f, 0f, 1f);
		Random rand = capi.World.Rand;
		double rndval = rand.NextDouble();
		rndval -= (double)(weatherData.distantLightningRate * weatherSysc.clientClimateCond.RainCloudOverlay);
		if (rndval <= 0.0)
		{
			lightningTime = 0.07f + (float)rand.NextDouble() * 0.17f;
			lightningIntensity = 0.25f + (float)rand.NextDouble();
			float pitch = GameMath.Clamp((float)rand.NextDouble() * 0.3f + lightningTime / 2f + lightningIntensity / 2f - deepnessSub / 2f, 0.6f, 1.15f);
			float volume = GameMath.Clamp(Math.Min(1f, 0.25f + lightningTime + lightningIntensity / 2f) - 2f * deepnessSub, 0f, 1f);
			capi.World.PlaySoundAt(new AssetLocation("sounds/weather/lightning-distant.ogg"), 0.0, 0.0, 0.0, null, EnumSoundType.Weather, pitch, 32f, volume);
		}
		else
		{
			if (!(nearLightningCoolDown <= 0f))
			{
				return;
			}
			rndval -= (double)(weatherData.nearLightningRate * weatherSysc.clientClimateCond.RainCloudOverlay);
			if (rndval <= 0.0)
			{
				lightningTime = 0.07f + (float)rand.NextDouble() * 0.17f;
				lightningIntensity = 1f + (float)rand.NextDouble() * 0.9f;
				float pitch2 = GameMath.Clamp(0.75f + (float)rand.NextDouble() * 0.3f - deepnessSub / 2f, 0.5f, 1.2f);
				float volume2 = GameMath.Clamp(0.5f + (float)rand.NextDouble() * 0.5f - 2f * deepnessSub, 0f, 1f);
				AssetLocation loc;
				if (rand.NextDouble() > 0.25)
				{
					loc = new AssetLocation("sounds/weather/lightning-near.ogg");
					nearLightningCoolDown = 5f;
				}
				else
				{
					loc = new AssetLocation("sounds/weather/lightning-verynear.ogg");
					nearLightningCoolDown = 10f;
				}
				capi.World.PlaySoundAt(loc, 0.0, 0.0, 0.0, null, EnumSoundType.Weather, pitch2, 32f, volume2);
			}
		}
	}

	public void OnRenderFrame(float dt, EnumRenderStage stage)
	{
		if (prog.LoadError)
		{
			return;
		}
		switch (stage)
		{
		case EnumRenderStage.Opaque:
		{
			prog.Use();
			prog.UniformMatrix("projection", capi.Render.CurrentProjectionMatrix);
			prog.UniformMatrix("view", capi.Render.CameraMatrixOriginf);
			for (int i = 0; i < lightningFlashes.Count; i++)
			{
				LightningFlash lflash = lightningFlashes[i];
				lflash.Render(dt);
				if (!lflash.Alive)
				{
					lflash.Dispose();
					lightningFlashes.RemoveAt(i);
					i--;
				}
			}
			prog.Stop();
			return;
		}
		case EnumRenderStage.Done:
		{
			AmbientModifier sunGlowAmb = capi.Ambient.CurrentModifiers["sunglow"];
			actualSunGlowAmb.FogColor.Weight = sunGlowAmb.FogColor.Weight;
			dt = Math.Min(0.5f, dt);
			if (nearLightningCoolDown > 0f)
			{
				nearLightningCoolDown -= dt;
			}
			return;
		}
		}
		if (lightningTime > 0f)
		{
			float mul = Math.Min(10f * lightningIntensity * lightningTime, 1.5f);
			WeatherDataSnapshot weatherData = weatherSysc.BlendedWeatherData;
			LightningAmbient.CloudBrightness.Value = Math.Max(weatherData.Ambient.SceneBrightness.Value, mul);
			LightningAmbient.FogBrightness.Value = Math.Max(weatherData.Ambient.FogBrightness.Value, mul);
			LightningAmbient.CloudBrightness.Weight = Math.Min(1f, mul);
			LightningAmbient.FogBrightness.Weight = Math.Min(1f, mul);
			float sceneBrightIncrease = GameMath.Min(mul, GameMath.Max(0f, lightningIntensity - 0.75f));
			if (sceneBrightIncrease > 0f)
			{
				LightningAmbient.SceneBrightness.Weight = Math.Min(1f, sceneBrightIncrease);
				LightningAmbient.SceneBrightness.Value = 1f;
				AmbientModifier sunGlowAmb2 = capi.Ambient.CurrentModifiers["sunglow"];
				float nowWeight = GameMath.Clamp(1f - sceneBrightIncrease, 0f, 1f);
				sunGlowAmb2.FogColor.Weight = Math.Min(sunGlowAmb2.FogColor.Weight, nowWeight);
				sunGlowAmb2.AmbientColor.Weight = Math.Min(sunGlowAmb2.AmbientColor.Weight, nowWeight);
			}
			lightningTime -= dt / 1.7f;
			if (lightningTime <= 0f)
			{
				AmbientModifier ambientModifier = capi.Ambient.CurrentModifiers["sunglow"];
				ambientModifier.FogColor.Weight = actualSunGlowAmb.FogColor.Weight;
				ambientModifier.AmbientColor.Weight = actualSunGlowAmb.AmbientColor.Weight;
				LightningAmbient.CloudBrightness.Weight = 0f;
				LightningAmbient.FogBrightness.Weight = 0f;
				LightningAmbient.SceneBrightness.Weight = 0f;
			}
		}
	}

	public void OnServerTick(float dt)
	{
		for (int i = 0; i < lightningFlashes.Count; i++)
		{
			LightningFlash lflash = lightningFlashes[i];
			lflash.GameTick(dt);
			if (!lflash.Alive)
			{
				lflash.Dispose();
				lightningFlashes.RemoveAt(i);
				i--;
			}
		}
	}

	public bool LoadShader()
	{
		prog = capi.Shader.NewShaderProgram();
		prog.VertexShader = capi.Shader.NewShader(EnumShaderType.VertexShader);
		prog.FragmentShader = capi.Shader.NewShader(EnumShaderType.FragmentShader);
		capi.Shader.RegisterFileShaderProgram("lines", prog);
		return prog.Compile();
	}

	public void genLightningFlash(Vec3d pos, int? seed = null)
	{
		LightningFlash lflash = new LightningFlash(weatherSys, capi, seed, pos);
		lflash.ClientInit();
		lightningFlashes.Add(lflash);
	}

	public void Dispose()
	{
		foreach (LightningFlash lightningFlash in lightningFlashes)
		{
			lightningFlash.Dispose();
		}
	}
}
