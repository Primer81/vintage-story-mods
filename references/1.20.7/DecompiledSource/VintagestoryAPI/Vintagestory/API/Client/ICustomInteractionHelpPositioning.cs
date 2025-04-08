using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public interface ICustomInteractionHelpPositioning
{
	bool TransparentCenter { get; }

	Vec3d GetInteractionHelpPosition();
}
