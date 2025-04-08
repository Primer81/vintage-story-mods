using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ShaderProgramSsao : ShaderProgram
{
	public int GPosition2D
	{
		set
		{
			BindTexture2D("gPosition", value, 0);
		}
	}

	public int GNormal2D
	{
		set
		{
			BindTexture2D("gNormal", value, 1);
		}
	}

	public int TexNoise2D
	{
		set
		{
			BindTexture2D("texNoise", value, 2);
		}
	}

	public Vec2f ScreenSize
	{
		set
		{
			Uniform("screenSize", value);
		}
	}

	public int Revealage2D
	{
		set
		{
			BindTexture2D("revealage", value, 3);
		}
	}

	public float[] Projection
	{
		set
		{
			UniformMatrix("projection", value);
		}
	}

	public void SamplesArray(int count, float[] values)
	{
		Uniforms3("samples", count, values);
	}
}
