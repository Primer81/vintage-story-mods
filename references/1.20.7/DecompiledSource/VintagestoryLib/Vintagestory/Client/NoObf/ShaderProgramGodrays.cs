using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ShaderProgramGodrays : ShaderProgram
{
	public int InputTexture2D
	{
		set
		{
			BindTexture2D("inputTexture", value, 0);
		}
	}

	public int GlowParts2D
	{
		set
		{
			BindTexture2D("glowParts", value, 1);
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

	public float IGlobalTimeIn
	{
		set
		{
			Uniform("iGlobalTimeIn", value);
		}
	}

	public float DirectionIn
	{
		set
		{
			Uniform("directionIn", value);
		}
	}

	public int Dusk
	{
		set
		{
			Uniform("dusk", value);
		}
	}
}
