using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods;

internal class WeightedOctavedNoise : NormalizedSimplexNoise
{
	private double[] offsets;

	public WeightedOctavedNoise(double[] offsets, double[] inputAmplitudes, double[] frequencies, long seed)
		: base(inputAmplitudes, frequencies, seed)
	{
		this.offsets = offsets;
	}

	public override double Noise(double x, double y)
	{
		double value = 1.0;
		for (int i = 0; i < inputAmplitudes.Length; i++)
		{
			double amp = inputAmplitudes[i];
			value += Math.Min(amp, Math.Max(0.0 - amp, octaves[i].Evaluate(x * frequencies[i], y * frequencies[i]) * amp - offsets[i]));
		}
		return value / 2.0;
	}

	public override double Noise(double x, double y, double z)
	{
		double value = 1.0;
		for (int i = 0; i < inputAmplitudes.Length; i++)
		{
			double amp = inputAmplitudes[i];
			value += Math.Min(amp, Math.Max(0.0 - amp, octaves[i].Evaluate(x * frequencies[i], y * frequencies[i], z * frequencies[i]) * amp));
		}
		return value / 2.0;
	}
}
