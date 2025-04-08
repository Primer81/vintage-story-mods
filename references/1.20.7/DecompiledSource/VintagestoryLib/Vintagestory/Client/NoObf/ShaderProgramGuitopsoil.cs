using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ShaderProgramGuitopsoil : ShaderProgram
{
	public int TerrainTex2D
	{
		set
		{
			BindTexture2D("terrainTex", value, 0);
		}
	}

	public float BlockTextureSize
	{
		set
		{
			Uniform("blockTextureSize", value);
		}
	}

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

	public Vec4f RgbaIn
	{
		set
		{
			Uniform("rgbaIn", value);
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

	public int ApplyColor
	{
		set
		{
			Uniform("applyColor", value);
		}
	}
}
