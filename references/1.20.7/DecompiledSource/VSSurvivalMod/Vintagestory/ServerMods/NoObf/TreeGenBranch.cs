using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods.NoObf;

public class TreeGenBranch
{
	public Inheritance inherit;

	public float widthMultiplier = 1f;

	public float widthloss = 0.05f;

	public NatFloat randomWidthLoss;

	public float widthlossCurve = 1f;

	public NatFloat dieAt = NatFloat.createUniform(0.0002f, 0f);

	public float gravityDrag;

	public NatFloat angleVert;

	public NatFloat angleHori = NatFloat.createUniform(0f, (float)Math.PI);

	public float branchWidthLossMul = 1f;

	public EvolvingNatFloat angleVertEvolve = EvolvingNatFloat.createIdentical(0f);

	public EvolvingNatFloat angleHoriEvolve = EvolvingNatFloat.createIdentical(0f);

	public bool NoLogs;

	public NatFloat branchStart = NatFloat.createUniform(0.7f, 0f);

	public NatFloat branchSpacing = NatFloat.createUniform(0.3f, 0f);

	public NatFloat branchVerticalAngle = NatFloat.createUniform(0f, (float)Math.PI);

	public NatFloat branchHorizontalAngle = NatFloat.createUniform(0f, (float)Math.PI);

	public NatFloat branchWidthMultiplier = NatFloat.createUniform(0f, 0f);

	public EvolvingNatFloat branchWidthMultiplierEvolve;

	public NatFloat branchQuantity = NatFloat.createUniform(1f, 0f);

	public EvolvingNatFloat branchQuantityEvolve;

	public void InheritFrom(TreeGenBranch treeGenTrunk, string[] skip)
	{
		FieldInfo[] fields = GetType().GetFields();
		foreach (FieldInfo field in fields)
		{
			if (skip == null || !skip.Contains(field.Name))
			{
				field.SetValue(this, treeGenTrunk.GetType().GetField(field.Name).GetValue(treeGenTrunk));
			}
		}
	}

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		if (angleVert == null)
		{
			angleVert = NatFloat.createUniform(0f, 0f);
		}
	}

	public float WidthLoss(IRandom rand)
	{
		if (randomWidthLoss == null)
		{
			return widthloss;
		}
		return randomWidthLoss.nextFloat(1f, rand);
	}

	public virtual int getBlockId(IRandom rand, float width, TreeGenBlocks blocks, TreeGen gen, int treeSubType)
	{
		if (!(width < 0.3f) && !NoLogs)
		{
			if (!(blocks.otherLogBlockCode != null) || !gen.TriggerRandomOtherBlock(rand))
			{
				return blocks.logBlockId;
			}
			return blocks.otherLogBlockId;
		}
		return blocks.GetLeaves(width, treeSubType);
	}
}
