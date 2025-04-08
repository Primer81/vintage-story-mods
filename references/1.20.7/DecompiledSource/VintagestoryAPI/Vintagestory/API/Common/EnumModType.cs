namespace Vintagestory.API.Common;

/// <summary>
/// Describes the type of a mod. Allows easy recognition and limiting of
/// what any particular mod can do.
/// </summary>
public enum EnumModType
{
	/// <summary>
	/// Makes only theme changes (texture, shape, sound, music) to existing
	/// game or mod assets / content without adding new content or code.
	/// </summary>
	Theme,
	/// <summary>
	/// Can modify any existing assets, or add new content, but no code.
	/// </summary>
	Content,
	/// <summary>
	/// Can modify existing assets, add new content and make use of C#
	/// source files (.cs) and pre-compiled assemblies (.dll).
	/// </summary>
	Code
}
