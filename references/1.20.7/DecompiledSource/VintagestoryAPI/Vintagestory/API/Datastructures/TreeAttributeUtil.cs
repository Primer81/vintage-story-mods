using Vintagestory.API.MathTools;

namespace Vintagestory.API.Datastructures;

public static class TreeAttributeUtil
{
	public static Vec3i GetVec3i(this ITreeAttribute tree, string code, Vec3i defaultValue = null)
	{
		if (!tree.TryGetAttribute(code + "X", out var attr) || !(attr is IntAttribute codeX))
		{
			return defaultValue;
		}
		return new Vec3i(codeX.value, tree.GetInt(code + "Y"), tree.GetInt(code + "Z"));
	}

	public static BlockPos GetBlockPos(this ITreeAttribute tree, string code, BlockPos defaultValue = null)
	{
		if (!tree.TryGetAttribute(code + "X", out var attr) || !(attr is IntAttribute codeX))
		{
			return defaultValue;
		}
		return new BlockPos(codeX.value, tree.GetInt(code + "Y"), tree.GetInt(code + "Z"));
	}

	public static void SetVec3i(this ITreeAttribute tree, string code, Vec3i value)
	{
		tree.SetInt(code + "X", value.X);
		tree.SetInt(code + "Y", value.Y);
		tree.SetInt(code + "Z", value.Z);
	}

	public static void SetBlockPos(this ITreeAttribute tree, string code, BlockPos value)
	{
		tree.SetInt(code + "X", value.X);
		tree.SetInt(code + "Y", value.Y);
		tree.SetInt(code + "Z", value.Z);
	}

	public static Vec3i[] GetVec3is(this ITreeAttribute tree, string code, Vec3i[] defaultValue = null)
	{
		if (!tree.TryGetAttribute(code + "X", out var attrX) || !(attrX is IntArrayAttribute codeX))
		{
			return defaultValue;
		}
		if (!tree.TryGetAttribute(code + "Y", out var attrY) || !(attrY is IntArrayAttribute codeY))
		{
			return defaultValue;
		}
		if (!tree.TryGetAttribute(code + "Z", out var attrZ) || !(attrZ is IntArrayAttribute codeZ))
		{
			return defaultValue;
		}
		int[] x = codeX.value;
		int[] y = codeY.value;
		int[] z = codeZ.value;
		Vec3i[] values = new Vec3i[x.Length];
		for (int i = 0; i < x.Length; i++)
		{
			values[i] = new Vec3i(x[i], y[i], z[i]);
		}
		return values;
	}

	public static void SetVec3is(this ITreeAttribute tree, string code, Vec3i[] value)
	{
		int[] x = new int[value.Length];
		int[] y = new int[value.Length];
		int[] z = new int[value.Length];
		for (int i = 0; i < value.Length; i++)
		{
			Vec3i v = value[i];
			x[i] = v.X;
			y[i] = v.Y;
			z[i] = v.Z;
		}
		tree[code + "X"] = new IntArrayAttribute(x);
		tree[code + "Y"] = new IntArrayAttribute(y);
		tree[code + "Z"] = new IntArrayAttribute(z);
	}
}
