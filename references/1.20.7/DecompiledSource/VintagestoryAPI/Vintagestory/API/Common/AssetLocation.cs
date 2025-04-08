using System;
using System.ComponentModel;
using ProtoBuf;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

/// <summary>
/// Defines a complete path to an assets, including it's domain.
/// </summary>
/// <example>
/// In JSON assets, asset locations are represented as single strings in the form "domain:path". To access an asset in the vanilla game, use the domain 'game'.
/// <code language="json">"code": "game:vegetable-cookedcattailroot",</code>
/// </example>
[DocumentAsJson]
[TypeConverter(typeof(StringAssetLocationConverter))]
[ProtoContract]
public class AssetLocation : IEquatable<AssetLocation>, IComparable<AssetLocation>
{
	public const char LocationSeparator = ':';

	[ProtoMember(1)]
	private string domain;

	[ProtoMember(2)]
	private string path;

	public string Domain
	{
		get
		{
			return domain ?? "game";
		}
		set
		{
			domain = ((value == null) ? null : string.Intern(value.ToLowerInvariant()));
		}
	}

	public string Path
	{
		get
		{
			return path;
		}
		set
		{
			path = value;
		}
	}

	public bool IsWildCard
	{
		get
		{
			if (Path.IndexOf('*') < 0)
			{
				return Path[0] == '@';
			}
			return true;
		}
	}

	public bool EndsWithWildCard
	{
		get
		{
			if (path.Length > 1)
			{
				return path[path.Length - 1] == '*';
			}
			return false;
		}
	}

	/// <summary>
	/// Returns true if this is a valid path. For an asset location to be valid it needs to 
	/// have any string as domain, any string as path, the domain may not contain slashes, and the path may not contain 2 consecutive slashes
	/// </summary>
	public bool Valid
	{
		get
		{
			string text = domain;
			int num;
			if ((text == null || text.Length != 0) && path.Length != 0)
			{
				string text2 = domain;
				if ((text2 == null || !text2.Contains('/')) && path[0] != '/' && path[path.Length - 1] != '/')
				{
					num = (path.Contains("//") ? 1 : 0);
					goto IL_0079;
				}
			}
			num = 1;
			goto IL_0079;
			IL_0079:
			return num == 0;
		}
	}

	/// <summary>
	/// Gets the category of the asset.
	/// </summary>
	public AssetCategory Category => AssetCategory.FromCode(FirstPathPart());

	public AssetLocation()
	{
	}

	/// <summary>
	/// Create a new AssetLocation from a single string (e.g. when parsing an AssetLocation in a JSON file). If no domain is prefixed, the default 'game' domain is used.
	/// This ensures the domain and path in the created AssetLocation are lowercase (as the input string could have any case)
	/// </summary>
	/// <param name="domainAndPath"></param>
	public AssetLocation(string domainAndPath)
	{
		ResolveToDomainAndPath(domainAndPath, out domain, out path);
	}

	/// <summary>
	/// Helper function to resolve path dependancies.
	/// </summary>
	/// <param name="domainAndPath">Full path</param>
	/// <param name="domain">The mod domain to get</param>
	/// <param name="path">The resulting path to get</param>
	private static void ResolveToDomainAndPath(string domainAndPath, out string domain, out string path)
	{
		domainAndPath = domainAndPath.ToLowerInvariant();
		int colonIndex = domainAndPath.IndexOf(':');
		if (colonIndex == -1)
		{
			domain = null;
			path = string.Intern(domainAndPath);
		}
		else
		{
			domain = string.Intern(domainAndPath.Substring(0, colonIndex));
			path = string.Intern(domainAndPath.Substring(colonIndex + 1));
		}
	}

	/// <summary>
	/// Create a new AssetLocation with given domain and path: for efficiency it is the responsibility of calling code to ensure these are lowercase
	/// </summary>
	public AssetLocation(string domain, string path)
	{
		this.domain = ((domain == null) ? null : string.Intern(domain));
		this.path = path;
	}

	/// <summary>
	/// Create a new AssetLocation if a non-empty string is provided, otherwise return null. Useful when deserializing packets
	/// </summary>
	/// <param name="domainAndPath"></param>
	/// <returns></returns>
	public static AssetLocation CreateOrNull(string domainAndPath)
	{
		if (domainAndPath.Length == 0)
		{
			return null;
		}
		return new AssetLocation(domainAndPath);
	}

	/// <summary>
	/// Create an Asset Location from a string which may optionally have no prefixed domain: - in which case the defaultDomain is used.
	/// This may be used to create an AssetLocation from any string (e.g. from custom Attributes in a JSON file).  For safety and consistency it ensures the domainAndPath string is lowercase.
	/// BUT: the calling code has the responsibility to ensure the defaultDomain parameter is lowercase (normally the defaultDomain will be taken from another existing AssetLocation, in which case it should already be lowercase).
	/// </summary>
	public static AssetLocation Create(string domainAndPath, string defaultDomain = "game")
	{
		if (!domainAndPath.Contains(':'))
		{
			return new AssetLocation(defaultDomain, domainAndPath.ToLowerInvariant().DeDuplicate());
		}
		return new AssetLocation(domainAndPath);
	}

	public virtual bool IsChild(AssetLocation Location)
	{
		if (Location.Domain.Equals(Domain))
		{
			return Location.path.StartsWithFast(path);
		}
		return false;
	}

	public virtual bool BeginsWith(string domain, string partialPath)
	{
		if (path.StartsWithFast(partialPath))
		{
			return domain?.Equals(Domain) ?? true;
		}
		return false;
	}

	internal virtual bool BeginsWith(string domain, string partialPath, int offset)
	{
		if (path.StartsWithFast(partialPath, offset))
		{
			return domain?.Equals(Domain) ?? true;
		}
		return false;
	}

	public bool PathStartsWith(string partialPath)
	{
		return path.StartsWithOrdinal(partialPath);
	}

	public string ToShortString()
	{
		if (domain == null || domain.Equals("game"))
		{
			return path;
		}
		return ToString();
	}

	public string ShortDomain()
	{
		if (domain != null && !domain.Equals("game"))
		{
			return domain;
		}
		return "";
	}

	/// <summary>
	/// Returns the n-th path part
	/// </summary>
	/// <param name="posFromLeft"></param>
	/// <returns></returns>
	public string FirstPathPart(int posFromLeft = 0)
	{
		return path.Split('/')[posFromLeft];
	}

	public string FirstCodePart()
	{
		int boundary = path.IndexOf('-');
		if (boundary >= 0)
		{
			return path.Substring(0, boundary);
		}
		return path;
	}

	public string SecondCodePart()
	{
		int boundary1 = path.IndexOf('-') + 1;
		int boundary2 = ((boundary1 <= 0) ? (-1) : path.IndexOf('-', boundary1));
		if (boundary2 >= 0)
		{
			return path.Substring(boundary1, boundary2 - boundary1);
		}
		return path;
	}

	public string CodePartsAfterSecond()
	{
		int boundary1 = path.IndexOf('-') + 1;
		int boundary2 = ((boundary1 <= 0) ? (-1) : path.IndexOf('-', boundary1));
		if (boundary2 >= 0)
		{
			return path.Substring(boundary2 + 1);
		}
		return path;
	}

	public AssetLocation WithPathPrefix(string prefix)
	{
		path = prefix + path;
		return this;
	}

	public AssetLocation WithPathPrefixOnce(string prefix)
	{
		if (!path.StartsWithFast(prefix))
		{
			path = prefix + path;
		}
		return this;
	}

	public AssetLocation WithLocationPrefixOnce(AssetLocation prefix)
	{
		Domain = prefix.Domain;
		return WithPathPrefixOnce(prefix.Path);
	}

	public AssetLocation WithPathAppendix(string appendix)
	{
		path += appendix;
		return this;
	}

	public AssetLocation WithoutPathAppendix(string appendix)
	{
		if (path.EndsWithOrdinal(appendix))
		{
			path = path.Substring(0, path.Length - appendix.Length);
		}
		return this;
	}

	public AssetLocation WithPathAppendixOnce(string appendix)
	{
		if (!path.EndsWithOrdinal(appendix))
		{
			path += appendix;
		}
		return this;
	}

	/// <summary>
	/// Whether or not the Asset has a domain.
	/// </summary>
	/// <returns></returns>
	public virtual bool HasDomain()
	{
		return domain != null;
	}

	/// <summary>
	/// Gets the name of the asset.
	/// </summary>
	/// <returns></returns>
	public virtual string GetName()
	{
		int index = Path.LastIndexOf('/');
		return Path.Substring(index + 1);
	}

	/// <summary>
	/// Removes the file ending from the asset path.
	/// </summary>
	public virtual void RemoveEnding()
	{
		path = path.Substring(0, path.LastIndexOf('.'));
	}

	public string PathOmittingPrefixAndSuffix(string prefix, string suffix)
	{
		int endpoint = (path.EndsWithOrdinal(suffix) ? (path.Length - suffix.Length) : path.Length);
		string text = path;
		int length = prefix.Length;
		return text.Substring(length, endpoint - length);
	}

	/// <summary>
	/// Returns the code of the last variant in the path, for example for a path of "water-still-7" it would return "7"
	/// </summary>
	public string EndVariant()
	{
		int i = path.LastIndexOf('-');
		if (i < 0)
		{
			return "";
		}
		return path.Substring(i + 1);
	}

	/// <summary>
	/// Clones this asset.
	/// </summary>
	/// <returns>the cloned asset.</returns>
	public virtual AssetLocation Clone()
	{
		return new AssetLocation(domain, path);
	}

	/// <summary>
	/// Clones this asset in a way which saves RAM, if the calling code created the AssetLocation from JSON/deserialisation. Use for objects expected to be held for a long time - for example, building Blocks at game launch
	/// (at the cost of slightly more CPU time when creating the Clone())
	/// </summary>
	/// <returns></returns>
	public virtual AssetLocation PermanentClone()
	{
		return new AssetLocation(domain.DeDuplicate(), path.DeDuplicate());
	}

	public virtual AssetLocation CloneWithoutPrefixAndEnding(int prefixLength)
	{
		int i = path.LastIndexOf('.');
		string newPath = ((i >= prefixLength) ? path.Substring(prefixLength, i - prefixLength) : path.Substring(prefixLength));
		return new AssetLocation(domain, newPath);
	}

	/// <summary>
	/// Makes a copy of the asset with a modified path.
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	public virtual AssetLocation CopyWithPath(string path)
	{
		return new AssetLocation(domain, path);
	}

	public virtual AssetLocation CopyWithPathPrefixAndAppendix(string prefix, string appendix)
	{
		return new AssetLocation(domain, prefix + path + appendix);
	}

	public virtual AssetLocation CopyWithPathPrefixAndAppendixOnce(string prefix, string appendix)
	{
		if (path.StartsWithFast(prefix))
		{
			return new AssetLocation(domain, path.EndsWithOrdinal(appendix) ? path : (path + appendix));
		}
		return new AssetLocation(domain, path.EndsWithOrdinal(appendix) ? (prefix + path) : (prefix + path + appendix));
	}

	/// <summary>
	/// Sets the path of the asset location
	/// </summary>
	/// <param name="path">the new path to set.</param>
	/// <returns>The modified AssetLocation</returns>
	public virtual AssetLocation WithPath(string path)
	{
		Path = path;
		return this;
	}

	/// <summary>
	/// Sets the last part after the last /
	/// </summary>
	/// <param name="filename"></param>
	/// <returns></returns>
	public virtual AssetLocation WithFilename(string filename)
	{
		Path = Path.Substring(0, Path.LastIndexOf('/') + 1) + filename;
		return this;
	}

	/// <summary>
	/// Converts a collection of paths to AssetLocations.
	/// </summary>
	/// <param name="names">The names of all of the locations</param>
	/// <returns>The AssetLocations for all the names given.</returns>
	public static AssetLocation[] toLocations(string[] names)
	{
		AssetLocation[] locations = new AssetLocation[names.Length];
		for (int i = 0; i < locations.Length; i++)
		{
			locations[i] = new AssetLocation(names[i]);
		}
		return locations;
	}

	public override int GetHashCode()
	{
		return Domain.GetHashCode() ^ path.GetHashCode();
	}

	public bool Equals(AssetLocation other)
	{
		if (other == null)
		{
			return false;
		}
		if (path.EqualsFast(other.path))
		{
			return Domain.Equals(other.Domain);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		return Equals(obj as AssetLocation);
	}

	public static bool operator ==(AssetLocation left, AssetLocation right)
	{
		return left?.Equals(right) ?? ((object)right == null);
	}

	public static bool operator !=(AssetLocation left, AssetLocation right)
	{
		return !(left == right);
	}

	public override string ToString()
	{
		return Domain + ":" + Path;
	}

	public int CompareTo(AssetLocation other)
	{
		return ToString().CompareOrdinal(other.ToString());
	}

	public bool WildCardMatch(AssetLocation other, string pathAsRegex)
	{
		if (Domain == other.Domain)
		{
			return WildcardUtil.fastMatch(path, other.path, pathAsRegex);
		}
		return false;
	}

	public static implicit operator string(AssetLocation loc)
	{
		return loc?.ToString();
	}

	public static implicit operator AssetLocation(string code)
	{
		return Create(code);
	}
}
