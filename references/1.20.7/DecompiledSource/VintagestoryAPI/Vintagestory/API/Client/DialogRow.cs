using System.IO;

namespace Vintagestory.API.Client;

public class DialogRow
{
	public DialogElement[] Elements;

	public float BottomPadding = 10f;

	public float TopPadding;

	public DialogRow()
	{
	}

	public DialogRow(params DialogElement[] elements)
	{
		Elements = elements;
	}

	internal void FromBytes(BinaryReader reader)
	{
		Elements = new DialogElement[reader.ReadInt32()];
		for (int i = 0; i < Elements.Length; i++)
		{
			Elements[i] = new DialogElement();
			Elements[i].FromBytes(reader);
		}
		TopPadding = reader.ReadInt16();
		BottomPadding = reader.ReadInt16();
	}

	internal void ToBytes(BinaryWriter writer)
	{
		writer.Write(Elements.Length);
		for (int i = 0; i < Elements.Length; i++)
		{
			Elements[i].ToBytes(writer);
		}
		writer.Write((short)TopPadding);
		writer.Write((short)BottomPadding);
	}
}
