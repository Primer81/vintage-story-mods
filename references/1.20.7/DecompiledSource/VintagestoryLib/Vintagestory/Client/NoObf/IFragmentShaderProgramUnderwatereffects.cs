using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public interface IFragmentShaderProgramUnderwatereffects
{
	int LiquidDepth2D { set; }

	float CameraUnderwater { set; }

	Vec2f FrameSize { set; }

	Vec4f WaterMurkColor { set; }
}
