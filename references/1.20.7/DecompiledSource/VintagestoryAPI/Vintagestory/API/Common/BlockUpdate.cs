using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

public class BlockUpdate
{
	public bool ExchangeOnly;

	public BlockPos Pos;

	/// <summary>
	/// Contains either liquid layer of solid layer block
	/// </summary>
	public int OldBlockId;

	/// <summary>
	/// Contains liquid layer of block
	/// </summary>
	public int OldFluidBlockId;

	/// <summary>
	/// If this value is negative, it indicates no change to the block (neither air block nor anything else) because only the fluid is being updated
	/// </summary>
	public int NewSolidBlockId = -1;

	/// <summary>
	/// If this value is negative, it indicates no change to the fluids layer block (neither air block nor anything else) because only the solid block is being updated
	/// </summary>
	public int NewFluidBlockId = -1;

	public ItemStack ByStack;

	public byte[] OldBlockEntityData;

	public byte[] NewBlockEntityData;

	public List<DecorUpdate> Decors;

	public List<DecorUpdate> OldDecors;
}
