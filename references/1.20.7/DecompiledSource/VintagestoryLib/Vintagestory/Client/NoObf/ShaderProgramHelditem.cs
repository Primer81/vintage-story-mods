using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ShaderProgramHelditem : ShaderProgram
{
	public int ItemTex2D
	{
		set
		{
			BindTexture2D("itemTex", value, 0);
		}
	}

	public float AlphaTest
	{
		set
		{
			Uniform("alphaTest", value);
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

	public Vec3f LightPosition
	{
		set
		{
			Uniform("lightPosition", value);
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

	public int ExtraGlow
	{
		set
		{
			Uniform("extraGlow", value);
		}
	}

	public float[] ProjectionMatrix
	{
		set
		{
			UniformMatrix("projectionMatrix", value);
		}
	}

	public float[] ModelViewMatrix
	{
		set
		{
			UniformMatrix("modelViewMatrix", value);
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

	public void PointLightsArray(int count, float[] values)
	{
		Uniforms3("pointLights", count, values);
	}

	public void PointLightColorsArray(int count, float[] values)
	{
		Uniforms3("pointLightColors", count, values);
	}
}
