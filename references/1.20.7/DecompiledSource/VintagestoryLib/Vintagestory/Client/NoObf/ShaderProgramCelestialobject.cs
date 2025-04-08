using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ShaderProgramCelestialobject : ShaderProgram
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

	public float FogDensityIn
	{
		set
		{
			Uniform("fogDensityIn", value);
		}
	}

	public float FogMinIn
	{
		set
		{
			Uniform("fogMinIn", value);
		}
	}

	public float HorizonFog
	{
		set
		{
			Uniform("horizonFog", value);
		}
	}

	public Vec3f SunPosition
	{
		set
		{
			Uniform("sunPosition", value);
		}
	}

	public int WeirdMathToMakeMoonLookNicer
	{
		set
		{
			Uniform("weirdMathToMakeMoonLookNicer", value);
		}
	}

	public float DayLight
	{
		set
		{
			Uniform("dayLight", value);
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
			BindTexture2D("shadowMapFar", value, 1);
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
			BindTexture2D("shadowMapNear", value, 2);
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

	public float PlayerToSealevelOffset
	{
		set
		{
			Uniform("playerToSealevelOffset", value);
		}
	}

	public int DitherSeed
	{
		set
		{
			Uniform("ditherSeed", value);
		}
	}

	public int HorizontalResolution
	{
		set
		{
			Uniform("horizontalResolution", value);
		}
	}

	public float FogWaveCounter
	{
		set
		{
			Uniform("fogWaveCounter", value);
		}
	}

	public int Glow2D
	{
		set
		{
			BindTexture2D("glow", value, 3);
		}
	}

	public int Sky2D
	{
		set
		{
			BindTexture2D("sky", value, 4);
		}
	}

	public float SunsetMod
	{
		set
		{
			Uniform("sunsetMod", value);
		}
	}

	public Vec4f RgbaFogIn
	{
		set
		{
			Uniform("rgbaFogIn", value);
		}
	}

	public Vec3f RgbaAmbientIn
	{
		set
		{
			Uniform("rgbaAmbientIn", value);
		}
	}

	public float[] ProjectionMatrix
	{
		set
		{
			UniformMatrix("projectionMatrix", value);
		}
	}

	public float[] ModelMatrix
	{
		set
		{
			UniformMatrix("modelMatrix", value);
		}
	}

	public float[] ViewMatrix
	{
		set
		{
			UniformMatrix("viewMatrix", value);
		}
	}

	public int ExtraGlow
	{
		set
		{
			Uniform("extraGlow", value);
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

	public void PointLightsArray(int count, float[] values)
	{
		Uniforms3("pointLights", count, values);
	}

	public void PointLightColorsArray(int count, float[] values)
	{
		Uniforms3("pointLightColors", count, values);
	}
}
