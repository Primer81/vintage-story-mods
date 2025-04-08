using System;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Vintagestory.API.Client;

public static class GlKeyNames
{
	/// <summary>
	/// Converts the given key to a string.
	/// </summary>
	/// <param name="key">the key being passed in.</param>
	/// <returns>the string name of the key.</returns>
	public static string ToString(GlKeys key)
	{
		return key switch
		{
			GlKeys.Keypad0 => "Keypad 0", 
			GlKeys.Keypad1 => "Keypad 1", 
			GlKeys.Keypad2 => "Keypad 2", 
			GlKeys.Keypad3 => "Keypad 3", 
			GlKeys.Keypad4 => "Keypad 4", 
			GlKeys.Keypad5 => "Keypad 5", 
			GlKeys.Keypad6 => "Keypad 6", 
			GlKeys.Keypad7 => "Keypad 7", 
			GlKeys.Keypad8 => "Keypad 8", 
			GlKeys.Keypad9 => "Keypad 9", 
			GlKeys.KeypadDivide => "Keypad Divide", 
			GlKeys.KeypadMultiply => "Keypad Multiply", 
			GlKeys.KeypadMinus => "Keypad Subtract", 
			GlKeys.KeypadAdd => "Keypad Add", 
			GlKeys.KeypadDecimal => "Keypad Decimal", 
			GlKeys.KeypadEnter => "Keypad Enter", 
			GlKeys.Unknown => "Unknown", 
			GlKeys.LShift => "Shift", 
			GlKeys.LControl => "Ctrl", 
			GlKeys.AltLeft => "Alt", 
			_ => GetKeyName(key), 
		};
	}

	/// <summary>
	/// Gets the string the key would produce upon pressing it without considering any modifiers (but single keys get converted to uppercase).
	/// So GlKeys.W on QWERTY Keyboard layout returns W, GlKeys.Space returns Space etc.
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public static string GetKeyName(GlKeys key)
	{
		string keyName = GetPrintableChar((int)key);
		if (string.IsNullOrWhiteSpace(keyName))
		{
			return key.ToString();
		}
		return keyName.ToUpperInvariant();
	}

	/// <summary>
	/// Returns the printable character for a key. Does return null on none printable keys like <see cref="F:Vintagestory.API.Client.GlKeys.Enter" />
	/// </summary>
	/// <param name="key"></param>
	/// <returns></returns>
	public static string GetPrintableChar(int key)
	{
		try
		{
			return GLFW.GetKeyName((Keys)KeyConverter.GlKeysToNew[key], 0);
		}
		catch (IndexOutOfRangeException)
		{
			return string.Empty;
		}
	}
}
