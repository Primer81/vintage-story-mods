using Newtonsoft.Json;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

/// <summary>
/// Special class to handle the vertex flagging in a very nicely compressed space.<br />
/// Bit 0-7: Glow level<br />
/// Bit 8-10: Z-Offset<br />
/// Bit 11: Reflective bit<br />
/// Bit 12: Lod 0 Bit<br />
/// Bit 13-24: X/Y/Z Normals<br />
/// Bit 25, 26, 27, 28: Wind mode<br />
/// Bit 29, 30, 31: Wind data  (also sometimes used for other data, e.g. reflection mode if Reflective bit is set, or additional water surface data if this is a water block)<br />
/// </summary>
/// <example>
/// <code language="json">
///             "vertexFlagsByType": {
///             	"metalblock-new-*": {
///             		"reflective": true,
///             		"windDataByType": {
///             			"*-gold": 1,
///             			"*": 1
///             		}
///             	}
///             },
/// </code>
/// </example>
[DocumentAsJson]
[JsonObject(MemberSerialization.OptIn)]
public class VertexFlags
{
	/// <summary>
	/// Bit 0..7
	/// </summary>
	public const int GlowLevelBitMask = 255;

	public const int ZOffsetBitPos = 8;

	/// <summary>
	/// Bit 8..10
	/// </summary>
	public const int ZOffsetBitMask = 1792;

	/// <summary>
	/// Bit 11.   Note if this is set to 1, then WindData has a different meaning, 
	/// </summary>
	public const int ReflectiveBitMask = 2048;

	/// <summary>
	/// Bit 12
	/// </summary>
	public const int Lod0BitMask = 4096;

	public const int NormalBitPos = 13;

	/// <summary>
	/// Bit 13..24
	/// </summary>
	public const int NormalBitMask = 33546240;

	/// <summary>
	/// Bit 25..28
	/// </summary>
	public const int WindModeBitsMask = 503316480;

	public const int WindModeBitsPos = 25;

	/// <summary>
	/// Bit 29..31   Note that WindData is sometimes used for other purposes if WindMode == 0, for example it can hold reflections data, see EnumReflectiveMode.
	/// <br />Also worth noting that WindMode and WindData have totally different meanings for liquid water
	/// </summary>
	public const int WindDataBitsMask = -536870912;

	public const int WindDataBitsPos = 29;

	/// <summary>
	/// Bit 26..31
	/// </summary>
	public const int WindBitsMask = -33554432;

	public const int LiquidIsLavaBitMask = 33554432;

	public const int LiquidWeakFoamBitMask = 67108864;

	public const int LiquidWeakWaveBitMask = 134217728;

	public const int LiquidFullAlphaBitMask = 268435456;

	public const int LiquidExposedToSkyBitMask = 536870912;

	public const int ClearWindBitsMask = 33554431;

	public const int ClearWindModeBitsMask = -503316481;

	public const int ClearWindDataBitsMask = 536870911;

	public const int ClearZOffsetMask = -1793;

	public const int ClearNormalBitMask = -33546241;

	private int all;

	private byte glowLevel;

	private byte zOffset;

	private bool reflective;

	private bool lod0;

	private short normal;

	private EnumWindBitMode windMode;

	private byte windData;

	private const int nValueBitMask = 14;

	private const int nXValueBitMask = 114688;

	private const int nYValueBitMask = 1835008;

	private const int nZValueBitMask = 29360128;

	private const int nXSignBitPos = 12;

	private const int nYSignBitPos = 16;

	private const int nZSignBitPos = 20;

	/// <summary>
	/// Sets all the vertex flags from one integer.
	/// </summary>
	[JsonProperty]
	public int All
	{
		get
		{
			return all;
		}
		set
		{
			glowLevel = (byte)((uint)value & 0xFFu);
			zOffset = (byte)((uint)(value >> 8) & 7u);
			reflective = ((value >> 11) & 1) != 0;
			lod0 = ((value >> 12) & 1) != 0;
			normal = (short)((value >> 13) & 0xFFF);
			windMode = (EnumWindBitMode)((value >> 25) & 0xF);
			windData = (byte)((uint)(value >> 29) & 7u);
			all = value;
		}
	}

	[JsonProperty]
	public byte GlowLevel
	{
		get
		{
			return glowLevel;
		}
		set
		{
			glowLevel = value;
			UpdateAll();
		}
	}

	[JsonProperty]
	public byte ZOffset
	{
		get
		{
			return zOffset;
		}
		set
		{
			zOffset = value;
			UpdateAll();
		}
	}

	[JsonProperty]
	public bool Reflective
	{
		get
		{
			return reflective;
		}
		set
		{
			reflective = value;
			UpdateAll();
		}
	}

	[JsonProperty]
	public bool Lod0
	{
		get
		{
			return lod0;
		}
		set
		{
			lod0 = value;
			UpdateAll();
		}
	}

	[JsonProperty]
	public short Normal
	{
		get
		{
			return normal;
		}
		set
		{
			normal = value;
			UpdateAll();
		}
	}

	[JsonProperty]
	public EnumWindBitMode WindMode
	{
		get
		{
			return windMode;
		}
		set
		{
			windMode = value;
			UpdateAll();
		}
	}

	[JsonProperty]
	public byte WindData
	{
		get
		{
			return windData;
		}
		set
		{
			windData = value;
			UpdateAll();
		}
	}

	/// <summary>
	/// Creates an already bit shifted normal
	/// </summary>
	/// <param name="normal"></param>
	/// <returns></returns>
	public static int PackNormal(Vec3d normal)
	{
		return PackNormal(normal.X, normal.Y, normal.Z);
	}

	/// <summary>
	/// Creates an already bit shifted normal
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <returns></returns>
	public static int PackNormal(double x, double y, double z)
	{
		int xN = (int)(x * 7.000001) * 2;
		int yN = (int)(y * 7.000001) * 2;
		int zN = (int)(z * 7.000001) * 2;
		return (((xN < 0) ? (1 - xN) : xN) << 13) | (((yN < 0) ? (1 - yN) : yN) << 17) | (((zN < 0) ? (1 - zN) : zN) << 21);
	}

	/// <summary>
	/// Creates an already bit shifted normal
	/// </summary>
	/// <param name="normal"></param>
	/// <returns></returns>
	public static int PackNormal(Vec3f normal)
	{
		int xN = (int)(normal.X * 7.000001f) * 2;
		int yN = (int)(normal.Y * 7.000001f) * 2;
		int zN = (int)(normal.Z * 7.000001f) * 2;
		return (((xN < 0) ? (1 - xN) : xN) << 13) | (((yN < 0) ? (1 - yN) : yN) << 17) | (((zN < 0) ? (1 - zN) : zN) << 21);
	}

	/// <summary>
	/// Creates an already bit shifted normal
	/// </summary>
	/// <param name="normal"></param>
	/// <returns></returns>
	public static int PackNormal(Vec3i normal)
	{
		int xN = (int)((float)normal.X * 7.000001f) * 2;
		int yN = (int)((float)normal.Y * 7.000001f) * 2;
		int zN = (int)((float)normal.Z * 7.000001f) * 2;
		return (((xN < 0) ? (1 - xN) : xN) << 13) | (((yN < 0) ? (1 - yN) : yN) << 17) | (((zN < 0) ? (1 - zN) : zN) << 21);
	}

	public static void UnpackNormal(int vertexFlags, float[] intoFloats)
	{
		int x = vertexFlags & 0x1C000;
		int y = vertexFlags & 0x1C0000;
		int z = vertexFlags & 0x1C00000;
		int signx = 1 - ((vertexFlags >> 12) & 2);
		int signy = 1 - ((vertexFlags >> 16) & 2);
		int signz = 1 - ((vertexFlags >> 20) & 2);
		intoFloats[0] = (float)(signx * x) / 114688f;
		intoFloats[1] = (float)(signy * y) / 1835008f;
		intoFloats[2] = (float)(signz * z) / 29360128f;
	}

	public static void UnpackNormal(int vertexFlags, double[] intoDouble)
	{
		int x = vertexFlags & 0x1C000;
		int y = vertexFlags & 0x1C0000;
		int z = vertexFlags & 0x1C00000;
		int signx = 1 - ((vertexFlags >> 12) & 2);
		int signy = 1 - ((vertexFlags >> 16) & 2);
		int signz = 1 - ((vertexFlags >> 20) & 2);
		intoDouble[0] = (float)(signx * x) / 114688f;
		intoDouble[1] = (float)(signy * y) / 1835008f;
		intoDouble[2] = (float)(signz * z) / 29360128f;
	}

	public VertexFlags()
	{
	}

	public VertexFlags(int flags)
	{
		All = flags;
	}

	private void UpdateAll()
	{
		all = glowLevel | ((zOffset & 7) << 8) | (int)((reflective ? 1u : 0u) << 11) | (int)((Lod0 ? 1u : 0u) << 12) | ((normal & 0xFFF) << 13) | ((int)(windMode & (EnumWindBitMode)15) << 25) | ((windData & 7) << 29);
	}

	/// <summary>
	/// Clones this set of vertex flags.  
	/// </summary>
	/// <returns></returns>
	public VertexFlags Clone()
	{
		return new VertexFlags(All);
	}

	public override string ToString()
	{
		return $"Glow: {glowLevel}, ZOffset: {ZOffset}, Reflective: {reflective}, Lod0: {lod0}, Normal: {normal}, WindMode: {WindMode}, WindData: {windData}";
	}

	public static void SetWindMode(ref int flags, int windMode)
	{
		flags |= windMode << 25;
	}

	public static void SetWindData(ref int flags, int windData)
	{
		flags |= windData << 29;
	}

	public static void ReplaceWindData(ref int flags, int windData)
	{
		flags = (flags & 0x1FFFFFFF) | (windData << 29);
	}
}
