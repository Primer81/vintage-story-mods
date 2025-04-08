using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ShaderProgramBlur : ShaderProgram
{
	public int InputTexture2D
	{
		set
		{
			BindTexture2D("inputTexture", value, 0);
		}
	}

	public Vec2f FrameSize
	{
		set
		{
			Uniform("frameSize", value);
		}
	}

	public int IsVertical
	{
		set
		{
			Uniform("isVertical", value);
		}
	}
}
