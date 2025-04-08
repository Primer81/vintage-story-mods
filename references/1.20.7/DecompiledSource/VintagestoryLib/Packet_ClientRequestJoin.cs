public class Packet_ClientRequestJoin
{
	public string Language;

	public const int LanguageFieldID = 1;

	public void SetLanguage(string value)
	{
		Language = value;
	}

	internal void InitializeValues()
	{
	}
}
