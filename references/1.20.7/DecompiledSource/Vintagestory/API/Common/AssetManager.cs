#region Assembly VintagestoryLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// location unknown
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.Common;

public class AssetManager : IAssetManager
{
    private EnumAppSide side;

    public bool allAssetsLoaded;

    public Dictionary<AssetLocation, IAsset> Assets;

    public Dictionary<AssetLocation, IAsset> RuntimeAssets = new Dictionary<AssetLocation, IAsset>();

    private IDictionary<string, List<IAsset>> assetsByCategory;

    public List<IAssetOrigin> Origins;

    public List<IAssetOrigin> CustomAppOrigins = new List<IAssetOrigin>();

    public List<IAssetOrigin> CustomModOrigins = new List<IAssetOrigin>();

    private string assetsPath;

    public Dictionary<AssetLocation, IAsset> AllAssets => Assets;

    List<IAssetOrigin> IAssetManager.Origins => Origins;

    public AssetManager(string assetsPath, EnumAppSide side)
    {
        this.assetsPath = assetsPath;
        this.side = side;
    }

    public void Add(AssetLocation path, IAsset asset)
    {
        if (!Assets.ContainsKey(path))
        {
            Assets[path] = asset;
            assetsByCategory[path.Category.Code].Add(asset);
        }

        if (!RuntimeAssets.ContainsKey(path))
        {
            RuntimeAssets[path] = asset;
        }
    }

    public int InitAndLoadBaseAssets(ILogger Logger)
    {
        return InitAndLoadBaseAssets(Logger, null);
    }

    public int InitAndLoadBaseAssets(ILogger Logger, string pathForReservedCharsCheck)
    {
        allAssetsLoaded = false;
        Origins = new List<IAssetOrigin>();
        Origins.Add(new GameOrigin(assetsPath, pathForReservedCharsCheck));
        Assets = new Dictionary<AssetLocation, IAsset>();
        assetsByCategory = new FastSmallDictionary<string, List<IAsset>>(AssetCategory.categories.Values.Count + 1);
        int num = 0;
        foreach (AssetCategory value in AssetCategory.categories.Values)
        {
            if ((value.SideType & side) <= (EnumAppSide)0)
            {
                continue;
            }

            Dictionary<AssetLocation, IAsset> assetsDontLoad = GetAssetsDontLoad(value, Origins);
            foreach (IAsset value2 in assetsDontLoad.Values)
            {
                Assets[value2.Location] = value2;
            }

            num += assetsDontLoad.Count;
            assetsByCategory[value.Code] = assetsDontLoad.Values.ToList();
            Logger?.Notification("Found {1} base assets in category {0}", value, assetsDontLoad.Count);
        }

        return num;
    }

    public int AddExternalAssets(ILogger Logger, ModLoader modloader = null)
    {
        List<string> list = new List<string>();
        List<IAssetOrigin> list2 = new List<IAssetOrigin>();
        foreach (IAssetOrigin customAppOrigin in CustomAppOrigins)
        {
            Origins.Add(customAppOrigin);
            list2.Add(customAppOrigin);
            list.Add("arg@" + customAppOrigin.OriginPath);
        }

        foreach (IAssetOrigin customModOrigin in CustomModOrigins)
        {
            Origins.Add(customModOrigin);
            list2.Add(customModOrigin);
            list.Add("modorigin@" + customModOrigin.OriginPath);
        }

        if (modloader != null)
        {
            foreach (KeyValuePair<string, IAssetOrigin> contentArchive in modloader.GetContentArchives())
            {
                list2.Add(contentArchive.Value);
                Origins.Add(contentArchive.Value);
                list.Add("mod@" + contentArchive.Key);
            }

            foreach (KeyValuePair<string, IAssetOrigin> themeArchive in modloader.GetThemeArchives())
            {
                list2.Add(themeArchive.Value);
                Origins.Add(themeArchive.Value);
                list.Add("themepack@" + themeArchive.Key);
            }
        }

        if (list.Count > 0)
        {
            Logger.Notification("External Origins in load order: {0}", string.Join(", ", list));
        }

        int num = 0;
        int num2 = 0;
        foreach (AssetCategory value2 in AssetCategory.categories.Values)
        {
            if ((value2.SideType & side) > (EnumAppSide)0)
            {
                Dictionary<AssetLocation, IAsset> assetsDontLoad = GetAssetsDontLoad(value2, list2);
                foreach (IAsset value3 in assetsDontLoad.Values)
                {
                    Assets[value3.Location] = value3;
                }

                num2 += assetsDontLoad.Count;
                if (!assetsByCategory.TryGetValue(value2.Code, out var value))
                {
                    value = (assetsByCategory[value2.Code] = new List<IAsset>());
                }

                value.AddRange(assetsDontLoad.Values);
                Logger.Notification("Found {1} external assets in category {0}", value2, assetsDontLoad.Count);
            }

            num++;
        }

        allAssetsLoaded = true;
        return num2;
    }

    public void UnloadExternalAssets(ILogger logger)
    {
        allAssetsLoaded = false;
        InitAndLoadBaseAssets(null);
    }

    public void UnloadAssets(AssetCategory category)
    {
        foreach (KeyValuePair<AssetLocation, IAsset> asset in Assets)
        {
            if (asset.Key.Category == category)
            {
                asset.Value.Data = null;
            }
        }
    }

    public void UnloadAssets()
    {
        foreach (KeyValuePair<AssetLocation, IAsset> asset in Assets)
        {
            asset.Value.Data = null;
        }
    }

    public void UnloadUnpatchedAssets()
    {
        foreach (KeyValuePair<AssetLocation, IAsset> asset in Assets)
        {
            if (!asset.Value.IsPatched)
            {
                asset.Value.Data = null;
            }
        }
    }

    public List<AssetLocation> GetLocations(string fullPathBeginsWith, string domain = null)
    {
        List<AssetLocation> list = new List<AssetLocation>();
        foreach (IAsset value in Assets.Values)
        {
            if (value.Location.BeginsWith(domain, fullPathBeginsWith))
            {
                list.Add(value.Location);
            }
        }

        return list;
    }

    public bool Exists(AssetLocation location)
    {
        return Assets.ContainsKey(location);
    }

    public IAsset TryGet(string Path, bool loadAsset = true)
    {
        return TryGet(new AssetLocation(Path), loadAsset);
    }

    public IAsset TryGet(AssetLocation Location, bool loadAsset = true)
    {
        if (!allAssetsLoaded)
        {
            throw new Exception("Coding error: Mods must not get assets before AssetsLoaded stage - do not load assets in a Start() method!");
        }

        return TryGet_BaseAssets(Location, loadAsset);
    }

    public IAsset TryGet_BaseAssets(string Path, bool loadAsset = true)
    {
        return TryGet_BaseAssets(new AssetLocation(Path), loadAsset);
    }

    public IAsset TryGet_BaseAssets(AssetLocation Location, bool loadAsset = true)
    {
        IAsset value = null;
        if (!Assets.TryGetValue(Location, out value))
        {
            return null;
        }

        if (!value.IsLoaded() && loadAsset)
        {
            value.Origin.TryLoadAsset(value);
        }

        return value;
    }

    public IAsset Get(string Path)
    {
        return Get(new AssetLocation(Path));
    }

    public IAsset Get(AssetLocation Location)
    {
        return TryGet_BaseAssets(Location) ?? throw new Exception(string.Concat("Asset ", Location, " could not be found"));
    }

    public T Get<T>(AssetLocation Location)
    {
        return Get(Location).ToObject<T>();
    }

    public List<IAsset> GetMany(AssetCategory category, bool loadAsset = true)
    {
        List<IAsset> list = new List<IAsset>();
        if (assetsByCategory.TryGetValue(category.Code, out var value))
        {
            foreach (IAsset item in value)
            {
                if (item.Location.Category == category)
                {
                    if (!item.IsLoaded() && loadAsset)
                    {
                        item.Origin.LoadAsset(item);
                    }

                    list.Add(item);
                }
            }
        }

        return list;
    }

    public List<IAsset> GetManyInCategory(string categoryCode, string pathBegins, string domain = null, bool loadAsset = true)
    {
        List<IAsset> list = new List<IAsset>();
        if (assetsByCategory.TryGetValue(categoryCode, out var value))
        {
            int offset = categoryCode.Length + 1;
            foreach (IAsset item in value)
            {
                if (item.Location.BeginsWith(domain, pathBegins, offset))
                {
                    if (loadAsset && !item.IsLoaded())
                    {
                        item.Origin.LoadAsset(item);
                    }

                    list.Add(item);
                }
            }
        }

        return list;
    }

    public List<IAsset> GetMany(string partialPath, string domain = null, bool loadAsset = true)
    {
        List<IAsset> list = new List<IAsset>();
        foreach (KeyValuePair<AssetLocation, IAsset> asset in Assets)
        {
            IAsset value = asset.Value;
            if (asset.Key.BeginsWith(domain, partialPath))
            {
                if (loadAsset && !value.IsLoaded())
                {
                    value.Origin.LoadAsset(value);
                }

                list.Add(value);
            }
        }

        return list;
    }

    public Dictionary<AssetLocation, T> GetMany<T>(ILogger logger, string fullPath, string domain = null)
    {
        Dictionary<AssetLocation, T> dictionary = new Dictionary<AssetLocation, T>();
        foreach (Asset item in GetMany(fullPath, domain))
        {
            try
            {
                dictionary.Add(item.Location, item.ToObject<T>());
            }
            catch (JsonReaderException ex)
            {
                logger.Error("Syntax error in json file '{0}': {1}", item, ex.Message);
            }
        }

        return dictionary;
    }

    internal Dictionary<AssetLocation, IAsset> GetAssetsDontLoad(AssetCategory category, List<IAssetOrigin> fromOrigins)
    {
        Dictionary<AssetLocation, IAsset> dictionary = new Dictionary<AssetLocation, IAsset>();
        foreach (IAssetOrigin fromOrigin in fromOrigins)
        {
            if (!fromOrigin.IsAllowedToAffectGameplay() && category.AffectsGameplay)
            {
                continue;
            }

            foreach (IAsset asset in fromOrigin.GetAssets(category, shouldLoad: false))
            {
                dictionary[asset.Location] = asset;
            }
        }

        return dictionary;
    }

    public int Reload(AssetLocation location)
    {
        Assets.RemoveAllByKey((AssetLocation x) => location == null || location.IsChild(x));
        int num = 0;
        List<IAsset> value = null;
        if (location != null)
        {
            int num2 = location.Path.IndexOf('/');
            if (num2 > 0)
            {
                string key = location.Path.Substring(0, num2);
                if (assetsByCategory.TryGetValue(key, out value))
                {
                    value.RemoveAll((IAsset a) => location.IsChild(a.Location));
                }
            }
        }

        foreach (IAssetOrigin origin in Origins)
        {
            List<IAsset> assets = origin.GetAssets(location);
            foreach (IAsset item in assets)
            {
                Assets[item.Location] = item;
                num++;
            }

            value?.AddRange(assets);
        }

        return num;
    }

    public int Reload(AssetCategory category)
    {
        Assets.RemoveAllByKey((AssetLocation x) => category == null || x.Category == category);
        int num = 0;
        if (!assetsByCategory.TryGetValue(category.Code, out var value))
        {
            value = (assetsByCategory[category.Code] = new List<IAsset>());
        }
        else
        {
            value.Clear();
        }

        foreach (IAssetOrigin origin in Origins)
        {
            List<IAsset> assets = origin.GetAssets(category);
            foreach (IAsset item in assets)
            {
                Assets[item.Location] = item;
                num++;
            }

            value.AddRange(assets);
        }

        foreach (KeyValuePair<AssetLocation, IAsset> runtimeAsset in RuntimeAssets)
        {
            if (runtimeAsset.Key.Category == category)
            {
                Add(runtimeAsset.Key, runtimeAsset.Value);
            }
        }

        return num;
    }

    public AssetCategory GetCategoryFromFullPath(string fullpath)
    {
        return AssetCategory.FromCode(fullpath.Split('/')[0]);
    }

    public void AddPathOrigin(string domain, string fullPath)
    {
        AddModOrigin(domain, fullPath, null);
    }

    public void AddModOrigin(string domain, string fullPath)
    {
        AddModOrigin(domain, fullPath, null);
    }

    public void AddModOrigin(string domain, string fullPath, string pathForReservedCharsCheck)
    {
        for (int i = 0; i < CustomModOrigins.Count; i++)
        {
            IAssetOrigin assetOrigin = CustomModOrigins[i];
            if ((assetOrigin as PathOrigin)?.OriginPath == fullPath && (assetOrigin as PathOrigin)?.Domain == domain)
            {
                return;
            }
        }

        CustomModOrigins.Add(new PathOrigin(domain, fullPath, pathForReservedCharsCheck));
    }
}
#if false // Decompilation log
'182' items in cache
------------------
Resolve: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.dll'
------------------
Resolve: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime.InteropServices, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.InteropServices.dll'
------------------
Resolve: 'VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'VintagestoryAPI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\VintagestoryAPI.dll'
------------------
Resolve: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'cairo-sharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\cairo-sharp.dll'
------------------
Resolve: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.dll'
------------------
Resolve: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Found single assembly: 'protobuf-net, Version=2.4.0.0, Culture=neutral, PublicKeyToken=257b51d87d2e4d67'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\protobuf-net.dll'
------------------
Resolve: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Concurrent, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Collections.Concurrent.dll'
------------------
Resolve: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Found single assembly: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Newtonsoft.Json.dll'
------------------
Resolve: 'CommandLine, Version=2.9.1.0, Culture=neutral, PublicKeyToken=5a870481e358d379'
Could not find by name: 'CommandLine, Version=2.9.1.0, Culture=neutral, PublicKeyToken=5a870481e358d379'
------------------
Resolve: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.dll'
------------------
Resolve: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Threading.Thread, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Threading.Thread.dll'
------------------
Resolve: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Text.RegularExpressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Text.RegularExpressions.dll'
------------------
Resolve: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Found single assembly: 'SkiaSharp, Version=2.88.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\SkiaSharp.dll'
------------------
Resolve: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.StackTrace, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.StackTrace.dll'
------------------
Resolve: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Console.dll'
------------------
Resolve: 'System.Net.Http, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Http, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Http.dll'
------------------
Resolve: 'Open.Nat, Version=1.0.0.0, Culture=neutral, PublicKeyToken=f22a6a4582336c76'
Could not find by name: 'Open.Nat, Version=1.0.0.0, Culture=neutral, PublicKeyToken=f22a6a4582336c76'
------------------
Resolve: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Primitives.dll'
------------------
Resolve: 'Mono.Nat, Version=3.0.0.0, Culture=neutral, PublicKeyToken=6c9468a3c21bc6d1'
Could not find by name: 'Mono.Nat, Version=3.0.0.0, Culture=neutral, PublicKeyToken=6c9468a3c21bc6d1'
------------------
Resolve: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.Process, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.Process.dll'
------------------
Resolve: 'System.Net.Sockets, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Sockets, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Net.Sockets.dll'
------------------
Resolve: 'Mono.Cecil, Version=0.11.5.0, Culture=neutral, PublicKeyToken=50cebf1cceb9d05e'
Could not find by name: 'Mono.Cecil, Version=0.11.5.0, Culture=neutral, PublicKeyToken=50cebf1cceb9d05e'
------------------
Resolve: 'Microsoft.CodeAnalysis, Version=4.9.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
Could not find by name: 'Microsoft.CodeAnalysis, Version=4.9.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
------------------
Resolve: 'Microsoft.CodeAnalysis.CSharp, Version=4.9.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
Could not find by name: 'Microsoft.CodeAnalysis.CSharp, Version=4.9.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
------------------
Resolve: 'System.Collections.Immutable, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections.Immutable, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Users\gwlar\.nuget\packages\system.collections.immutable\8.0.0\lib\net7.0\System.Collections.Immutable.dll'
------------------
Resolve: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Security.Cryptography, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Security.Cryptography.dll'
------------------
Resolve: 'ICSharpCode.SharpZipLib, Version=1.4.2.13, Culture=neutral, PublicKeyToken=1b03e6acf1164f73'
Could not find by name: 'ICSharpCode.SharpZipLib, Version=1.4.2.13, Culture=neutral, PublicKeyToken=1b03e6acf1164f73'
------------------
Resolve: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Linq.dll'
------------------
Resolve: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Drawing.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Drawing.Primitives.dll'
------------------
Resolve: 'System.IO.Compression, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.IO.Compression, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.IO.Compression.dll'
------------------
Resolve: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Found single assembly: 'Microsoft.Data.Sqlite, Version=8.0.3.0, Culture=neutral, PublicKeyToken=adb9793829ddae60'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\Microsoft.Data.Sqlite.dll'
------------------
Resolve: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Data.Common, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Data.Common.dll'
------------------
Resolve: '0Harmony, Version=2.3.5.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: '0Harmony, Version=2.3.5.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\0Harmony.dll'
------------------
Resolve: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.Desktop, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.Desktop.dll'
------------------
Resolve: 'OpenTK.Audio.OpenAL, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Audio.OpenAL, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Audio.OpenAL.dll'
------------------
Resolve: 'OpenTK.Mathematics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Mathematics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Mathematics.dll'
------------------
Resolve: 'OpenTK.Windowing.Common, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.Common, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.Common.dll'
------------------
Resolve: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Windowing.GraphicsLibraryFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Windowing.GraphicsLibraryFramework.dll'
------------------
Resolve: 'DnsClient, Version=1.7.0.0, Culture=neutral, PublicKeyToken=4574bb5573c51424'
Could not find by name: 'DnsClient, Version=1.7.0.0, Culture=neutral, PublicKeyToken=4574bb5573c51424'
------------------
Resolve: 'System.IO.Pipes, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.IO.Pipes, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.IO.Pipes.dll'
------------------
Resolve: 'OpenTK.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'OpenTK.Graphics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Users\gwlar\AppData\Roaming\Vintagestory\Lib\OpenTK.Graphics.dll'
------------------
Resolve: 'System.Diagnostics.FileVersionInfo, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.FileVersionInfo, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.FileVersionInfo.dll'
------------------
Resolve: 'System.ComponentModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.dll'
------------------
Resolve: 'csvorbis, Version=1.0.4143.14181, Culture=neutral, PublicKeyToken=eaa89a1626beb708'
Could not find by name: 'csvorbis, Version=1.0.4143.14181, Culture=neutral, PublicKeyToken=eaa89a1626beb708'
------------------
Resolve: 'csogg, Version=1.0.4143.14181, Culture=neutral, PublicKeyToken=cbfcc0aaeece6bdb'
Could not find by name: 'csogg, Version=1.0.4143.14181, Culture=neutral, PublicKeyToken=cbfcc0aaeece6bdb'
------------------
Resolve: 'System.Diagnostics.TraceSource, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Diagnostics.TraceSource, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Diagnostics.TraceSource.dll'
------------------
Resolve: 'xplatforminterface, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'xplatforminterface, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.ComponentModel.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.ComponentModel.Primitives.dll'
------------------
Resolve: 'Microsoft.Win32.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'Microsoft.Win32.Primitives, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\Microsoft.Win32.Primitives.dll'
------------------
Resolve: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'System.Runtime.CompilerServices.Unsafe, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.10\ref\net7.0\System.Runtime.CompilerServices.Unsafe.dll'
#endif
