using System;
using System.IO;
using System.Text;

namespace Vintagestory.API.MathTools;

/// <summary>
/// Converts between binary data and an Ascii85-encoded string.
/// </summary>
/// <remarks>See <a href="http://en.wikipedia.org/wiki/Ascii85">Ascii85 at Wikipedia</a>.</remarks>
public static class Ascii85
{
	private const char c_firstCharacter = '!';

	private const char c_lastCharacter = 'u';

	private static readonly uint[] s_powersOf85 = new uint[5] { 52200625u, 614125u, 7225u, 85u, 1u };

	/// <summary>
	/// Encodes the specified byte array in Ascii85.
	/// </summary>
	/// <param name="bytes">The bytes to encode.</param>
	/// <returns>An Ascii85-encoded string representing the input byte array.</returns>
	public static string Encode(byte[] bytes)
	{
		if (bytes == null)
		{
			throw new ArgumentNullException("bytes");
		}
		StringBuilder sb = new StringBuilder(bytes.Length * 5 / 4);
		int count = 0;
		uint value = 0u;
		foreach (byte b in bytes)
		{
			value |= (uint)(b << 24 - count * 8);
			count++;
			if (count == 4)
			{
				if (value == 0)
				{
					sb.Append('z');
				}
				else
				{
					EncodeValue(sb, value, 0);
				}
				count = 0;
				value = 0u;
			}
		}
		if (count > 0)
		{
			EncodeValue(sb, value, 4 - count);
		}
		return sb.ToString();
	}

	/// <summary>
	/// Decodes the specified Ascii85 string into the corresponding byte array.
	/// </summary>
	/// <param name="encoded">The Ascii85 string.</param>
	/// <returns>The decoded byte array.</returns>
	public static byte[] Decode(string encoded)
	{
		if (encoded == null)
		{
			throw new ArgumentNullException("encoded");
		}
		using MemoryStream stream = new MemoryStream(encoded.Length * 4 / 5);
		int count = 0;
		uint value = 0u;
		foreach (char ch in encoded)
		{
			if (ch == 'z' && count == 0)
			{
				DecodeValue(stream, value, 0);
				continue;
			}
			if (ch < '!' || ch > 'u')
			{
				ReadOnlySpan<char> readOnlySpan = "Invalid character '";
				char reference = ch;
				throw new FormatException(string.Concat(readOnlySpan, new ReadOnlySpan<char>(in reference), "' in Ascii85 block."));
			}
			try
			{
				value = checked(value + (uint)(s_powersOf85[count] * (ch - 33)));
			}
			catch (OverflowException ex2)
			{
				throw new FormatException("The current group of characters decodes to a value greater than UInt32.MaxValue.", ex2);
			}
			count++;
			if (count == 5)
			{
				DecodeValue(stream, value, 0);
				count = 0;
				value = 0u;
			}
		}
		if (count == 1)
		{
			throw new FormatException("The final Ascii85 block must contain more than one character.");
		}
		if (count > 1)
		{
			for (int padding = count; padding < 5; padding++)
			{
				try
				{
					value = checked(value + 84 * s_powersOf85[padding]);
				}
				catch (OverflowException ex)
				{
					throw new FormatException("The current group of characters decodes to a value greater than UInt32.MaxValue.", ex);
				}
			}
			DecodeValue(stream, value, 5 - count);
		}
		return stream.ToArray();
	}

	private static void EncodeValue(StringBuilder sb, uint value, int paddingBytes)
	{
		char[] encoded = new char[5];
		for (int index = 4; index >= 0; index--)
		{
			encoded[index] = (char)(value % 85 + 33);
			value /= 85;
		}
		if (paddingBytes != 0)
		{
			Array.Resize(ref encoded, 5 - paddingBytes);
		}
		sb.Append(encoded);
	}

	private static void DecodeValue(Stream stream, uint value, int paddingChars)
	{
		stream.WriteByte((byte)(value >> 24));
		if (paddingChars == 3)
		{
			return;
		}
		stream.WriteByte((byte)((value >> 16) & 0xFFu));
		if (paddingChars != 2)
		{
			stream.WriteByte((byte)((value >> 8) & 0xFFu));
			if (paddingChars != 1)
			{
				stream.WriteByte((byte)(value & 0xFFu));
			}
		}
	}
}
