public class Packet_VariantPart
{
	public string Code;

	public string Value;

	public const int CodeFieldID = 1;

	public const int ValueFieldID = 2;

	public void SetCode(string value)
	{
		Code = value;
	}

	public void SetValue(string value)
	{
		Value = value;
	}

	internal void InitializeValues()
	{
	}
}
