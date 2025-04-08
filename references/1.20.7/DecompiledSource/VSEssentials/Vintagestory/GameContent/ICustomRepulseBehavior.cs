using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public interface ICustomRepulseBehavior
{
	bool Repulse(Entity entity, Vec3d pushVector);
}
