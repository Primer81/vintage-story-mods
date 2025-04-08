namespace Vintagestory.API.Common;

public static class AssetLocationExtensions
{
	/// <summary>
	/// Convenience method and aids performance, avoids double field look-up
	/// </summary>
	/// <param name="loc"></param>
	/// <returns></returns>
	public static string ToNonNullString(this AssetLocation loc)
	{
		if (!(loc == null))
		{
			return loc.ToShortString();
		}
		return "";
	}
}
