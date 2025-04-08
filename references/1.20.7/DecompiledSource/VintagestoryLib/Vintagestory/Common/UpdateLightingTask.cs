using Vintagestory.API.MathTools;

namespace Vintagestory.Common;

public class UpdateLightingTask
{
	public BlockPos pos;

	public int oldBlockId;

	public int newBlockId;

	public byte oldAbsorb;

	public byte newAbsorb;

	public bool absorbUpdate;

	public byte[] removeLightHsv;
}
