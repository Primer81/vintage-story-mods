using System;
using System.Runtime.CompilerServices;

namespace Vintagestory.API.MathTools;

/// <summary>
/// It's generally pretty hard to get a neatly normalized coherent noise function due to the way perlin/open simplex works (gauss curve) and how random numbers are generated. So instead of trying to find the perfect normalization factor and instead try to perform some approximate normalization this class allows a small overflow and brings it down very close to the [0, 1] range using tanh().
///
/// Returns values in a range of [0..1]
/// </summary>
public class NormalizedSimplexNoise
{
	public struct ColumnNoise
	{
		private struct OctaveEntry
		{
			public SimplexNoiseOctave Octave;

			public double X;

			public double FrequencyY;

			public double Z;

			public double Amplitude;

			public double Threshold;

			public double StopBound;
		}

		private OctaveEntry[] orderedOctaveEntries;

		public double UncurvedBound { get; private set; }

		public double BoundMin { get; private set; }

		public double BoundMax { get; private set; }

		public ColumnNoise(NormalizedSimplexNoise terrainNoise, double relativeYFrequency, double[] amplitudes, double[] thresholds, double noiseX, double noiseZ)
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
			double uncertaintySum = 0.0;
			for (int k = nUsedOctaves - 1; k >= 0; k--)
			{
				int i = order[k];
				uncertaintySum += maxValues[i];
				double thisOctaveFrequency = terrainNoise.frequencies[i];
				orderedOctaveEntries[k] = new OctaveEntry
				{
					Octave = terrainNoise.octaves[i],
					X = noiseX * thisOctaveFrequency,
					Z = noiseZ * thisOctaveFrequency,
					FrequencyY = thisOctaveFrequency * relativeYFrequency,
					Amplitude = amplitudes[i] * 1.2,
					Threshold = thresholds[i] * 1.2,
					StopBound = uncertaintySum
				};
			}
		}

		public double NoiseSign(double y, double inverseCurvedThresholder)
		{
			double value = inverseCurvedThresholder;
			for (int i = 0; i < orderedOctaveEntries.Length; i++)
			{
				ref OctaveEntry octaveEntry = ref orderedOctaveEntries[i];
				if (value >= octaveEntry.StopBound || value <= 0.0 - octaveEntry.StopBound)
				{
					break;
				}
				double noiseValue = octaveEntry.Octave.Evaluate(octaveEntry.X, y * octaveEntry.FrequencyY, octaveEntry.Z) * octaveEntry.Amplitude;
				value += ApplyThresholding(noiseValue, octaveEntry.Threshold);
			}
			return value;
		}

		public double Noise(double y)
		{
			double value = 0.0;
			for (int i = 0; i < orderedOctaveEntries.Length; i++)
			{
				ref OctaveEntry octaveEntry = ref orderedOctaveEntries[i];
				double noiseValue = octaveEntry.Octave.Evaluate(octaveEntry.X, y * octaveEntry.FrequencyY, octaveEntry.Z) * octaveEntry.Amplitude;
				value += ApplyThresholding(noiseValue, octaveEntry.Threshold);
			}
			return NoiseValueCurve(value);
		}
	}

	private const double VALUE_MULTIPLIER = 1.2;

	public double[] scaledAmplitudes2D;

	public double[] scaledAmplitudes3D;

	public double[] inputAmplitudes;

	public double[] frequencies;

	public SimplexNoiseOctave[] octaves;

	public NormalizedSimplexNoise(double[] inputAmplitudes, double[] frequencies, long seed)
	{
		this.frequencies = frequencies;
		this.inputAmplitudes = inputAmplitudes;
		octaves = new SimplexNoiseOctave[inputAmplitudes.Length];
		for (int i = 0; i < octaves.Length; i++)
		{
			octaves[i] = new SimplexNoiseOctave(seed * 65599 + i);
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
	public static NormalizedSimplexNoise FromDefaultOctaves(int quantityOctaves, double baseFrequency, double persistence, long seed)
	{
		double[] frequencies = new double[quantityOctaves];
		double[] amplitudes = new double[quantityOctaves];
		for (int i = 0; i < quantityOctaves; i++)
		{
			frequencies[i] = Math.Pow(2.0, i) * baseFrequency;
			amplitudes[i] = Math.Pow(persistence, i);
		}
		return new NormalizedSimplexNoise(amplitudes, frequencies, seed);
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

	/// <summary>
	/// 2d noise
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <returns></returns>
	public virtual double Noise(double x, double y)
	{
		double value = 0.0;
		for (int i = 0; i < scaledAmplitudes2D.Length; i++)
		{
			value += 1.2 * octaves[i].Evaluate(x * frequencies[i], y * frequencies[i]) * scaledAmplitudes2D[i];
		}
		return NoiseValueCurve(value);
	}

	public double Noise(double x, double y, double[] thresholds)
	{
		double value = 0.0;
		for (int i = 0; i < scaledAmplitudes2D.Length; i++)
		{
			double val = octaves[i].Evaluate(x * frequencies[i], y * frequencies[i]) * scaledAmplitudes2D[i];
			value += 1.2 * ((val > 0.0) ? Math.Max(0.0, val - thresholds[i]) : Math.Min(0.0, val + thresholds[i]));
		}
		return NoiseValueCurve(value);
	}

	/// <summary>
	/// 3d noise
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <returns></returns>
	public virtual double Noise(double x, double y, double z)
	{
		double value = 0.0;
		for (int i = 0; i < scaledAmplitudes3D.Length; i++)
		{
			value += 1.2 * octaves[i].Evaluate(x * frequencies[i], y * frequencies[i], z * frequencies[i]) * scaledAmplitudes3D[i];
		}
		return NoiseValueCurve(value);
	}

	/// <summary>
	/// 3d Noise using custom amplitudes
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <param name="amplitudes"></param>
	/// <returns></returns>
	public virtual double Noise(double x, double y, double z, double[] amplitudes)
	{
		double value = 0.0;
		for (int i = 0; i < scaledAmplitudes3D.Length; i++)
		{
			value += 1.2 * octaves[i].Evaluate(x * frequencies[i], y * frequencies[i], z * frequencies[i]) * amplitudes[i];
		}
		return NoiseValueCurve(value);
	}

	public double Noise(double x, double y, double z, double[] amplitudes, double[] thresholds)
	{
		double value = 0.0;
		for (int i = 0; i < scaledAmplitudes3D.Length; i++)
		{
			double freq = frequencies[i];
			double val = octaves[i].Evaluate(x * freq, y * freq, z * freq) * amplitudes[i];
			value += 1.2 * ApplyThresholding(val, thresholds[i]);
		}
		return NoiseValueCurve(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double NoiseValueCurve(double value)
	{
		return Math.Tanh(value) * 0.5 + 0.5;
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
		return 0.5 * Math.Log(value / (1.0 - value));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static double ApplyThresholding(double value, double threshold)
	{
		if (!(value > 0.0))
		{
			return Math.Min(0.0, value + threshold);
		}
		return Math.Max(0.0, value - threshold);
	}

	public ColumnNoise ForColumn(double relativeYFrequency, double[] amplitudes, double[] thresholds, double noiseX, double noiseZ)
	{
		return new ColumnNoise(this, relativeYFrequency, amplitudes, thresholds, noiseX, noiseZ);
	}
}
