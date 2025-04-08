using System;
using System.Runtime.CompilerServices;

namespace Vintagestory.API.MathTools;

public class NewNormalizedSimplexFractalNoise
{
	public struct ColumnNoise
	{
		private struct OctaveEntry
		{
			public long Seed;

			public double X;

			public double FrequencyY;

			public double Z;

			public double Amplitude;

			public double Threshold;

			public double SmoothingFactor;

			public double StopBound;
		}

		private struct PastEvaluation
		{
			public double Value;

			public double Y;
		}

		private OctaveEntry[] orderedOctaveEntries;

		private PastEvaluation[] pastEvaluations;

		public double UncurvedBound { get; private set; }

		public double BoundMin { get; private set; }

		public double BoundMax { get; private set; }

		public ColumnNoise(NewNormalizedSimplexFractalNoise terrainNoise, double relativeYFrequency, double[] amplitudes, double[] thresholds, double noiseX, double noiseZ)
		{
			int num = terrainNoise.frequencies.Length;
			int nUsedOctaves = 0;
			double[] maxValues = new double[num];
			int[] order = new int[num];
			double bound = 0.0;
			for (int j = num - 1; j >= 0; j--)
			{
				maxValues[j] = Math.Max(0.0, Math.Abs(amplitudes[j]) - thresholds[j]) * 1.1845758506756423;
				bound += maxValues[j];
				if (maxValues[j] != 0.0)
				{
					order[nUsedOctaves] = j;
					for (int l = nUsedOctaves - 1; l >= 0; l--)
					{
						if (maxValues[order[l + 1]] > maxValues[order[l]])
						{
							int temp = order[l];
							order[l] = order[l + 1];
							order[l + 1] = temp;
						}
					}
					nUsedOctaves++;
				}
			}
			UncurvedBound = bound;
			BoundMin = NoiseValueCurve(0.0 - bound);
			BoundMax = NoiseValueCurve(bound);
			orderedOctaveEntries = new OctaveEntry[nUsedOctaves];
			pastEvaluations = new PastEvaluation[nUsedOctaves];
			double uncertaintySum = 0.0;
			for (int k = nUsedOctaves - 1; k >= 0; k--)
			{
				int i = order[k];
				uncertaintySum += maxValues[i];
				double thisOctaveFrequency = terrainNoise.frequencies[i];
				orderedOctaveEntries[k] = new OctaveEntry
				{
					Seed = terrainNoise.octaveSeeds[i],
					X = noiseX * thisOctaveFrequency,
					Z = noiseZ * thisOctaveFrequency,
					FrequencyY = thisOctaveFrequency * relativeYFrequency,
					Amplitude = amplitudes[i] * 1.1845758506756423,
					Threshold = thresholds[i] * 1.2000000000000002,
					SmoothingFactor = amplitudes[i] * thisOctaveFrequency * 3.5,
					StopBound = uncertaintySum
				};
				pastEvaluations[k] = new PastEvaluation
				{
					Y = double.NaN
				};
			}
		}

		public double NoiseSign(double y, double inverseCurvedThresholder)
		{
			double value = inverseCurvedThresholder;
			double valueTempMin = inverseCurvedThresholder;
			double valueTempMax = inverseCurvedThresholder;
			for (int j = 0; j < orderedOctaveEntries.Length; j++)
			{
				if (!(valueTempMax <= 0.0) && !(valueTempMin >= 0.0))
				{
					break;
				}
				ref OctaveEntry octaveEntry2 = ref orderedOctaveEntries[j];
				if (valueTempMin >= octaveEntry2.StopBound)
				{
					return valueTempMin;
				}
				if (valueTempMax <= 0.0 - octaveEntry2.StopBound)
				{
					return valueTempMax;
				}
				double evalY2 = y * octaveEntry2.FrequencyY;
				double deltaY = Math.Abs(pastEvaluations[j].Y - evalY2);
				valueTempMin += ApplyThresholding(Math.Max(-1.0, pastEvaluations[j].Value - deltaY * 5.0) * octaveEntry2.Amplitude, octaveEntry2.Threshold, octaveEntry2.SmoothingFactor);
				valueTempMax += ApplyThresholding(Math.Min(1.0, pastEvaluations[j].Value + deltaY * 5.0) * octaveEntry2.Amplitude, octaveEntry2.Threshold, octaveEntry2.SmoothingFactor);
			}
			for (int i = 0; i < orderedOctaveEntries.Length; i++)
			{
				ref OctaveEntry octaveEntry = ref orderedOctaveEntries[i];
				if (value >= octaveEntry.StopBound || value <= 0.0 - octaveEntry.StopBound)
				{
					break;
				}
				double evalY = y * octaveEntry.FrequencyY;
				double noiseValue = NewSimplexNoiseLayer.Evaluate_ImprovedXZ(octaveEntry.Seed, octaveEntry.X, evalY, octaveEntry.Z);
				pastEvaluations[i].Value = noiseValue;
				pastEvaluations[i].Y = evalY;
				value += ApplyThresholding(noiseValue * octaveEntry.Amplitude, octaveEntry.Threshold, octaveEntry.SmoothingFactor);
			}
			return value;
		}

		public double Noise(double y)
		{
			double value = 0.0;
			for (int i = 0; i < orderedOctaveEntries.Length; i++)
			{
				ref OctaveEntry octaveEntry = ref orderedOctaveEntries[i];
				double noiseValue = (double)NewSimplexNoiseLayer.Evaluate_ImprovedXZ(octaveEntry.Seed, octaveEntry.X, y * octaveEntry.FrequencyY, octaveEntry.Z) * octaveEntry.Amplitude;
				value += ApplyThresholding(noiseValue, octaveEntry.Threshold, octaveEntry.SmoothingFactor);
			}
			return NoiseValueCurve(value);
		}
	}

	private const double ValueMultiplier = 1.1845758506756423;

	private const double ThresholdRescaleOldToNew = 1.0130208203346036;

	private const double AmpAndFreqToThresholdSmoothing = 3.5;

	public double[] scaledAmplitudes2D;

	public double[] scaledAmplitudes3D;

	public double[] inputAmplitudes;

	public double[] frequencies;

	public long[] octaveSeeds;

	public NewNormalizedSimplexFractalNoise(double[] inputAmplitudes, double[] frequencies, long seed)
	{
		this.frequencies = frequencies;
		this.inputAmplitudes = inputAmplitudes;
		octaveSeeds = new long[inputAmplitudes.Length];
		for (int i = 0; i < octaveSeeds.Length; i++)
		{
			octaveSeeds[i] = seed * 65599 + i;
		}
		CalculateAmplitudes(inputAmplitudes);
	}

	/// <summary>
	/// Generates the octaves and frequencies using following formulas 
	/// freq[i] = baseFrequency * 2^i
	/// amp[i] = persistence^i
	/// </summary>
	/// <param name="quantityOctaves"></param>
	/// <param name="baseFrequency"></param>
	/// <param name="persistence"></param>
	/// <param name="seed"></param>
	/// <returns></returns>
	public static NewNormalizedSimplexFractalNoise FromDefaultOctaves(int quantityOctaves, double baseFrequency, double persistence, long seed)
	{
		double[] frequencies = new double[quantityOctaves];
		double[] amplitudes = new double[quantityOctaves];
		for (int i = 0; i < quantityOctaves; i++)
		{
			frequencies[i] = Math.Pow(2.0, i) * baseFrequency;
			amplitudes[i] = Math.Pow(persistence, i);
		}
		return new NewNormalizedSimplexFractalNoise(amplitudes, frequencies, seed);
	}

	internal virtual void CalculateAmplitudes(double[] inputAmplitudes)
	{
		double normalizationValue3D = 0.0;
		double normalizationValue2D = 0.0;
		for (int k = 0; k < inputAmplitudes.Length; k++)
		{
			normalizationValue3D += inputAmplitudes[k] * Math.Pow(0.64, k + 1);
			normalizationValue2D += inputAmplitudes[k] * Math.Pow(0.73, k + 1);
		}
		scaledAmplitudes2D = new double[inputAmplitudes.Length];
		for (int j = 0; j < inputAmplitudes.Length; j++)
		{
			scaledAmplitudes2D[j] = inputAmplitudes[j] / normalizationValue2D;
		}
		scaledAmplitudes3D = new double[inputAmplitudes.Length];
		for (int i = 0; i < inputAmplitudes.Length; i++)
		{
			scaledAmplitudes3D[i] = inputAmplitudes[i] / normalizationValue3D;
		}
	}

	public double Noise(double x, double y, double z, double[] amplitudes, double[] thresholds)
	{
		double value = 0.0;
		for (int i = 0; i < scaledAmplitudes3D.Length; i++)
		{
			double freq = frequencies[i];
			double val = (double)NewSimplexNoiseLayer.Evaluate_ImprovedXZ(octaveSeeds[i], x * freq, y * freq, z * freq) * amplitudes[i];
			double smoothingFactor = amplitudes[i] * freq * 3.5;
			value += 1.1845758506756423 * ApplyThresholding(val, thresholds[i] * 1.0130208203346036, smoothingFactor);
		}
		return NoiseValueCurve(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double NoiseValueCurve(double value)
	{
		return value / Math.Sqrt(1.0 + value * value) * 0.5 + 0.5;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double NoiseValueCurveInverse(double value)
	{
		if (value <= 0.0)
		{
			return double.NegativeInfinity;
		}
		if (value >= 1.0)
		{
			return double.PositiveInfinity;
		}
		value = value * 2.0 - 1.0;
		return value / Math.Sqrt(1.0 - value * value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static double ApplyThresholding(double value, double threshold, double smoothingFactor)
	{
		return GameMath.SmoothMax(0.0, value - threshold, smoothingFactor) + GameMath.SmoothMin(0.0, value + threshold, smoothingFactor);
	}

	public ColumnNoise ForColumn(double relativeYFrequency, double[] amplitudes, double[] thresholds, double noiseX, double noiseZ)
	{
		return new ColumnNoise(this, relativeYFrequency, amplitudes, thresholds, noiseX, noiseZ);
	}
}
