#region Assembly VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.ComponentModel;
using ProtoBuf;
using Vintagestory.API.Util;

namespace Vintagestory.API.Common;

//
// Summary:
//     Defines a complete path to an assets, including it's domain.
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

    //
    // Summary:
    //     Returns true if this is a valid path. For an asset location to be valid it needs
    //     to have any string as domain, any string as path, the domain may not contain
    //     slashes, and the path may not contain 2 consecutive slashes
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

    //
    // Summary:
    //     Gets the category of the asset.
    public AssetCategory Category => AssetCategory.FromCode(FirstPathPart());

    public AssetLocation()
    {
    }

    //
    // Summary:
    //     Create a new AssetLocation from a single string (e.g. when parsing an AssetLocation
    //     in a JSON file). If no domain is prefixed, the default 'game' domain is used.
    //     This ensures the domain and path in the created AssetLocation are lowercase (as
    //     the input string could have any case)
    //
    // Parameters:
    //   domainAndPath:
    public AssetLocation(string domainAndPath)
    {
        ResolveToDomainAndPath(domainAndPath, out domain, out path);
    }

    //
    // Summary:
    //     Helper function to resolve path dependancies.
    //
    // Parameters:
    //   domainAndPath:
    //     Full path
    //
    //   domain:
    //     The mod domain to get
    //
    //   path:
    //     The resulting path to get
    private static void ResolveToDomainAndPath(string domainAndPath, out string domain, out string path)
    {
        domainAndPath = domainAndPath.ToLowerInvariant();
        int num = domainAndPath.IndexOf(':');
        if (num == -1)
        {
            domain = null;
            path = string.Intern(domainAndPath);
        }
        else
        {
            domain = string.Intern(domainAndPath.Substring(0, num));
            path = string.Intern(domainAndPath.Substring(num + 1));
        }
    }

    //
    // Summary:
    //     Create a new AssetLocation with given domain and path: for efficiency it is the
    //     responsibility of calling code to ensure these are lowercase
    public AssetLocation(string domain, string path)
    {
        this.domain = ((domain == null) ? null : string.Intern(domain));
        this.path = path;
    }

    //
    // Summary:
    //     Create a new AssetLocation if a non-empty string is provided, otherwise return
    //     null. Useful when deserializing packets
    //
    // Parameters:
    //   domainAndPath:
    public static AssetLocation CreateOrNull(string domainAndPath)
    {
        if (domainAndPath.Length == 0)
        {
            return null;
        }

        return new AssetLocation(domainAndPath);
    }

    //
    // Summary:
    //     Create an Asset Location from a string which may optionally have no prefixed
    //     domain: - in which case the defaultDomain is used. This may be used to create
    //     an AssetLocation from any string (e.g. from custom Attributes in a JSON file).
    //     For safety and consistency it ensures the domainAndPath string is lowercase.
    //     BUT: the calling code has the responsibility to ensure the defaultDomain parameter
    //     is lowercase (normally the defaultDomain will be taken from another existing
    //     AssetLocation, in which case it should already be lowercase).
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

    //
    // Summary:
    //     Returns the n-th path part
    //
    // Parameters:
    //   posFromLeft:
    public string FirstPathPart(int posFromLeft = 0)
    {
        return path.Split('/')[posFromLeft];
    }

    public string FirstCodePart()
    {
        int num = path.IndexOf('-');
        if (num >= 0)
        {
            return path.Substring(0, num);
        }

        return path;
    }

    public string SecondCodePart()
    {
        int num = path.IndexOf('-') + 1;
        int num2 = ((num <= 0) ? (-1) : path.IndexOf('-', num));
        if (num2 >= 0)
        {
            return path.Substring(num, num2 - num);
        }

        return path;
    }

    public string CodePartsAfterSecond()
    {
        int num = path.IndexOf('-') + 1;
        int num2 = ((num <= 0) ? (-1) : path.IndexOf('-', num));
        if (num2 >= 0)
        {
            return path.Substring(num2 + 1);
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

    //
    // Summary:
    //     Whether or not the Asset has a domain.
    public virtual bool HasDomain()
    {
        return domain != null;
    }

    //
    // Summary:
    //     Gets the name of the asset.
    public virtual string GetName()
    {
        int num = Path.LastIndexOf('/');
        return Path.Substring(num + 1);
    }

    //
    // Summary:
    //     Removes the file ending from the asset path.
    public virtual void RemoveEnding()
    {
        path = path.Substring(0, path.LastIndexOf('.'));
    }

    public string PathOmittingPrefixAndSuffix(string prefix, string suffix)
    {
        int num = (path.EndsWithOrdinal(suffix) ? (path.Length - suffix.Length) : path.Length);
        string text = path;
        int length = prefix.Length;
        return text.Substring(length, num - length);
    }

    //
    // Summary:
    //     Returns the code of the last variant in the path, for example for a path of "water-still-7"
    //     it would return "7"
    public string EndVariant()
    {
        int num = path.LastIndexOf('-');
        if (num < 0)
        {
            return "";
        }

        return path.Substring(num + 1);
    }

    //
    // Summary:
    //     Clones this asset.
    //
    // Returns:
    //     the cloned asset.
    public virtual AssetLocation Clone()
    {
        return new AssetLocation(domain, path);
    }

    //
    // Summary:
    //     Clones this asset in a way which saves RAM, if the calling code created the AssetLocation
    //     from JSON/deserialisation. Use for objects expected to be held for a long time
    //     - for example, building Blocks at game launch (at the cost of slightly more CPU
    //     time when creating the Clone())
    public virtual AssetLocation PermanentClone()
    {
        return new AssetLocation(domain.DeDuplicate(), path.DeDuplicate());
    }

    public virtual AssetLocation CloneWithoutPrefixAndEnding(int prefixLength)
    {
        int num = path.LastIndexOf('.');
        string text = ((num >= prefixLength) ? path.Substring(prefixLength, num - prefixLength) : path.Substring(prefixLength));
        return new AssetLocation(domain, text);
    }

    //
    // Summary:
    //     Makes a copy of the asset with a modified path.
    //
    // Parameters:
    //   path:
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

    //
    // Summary:
    //     Sets the path of the asset location
    //
    // Parameters:
    //   path:
    //     the new path to set.
    //
    // Returns:
    //     The modified AssetLocation
    public virtual AssetLocation WithPath(string path)
    {
        Path = path;
        return this;
    }

    //
    // Summary:
    //     Sets the last part after the last /
    //
    // Parameters:
    //   filename:
    public virtual AssetLocation WithFilename(string filename)
    {
        Path = Path.Substring(0, Path.LastIndexOf('/') + 1) + filename;
        return this;
    }

    //
    // Summary:
    //     Converts a collection of paths to AssetLocations.
    //
    // Parameters:
    //   names:
    //     The names of all of the locations
    //
    // Returns:
    //     The AssetLocations for all the names given.
    public static AssetLocation[] toLocations(string[] names)
    {
        AssetLocation[] array = new AssetLocation[names.Length];
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = new AssetLocation(names[i]);
        }

        return array;
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
#if false // Decompilation log
'181' items in cache
------------------
Resolve: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.InteropServices.dll'
------------------
Resolve: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.dll'
------------------
Resolve: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Found single assembly: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\SkiaSharp.dll'
------------------
Resolve: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Concurrent.dll'
------------------
Resolve: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Could not find by name: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
------------------
Resolve: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Found single assembly: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\protobuf-net.dll'
------------------
Resolve: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Security.Cryptography.dll'
------------------
Resolve: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Specialized, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Specialized.dll'
------------------
Resolve: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NetworkInformation, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NetworkInformation.dll'
------------------
Resolve: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Primitives.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Text.RegularExpressions.dll'
------------------
Resolve: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\cairo-sharp.dll'
------------------
Resolve: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Drawing.Primitives.dll'
------------------
Resolve: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ObjectModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ObjectModel.dll'
------------------
Resolve: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.TypeConverter, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.TypeConverter.dll'
------------------
Resolve: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.GraphicsLibraryFramework.dll'
------------------
Resolve: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Found single assembly: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Microsoft.Data.Sqlite.dll'
------------------
Resolve: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Data.Common.dll'
------------------
Resolve: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.dll'
------------------
Resolve: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.Desktop.dll'
------------------
Resolve: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.Thread.dll'
------------------
Resolve: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.Process.dll'
------------------
Resolve: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Linq.dll'
------------------
Resolve: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Console.dll'
------------------
Resolve: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.NameResolution, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.NameResolution.dll'
------------------
Resolve: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.StackTrace.dll'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.ThreadPool, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.ThreadPool.dll'
------------------
Resolve: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.NonGeneric, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.NonGeneric.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.CompilerServices.Unsafe.dll'
#endif
