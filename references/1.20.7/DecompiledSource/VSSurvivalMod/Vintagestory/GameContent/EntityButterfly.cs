using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public class EntityButterfly : EntityAgent
{
	public double windMotion;

	private int cnt;

	private float flapPauseDt;

	public override bool IsInteractable => false;

	static EntityButterfly()
	{
		AiTaskRegistry.Register<AiTaskButterflyWander>("butterflywander");
		AiTaskRegistry.Register<AiTaskButterflyRest>("butterflyrest");
		AiTaskRegistry.Register<AiTaskButterflyChase>("butterflychase");
		AiTaskRegistry.Register<AiTaskButterflyFlee>("butterflyflee");
		AiTaskRegistry.Register<AiTaskButterflyFeedOnFlowers>("butterflyfeedonflowers");
	}

	public override void Initialize(EntityProperties properties, ICoreAPI api, long InChunkIndex3d)
	{
		base.Initialize(properties, api, InChunkIndex3d);
		if (api.Side == EnumAppSide.Client)
		{
			WatchedAttributes.RegisterModifiedListener("windWaveIntensity", delegate
			{
				(base.Properties.Client.Renderer as EntityShapeRenderer).WindWaveIntensity = WatchedAttributes.GetDouble("windWaveIntensity");
			});
		}
		if (api.World.BlockAccessor.GetClimateAt(Pos.AsBlockPos, EnumGetClimateMode.ForSuppliedDate_TemperatureOnly, api.World.Calendar.TotalDays).Temperature < 0f)
		{
			Die(EnumDespawnReason.Removed);
		}
	}

	public override void OnGameTick(float dt)
	{
		if (World.Side == EnumAppSide.Server)
		{
			base.OnGameTick(dt);
			return;
		}
		if (FeetInLiquid)
		{
			(base.Properties.Client.Renderer as EntityShapeRenderer).AddRenderFlags |= 201326592;
		}
		else
		{
			(base.Properties.Client.Renderer as EntityShapeRenderer).AddRenderFlags &= -201326593;
		}
		if (!AnimManager.ActiveAnimationsByAnimCode.ContainsKey("feed") && !AnimManager.ActiveAnimationsByAnimCode.ContainsKey("rest"))
		{
			if (ServerPos.Y < Pos.Y - 0.05 && !base.Collided && !FeetInLiquid)
			{
				SetAnimation("glide", 1f);
			}
			if (FeetInLiquid)
			{
				StopAnimation("glide");
			}
			if ((ServerPos.Y > Pos.Y - 0.02 || base.Collided) && !FeetInLiquid)
			{
				SetAnimation("fly", 2.5f);
			}
			if (FeetInLiquid && flapPauseDt <= 0f && Api.World.Rand.NextDouble() < 0.06)
			{
				flapPauseDt = 2f + 6f * (float)Api.World.Rand.NextDouble();
				StopAnimation("fly");
			}
			if (flapPauseDt > 0f)
			{
				flapPauseDt -= dt;
				if (flapPauseDt <= 0f)
				{
					SetAnimation("fly", 2.5f);
				}
			}
			else if (FeetInLiquid)
			{
				EntityPos herepos = Pos;
				double width = SelectionBox.XSize * 0.75f;
				Entity.SplashParticleProps.BasePos.Set(herepos.X - width / 2.0, herepos.Y - 0.05, herepos.Z - width / 2.0);
				Entity.SplashParticleProps.AddPos.Set(width, 0.0, width);
				Entity.SplashParticleProps.AddVelocity.Set(0f, 0f, 0f);
				Entity.SplashParticleProps.QuantityMul = 0.01f;
				World.SpawnParticles(Entity.SplashParticleProps);
				SpawnWaterMovementParticles(1f, 0.0, 0.05);
			}
		}
		base.OnGameTick(dt);
		if (cnt++ > 30)
		{
			float affectedness = ((World.BlockAccessor.GetLightLevel(base.SidedPos.XYZ.AsBlockPos, EnumLightLevelType.OnlySunLight) < 14) ? 1 : 0);
			windMotion = Api.ModLoader.GetModSystem<WeatherSystemBase>().WeatherDataSlowAccess.GetWindSpeed(base.SidedPos.XYZ) * (double)affectedness;
			cnt = 0;
		}
		if (AnimManager.ActiveAnimationsByAnimCode.ContainsKey("fly"))
		{
			base.SidedPos.X += Math.Max(0.0, (windMotion - 0.2) / 20.0);
		}
		if (ServerPos.SquareDistanceTo(Pos.XYZ) > 0.01 && !FeetInLiquid)
		{
			float desiredYaw = (float)Math.Atan2(ServerPos.X - Pos.X, ServerPos.Z - Pos.Z);
			float yawDist = GameMath.AngleRadDistance(base.SidedPos.Yaw, desiredYaw);
			Pos.Yaw += GameMath.Clamp(yawDist, -35f * dt, 35f * dt);
			Pos.Yaw %= (float)Math.PI * 2f;
		}
	}

	private void SetAnimation(string animCode, float speed)
	{
		if (!AnimManager.ActiveAnimationsByAnimCode.TryGetValue(animCode, out var animMeta))
		{
			animMeta = new AnimationMetaData
			{
				Code = animCode,
				Animation = animCode,
				AnimationSpeed = speed
			};
			AnimManager.ActiveAnimationsByAnimCode.Clear();
			AnimManager.ActiveAnimationsByAnimCode[animMeta.Animation] = animMeta;
		}
		else
		{
			animMeta.AnimationSpeed = speed;
			UpdateDebugAttributes();
		}
	}

	public override void OnReceivedServerAnimations(int[] activeAnimations, int activeAnimationsCount, float[] activeAnimationSpeeds)
	{
		if (activeAnimationsCount == 0)
		{
			AnimManager.ActiveAnimationsByAnimCode.Clear();
			AnimManager.StartAnimation("fly");
		}
		string active = "";
		bool found = false;
		for (int i = 0; i < activeAnimationsCount; i++)
		{
			int crc32 = activeAnimations[i];
			for (int j = 0; j < base.Properties.Client.LoadedShape.Animations.Length; j++)
			{
				Animation anim = base.Properties.Client.LoadedShape.Animations[j];
				int mask = int.MaxValue;
				if ((anim.CodeCrc32 & mask) != (crc32 & mask))
				{
					continue;
				}
				if (AnimManager.ActiveAnimationsByAnimCode.ContainsKey(anim.Code))
				{
					break;
				}
				if (!(anim.Code == "glide") && !(anim.Code == "fly"))
				{
					string code = ((anim.Code == null) ? anim.Name.ToLowerInvariant() : anim.Code);
					active = active + ", " + code;
					base.Properties.Client.AnimationsByMetaCode.TryGetValue(code, out var animmeta);
					if (animmeta == null)
					{
						animmeta = new AnimationMetaData
						{
							Code = code,
							Animation = code,
							CodeCrc32 = anim.CodeCrc32
						};
					}
					animmeta.AnimationSpeed = activeAnimationSpeeds[i];
					AnimManager.ActiveAnimationsByAnimCode[anim.Code] = animmeta;
					found = true;
				}
			}
		}
		if (found)
		{
			AnimManager.StopAnimation("fly");
			AnimManager.StopAnimation("glide");
			(base.Properties.Client.Renderer as EntityShapeRenderer).AddRenderFlags = 134217728;
			(base.Properties.Client.Renderer as EntityShapeRenderer).AddRenderFlags |= 536870912;
		}
		else
		{
			(base.Properties.Client.Renderer as EntityShapeRenderer).AddRenderFlags = 0;
		}
	}
}
