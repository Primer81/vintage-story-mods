using System.IO;

namespace Vintagestory.API.Util;

public static class ReaderWriterExtensions
{
	public static void WriteArray(this BinaryWriter writer, string[] values)
	{
		writer.Write(values.Length);
		for (int i = 0; i < values.Length; i++)
		{
			writer.Write(values[i]);
		}
	}

	public static string[] ReadStringArray(this BinaryReader reader)
	{
		string[] values = new string[reader.ReadInt32()];
		for (int i = 0; i < values.Length; i++)
		{
			values[i] = reader.ReadString();
		}
		return values;
	}

	public static void WriteArray(this BinaryWriter writer, int[] values)
	{
		writer.Write(values.Length);
		for (int i = 0; i < values.Length; i++)
		{
			writer.Write(values[i]);
		}
	}

	public static int[] ReadIntArray(this BinaryReader reader)
	{
		int[] values = new int[reader.ReadInt32()];
		for (int i = 0; i < values.Length; i++)
		{
			values[i] = reader.ReadInt32();
		}
		return values;
	}

	public static void Clear(this MemoryStream ms)
	{
		ms.Seek(0L, SeekOrigin.Begin);
		ms.SetLength(0L);
	}
}
