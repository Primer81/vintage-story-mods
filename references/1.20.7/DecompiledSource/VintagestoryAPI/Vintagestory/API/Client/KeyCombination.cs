using System.Collections.Generic;
using Vintagestory.API.Config;

namespace Vintagestory.API.Client;

/// <summary>
/// Mapping of an input key combination.   Note: the "key" might also be a mouse button if a hotkey has been configured to be activated by a mouse button
/// </summary>
public class KeyCombination
{
	/// <summary>
	/// The first keycode representing a mouse button
	/// </summary>
	public const int MouseStart = 240;

	/// <summary>
	/// The KeyCode (from 1.19.4, the keycodes map to either keys or mouse buttons)
	/// </summary>
	public int KeyCode;

	/// <summary>
	/// The second key code (if it exists).
	/// </summary>
	public int? SecondKeyCode;

	/// <summary>
	/// Ctrl pressed condition.
	/// </summary>
	public bool Ctrl;

	/// <summary>
	/// Alt pressed condition.
	/// </summary>
	public bool Alt;

	/// <summary>
	/// Shift pressed condition.
	/// </summary>
	public bool Shift;

	public bool OnKeyUp;

	public bool IsMouseButton(int KeyCode)
	{
		if (KeyCode >= 240)
		{
			return KeyCode < 248;
		}
		return false;
	}

	/// <summary>
	/// Converts this key combination into a string.
	/// </summary>
	/// <returns>The string code for this Key Combination.</returns>
	public override string ToString()
	{
		if (KeyCode < 0)
		{
			return "?";
		}
		if (IsMouseButton(KeyCode))
		{
			return MouseButtonAsString(KeyCode);
		}
		List<string> keys = new List<string>();
		if (Ctrl)
		{
			keys.Add("CTRL");
		}
		if (Alt)
		{
			keys.Add("ALT");
		}
		if (Shift)
		{
			keys.Add("SHIFT");
		}
		if (KeyCode == 50)
		{
			keys.Add("Esc");
		}
		else
		{
			keys.Add(GlKeyNames.ToString((GlKeys)KeyCode) ?? "");
		}
		if (SecondKeyCode.HasValue && SecondKeyCode > 0)
		{
			keys.Add(SecondaryAsString());
		}
		return string.Join(" + ", keys.ToArray());
	}

	/// <summary>
	/// Clones the current key combination.
	/// </summary>
	/// <returns>The cloned key combination.</returns>
	public KeyCombination Clone()
	{
		return (KeyCombination)MemberwiseClone();
	}

	public string PrimaryAsString()
	{
		if (IsMouseButton(KeyCode))
		{
			return MouseButtonAsString(KeyCode);
		}
		if (KeyCode == 50)
		{
			return "Esc";
		}
		return GlKeyNames.ToString((GlKeys)KeyCode);
	}

	public string SecondaryAsString()
	{
		if (IsMouseButton(SecondKeyCode.Value))
		{
			return MouseButtonAsString(SecondKeyCode.Value);
		}
		return GlKeyNames.ToString((GlKeys)SecondKeyCode.Value);
	}

	private string MouseButtonAsString(int keyCode)
	{
		int button = keyCode - 240;
		return button switch
		{
			0 => Lang.Get("Left mouse button"), 
			1 => Lang.Get("Middle mouse button"), 
			2 => Lang.Get("Right mouse button"), 
			_ => Lang.Get("Mouse button {0}", button + 1), 
		};
	}
}
