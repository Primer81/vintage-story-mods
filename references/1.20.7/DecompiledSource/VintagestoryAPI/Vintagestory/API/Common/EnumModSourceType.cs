namespace Vintagestory.API.Common;

/// <summary>
/// Represents the origin file type of the mod.
/// </summary>
public enum EnumModSourceType
{
	/// <summary> A single .cs source file. (Code mod without assets.) </summary>
	CS,
	/// <summary> A single .dll source file. (Code mod without assets.) </summary>
	DLL,
	/// <summary> A .zip archive able to contain assets and code files. </summary>
	ZIP,
	/// <summary> A folder able to contain assets and code files. </summary>
	Folder
}
