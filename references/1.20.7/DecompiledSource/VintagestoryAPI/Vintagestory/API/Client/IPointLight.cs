using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public interface IPointLight
{
	Vec3f Color { get; }

	Vec3d Pos { get; }
}
