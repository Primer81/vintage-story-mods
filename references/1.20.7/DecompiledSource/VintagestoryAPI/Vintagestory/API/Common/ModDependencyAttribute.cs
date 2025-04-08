using System;

namespace Vintagestory.API.Common;

/// <summary>
/// Applied to a mod assembly multiple times for each required dependency.
/// Superseded by this mod's "modinfo.json" file, if available.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class ModDependencyAttribute : Attribute
{
	/// <summary> The required mod id (domain) of this dependency. </summary>
	public string ModID { get; }

	/// <summary>
	/// The minimum version requirement of this dependency.
	/// May be empty if the no specific version is required.
	/// </summary>
	public string Version { get; }

	public ModDependencyAttribute(string modID, string version = "")
	{
		if (modID == null)
		{
			throw new ArgumentNullException("modID");
		}
		if (!ModInfo.IsValidModID(modID))
		{
			throw new ArgumentException("'" + modID + "' is not a valid mod ID. Please use only lowercase letters and numbers.", "modID");
		}
		ModID = modID;
		Version = version ?? "";
	}
}
