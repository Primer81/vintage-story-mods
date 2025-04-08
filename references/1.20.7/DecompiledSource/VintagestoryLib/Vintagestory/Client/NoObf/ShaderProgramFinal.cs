using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ShaderProgramFinal : ShaderProgram
{
	public int PrimaryScene2D
	{
		set
		{
			BindTexture2D("primaryScene", value, 0);
		}
	}

	public int GlowParts2D
	{
		set
		{
			BindTexture2D("glowParts", value, 1);
		}
	}

	public int BloomParts2D
	{
		set
		{
			BindTexture2D("bloomParts", value, 2);
		}
	}

	public int GodrayParts2D
	{
		set
		{
			BindTexture2D("godrayParts", value, 3);
		}
	}

	public int SsaoScene2D
	{
		set
		{
			BindTexture2D("ssaoScene", value, 4);
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

	public float ContrastLevel
	{
		set
		{
			Uniform("contrastLevel", value);
		}
	}

	public float SepiaLevel
	{
		set
		{
			Uniform("sepiaLevel", value);
		}
	}

	public float AmbientBloomLevel
	{
		set
		{
			Uniform("ambientBloomLevel", value);
		}
	}

	public float DamageVignetting
	{
		set
		{
			Uniform("damageVignetting", value);
		}
	}

	public float DamageVignettingSide
	{
		set
		{
			Uniform("damageVignettingSide", value);
		}
	}

	public float FrostVignetting
	{
		set
		{
			Uniform("frostVignetting", value);
		}
	}

	public float ExtraGamma
	{
		set
		{
			Uniform("extraGamma", value);
		}
	}

	public float WindWaveCounter
	{
		set
		{
			Uniform("windWaveCounter", value);
		}
	}

	public float GlitchEffectStrength
	{
		set
		{
			Uniform("glitchEffectStrength", value);
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

	public Vec3f SunPosScreenIn
	{
		set
		{
			Uniform("sunPosScreenIn", value);
		}
	}

	public Vec3f SunPos3dIn
	{
		set
		{
			Uniform("sunPos3dIn", value);
		}
	}

	public Vec3f PlayerViewVector
	{
		set
		{
			Uniform("playerViewVector", value);
		}
	}
}
