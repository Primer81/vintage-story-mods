using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ShaderProgramGui : ShaderProgram
{
	public float NoTexture
	{
		set
		{
			Uniform("noTexture", value);
		}
	}

	public float AlphaTest
	{
		set
		{
			Uniform("alphaTest", value);
		}
	}

	public int DarkEdges
	{
		set
		{
			Uniform("darkEdges", value);
		}
	}

	public int TempGlowMode
	{
		set
		{
			Uniform("tempGlowMode", value);
		}
	}

	public int TransparentCenter
	{
		set
		{
			Uniform("transparentCenter", value);
		}
	}

	public int Tex2d2D
	{
		set
		{
			BindTexture2D("tex2d", value, 0);
		}
	}

	public int NormalShaded
	{
		set
		{
			Uniform("normalShaded", value);
		}
	}

	public float SepiaLevel
	{
		set
		{
			Uniform("sepiaLevel", value);
		}
	}

	public float DamageEffect
	{
		set
		{
			Uniform("damageEffect", value);
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

	public Vec3f LightPosition
	{
		set
		{
			Uniform("lightPosition", value);
		}
	}

	public Vec4f RgbaIn
	{
		set
		{
			Uniform("rgbaIn", value);
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

	public float[] ModelMatrix
	{
		set
		{
			UniformMatrix("modelMatrix", value);
		}
	}

	public int ApplyModelMat
	{
		set
		{
			Uniform("applyModelMat", value);
		}
	}

	public int ApplyColor
	{
		set
		{
			Uniform("applyColor", value);
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
