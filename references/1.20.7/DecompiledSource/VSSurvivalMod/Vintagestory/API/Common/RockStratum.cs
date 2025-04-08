using Vintagestory.ServerMods;

namespace Vintagestory.API.Common;

public class RockStratum
{
	public AssetLocation BlockCode;

	public string Generator;

	public double[] Amplitudes;

	public double[] Frequencies;

	public double[] Thresholds;

	public EnumStratumGenDir GenDir;

	public bool IsDeposit;

	public EnumRockGroup RockGroup;

	public int BlockId;

	public void Init(IWorldAccessor worldForResolve)
	{
		Block block = worldForResolve.GetBlock(BlockCode);
		if (block == null)
		{
			worldForResolve.Logger.Warning("Rock stratum with block code {0} - no such block was loaded. Will generate air instead!", BlockCode);
		}
		else
		{
			BlockId = block.BlockId;
		}
	}
}
