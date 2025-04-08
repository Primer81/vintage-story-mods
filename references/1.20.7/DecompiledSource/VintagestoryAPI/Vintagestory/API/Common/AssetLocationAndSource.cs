using System;
using System.ComponentModel;

namespace Vintagestory.API.Common;

/// <summary>
/// Defines a complete path to an assets, including it's domain. Includes an extra Source field for debugging.
/// </summary>
[TypeConverter(typeof(StringAssetLocationConverter))]
public class AssetLocationAndSource : AssetLocation, IEquatable<AssetLocation>
{
	public bool AddToAllAtlasses;

	/// <summary>
	/// The source of a given asset.
	/// </summary>
	public SourceStringComponents Source;

	/// <summary>
	/// Used to avoid duplication when loading colormaps and shapes
	/// </summary>
	public volatile int loadedAlready;

	public AssetLocationAndSource(string location)
		: base(location)
	{
	}

	public AssetLocationAndSource(AssetLocation loc)
		: base(loc.Domain, loc.Path)
	{
	}

	public AssetLocationAndSource(AssetLocation loc, string message, AssetLocation sourceLoc, int alternateNo = -1)
		: base(loc.Domain, loc.Path)
	{
		Source = new SourceStringComponents(message, sourceLoc, alternateNo);
	}

	public AssetLocationAndSource(string domain, string path, string message, string sourceDomain, string sourcePath, int alternateNo = -1)
		: base(domain, path)
	{
		Source = new SourceStringComponents(message, sourceDomain, sourcePath, alternateNo);
	}

	public AssetLocationAndSource(AssetLocation loc, SourceStringComponents source)
		: base(loc.Domain, loc.Path)
	{
		Source = source;
	}

	public AssetLocationAndSource(string domain, string path, SourceStringComponents source)
		: base(domain, path)
	{
		Source = source;
	}

	[Obsolete("For reduced RAM usage please use newer overloads e.g. AssetLocationAndSource(loc, message, sourceAssetLoc)", false)]
	public AssetLocationAndSource(AssetLocation loc, string oldStyleSource)
		: base(loc.Domain, loc.Path)
	{
		Source = new SourceStringComponents(oldStyleSource, "", "", -1);
	}

	[Obsolete("For reduced RAM usage please use newer overloads e.g. AssetLocationAndSource(domain, path, message, sourceDomain, sourcePath)", false)]
	public AssetLocationAndSource(string domain, string path, string oldStyleSource)
		: base(domain, path)
	{
		Source = new SourceStringComponents(oldStyleSource, "", "", -1);
	}
}
