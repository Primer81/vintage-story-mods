using System.IO;

namespace Vintagestory.API.Client;

public class JsonDialogSettings
{
	public string Code;

	public EnumDialogArea Alignment;

	public float PosX;

	public float PosY;

	public DialogRow[] Rows;

	public double SizeMultiplier = 1.0;

	public double Padding = 10.0;

	public bool DisableWorldInteract = true;

	/// <summary>
	/// Called whenever the value of a input field has changed
	/// </summary>
	public OnValueSetDelegate OnSet;

	/// <summary>
	/// Called when the dialog is opened the first time or when dialog.ReloadValues() is called. Should return the values the input fields should be set to
	/// </summary>
	public OnValueGetDelegate OnGet;

	/// <summary>
	/// Writes the content to the writer.
	/// </summary>
	/// <param name="writer">The writer to fill with data.</param>
	public void ToBytes(BinaryWriter writer)
	{
		writer.Write(Code);
		writer.Write((int)Alignment);
		writer.Write(PosX);
		writer.Write(PosY);
		writer.Write(Rows.Length);
		for (int i = 0; i < Rows.Length; i++)
		{
			Rows[i].ToBytes(writer);
		}
		writer.Write(SizeMultiplier);
	}

	/// <summary>
	/// Reads the content to the dialog.
	/// </summary>
	/// <param name="reader">The reader to read from.</param>
	public void FromBytes(BinaryReader reader)
	{
		Code = reader.ReadString();
		Alignment = (EnumDialogArea)reader.ReadInt32();
		PosX = reader.ReadSingle();
		PosY = reader.ReadSingle();
		Rows = new DialogRow[reader.ReadInt32()];
		for (int i = 0; i < Rows.Length; i++)
		{
			Rows[i] = new DialogRow();
			Rows[i].FromBytes(reader);
		}
		SizeMultiplier = reader.ReadSingle();
	}
}
