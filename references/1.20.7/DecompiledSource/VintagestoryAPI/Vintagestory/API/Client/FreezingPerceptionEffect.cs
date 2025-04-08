using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public class FreezingPerceptionEffect : PerceptionEffect
{
	private float currentStrength;

	private readonly NormalizedSimplexNoise noiseGenerator;

	public FreezingPerceptionEffect(ICoreClientAPI capi)
		: base(capi)
	{
		noiseGenerator = NormalizedSimplexNoise.FromDefaultOctaves(4, 1.0, 0.9, 123L);
	}

	public override void OnBeforeGameRender(float dt)
	{
		if (!capi.IsGamePaused)
		{
			HandleFreezingEffects(Math.Min(dt, 1f));
		}
	}

	private void HandleFreezingEffects(float dt)
	{
		float strength = capi.World.Player.Entity.WatchedAttributes.GetFloat("freezingEffectStrength");
		currentStrength += (strength - currentStrength) * dt;
		ApplyFrostVignette(currentStrength);
		if ((double)currentStrength > 0.1 && capi.World.Player.CameraMode == EnumCameraMode.FirstPerson)
		{
			ApplyMotionEffects(currentStrength);
		}
	}

	private void ApplyFrostVignette(float strength)
	{
		capi.Render.ShaderUniforms.FrostVignetting = strength;
	}

	private void ApplyMotionEffects(float strength)
	{
		float elapsedSeconds = (float)capi.InWorldEllapsedMilliseconds / 1000f;
		capi.Input.MouseYaw += capi.Settings.Float["cameraShakeStrength"] * (float)(Math.Max(0.0, noiseGenerator.Noise(elapsedSeconds, 12.0) - 0.4000000059604645) * Math.Sin(elapsedSeconds * 90f) * 0.01) * GameMath.Clamp(strength * 3f, 0f, 1f);
	}
}
