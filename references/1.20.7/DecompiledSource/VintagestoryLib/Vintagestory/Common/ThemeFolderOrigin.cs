namespace Vintagestory.Common;

public class ThemeFolderOrigin : FolderOrigin
{
	public ThemeFolderOrigin(string fullPath)
		: base(fullPath, null)
	{
	}

	public ThemeFolderOrigin(string fullPath, string pathForReservedCharsCheck)
		: base(fullPath, pathForReservedCharsCheck)
	{
	}

	public override bool IsAllowedToAffectGameplay()
	{
		return false;
	}
}
