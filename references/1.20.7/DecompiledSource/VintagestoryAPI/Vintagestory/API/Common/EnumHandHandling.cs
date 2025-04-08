namespace Vintagestory.API.Common;

/// <summary>
/// How the engine should handle attacking with an item in hands
/// </summary>
public enum EnumHandHandling
{
	/// <summary>
	/// Uses the engine default behavior which is to play an attack animation and do block breaking/damage entities if in range. Will not call the *Step and *Stop methods.
	/// </summary>
	NotHandled,
	/// <summary>
	/// Uses the engine default behavior which is to play an attack or build animation and do block breaking/damage entities if in range,
	/// but also notify the server that the Use/Attack method has to be called serverside as well. Will call the *Step and *Stop methods.
	/// </summary>
	Handled,
	/// <summary>
	/// Do not play any default first person attack animation, but do block breaking/damage entities if in range. Notifies that the server that the Use/Attack method has to be called serverside as well. Will call the *Step and *Stop methods.
	/// </summary>
	PreventDefaultAnimation,
	/// <summary>
	/// Do play first person attack animation, don't break blocks/damage entities in range. Notifies that the server that the Use/Attack method has to be called serverside as well. Will call the *Step and *Stop methods.
	/// </summary>
	PreventDefaultAction,
	/// <summary>
	/// Do not play any first person attack animation, don't break blocks in range or damage entities in range. Notifies that the server that the Use/Attack method has to be called serverside as well. Will call the *Step and *Stop methods.
	/// </summary>
	PreventDefault
}
