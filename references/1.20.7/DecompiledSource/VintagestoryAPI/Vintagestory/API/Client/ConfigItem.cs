namespace Vintagestory.API.Client;

/// <summary>
/// A config item for the GUIElementConfigList.
/// </summary>
public class ConfigItem
{
	/// <summary>
	/// Item or title
	/// </summary>
	public EnumItemType Type;

	/// <summary>
	/// The name of the config item.
	/// </summary>
	public string Key;

	/// <summary>
	/// the value of the config item.  
	/// </summary>
	public string Value;

	/// <summary>
	/// The code of the config item.
	/// </summary>
	public string Code;

	/// <summary>
	/// Has this particular config item errored?
	/// </summary>
	public bool error;

	/// <summary>
	/// The y position of the config item.
	/// </summary>
	public double posY;

	/// <summary>
	/// The height of the config item.
	/// </summary>
	public double height;

	public object Data;
}
