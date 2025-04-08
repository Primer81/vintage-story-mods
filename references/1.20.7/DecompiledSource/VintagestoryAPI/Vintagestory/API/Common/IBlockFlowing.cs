using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public interface IBlockFlowing
{
	string Flow { get; set; }

	Vec3i FlowNormali { get; set; }

	bool IsLava { get; }
}
