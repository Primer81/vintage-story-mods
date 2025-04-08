using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class ShaderProgramBilateralblur : ShaderProgram
{
	public int InputTexture2D
	{
		set
		{
			BindTexture2D("inputTexture", value, 0);
		}
	}

	public int DepthTexture2D
	{
		set
		{
			BindTexture2D("depthTexture", value, 1);
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
