using System;

namespace Vintagestory.API.Common;

/// <summary>
/// Represents a mod dependency requirement of one mod for another.
/// </summary>
public class ModDependency
{
	/// <summary> The required mod id (domain) of this dependency. </summary>
	public string ModID { get; }

	/// <summary>
	/// The minimum version requirement of this dependency.
	/// May be empty if the no specific version is required.
	/// </summary>
	public string Version { get; }

	/// <summary>
	/// Creates a new ModDependancy object.
	/// </summary>
	/// <param name="modID">The ID of the required mod.</param>
	/// <param name="version">The version of the required mod (default: empty string.)</param>
	public ModDependency(string modID, string version = "")
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

	/// <summary>
	/// Returns the Mod Dependancy as a string.
	/// </summary>
	/// <returns></returns>
	public override string ToString()
	{
		if (string.IsNullOrEmpty(Version))
		{
			return ModID;
		}
		return ModID + "@" + Version;
	}
}
