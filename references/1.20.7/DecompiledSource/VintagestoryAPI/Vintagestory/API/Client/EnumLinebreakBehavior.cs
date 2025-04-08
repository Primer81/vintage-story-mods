namespace Vintagestory.API.Client;

public enum EnumLinebreakBehavior
{
	/// <summary>
	/// Language specific default setting
	/// </summary>
	Default,
	/// <summary>
	/// After every word
	/// </summary>
	AfterWord,
	/// <summary>
	/// After any character
	/// </summary>
	AfterCharacter,
	/// <summary>
	/// Do not auto-line break, only explicitly after \n
	/// </summary>
	None
}
