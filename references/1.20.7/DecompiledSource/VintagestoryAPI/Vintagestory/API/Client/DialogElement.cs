using System.IO;
using Vintagestory.API.Util;

namespace Vintagestory.API.Client;

public class DialogElement
{
	public string Code;

	public int Width = 150;

	public int Height = 30;

	public int PaddingLeft;

	public EnumDialogElementType Type;

	public EnumDialogElementMode Mode;

	public string Label;

	public string Icon;

	public string Text;

	public string Tooltip;

	public float FontSize = 14f;

	public string[] Icons;

	public string[] Values;

	public string[] Names;

	public string[] Tooltips;

	public int MinValue;

	public int MaxValue;

	public int Step;

	/// <summary>
	/// To hold generic data
	/// </summary>
	public string Param;

	internal void FromBytes(BinaryReader reader)
	{
		Code = reader.ReadString();
		Width = reader.ReadInt32();
		Height = reader.ReadInt32();
		Type = (EnumDialogElementType)reader.ReadInt32();
		Mode = (EnumDialogElementMode)reader.ReadInt32();
		Label = reader.ReadString();
		Icons = reader.ReadStringArray();
		Values = reader.ReadStringArray();
		Names = reader.ReadStringArray();
		Tooltips = reader.ReadStringArray();
		Param = reader.ReadString();
		MinValue = reader.ReadInt32();
		MaxValue = reader.ReadInt32();
		Step = reader.ReadInt32();
		Icon = reader.ReadString();
		Text = reader.ReadString();
		FontSize = reader.ReadSingle();
	}

	internal void ToBytes(BinaryWriter writer)
	{
		writer.Write(Code);
		writer.Write(Width);
		writer.Write(Height);
		writer.Write((int)Type);
		writer.Write((int)Mode);
		writer.Write(Label);
		writer.WriteArray(Icons);
		writer.WriteArray(Values);
		writer.WriteArray(Names);
		writer.WriteArray(Tooltips);
		writer.Write(Param);
		writer.Write(MinValue);
		writer.Write(MaxValue);
		writer.Write(Step);
		writer.Write(Icon);
		writer.Write(Text);
		writer.Write(FontSize);
	}
}
