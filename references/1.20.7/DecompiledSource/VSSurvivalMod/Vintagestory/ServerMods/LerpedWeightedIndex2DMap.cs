using System;
using System.Collections.Generic;
using System.Linq;

namespace Vintagestory.ServerMods;

public class LerpedWeightedIndex2DMap
{
	public int sizeX;

	public int topleftPadding;

	public int botRightPadding;

	private WeightedIndex[][] groups;

	public WeightedIndex[] this[float x, float z]
	{
		get
		{
			int posXLeft = (int)Math.Floor(x - 0.5f);
			int posXRight = posXLeft + 1;
			int posZLeft = (int)Math.Floor(z - 0.5f);
			int posZRight = posZLeft + 1;
			float fx = x - ((float)posXLeft + 0.5f);
			float fz = z - ((float)posZLeft + 0.5f);
			WeightedIndex[] weightedIndicesTop = Lerp(groups[(posZLeft + topleftPadding) * sizeX + posXLeft + topleftPadding], groups[(posZLeft + topleftPadding) * sizeX + posXRight + topleftPadding], fx);
			WeightedIndex[] weightedIndicesBottom = Lerp(groups[(posZRight + topleftPadding) * sizeX + posXLeft + topleftPadding], groups[(posZRight + topleftPadding) * sizeX + posXRight + topleftPadding], fx);
			return LerpSorted(weightedIndicesTop, weightedIndicesBottom, fz);
		}
	}

	public LerpedWeightedIndex2DMap(int[] discreteValues2d, int sizeX)
	{
		this.sizeX = sizeX;
		groups = new WeightedIndex[discreteValues2d.Length][];
		for (int i = 0; i < discreteValues2d.Length; i++)
		{
			groups[i] = new WeightedIndex[1]
			{
				new WeightedIndex
				{
					Index = discreteValues2d[i],
					Weight = 1f
				}
			};
		}
	}

	public LerpedWeightedIndex2DMap(int[] rawScalarValues, int sizeX, int boxBlurRadius, int dataTopLeftPadding, int dataBotRightPadding)
	{
		this.sizeX = sizeX;
		topleftPadding = dataTopLeftPadding;
		botRightPadding = dataBotRightPadding;
		groups = new WeightedIndex[rawScalarValues.Length][];
		Dictionary<int, float> indices = new Dictionary<int, float>();
		for (int x = 0; x < sizeX; x++)
		{
			for (int z = 0; z < sizeX; z++)
			{
				int minx = Math.Max(0, x - boxBlurRadius);
				int minz = Math.Max(0, z - boxBlurRadius);
				int maxx = Math.Min(sizeX - 1, x + boxBlurRadius);
				int maxz = Math.Min(sizeX - 1, z + boxBlurRadius);
				indices.Clear();
				float weightFrac = 1f / (float)((maxx - minx + 1) * (maxz - minz + 1));
				for (int bx = minx; bx <= maxx; bx++)
				{
					for (int bz = minz; bz <= maxz; bz++)
					{
						int index = rawScalarValues[bz * sizeX + bx];
						if (indices.TryGetValue(index, out var prevValue))
						{
							indices[index] = weightFrac + prevValue;
						}
						else
						{
							indices[index] = weightFrac;
						}
					}
				}
				groups[z * sizeX + x] = new WeightedIndex[indices.Count];
				int i = 0;
				foreach (KeyValuePair<int, float> val in indices)
				{
					groups[z * sizeX + x][i++] = new WeightedIndex
					{
						Index = val.Key,
						Weight = val.Value
					};
				}
			}
		}
	}

	public float[] WeightsAt(float x, float z, float[] output)
	{
		for (int i = 0; i < output.Length; i++)
		{
			output[i] = 0f;
		}
		int posXLeft = (int)Math.Floor(x - 0.5f) + topleftPadding;
		int posXRight = posXLeft + 1;
		int posZLeft = (int)Math.Floor(z - 0.5f) + topleftPadding;
		int posZRight = posZLeft + 1;
		float fx = x - ((float)(posXLeft - topleftPadding) + 0.5f);
		float fz = z - ((float)(posZLeft - topleftPadding) + 0.5f);
		HalfBiLerp(groups[posZLeft * sizeX + posXLeft], groups[posZLeft * sizeX + posXRight], fx, output, 1f - fz);
		HalfBiLerp(groups[posZRight * sizeX + posXLeft], groups[posZRight * sizeX + posXRight], fx, output, fz);
		return output;
	}

	private WeightedIndex[] Lerp(WeightedIndex[] left, WeightedIndex[] right, float lerp)
	{
		Dictionary<int, WeightedIndex> indices = new Dictionary<int, WeightedIndex>();
		for (int j = 0; j < left.Length; j++)
		{
			int index2 = left[j].Index;
			indices.TryGetValue(index2, out var windex2);
			indices[index2] = new WeightedIndex(index2, windex2.Weight + (1f - lerp) * left[j].Weight);
		}
		for (int i = 0; i < right.Length; i++)
		{
			int index = right[i].Index;
			indices.TryGetValue(index, out var windex);
			indices[index] = new WeightedIndex(index, windex.Weight + lerp * right[i].Weight);
		}
		return indices.Values.ToArray();
	}

	private WeightedIndex[] LerpSorted(WeightedIndex[] left, WeightedIndex[] right, float lerp)
	{
		SortedDictionary<int, WeightedIndex> indices = new SortedDictionary<int, WeightedIndex>();
		for (int j = 0; j < left.Length; j++)
		{
			int index2 = left[j].Index;
			indices.TryGetValue(index2, out var windex2);
			indices[index2] = new WeightedIndex
			{
				Index = index2,
				Weight = windex2.Weight + (1f - lerp) * left[j].Weight
			};
		}
		for (int i = 0; i < right.Length; i++)
		{
			int index = right[i].Index;
			indices.TryGetValue(index, out var windex);
			indices[index] = new WeightedIndex
			{
				Index = index,
				Weight = windex.Weight + lerp * right[i].Weight
			};
		}
		return indices.Values.ToArray();
	}

	public void Split(WeightedIndex[] weightedIndices, out int[] indices, out float[] weights)
	{
		indices = new int[weightedIndices.Length];
		weights = new float[weightedIndices.Length];
		for (int i = 0; i < weightedIndices.Length; i++)
		{
			indices[i] = weightedIndices[i].Index;
			weights[i] = weightedIndices[i].Weight;
		}
	}

	private void HalfBiLerp(WeightedIndex[] left, WeightedIndex[] right, float lerp, float[] output, float overallweight)
	{
		for (int j = 0; j < left.Length; j++)
		{
			int index2 = left[j].Index;
			output[index2] += (1f - lerp) * left[j].Weight * overallweight;
		}
		for (int i = 0; i < right.Length; i++)
		{
			int index = right[i].Index;
			output[index] += lerp * right[i].Weight * overallweight;
		}
	}
}
