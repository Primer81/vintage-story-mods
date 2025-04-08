using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ShaderProgramColorgrade : ShaderProgram
{
	public int PrimaryScene2D
	{
		set
		{
			BindTexture2D("primaryScene", value, 0);
		}
	}

	public float GammaLevel
	{
		set
		{
			Uniform("gammaLevel", value);
		}
	}

	public float BrightnessLevel
	{
		set
		{
			Uniform("brightnessLevel", value);
		}
	}

	public float SepiaLevel
	{
		set
		{
			Uniform("sepiaLevel", value);
		}
	}

	public float DamageVignetting
	{
		set
		{
			Uniform("damageVignetting", value);
		}
	}

	public float Minlight
	{
		set
		{
			Uniform("minlight", value);
		}
	}

	public float Maxlight
	{
		set
		{
			Uniform("maxlight", value);
		}
	}

	public float Minsat
	{
		set
		{
			Uniform("minsat", value);
		}
	}

	public float Maxsat
	{
		set
		{
			Uniform("maxsat", value);
		}
	}

	public Vec2f InvFrameSizeIn
	{
		set
		{
			Uniform("invFrameSizeIn", value);
		}
	}
}
