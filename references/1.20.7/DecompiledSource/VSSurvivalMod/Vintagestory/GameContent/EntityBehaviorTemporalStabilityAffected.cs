using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace Vintagestory.GameContent;

public class EntityBehaviorTemporalStabilityAffected : EntityBehavior
{
	private ILoadedSound tempStabSoundDrain;

	private ILoadedSound tempStabSoundLow;

	private ILoadedSound tempStabSoundVeryLow;

	private AmbientModifier rainfogAmbient;

	private SimpleParticleProperties rustParticles;

	private NormalizedSimplexNoise fogNoise;

	private ICoreClientAPI capi;

	private SystemTemporalStability tempStabilitySystem;

	private WeatherSimulationParticles precipParticleSys;

	private float oneSecAccum;

	private float threeSecAccum;

	private double hereTempStabChangeVelocity;

	private double glitchEffectStrength;

	private double fogEffectStrength;

	private double rustPrecipColorStrength;

	private bool requireInitSounds;

	private bool enabled = true;

	private bool isSelf;

	private bool isCommand;

	private BlockPos tmpPos = new BlockPos();

	public double stabilityOffset;

	private float jitterOffset;

	private float jitterOffsetedDuration;

	public double TempStabChangeVelocity { get; set; }

	public double GlichEffectStrength => glitchEffectStrength;

	public double OwnStability
	{
		get
		{
			return entity.WatchedAttributes.GetDouble("temporalStability");
		}
		set
		{
			entity.WatchedAttributes.SetDouble("temporalStability", value);
		}
	}

	public EntityBehaviorTemporalStabilityAffected(Entity entity)
		: base(entity)
	{
	}

	public override void Initialize(EntityProperties properties, JsonObject attributes)
	{
		base.Initialize(properties, attributes);
		tempStabilitySystem = entity.Api.ModLoader.GetModSystem<SystemTemporalStability>();
		if (entity.Api.Side == EnumAppSide.Client)
		{
			requireInitSounds = true;
			precipParticleSys = entity.Api.ModLoader.GetModSystem<WeatherSystemClient>().simParticles;
		}
		enabled = entity.Api.World.Config.GetBool("temporalStability", defaultValue: true);
		if (!entity.WatchedAttributes.HasAttribute("temporalStability"))
		{
			OwnStability = 1.0;
		}
	}

	public override void OnEntityLoaded()
	{
		capi = entity.Api as ICoreClientAPI;
		if (capi != null && (entity as EntityPlayer)?.PlayerUID == capi.Settings.String["playeruid"])
		{
			capi.Event.RegisterEventBusListener(onChatKeyDownPre, 1.0, "chatkeydownpre");
			capi.Event.RegisterEventBusListener(onChatKeyDownPost, 1.0, "chatkeydownpost");
		}
	}

	private void onChatKeyDownPost(string eventName, ref EnumHandling handling, IAttribute data)
	{
		TreeAttribute treeAttr = data as TreeAttribute;
		string text = (treeAttr["text"] as StringAttribute).value;
		if (isCommand && text.Length > 0 && text[0] != '.' && text[0] != '/')
		{
			float str = (capi.Render.ShaderUniforms.GlitchStrength - 0.5f) * 2f;
			(treeAttr["text"] as StringAttribute).value = destabilizeText(text, str);
		}
	}

	private void onChatKeyDownPre(string eventName, ref EnumHandling handling, IAttribute data)
	{
		TreeAttribute treeAttr = data as TreeAttribute;
		int keyCode = (treeAttr["key"] as IntAttribute).value;
		string text = (treeAttr["text"] as StringAttribute).value;
		isCommand = text.Length > 0 && (text[0] == '.' || text[0] == '/');
		if (keyCode != 53 && capi.Render.ShaderUniforms.GlitchStrength > 0.5f && (text.Length == 0 || !isCommand))
		{
			float str = (capi.Render.ShaderUniforms.GlitchStrength - 0.5f) * 2f;
			(treeAttr["text"] as StringAttribute).value = destabilizeText(text, str);
		}
	}

	private string destabilizeText(string text, float str)
	{
		char[] zalgo_mid = new char[23]
		{
			'\u0315', '\u031b', '\u0340', '\u0341', '\u0358', '\u0321', '\u0322', '\u0327', '\u0328', '\u0334',
			'\u0335', '\u0336', '\u034f', '\u035c', '\u035d', '\u035e', '\u035f', '\u0360', '\u0362', '\u0338',
			'\u0337', '\u0361', '\u0489'
		};
		string text2 = "";
		for (int i = 0; i < text.Length; i++)
		{
			ReadOnlySpan<char> readOnlySpan = text2;
			char reference = text[i];
			text2 = string.Concat(readOnlySpan, new ReadOnlySpan<char>(in reference));
			if (i < text.Length - 1 && zalgo_mid.Contains(text[i + 1]))
			{
				ReadOnlySpan<char> readOnlySpan2 = text2;
				reference = text[i + 1];
				text2 = string.Concat(readOnlySpan2, new ReadOnlySpan<char>(in reference));
				i++;
			}
			else if (!zalgo_mid.Contains(text[i]) && capi.World.Rand.NextDouble() < (double)str)
			{
				ReadOnlySpan<char> readOnlySpan3 = text2;
				reference = zalgo_mid[capi.World.Rand.Next(zalgo_mid.Length)];
				text2 = string.Concat(readOnlySpan3, new ReadOnlySpan<char>(in reference));
			}
		}
		return text2;
	}

	private void initSoundsAndEffects()
	{
		capi = entity.Api as ICoreClientAPI;
		isSelf = capi.World.Player.Entity.EntityId == entity.EntityId;
		if (isSelf)
		{
			capi.Event.RegisterAsyncParticleSpawner(asyncParticleSpawn);
			fogNoise = NormalizedSimplexNoise.FromDefaultOctaves(4, 1.0, 0.9, 123L);
			rustParticles = new SimpleParticleProperties
			{
				Color = ColorUtil.ToRgba(150, 50, 25, 15),
				ParticleModel = EnumParticleModel.Quad,
				MinSize = 0.1f,
				MaxSize = 0.5f,
				GravityEffect = 0f,
				LifeLength = 2f,
				WithTerrainCollision = false,
				ShouldDieInLiquid = false,
				RandomVelocityChange = true,
				MinVelocity = new Vec3f(-1f, -1f, -1f),
				AddVelocity = new Vec3f(2f, 2f, 2f),
				MinQuantity = 1f,
				AddQuantity = 0f
			};
			rustParticles.AddVelocity = new Vec3f(0f, 30f, 0f);
			rustParticles.OpacityEvolve = EvolvingNatFloat.create(EnumTransformFunction.QUADRATIC, -8f);
			float b = 0.25f;
			capi.Ambient.CurrentModifiers["brownrainandfog"] = (rainfogAmbient = new AmbientModifier
			{
				AmbientColor = new WeightedFloatArray(new float[4]
				{
					22f / 85f,
					23f / 102f,
					0.21960784f,
					1f
				}, 0f),
				FogColor = new WeightedFloatArray(new float[4]
				{
					b * 132f / 255f,
					b * 115f / 255f,
					b * 112f / 255f,
					1f
				}, 0f),
				FogDensity = new WeightedFloat(0.05f, 0f)
			}.EnsurePopulated());
			tempStabSoundDrain = capi.World.LoadSound(new SoundParams
			{
				Location = new AssetLocation("sounds/effect/tempstab-drain.ogg"),
				ShouldLoop = true,
				RelativePosition = true,
				DisposeOnFinish = false,
				SoundType = EnumSoundType.SoundGlitchunaffected,
				Volume = 0f
			});
			tempStabSoundLow = capi.World.LoadSound(new SoundParams
			{
				Location = new AssetLocation("sounds/effect/tempstab-low.ogg"),
				ShouldLoop = true,
				RelativePosition = true,
				DisposeOnFinish = false,
				SoundType = EnumSoundType.SoundGlitchunaffected,
				Volume = 0f
			});
			tempStabSoundVeryLow = capi.World.LoadSound(new SoundParams
			{
				Location = new AssetLocation("sounds/effect/tempstab-verylow.ogg"),
				ShouldLoop = true,
				RelativePosition = true,
				DisposeOnFinish = false,
				SoundType = EnumSoundType.SoundGlitchunaffected,
				Volume = 0f
			});
		}
	}

	private bool asyncParticleSpawn(float dt, IAsyncParticleManager manager)
	{
		if (isSelf && (fogEffectStrength > 0.05 || glitchEffectStrength > 0.05))
		{
			tmpPos.Set((int)entity.Pos.X, (int)entity.Pos.Y, (int)entity.Pos.Z);
			float sunb = (float)capi.World.BlockAccessor.GetLightLevel(tmpPos, EnumLightLevelType.OnlySunLight) / 22f;
			float strength = Math.Min(1f, (float)glitchEffectStrength);
			double fognoise = fogEffectStrength * Math.Abs(fogNoise.Noise(0.0, (float)capi.InWorldEllapsedMilliseconds / 1000f)) / 60.0;
			rainfogAmbient.FogDensity.Value = 0.05f + (float)fognoise;
			rainfogAmbient.AmbientColor.Weight = strength;
			rainfogAmbient.FogColor.Weight = strength;
			rainfogAmbient.FogDensity.Weight = (float)Math.Pow(strength, 2.0);
			rainfogAmbient.FogColor.Value[0] = sunb * 116f / 255f;
			rainfogAmbient.FogColor.Value[1] = sunb * 77f / 255f;
			rainfogAmbient.FogColor.Value[2] = sunb * 49f / 255f;
			rainfogAmbient.AmbientColor.Value[0] = 0.22745098f;
			rainfogAmbient.AmbientColor.Value[1] = 0.1509804f;
			rainfogAmbient.AmbientColor.Value[2] = 0.09607843f;
			rustParticles.Color = ColorUtil.ToRgba((int)(strength * 150f), 50, 25, 15);
			rustParticles.MaxSize = 0.25f;
			rustParticles.RandomVelocityChange = false;
			rustParticles.MinVelocity.Set(0f, 1f, 0f);
			rustParticles.AddVelocity.Set(0f, 5f, 0f);
			rustParticles.LifeLength = 0.75f;
			Vec3d position = new Vec3d();
			EntityPos plrPos = capi.World.Player.Entity.Pos;
			float tries = 120f * strength;
			while (tries-- > 0f)
			{
				float offX = (float)capi.World.Rand.NextDouble() * 24f - 12f;
				float offY = (float)capi.World.Rand.NextDouble() * 24f - 12f;
				float offZ = (float)capi.World.Rand.NextDouble() * 24f - 12f;
				position.Set(plrPos.X + (double)offX, plrPos.Y + (double)offY, plrPos.Z + (double)offZ);
				BlockPos pos = new BlockPos((int)position.X, (int)position.Y, (int)position.Z);
				if (capi.World.BlockAccessor.IsValidPos(pos))
				{
					rustParticles.MinPos = position;
					capi.World.SpawnParticles(rustParticles);
				}
			}
		}
		return true;
	}

	internal void AddStability(double amount)
	{
		OwnStability += amount;
	}

	public override string PropertyName()
	{
		return "temporalstabilityaffected";
	}

	public override void OnGameTick(float deltaTime)
	{
		if (!enabled)
		{
			return;
		}
		if (requireInitSounds)
		{
			initSoundsAndEffects();
			requireInitSounds = false;
		}
		if (entity.World.Side == EnumAppSide.Client)
		{
			if (!(entity.World.Api as ICoreClientAPI).PlayerReadyFired)
			{
				return;
			}
		}
		else if (entity.World.PlayerByUid(((EntityPlayer)entity).PlayerUID) is IServerPlayer { ConnectionState: not EnumClientState.Playing })
		{
			return;
		}
		deltaTime = GameMath.Min(0.5f, deltaTime);
		float changeSpeed = deltaTime / 3f;
		double hereStability = stabilityOffset + (double)tempStabilitySystem.GetTemporalStability(entity.SidedPos.X, entity.SidedPos.Y, entity.SidedPos.Z);
		entity.Attributes.SetDouble("tempStabChangeVelocity", TempStabChangeVelocity);
		double gain = ((TempStabChangeVelocity > 0.0) ? (TempStabChangeVelocity / 200.0) : (TempStabChangeVelocity / 800.0));
		OwnStability = GameMath.Clamp(OwnStability + gain, 0.0, 1.0);
		double ownStability = OwnStability;
		TempStabChangeVelocity = (hereTempStabChangeVelocity - TempStabChangeVelocity) * (double)deltaTime;
		float glitchEffectExtraStrength = tempStabilitySystem.GetGlitchEffectExtraStrength();
		double targetGlitchEffectStrength = Math.Max(0.0, Math.Max(0.0, (0.20000000298023224 - ownStability) * 1.0 / 0.20000000298023224) + (double)glitchEffectExtraStrength);
		glitchEffectStrength += (targetGlitchEffectStrength - glitchEffectStrength) * (double)changeSpeed;
		glitchEffectStrength = GameMath.Clamp(glitchEffectStrength, 0.0, 1.100000023841858);
		double targetFogEffectStrength = Math.Max(0.0, Math.Max(0.0, (0.30000001192092896 - ownStability) * 1.0 / 0.30000001192092896) + (double)glitchEffectExtraStrength);
		fogEffectStrength += (targetFogEffectStrength - fogEffectStrength) * (double)changeSpeed;
		fogEffectStrength = GameMath.Clamp(fogEffectStrength, 0.0, 0.8999999761581421);
		double targetRustPrecipStrength = Math.Max(0.0, Math.Max(0.0, (0.30000001192092896 - ownStability) * 1.0 / 0.30000001192092896) + (double)glitchEffectExtraStrength);
		rustPrecipColorStrength += (targetRustPrecipStrength - rustPrecipColorStrength) * (double)changeSpeed;
		rustPrecipColorStrength = GameMath.Clamp(rustPrecipColorStrength, 0.0, 1.0);
		if (precipParticleSys != null)
		{
			precipParticleSys.rainParticleColor = ColorUtil.ColorOverlay(WeatherSimulationParticles.waterColor, WeatherSimulationParticles.lowStabColor, (float)rustPrecipColorStrength);
		}
		hereTempStabChangeVelocity = hereStability - 1.0;
		oneSecAccum += deltaTime;
		if (oneSecAccum > 1f)
		{
			oneSecAccum = 0f;
			updateSoundsAndEffects(hereStability, Math.Max(0.0, ownStability - (double)(1.5f * glitchEffectExtraStrength)));
		}
		threeSecAccum += deltaTime;
		if (threeSecAccum > 4f)
		{
			threeSecAccum = 0f;
			if (entity.World.Side == EnumAppSide.Server && ownStability < 0.13)
			{
				entity.ReceiveDamage(new DamageSource
				{
					DamageTier = 0,
					Source = EnumDamageSource.Machine,
					Type = EnumDamageType.Poison
				}, (float)(0.15 - ownStability));
			}
		}
		if (isSelf)
		{
			capi.Render.ShaderUniforms.GlitchStrength = 0f;
		}
		if (!isSelf || (!(fogEffectStrength > 0.05) && !(glitchEffectStrength > 0.05)))
		{
			return;
		}
		float str = capi.Settings.Float["instabilityWavingStrength"];
		capi.Render.ShaderUniforms.GlitchStrength = (float)glitchEffectStrength;
		capi.Render.ShaderUniforms.GlitchWaviness = (float)glitchEffectStrength * str;
		capi.Render.ShaderUniforms.GlobalWorldWarp = (float)((capi.World.Rand.NextDouble() < 0.015) ? (Math.Max(0.0, glitchEffectStrength - 0.05000000074505806) * capi.World.Rand.NextDouble() * capi.World.Rand.NextDouble()) : 0.0) * str;
		float tempStormJitterStrength = 9f;
		if (capi.Settings.Float.Exists("tempStormJitterStrength"))
		{
			tempStormJitterStrength = capi.Settings.Float["tempStormJitterStrength"];
		}
		if (capi.World.Rand.NextDouble() < 0.015 && jitterOffset == 0f)
		{
			jitterOffset = tempStormJitterStrength * (float)capi.World.Rand.NextDouble() + 3f;
			jitterOffsetedDuration = 0.25f + (float)capi.World.Rand.NextDouble() / 2f;
			capi.Render.ShaderUniforms.WindWaveCounter += jitterOffset;
			capi.Render.ShaderUniforms.WaterWaveCounter += jitterOffset;
		}
		if (jitterOffset > 0f)
		{
			capi.Render.ShaderUniforms.WindWaveCounter += (float)capi.World.Rand.NextDouble() / 2f - 0.25f;
			jitterOffsetedDuration -= deltaTime;
			if (jitterOffsetedDuration <= 0f)
			{
				jitterOffset = 0f;
			}
		}
		if (capi.World.Rand.NextDouble() < 0.002 && capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Survival)
		{
			capi.Input.MouseYaw += (float)capi.World.Rand.NextDouble() * 0.125f - 0.0625f;
			capi.Input.MousePitch += (float)capi.World.Rand.NextDouble() * 0.125f - 0.0625f;
		}
		double fognoise = fogEffectStrength * Math.Abs(fogNoise.Noise(0.0, (float)capi.InWorldEllapsedMilliseconds / 1000f)) / 60.0;
		rainfogAmbient.FogDensity.Value = 0.05f + (float)fognoise;
	}

	private void updateSoundsAndEffects(double hereStability, double ownStability)
	{
		if (!isSelf || tempStabSoundDrain == null)
		{
			return;
		}
		float fadeSpeed = 3f;
		if (hereStability < 0.949999988079071 && ownStability < 0.6499999761581421)
		{
			if (!tempStabSoundDrain.IsPlaying)
			{
				tempStabSoundDrain.Start();
			}
			tempStabSoundDrain.FadeTo(Math.Min(1.0, 3.0 * (1.0 - hereStability)), 0.95f * fadeSpeed, delegate
			{
			});
		}
		else
		{
			tempStabSoundDrain.FadeTo(0.0, 0.95f * fadeSpeed, delegate
			{
				tempStabSoundDrain.Stop();
			});
		}
		SurfaceMusicTrack.ShouldPlayMusic = ownStability > 0.44999998807907104;
		CaveMusicTrack.ShouldPlayCaveMusic = ownStability > 0.20000000298023224;
		if (ownStability < 0.4000000059604645)
		{
			if (!tempStabSoundLow.IsPlaying)
			{
				tempStabSoundLow.Start();
			}
			float volume2 = (0.4f - (float)ownStability) * 1f / 0.4f;
			tempStabSoundLow.FadeTo(Math.Min(1f, volume2), 0.95f * fadeSpeed, delegate
			{
			});
		}
		else
		{
			tempStabSoundLow.FadeTo(0.0, 0.95f * fadeSpeed, delegate
			{
				tempStabSoundLow.Stop();
			});
		}
		if (ownStability < 0.25)
		{
			if (!tempStabSoundVeryLow.IsPlaying)
			{
				tempStabSoundVeryLow.Start();
			}
			float volume = (0.25f - (float)ownStability) * 1f / 0.25f;
			tempStabSoundVeryLow.FadeTo(Math.Min(1f, volume) / 5f, 0.95f * fadeSpeed, delegate
			{
			});
		}
		else
		{
			tempStabSoundVeryLow.FadeTo(0.0, 0.95f * fadeSpeed, delegate
			{
				tempStabSoundVeryLow.Stop();
			});
		}
	}
}
