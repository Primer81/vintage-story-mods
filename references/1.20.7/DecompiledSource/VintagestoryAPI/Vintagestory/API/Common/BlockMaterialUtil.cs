using System;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

public static class BlockMaterialUtil
{
	private static double[][] blastResistances;

	private static double[][] blastDropChances;

	static BlockMaterialUtil()
	{
		Array values = Enum.GetValues(typeof(EnumBlastType));
		Array materials = Enum.GetValues(typeof(EnumBlockMaterial));
		blastResistances = new double[values.Length][];
		blastDropChances = new double[values.Length][];
		int blastType = 1;
		blastResistances[blastType] = new double[materials.Length];
		blastDropChances[blastType] = new double[materials.Length];
		blastDropChances[blastType].Fill(0.2);
		blastResistances[blastType][0] = 0.0;
		blastResistances[blastType][1] = 3.2;
		blastResistances[blastType][2] = 3.2;
		blastResistances[blastType][3] = 2.4;
		blastResistances[blastType][4] = 2.0;
		blastResistances[blastType][5] = 0.4;
		blastResistances[blastType][6] = 2.0;
		blastResistances[blastType][7] = 16.0;
		blastResistances[blastType][8] = 4.0;
		blastResistances[blastType][9] = 0.4;
		blastResistances[blastType][10] = 2.0;
		blastResistances[blastType][11] = 8.0;
		blastResistances[blastType][12] = 999999.0;
		blastResistances[blastType][13] = 0.1;
		blastResistances[blastType][14] = 0.1;
		blastResistances[blastType][15] = 0.3;
		blastResistances[blastType][16] = 0.2;
		blastResistances[blastType][17] = 6.0;
		blastResistances[blastType][18] = 4.0;
		blastResistances[blastType][21] = 1.0;
		blastType = 0;
		blastDropChances[blastType] = new double[materials.Length];
		blastDropChances[blastType].Fill(0.25);
		blastDropChances[blastType][7] = 0.9;
		blastResistances[blastType] = new double[materials.Length];
		blastResistances[blastType][0] = 0.0;
		blastResistances[blastType][1] = 1.6;
		blastResistances[blastType][2] = 3.2;
		blastResistances[blastType][3] = 2.4;
		blastResistances[blastType][4] = 2.0;
		blastResistances[blastType][5] = 0.4;
		blastResistances[blastType][6] = 3.0;
		blastResistances[blastType][8] = 4.0;
		blastResistances[blastType][9] = 0.4;
		blastResistances[blastType][10] = 2.0;
		blastResistances[blastType][11] = 8.0;
		blastResistances[blastType][12] = 999999.0;
		blastResistances[blastType][13] = 0.1;
		blastResistances[blastType][14] = 0.1;
		blastResistances[blastType][15] = 0.3;
		blastResistances[blastType][16] = 0.2;
		blastResistances[blastType][17] = 6.0;
		blastResistances[blastType][18] = 4.0;
		blastResistances[blastType][21] = 1.0;
		blastType = 2;
		blastDropChances[blastType] = new double[materials.Length];
		blastDropChances[blastType].Fill(0.5);
		blastResistances[blastType] = new double[materials.Length];
		blastResistances[blastType][0] = 0.0;
		blastResistances[blastType][1] = 38.400000000000006;
		blastResistances[blastType][2] = 38.400000000000006;
		blastResistances[blastType][3] = 28.799999999999997;
		blastResistances[blastType][4] = 24.0;
		blastResistances[blastType][5] = 4.800000000000001;
		blastResistances[blastType][6] = 67.19999999999999;
		blastResistances[blastType][7] = 48.0;
		blastResistances[blastType][8] = 48.0;
		blastResistances[blastType][9] = 4.800000000000001;
		blastResistances[blastType][10] = 24.0;
		blastResistances[blastType][11] = 96.0;
		blastResistances[blastType][12] = 11999988.0;
		blastResistances[blastType][13] = 1.2000000000000002;
		blastResistances[blastType][14] = 1.2000000000000002;
		blastResistances[blastType][15] = 3.5999999999999996;
		blastResistances[blastType][16] = 2.4000000000000004;
		blastResistances[blastType][17] = 72.0;
		blastResistances[blastType][18] = 48.0;
		blastResistances[blastType][21] = 12.0;
	}

	/// <summary>
	/// Calculates the blast resistance of a given material.
	/// </summary>
	/// <param name="blastType">The blast type the material is being it with.</param>
	/// <param name="material">The material of the block.</param>
	/// <returns>the resulting blast resistance.</returns>
	public static double MaterialBlastResistance(EnumBlastType blastType, EnumBlockMaterial material)
	{
		return blastResistances[(int)blastType][(int)material];
	}

	/// <summary>
	/// Calculates the blast drop chance of a given material.
	/// </summary>
	/// <param name="blastType">The blast type the material is being it with.</param>
	/// <param name="material">The material of the block.</param>
	/// <returns>the resulting drop chance.</returns>
	public static double MaterialBlastDropChances(EnumBlastType blastType, EnumBlockMaterial material)
	{
		return blastDropChances[(int)blastType][(int)material];
	}
}
