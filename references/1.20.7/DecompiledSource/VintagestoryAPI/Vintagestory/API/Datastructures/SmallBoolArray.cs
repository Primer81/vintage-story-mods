using System;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Datastructures;

/// <summary>
/// Suitable for up to 32 bool values, though normally used only for 6.  Offers most of the methods available for a bool[], so can be dropped in to existing code
/// </summary>
public struct SmallBoolArray : IEquatable<int>
{
	public const int OnAllSides = 63;

	private int bits;

	public bool this[int i]
	{
		get
		{
			return (bits & (1 << i)) != 0;
		}
		set
		{
			if (value)
			{
				bits |= 1 << i;
			}
			else
			{
				bits &= ~(1 << i);
			}
		}
	}

	public bool Any => bits != 0;

	public bool All => bits == 63;

	public bool SidesAndBase => (bits & 0x2F) == 47;

	public bool Horizontals => (bits & 0xF) == 15;

	public bool Verticals => (bits & 0x30) == 48;

	public static implicit operator int(SmallBoolArray a)
	{
		return a.bits;
	}

	public SmallBoolArray(int values)
	{
		bits = values;
	}

	public SmallBoolArray(int[] values)
	{
		bits = 0;
		for (int i = 0; i < values.Length; i++)
		{
			if (values[i] != 0)
			{
				bits |= 1 << i;
			}
		}
	}

	public SmallBoolArray(bool[] values)
	{
		bits = 0;
		for (int i = 0; i < values.Length; i++)
		{
			if (values[i])
			{
				bits |= 1 << i;
			}
		}
	}

	public bool Equals(int other)
	{
		return bits == other;
	}

	public override bool Equals(object o)
	{
		if (o is int other)
		{
			return bits == other;
		}
		if (o is SmallBoolArray ob)
		{
			return bits == ob.bits;
		}
		return false;
	}

	public static bool operator ==(SmallBoolArray left, int right)
	{
		return right == left.bits;
	}

	public static bool operator !=(SmallBoolArray left, int right)
	{
		return right != left.bits;
	}

	public void Fill(bool b)
	{
		bits = (b ? 63 : 0);
	}

	public int[] ToIntArray(int size)
	{
		int[] result = new int[size];
		int b = bits;
		for (int i = 0; i < result.Length; i++)
		{
			result[i] = b & 1;
			b >>= 1;
		}
		return result;
	}

	public bool Opposite(int i)
	{
		return (bits & (1 << (i ^ (2 - i / 4)))) != 0;
	}

	public bool OnSide(BlockFacing face)
	{
		return (bits & (1 << face.Index)) != 0;
	}

	public int Value()
	{
		return bits;
	}

	public override int GetHashCode()
	{
		return 1537853281 + bits.GetHashCode();
	}
}
