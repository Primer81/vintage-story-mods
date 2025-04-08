using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ShaderProgramStandard : ShaderProgram, IStandardShaderProgram, IShaderProgram, IDisposable
{
	public int Tex2D
	{
		set
		{
			BindTexture2D("tex", value, 0);
		}
	}

	public float ExtraGodray
	{
		set
		{
			Uniform("extraGodray", value);
		}
	}

	public float AlphaTest
	{
		set
		{
			Uniform("alphaTest", value);
		}
	}

	public float SsaoAttn
	{
		set
		{
			Uniform("ssaoAttn", value);
		}
	}

	public int ApplySsao
	{
		set
		{
			Uniform("applySsao", value);
		}
	}

	public int TempGlowMode
	{
		set
		{
			Uniform("tempGlowMode", value);
		}
	}

	public int Tex2dOverlay2D
	{
		set
		{
			BindTexture2D("tex2dOverlay", value, 1);
		}
	}

	public float OverlayOpacity
	{
		set
		{
			Uniform("overlayOpacity", value);
		}
	}

	public Vec2f OverlayTextureSize
	{
		set
		{
			Uniform("overlayTextureSize", value);
		}
	}

	public Vec2f BaseTextureSize
	{
		set
		{
			Uniform("baseTextureSize", value);
		}
	}

	public Vec2f BaseUvOrigin
	{
		set
		{
			Uniform("baseUvOrigin", value);
		}
	}

	public int NormalShaded
	{
		set
		{
			Uniform("normalShaded", value);
		}
	}

	public float DamageEffect
	{
		set
		{
			Uniform("damageEffect", value);
		}
	}

	public float DepthOffset
	{
		set
		{
			Uniform("depthOffset", value);
		}
	}

	public Vec4f AverageColor
	{
		set
		{
			Uniform("averageColor", value);
		}
	}

	public float FlatFogDensity
	{
		set
		{
			Uniform("flatFogDensity", value);
		}
	}

	public float FlatFogStart
	{
		set
		{
			Uniform("flatFogStart", value);
		}
	}

	public float ViewDistance
	{
		set
		{
			Uniform("viewDistance", value);
		}
	}

	public float ViewDistanceLod0
	{
		set
		{
			Uniform("viewDistanceLod0", value);
		}
	}

	public float ZNear
	{
		set
		{
			Uniform("zNear", value);
		}
	}

	public float ZFar
	{
		set
		{
			Uniform("zFar", value);
		}
	}

	public Vec3f LightPosition
	{
		set
		{
			Uniform("lightPosition", value);
		}
	}

	public float ShadowIntensity
	{
		set
		{
			Uniform("shadowIntensity", value);
		}
	}

	public int ShadowMapFar2D
	{
		set
		{
			BindTexture2D("shadowMapFar", value, 2);
		}
	}

	public float ShadowMapWidthInv
	{
		set
		{
			Uniform("shadowMapWidthInv", value);
		}
	}

	public float ShadowMapHeightInv
	{
		set
		{
			Uniform("shadowMapHeightInv", value);
		}
	}

	public int ShadowMapNear2D
	{
		set
		{
			BindTexture2D("shadowMapNear", value, 3);
		}
	}

	public float WindWaveCounter
	{
		set
		{
			Uniform("windWaveCounter", value);
		}
	}

	public float GlitchStrength
	{
		set
		{
			Uniform("glitchStrength", value);
		}
	}

	public float[] FogSpheres
	{
		set
		{
			Uniform("fogSpheres", value.Length, value);
		}
	}

	public int FogSphereQuantity
	{
		set
		{
			Uniform("fogSphereQuantity", value);
		}
	}

	public int LiquidDepth2D
	{
		set
		{
			BindTexture2D("liquidDepth", value, 4);
		}
	}

	public float CameraUnderwater
	{
		set
		{
			Uniform("cameraUnderwater", value);
		}
	}

	public Vec2f FrameSize
	{
		set
		{
			Uniform("frameSize", value);
		}
	}

	public Vec4f WaterMurkColor
	{
		set
		{
			Uniform("waterMurkColor", value);
		}
	}

	public Vec4f RgbaTint
	{
		set
		{
			Uniform("rgbaTint", value);
		}
	}

	public Vec3f RgbaAmbientIn
	{
		set
		{
			Uniform("rgbaAmbientIn", value);
		}
	}

	public Vec4f RgbaLightIn
	{
		set
		{
			Uniform("rgbaLightIn", value);
		}
	}

	public Vec4f RgbaGlowIn
	{
		set
		{
			Uniform("rgbaGlowIn", value);
		}
	}

	public Vec4f RgbaFogIn
	{
		set
		{
			Uniform("rgbaFogIn", value);
		}
	}

	public int ExtraGlow
	{
		set
		{
			Uniform("extraGlow", value);
		}
	}

	public float FogMinIn
	{
		set
		{
			Uniform("fogMinIn", value);
		}
	}

	public float FogDensityIn
	{
		set
		{
			Uniform("fogDensityIn", value);
		}
	}

	public float[] ProjectionMatrix
	{
		set
		{
			UniformMatrix("projectionMatrix", value);
		}
	}

	public float[] ViewMatrix
	{
		set
		{
			UniformMatrix("viewMatrix", value);
		}
	}

	public float[] ModelMatrix
	{
		set
		{
			UniformMatrix("modelMatrix", value);
		}
	}

	public int DontWarpVertices
	{
		set
		{
			Uniform("dontWarpVertices", value);
		}
	}

	public int FadeFromSpheresFog
	{
		set
		{
			Uniform("fadeFromSpheresFog", value);
		}
	}

	public int AddRenderFlags
	{
		set
		{
			Uniform("addRenderFlags", value);
		}
	}

	public float ExtraZOffset
	{
		set
		{
			Uniform("extraZOffset", value);
		}
	}

	public float ShadowRangeFar
	{
		set
		{
			Uniform("shadowRangeFar", value);
		}
	}

	public float[] ToShadowMapSpaceMatrixFar
	{
		set
		{
			UniformMatrix("toShadowMapSpaceMatrixFar", value);
		}
	}

	public float ShadowRangeNear
	{
		set
		{
			Uniform("shadowRangeNear", value);
		}
	}

	public float[] ToShadowMapSpaceMatrixNear
	{
		set
		{
			UniformMatrix("toShadowMapSpaceMatrixNear", value);
		}
	}

	public float GlitchStrengthFL
	{
		set
		{
			Uniform("glitchStrengthFL", value);
		}
	}

	public float NightVisionStrength
	{
		set
		{
			Uniform("nightVisionStrength", value);
		}
	}

	public int PointLightQuantity
	{
		set
		{
			Uniform("pointLightQuantity", value);
		}
	}

	public float TimeCounter
	{
		set
		{
			Uniform("timeCounter", value);
		}
	}

	public float WindWaveCounterHighFreq
	{
		set
		{
			Uniform("windWaveCounterHighFreq", value);
		}
	}

	public float WaterWaveCounter
	{
		set
		{
			Uniform("waterWaveCounter", value);
		}
	}

	public float WindSpeed
	{
		set
		{
			Uniform("windSpeed", value);
		}
	}

	public Vec3f Playerpos
	{
		set
		{
			Uniform("playerpos", value);
		}
	}

	public float GlobalWarpIntensity
	{
		set
		{
			Uniform("globalWarpIntensity", value);
		}
	}

	public float GlitchWaviness
	{
		set
		{
			Uniform("glitchWaviness", value);
		}
	}

	public float WindWaveIntensity
	{
		set
		{
			Uniform("windWaveIntensity", value);
		}
	}

	public float WaterWaveIntensity
	{
		set
		{
			Uniform("waterWaveIntensity", value);
		}
	}

	public int PerceptionEffectId
	{
		set
		{
			Uniform("perceptionEffectId", value);
		}
	}

	public float PerceptionEffectIntensity
	{
		set
		{
			Uniform("perceptionEffectIntensity", value);
		}
	}

	public void PointLightsArray(int count, float[] values)
	{
		Uniforms3("pointLights", count, values);
	}

	public void PointLightColorsArray(int count, float[] values)
	{
		Uniforms3("pointLightColors", count, values);
	}
}
