using System;
using System.IO;

namespace Vintagestory.API.MathTools;

/// <summary>
/// A more natural random number generator (nature usually doesn't grow by the exact same numbers nor does it completely randomly)
/// </summary>
/// <example>
/// <code language="json">
///             "quantity": {
///             	"dist": "strongerinvexp",
///             	"avg": 6,
///             	"var": 4
///             }
/// </code>
/// <code language="json">
///             "quantity": {
///             	"avg": 4,
/// "var": 2
///             }
/// </code>
/// </example>
[DocumentAsJson]
public class NatFloat
{
	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>0</jsondefault>-->
	/// A full offset to apply to any values returned.
	/// </summary>
	[DocumentAsJson]
	public float offset;

	/// <summary>
	/// <!--<jsonoptional>Recommended</jsonoptional><jsondefault>0</jsondefault>-->
	/// The average value for the random float.
	/// </summary>
	[DocumentAsJson]
	public float avg;

	/// <summary>
	/// <!--<jsonoptional>Recommended</jsonoptional><jsondefault>0</jsondefault>-->
	/// The variation for the random float.
	/// </summary>
	[DocumentAsJson]
	public float var;

	/// <summary>
	/// <!--<jsonoptional>Optional</jsonoptional><jsondefault>UNIFORM</jsondefault>-->
	/// The type of distribution to use that determines the commodity of values.
	/// </summary>
	[DocumentAsJson]
	public EnumDistribution dist;

	[ThreadStatic]
	private static Random threadsafeRand;

	/// <summary>
	/// Always 0
	/// </summary>
	public static NatFloat Zero => new NatFloat(0f, 0f, EnumDistribution.UNIFORM);

	/// <summary>
	/// Always 1
	/// </summary>
	public static NatFloat One => new NatFloat(1f, 0f, EnumDistribution.UNIFORM);

	public NatFloat(float averagevalue, float variance, EnumDistribution distribution)
	{
		avg = averagevalue;
		var = variance;
		dist = distribution;
	}

	public static NatFloat createInvexp(float averagevalue, float variance)
	{
		return new NatFloat(averagevalue, variance, EnumDistribution.INVEXP);
	}

	public static NatFloat createStrongInvexp(float averagevalue, float variance)
	{
		return new NatFloat(averagevalue, variance, EnumDistribution.STRONGINVEXP);
	}

	public static NatFloat createStrongerInvexp(float averagevalue, float variance)
	{
		return new NatFloat(averagevalue, variance, EnumDistribution.STRONGERINVEXP);
	}

	public static NatFloat createUniform(float averagevalue, float variance)
	{
		return new NatFloat(averagevalue, variance, EnumDistribution.UNIFORM);
	}

	public static NatFloat createGauss(float averagevalue, float variance)
	{
		return new NatFloat(averagevalue, variance, EnumDistribution.GAUSSIAN);
	}

	public static NatFloat createNarrowGauss(float averagevalue, float variance)
	{
		return new NatFloat(averagevalue, variance, EnumDistribution.NARROWGAUSSIAN);
	}

	public static NatFloat createInvGauss(float averagevalue, float variance)
	{
		return new NatFloat(averagevalue, variance, EnumDistribution.INVERSEGAUSSIAN);
	}

	public static NatFloat createTri(float averagevalue, float variance)
	{
		return new NatFloat(averagevalue, variance, EnumDistribution.TRIANGLE);
	}

	public static NatFloat createDirac(float averagevalue, float variance)
	{
		return new NatFloat(averagevalue, variance, EnumDistribution.DIRAC);
	}

	public static NatFloat create(EnumDistribution distribution, float averagevalue, float variance)
	{
		return new NatFloat(averagevalue, variance, distribution);
	}

	public NatFloat copyWithOffset(float value)
	{
		NatFloat natFloat = new NatFloat(value, value, dist);
		natFloat.offset += value;
		return natFloat;
	}

	public NatFloat addOffset(float value)
	{
		offset += value;
		return this;
	}

	public NatFloat setOffset(float offset)
	{
		this.offset = offset;
		return this;
	}

	public float nextFloat()
	{
		return nextFloat(1f, threadsafeRand ?? (threadsafeRand = new Random()));
	}

	public float nextFloat(float multiplier)
	{
		return nextFloat(multiplier, threadsafeRand ?? (threadsafeRand = new Random()));
	}

	public float nextFloat(float multiplier, Random rand)
	{
		switch (dist)
		{
		case EnumDistribution.UNIFORM:
		{
			float rnd = (float)rand.NextDouble() - 0.5f;
			return offset + multiplier * (avg + rnd * 2f * var);
		}
		case EnumDistribution.GAUSSIAN:
		{
			float rnd = (float)(rand.NextDouble() + rand.NextDouble() + rand.NextDouble()) / 3f;
			rnd -= 0.5f;
			return offset + multiplier * (avg + rnd * 2f * var);
		}
		case EnumDistribution.NARROWGAUSSIAN:
		{
			float rnd = (float)(rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble()) / 6f;
			rnd -= 0.5f;
			return offset + multiplier * (avg + rnd * 2f * var);
		}
		case EnumDistribution.VERYNARROWGAUSSIAN:
		{
			float rnd = (float)(rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble()) / 12f;
			rnd -= 0.5f;
			return offset + multiplier * (avg + rnd * 2f * var);
		}
		case EnumDistribution.INVEXP:
		{
			float rnd = (float)(rand.NextDouble() * rand.NextDouble());
			return offset + multiplier * (avg + rnd * var);
		}
		case EnumDistribution.STRONGINVEXP:
		{
			float rnd = (float)(rand.NextDouble() * rand.NextDouble() * rand.NextDouble());
			return offset + multiplier * (avg + rnd * var);
		}
		case EnumDistribution.STRONGERINVEXP:
		{
			float rnd = (float)(rand.NextDouble() * rand.NextDouble() * rand.NextDouble() * rand.NextDouble());
			return offset + multiplier * (avg + rnd * var);
		}
		case EnumDistribution.INVERSEGAUSSIAN:
		{
			float rnd = (float)(rand.NextDouble() + rand.NextDouble() + rand.NextDouble()) / 3f;
			rnd = ((!(rnd > 0.5f)) ? (rnd + 0.5f) : (rnd - 0.5f));
			rnd -= 0.5f;
			return offset + multiplier * (avg + 2f * rnd * var);
		}
		case EnumDistribution.NARROWINVERSEGAUSSIAN:
		{
			float rnd = (float)(rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble() + rand.NextDouble()) / 6f;
			rnd = ((!(rnd > 0.5f)) ? (rnd + 0.5f) : (rnd - 0.5f));
			rnd -= 0.5f;
			return offset + multiplier * (avg + 2f * rnd * var);
		}
		case EnumDistribution.DIRAC:
		{
			float rnd = (float)rand.NextDouble() - 0.5f;
			float result = offset + multiplier * (avg + rnd * 2f * var);
			avg = 0f;
			var = 0f;
			return result;
		}
		case EnumDistribution.TRIANGLE:
		{
			float rnd = (float)(rand.NextDouble() + rand.NextDouble()) / 2f;
			rnd -= 0.5f;
			return offset + multiplier * (avg + rnd * 2f * var);
		}
		default:
			return 0f;
		}
	}

	public float nextFloat(float multiplier, IRandom rand)
	{
		switch (dist)
		{
		case EnumDistribution.UNIFORM:
		{
			float rnd = rand.NextFloat() - 0.5f;
			return offset + multiplier * (avg + rnd * 2f * var);
		}
		case EnumDistribution.GAUSSIAN:
		{
			float rnd = (rand.NextFloat() + rand.NextFloat() + rand.NextFloat()) / 3f;
			rnd -= 0.5f;
			return offset + multiplier * (avg + rnd * 2f * var);
		}
		case EnumDistribution.NARROWGAUSSIAN:
		{
			float rnd = (rand.NextFloat() + rand.NextFloat() + rand.NextFloat() + rand.NextFloat() + rand.NextFloat() + rand.NextFloat()) / 6f;
			rnd -= 0.5f;
			return offset + multiplier * (avg + rnd * 2f * var);
		}
		case EnumDistribution.INVEXP:
		{
			float rnd = rand.NextFloat() * rand.NextFloat();
			return offset + multiplier * (avg + rnd * var);
		}
		case EnumDistribution.STRONGINVEXP:
		{
			float rnd = rand.NextFloat() * rand.NextFloat() * rand.NextFloat();
			return offset + multiplier * (avg + rnd * var);
		}
		case EnumDistribution.STRONGERINVEXP:
		{
			float rnd = rand.NextFloat() * rand.NextFloat() * rand.NextFloat() * rand.NextFloat();
			return offset + multiplier * (avg + rnd * var);
		}
		case EnumDistribution.INVERSEGAUSSIAN:
		{
			float rnd = (rand.NextFloat() + rand.NextFloat() + rand.NextFloat()) / 3f;
			rnd = ((!(rnd > 0.5f)) ? (rnd + 0.5f) : (rnd - 0.5f));
			rnd -= 0.5f;
			return offset + multiplier * (avg + 2f * rnd * var);
		}
		case EnumDistribution.NARROWINVERSEGAUSSIAN:
		{
			float rnd = (rand.NextFloat() + rand.NextFloat() + rand.NextFloat() + rand.NextFloat() + rand.NextFloat() + rand.NextFloat()) / 6f;
			rnd = ((!(rnd > 0.5f)) ? (rnd + 0.5f) : (rnd - 0.5f));
			rnd -= 0.5f;
			return offset + multiplier * (avg + 2f * rnd * var);
		}
		case EnumDistribution.DIRAC:
		{
			float rnd = rand.NextFloat() - 0.5f;
			float result = offset + multiplier * (avg + rnd * 2f * var);
			avg = 0f;
			var = 0f;
			return result;
		}
		case EnumDistribution.TRIANGLE:
		{
			float rnd = (rand.NextFloat() + rand.NextFloat()) / 2f;
			rnd -= 0.5f;
			return offset + multiplier * (avg + rnd * 2f * var);
		}
		default:
			return 0f;
		}
	}

	/// <summary>
	/// Clamps supplied value to avg-var and avg+var
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public float ClampToRange(float value)
	{
		EnumDistribution enumDistribution = dist;
		if ((uint)(enumDistribution - 6) <= 2u)
		{
			return Math.Min(value, value + var);
		}
		float min = avg - var;
		float max = avg + var;
		return GameMath.Clamp(value, Math.Min(min, max), Math.Max(min, max));
	}

	public static NatFloat createFromBytes(BinaryReader reader)
	{
		NatFloat zero = Zero;
		zero.FromBytes(reader);
		return zero;
	}

	public NatFloat Clone()
	{
		return (NatFloat)MemberwiseClone();
	}

	public void ToBytes(BinaryWriter writer)
	{
		writer.Write(offset);
		writer.Write(avg);
		writer.Write(var);
		writer.Write((byte)dist);
	}

	public void FromBytes(BinaryReader reader)
	{
		offset = reader.ReadSingle();
		avg = reader.ReadSingle();
		var = reader.ReadSingle();
		dist = (EnumDistribution)reader.ReadByte();
	}
}
