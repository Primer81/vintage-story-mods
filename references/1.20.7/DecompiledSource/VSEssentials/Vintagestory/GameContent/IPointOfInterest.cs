using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public interface IPointOfInterest
{
	Vec3d Position { get; }

	string Type { get; }
}
