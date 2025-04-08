using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public interface IVertexShaderProgramColormap
{
	Vec4f ColorMapRects { set; }

	float SeasonRel { set; }

	float SeaLevel { set; }

	float AtlasHeight { set; }

	float SeasonTemperature { set; }
}
