using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public interface ICustomTreeFellingBehavior
{
	EnumTreeFellingBehavior GetTreeFellingBehavior(BlockPos pos, Vec3i fromDir, int spreadIndex);
}
