using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class DrunkPerceptionEffect : PerceptionEffect
{
	private NormalizedSimplexNoise noisegen = NormalizedSimplexNoise.FromDefaultOctaves(4, 1.0, 0.9, 123L);

	private float accum;

	private float accum1s;

	private float targetIntensity;

	public DrunkPerceptionEffect(ICoreClientAPI capi)
		: base(capi)
	{
	}

	public override void OnBeforeGameRender(float dt)
	{
		if (!capi.IsGamePaused && capi.World.Player.Entity.AnimManager.HeadController != null)
		{
			capi.Render.ShaderUniforms.PerceptionEffectIntensity = Intensity;
			accum1s += dt;
			if (accum1s > 1f)
			{
				accum1s = 0f;
				targetIntensity = capi.World.Player.Entity.WatchedAttributes.GetFloat("intoxication");
			}
			Intensity += (targetIntensity - Intensity) * dt / 3f;
			accum = (float)((double)capi.InWorldEllapsedMilliseconds / 3000.0 % 100.0 * Math.PI);
			float f = Intensity / 250f;
			float dp = (float)(Math.Cos((double)accum / 1.15) + Math.Cos(accum / 1.35f)) * f / 2f;
			capi.World.Player.Entity.Pos.Pitch += dp;
			capi.Input.MousePitch += dp;
			capi.Input.MouseYaw += (float)(Math.Sin((double)accum / 1.1) + Math.Sin(accum / 1.5f) + Math.Sin(accum / 5f) * 0.20000000298023224) * f;
			if (!capi.Input.MouseGrabbed)
			{
				capi.World.Player.Entity.Pos.Yaw = capi.Input.MouseYaw;
			}
			EntityHeadController headController = capi.World.Player.Entity.AnimManager.HeadController;
			headController.yawOffset = (float)(Math.Cos((double)accum / 1.12) + Math.Cos(accum / 1.2f) + Math.Cos(accum / 4f) * 0.20000000298023224) * f * 60f;
			accum /= 2f;
			headController.pitchOffset = (float)(Math.Sin((double)accum / 1.12) + Math.Sin(accum / 1.2f) + Math.Sin(accum / 4f) * 0.20000000298023224) * f * 30f;
			headController.pitchOffset = (float)(Math.Sin((double)accum / 1.12) + Math.Sin(accum / 1.2f) + Math.Sin(accum / 4f) * 0.20000000298023224) * f * 30f;
			double accum2 = (float)((double)capi.InWorldEllapsedMilliseconds / 9000.0 % 100.0 * Math.PI);
			float intox = capi.Render.ShaderUniforms.PerceptionEffectIntensity;
			capi.Render.ShaderUniforms.AmbientBloomLevelAdd[1] = GameMath.Clamp((float)Math.Abs(Math.Cos(accum2 / 1.12) + Math.Sin(accum2 / 2.2) + Math.Cos(accum2 * 2.3)) * intox * 2f, intox / 3f, 1.8f);
		}
	}

	public override void ApplyToFpHand(Matrixf modelMat)
	{
		float f = Intensity / 10f;
		modelMat.Translate(GameMath.Sin(accum) * f, (double)GameMath.Sin(accum) * 1.2 * (double)f, 0.0);
		modelMat.RotateX(GameMath.Cos(accum * 0.8f) * f);
		modelMat.RotateZ(GameMath.Cos(accum * 1.1f) * f);
	}

	public override void ApplyToTpPlayer(EntityPlayer entityPlr, float[] modelMatrix, float? playerIntensity = null)
	{
		if (entityPlr.Player is IClientPlayer rplr && entityPlr.AnimManager.Animator != null && (rplr.CameraMode != 0 || rplr.ImmersiveFpMode))
		{
			float inten = ((!playerIntensity.HasValue) ? Intensity : playerIntensity.Value);
			ElementPose posebyName = entityPlr.AnimManager.Animator.GetPosebyName("root");
			posebyName.degOffX = GameMath.Sin(accum) / 5f * inten * (180f / (float)Math.PI);
			posebyName.degOffZ = GameMath.Sin(accum * 1.2f) / 5f * inten * (180f / (float)Math.PI);
		}
	}

	public override void NowActive(float intensity)
	{
		base.NowActive(intensity);
		capi.Render.ShaderUniforms.PerceptionEffectId = 2;
	}
}
