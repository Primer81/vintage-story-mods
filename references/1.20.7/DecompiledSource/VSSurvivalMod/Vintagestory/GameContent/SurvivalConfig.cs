using System.Collections.Generic;
using ProtoBuf;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

[ProtoContract]
public class SurvivalConfig
{
	public JsonItemStack[] StartStacks = new JsonItemStack[2]
	{
		new JsonItemStack
		{
			Type = EnumItemClass.Item,
			Code = new AssetLocation("bread-spelt-perfect"),
			StackSize = 8
		},
		new JsonItemStack
		{
			Type = EnumItemClass.Block,
			Code = new AssetLocation("torch-up"),
			StackSize = 1
		}
	};

	[ProtoMember(1)]
	public float[] SunLightLevels = new float[32]
	{
		0.015f, 0.176f, 0.206f, 0.236f, 0.266f, 0.296f, 0.326f, 0.356f, 0.386f, 0.416f,
		0.446f, 0.476f, 0.506f, 0.536f, 0.566f, 0.596f, 0.626f, 0.656f, 0.686f, 0.716f,
		0.746f, 0.776f, 0.806f, 0.836f, 0.866f, 0.896f, 0.926f, 0.956f, 0.986f, 1f,
		1f, 1f
	};

	[ProtoMember(2)]
	public float[] BlockLightLevels = new float[32]
	{
		0.0175f, 0.06f, 0.12f, 0.18f, 0.254f, 0.289f, 0.324f, 0.359f, 0.394f, 0.429f,
		0.464f, 0.499f, 0.534f, 0.569f, 0.604f, 0.639f, 0.674f, 0.709f, 0.744f, 0.779f,
		0.814f, 0.849f, 0.884f, 0.919f, 0.954f, 0.989f, 1f, 1f, 1f, 1f,
		1f, 1f
	};

	[ProtoMember(3)]
	public float PerishSpeedModifier = 1f;

	[ProtoMember(4)]
	public float CreatureDamageModifier = 1f;

	[ProtoMember(5)]
	public float ToolDurabilityModifier = 1f;

	[ProtoMember(6)]
	public float ToolMiningSpeedModifier = 1f;

	[ProtoMember(7)]
	public float HungerSpeedModifier = 1f;

	[ProtoMember(8)]
	public float BaseMoveSpeed = 1.5f;

	[ProtoMember(9)]
	public int SunBrightness = 22;

	[ProtoMember(10)]
	public int PolarEquatorDistance = 50000;

	public ItemStack[] ResolvedStartStacks;

	public void ResolveStartItems(IWorldAccessor world)
	{
		if (StartStacks == null)
		{
			ResolvedStartStacks = new ItemStack[0];
			return;
		}
		List<ItemStack> resolvedStacks = new List<ItemStack>();
		for (int i = 0; i < StartStacks.Length; i++)
		{
			if (StartStacks[i].Resolve(world, "start item stack"))
			{
				resolvedStacks.Add(StartStacks[i].ResolvedItemstack);
			}
		}
		ResolvedStartStacks = resolvedStacks.ToArray();
	}
}
