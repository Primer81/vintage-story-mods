using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public interface IMultiBlockActivate
{
	void MBActivate(IWorldAccessor world, Caller caller, BlockSelection blockSel, ITreeAttribute activationArgs, Vec3i offset);
}
