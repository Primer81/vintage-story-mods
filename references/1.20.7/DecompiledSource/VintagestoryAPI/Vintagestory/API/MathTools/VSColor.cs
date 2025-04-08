using System.Runtime.InteropServices;

namespace Vintagestory.API.MathTools;

[StructLayout(LayoutKind.Explicit)]
public struct VSColor
{
	[FieldOffset(0)]
	public int AsInt;

	[FieldOffset(0)]
	public byte R;

	[FieldOffset(1)]
	public byte G;

	[FieldOffset(2)]
	public byte B;

	[FieldOffset(3)]
	public byte A;

	public float Rn
	{
		get
		{
			return (float)(int)R / 255f;
		}
		set
		{
			R = (byte)GameMath.Clamp(value * 255f, 0f, 255f);
		}
	}

	public float Gn
	{
		get
		{
			return (float)(int)G / 255f;
		}
		set
		{
			G = (byte)GameMath.Clamp(value * 255f, 0f, 255f);
		}
	}

	public float Bn
	{
		get
		{
			return (float)(int)B / 255f;
		}
		set
		{
			B = (byte)GameMath.Clamp(value * 255f, 0f, 255f);
		}
	}

	public float An
	{
		get
		{
			return (float)(int)A / 255f;
		}
		set
		{
			A = (byte)GameMath.Clamp(value * 255f, 0f, 255f);
		}
	}

	public VSColor(int color)
	{
		this = default(VSColor);
		AsInt = color;
	}

	public VSColor(byte r, byte g, byte b, byte a)
	{
		this = default(VSColor);
		AsInt = r | (g << 8) | (b << 16) | (a << 24);
	}
}
