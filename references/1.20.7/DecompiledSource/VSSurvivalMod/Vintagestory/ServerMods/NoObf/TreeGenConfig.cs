using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods.NoObf;

public class TreeGenConfig
{
	[JsonProperty]
	public int yOffset;

	[JsonProperty]
	public float sizeMultiplier;

	[JsonProperty]
	public NatFloat sizeVar = NatFloat.Zero;

	[JsonProperty]
	public float heightMultiplier;

	[JsonProperty]
	public TreeGenTrunk[] trunks;

	[JsonProperty]
	public TreeGenBranch[] branches;

	[JsonProperty]
	public TreeGenBlocks treeBlocks;

	public EnumTreeType Treetype;

	internal void Init(AssetLocation location, ILogger logger)
	{
		if (trunks == null)
		{
			trunks = new TreeGenTrunk[0];
		}
		if (branches == null)
		{
			branches = new TreeGenBranch[0];
		}
		for (int j = 1; j < trunks.Length; j++)
		{
			if (trunks[j].inherit != null)
			{
				Inheritance inherit2 = trunks[j].inherit;
				if (inherit2.from >= j || inherit2.from < 0)
				{
					logger.Warning(string.Concat("Inheritance value out of bounds in trunk element ", j.ToString(), " in ", location, ". Skipping."));
				}
				else
				{
					trunks[j].InheritFrom(trunks[inherit2.from], inherit2.skip);
				}
			}
		}
		for (int i = 1; i < branches.Length; i++)
		{
			if (branches[i].inherit != null)
			{
				Inheritance inherit = branches[i].inherit;
				if (inherit.from >= i || inherit.from < 0)
				{
					logger.Warning(string.Concat("Inheritance value out of bounds in branch element ", i.ToString(), " in ", location, ". Skipping."));
				}
				else
				{
					branches[i].InheritFrom(branches[inherit.from], inherit.skip);
				}
			}
		}
	}
}
