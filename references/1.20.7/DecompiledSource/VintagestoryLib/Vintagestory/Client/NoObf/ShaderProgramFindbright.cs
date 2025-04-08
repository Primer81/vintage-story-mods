namespace Vintagestory.Client.NoObf;

public class ShaderProgramFindbright : ShaderProgram
{
	public int ColorTex2D
	{
		set
		{
			BindTexture2D("colorTex", value, 0);
		}
	}

	public int GlowTex2D
	{
		set
		{
			BindTexture2D("glowTex", value, 1);
		}
	}

	public float ExtraBloom
	{
		set
		{
			Uniform("extraBloom", value);
		}
	}

	public float AmbientBloomLevel
	{
		set
		{
			Uniform("ambientBloomLevel", value);
		}
	}
}
