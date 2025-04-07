#region Assembly VintagestoryLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// location unknown
// Decompiled with ICSharpCode.Decompiler 8.2.0.7535
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProperVersion;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace Vintagestory.Common;

public class ModLoader : IModLoader
{
    private readonly ICoreAPI api;

    private readonly EnumAppSide side;

    private readonly ILogger logger;

    private bool traceLog;

    private readonly ModCompilationContext compilationContext = new ModCompilationContext();

    private Dictionary<string, ModContainer> loadedMods = new Dictionary<string, ModContainer>();

    private List<ModSystem> enabledSystems = new List<ModSystem>();

    public List<string> MissingDependencies = new List<string>();

    internal OrderedDictionary<string, IAssetOrigin> contentAssetOrigins;

    internal OrderedDictionary<string, IAssetOrigin> themeAssetOrigins;

    public int TextureSize { get; set; } = 32;


    public IReadOnlyCollection<string> ModSearchPaths { get; }

    public string UnpackPath { get; } = Path.Combine(GamePaths.Cache, "unpack");


    public IEnumerable<Mod> Mods => loadedMods.Values.Where((ModContainer mod) => mod.Enabled);

    public IEnumerable<ModSystem> Systems => enabledSystems.Select((ModSystem x) => x);

    public Mod GetMod(string modID)
    {
        if (!loadedMods.TryGetValue(modID, out var value))
        {
            return null;
        }

        if (!value.Enabled)
        {
            return null;
        }

        return value;
    }

    public bool IsModEnabled(string modID)
    {
        return (GetMod(modID) as ModContainer)?.Enabled ?? false;
    }

    public ModSystem GetModSystem(string fullName)
    {
        return Systems.FirstOrDefault((ModSystem mod) => string.Equals(mod.GetType().FullName, fullName, StringComparison.InvariantCultureIgnoreCase));
    }

    public T GetModSystem<T>(bool withInheritance = true) where T : ModSystem
    {
        if (withInheritance)
        {
            return Systems.OfType<T>().FirstOrDefault();
        }

        return Systems.FirstOrDefault((ModSystem mod) => mod.GetType() == typeof(T)) as T;
    }

    public bool IsModSystemEnabled(string fullName)
    {
        return GetModSystem(fullName) != null;
    }

    public ModLoader(ILogger logger, EnumAppSide side, IEnumerable<string> modSearchPaths, bool traceLog)
        : this(null, side, logger, modSearchPaths, traceLog)
    {
    }

    public ModLoader(ICoreAPI api, IEnumerable<string> modSearchPaths, bool traceLog)
        : this(api, api.Side, api.World.Logger, modSearchPaths, traceLog)
    {
    }

    private ModLoader(ICoreAPI api, EnumAppSide side, ILogger logger, IEnumerable<string> modSearchPaths, bool traceLog)
    {
        this.api = api;
        this.side = side;
        this.logger = logger;
        this.traceLog = traceLog;
        ModSearchPaths = modSearchPaths.Select((string path) => Path.IsPathRooted(path) ? path : Path.Combine(GamePaths.Binaries, path)).ToList().AsReadOnly();
    }

    public OrderedDictionary<string, IAssetOrigin> GetContentArchives()
    {
        return contentAssetOrigins;
    }

    public OrderedDictionary<string, IAssetOrigin> GetThemeArchives()
    {
        return themeAssetOrigins;
    }

    public List<ModContainer> LoadModInfos()
    {
        List<ModContainer> list = CollectMods();
        using ModAssemblyLoader loader = new ModAssemblyLoader(ModSearchPaths, list);
        foreach (ModContainer item in list)
        {
            item.LoadModInfo(compilationContext, loader);
        }

        return list;
    }

    public List<ModContainer> LoadModInfosAndVerify(IEnumerable<string> disabledModsByIdAndVersion = null)
    {
        List<ModContainer> mods = LoadModInfos();
        return DisableAndVerify(mods, disabledModsByIdAndVersion);
    }

    public List<ModContainer> DisableAndVerify(List<ModContainer> mods, IEnumerable<string> disabledModsByIdAndVersion = null)
    {
        if (disabledModsByIdAndVersion != null && disabledModsByIdAndVersion.Count() > 0)
        {
            DisableMods(mods, disabledModsByIdAndVersion);
        }

        return verifyMods(mods);
    }

    public void LoadMods(IEnumerable<string> disabledModsByIdAndVersion = null)
    {
        List<ModContainer> mods = LoadModInfos();
        LoadMods(mods, disabledModsByIdAndVersion);
    }

    public void LoadMods(List<ModContainer> mods, IEnumerable<string> disabledModsByIdAndVersion = null)
    {
        if (disabledModsByIdAndVersion != null && disabledModsByIdAndVersion.Count() > 0)
        {
            using (ModAssemblyLoader loader = new ModAssemblyLoader(ModSearchPaths, mods))
            {
                foreach (ModContainer mod in mods)
                {
                    mod.LoadModInfo(compilationContext, loader);
                }
            }

            int num = DisableMods(mods, disabledModsByIdAndVersion);
            logger.Notification("Found {0} mods ({1} disabled)", mods.Count, num);
        }
        else
        {
            logger.Notification("Found {0} mods (0 disabled)", mods.Count);
        }

        mods = verifyMods(mods);
        logger.Notification("Mods, sorted by dependency: {0}", string.Join(", ", mods.Select((ModContainer m) => m.Info.ModID)));
        foreach (ModContainer mod2 in mods)
        {
            if (mod2.Enabled)
            {
                mod2.Unpack(UnpackPath);
            }
        }

        ClearCacheFolder(mods);
        enabledSystems = instantiateMods(mods);
    }

    private List<ModContainer> verifyMods(List<ModContainer> mods)
    {
        CheckDuplicateModIDMods(mods);
        return CheckAndSortDependencies(mods);
    }

    private List<ModSystem> instantiateMods(List<ModContainer> mods)
    {
        List<ModSystem> list = new List<ModSystem>();
        mods = mods.OrderBy((ModContainer mod) => mod.RequiresCompilation).ToList();
        using (ModAssemblyLoader loader = new ModAssemblyLoader(ModSearchPaths, mods))
        {
            foreach (ModContainer mod in mods)
            {
                if (mod.Enabled)
                {
                    mod.LoadAssembly(compilationContext, loader);
                }
            }
        }

        logger.VerboseDebug("{0} assemblies loaded", mods.Count);
        if (mods.Any((ModContainer mod) => mod.Error.HasValue && mod.RequiresCompilation))
        {
            logger.Warning("One or more source code mods failed to compile. Info to modders: In case you cannot find the problem, be aware that the game engine currently can only compile C# code until version 5.0. Any language features from C#6.0 or above will result in compile errors.");
        }

        foreach (ModContainer mod2 in mods)
        {
            if (mod2.Enabled)
            {
                logger.VerboseDebug("Instantiate mod systems for {0}", mod2.Info.ModID);
                mod2.InstantiateModSystems(side);
            }
        }

        contentAssetOrigins = new OrderedDictionary<string, IAssetOrigin>();
        themeAssetOrigins = new OrderedDictionary<string, IAssetOrigin>();
        OrderedDictionary<string, int> orderedDictionary = new OrderedDictionary<string, int>();
        foreach (ModContainer item in mods.Where((ModContainer mod) => mod.Enabled))
        {
            loadedMods.Add(item.Info.ModID, item);
            list.AddRange(item.Systems);
            if (item.FolderPath != null && Directory.Exists(Path.Combine(item.FolderPath, "assets")))
            {
                bool num = item.Info.Type == EnumModType.Theme;
                OrderedDictionary<string, IAssetOrigin> orderedDictionary2 = (num ? themeAssetOrigins : contentAssetOrigins);
                FolderOrigin value = (num ? new ThemeFolderOrigin(item.FolderPath, (api.Side == EnumAppSide.Client) ? "textures/" : null) : new FolderOrigin(item.FolderPath, (api.Side == EnumAppSide.Client) ? "textures/" : null));
                orderedDictionary2.Add(item.FileName, value);
                orderedDictionary.Add(item.FileName, item.Info.TextureSize);
            }
        }

        if (orderedDictionary.Count > 0)
        {
            TextureSize = orderedDictionary.Values.Last();
        }

        list = list.OrderBy((ModSystem system) => system.ExecuteOrder()).ToList();
        logger.Notification("Instantiated {0} mod systems from {1} enabled mods", list.Count, Mods.Count());
        return list;
    }

    private void ClearCacheFolder(IEnumerable<ModContainer> mods)
    {
        if (!Directory.Exists(UnpackPath))
        {
            return;
        }

        foreach (string item in Directory.GetDirectories(UnpackPath).Except<string>(from mod in mods
                                                                                    where !mod.Error.HasValue
                                                                                    select mod.FolderPath, StringComparer.InvariantCultureIgnoreCase))
        {
            try
            {
                string[] files = Directory.GetFiles(item, "*.dll");
                for (int i = 0; i < files.Length; i++)
                {
                    File.Delete(files[i]);
                }
            }
            catch
            {
                break;
            }

            try
            {
                Directory.Delete(item, recursive: true);
            }
            catch (Exception e)
            {
                logger.Error("There was an exception deleting the cached mod folder '{0}':");
                logger.Error(e);
            }
        }
    }

    private List<ModContainer> CollectMods()
    {
        List<DirectoryInfo> list = (from path in ModSearchPaths
                                    select new DirectoryInfo(path) into dirInfo
                                    group dirInfo by dirInfo.FullName.ToLowerInvariant() into @group
                                    select @group.First()).ToList();
        logger.Notification("Will search the following paths for mods:");
        foreach (DirectoryInfo item in list)
        {
            if (item.Exists)
            {
                logger.Notification("    {0}", item.FullName);
            }
            else
            {
                logger.Notification("    {0} (Not found?)", item.FullName);
            }
        }

        return (from fsInfo in list.Where((DirectoryInfo dirInfo) => dirInfo.Exists).SelectMany((DirectoryInfo dirInfo) => dirInfo.GetFileSystemInfos())
                where ModContainer.GetSourceType(fsInfo).HasValue
                select new ModContainer(fsInfo, logger, traceLog)).OrderBy<ModContainer, string>((ModContainer mod) => mod.FileName, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private int DisableMods(IEnumerable<ModContainer> mods, IEnumerable<string> disabledModsByIdAndVersion)
    {
        if (disabledModsByIdAndVersion == null)
        {
            return 0;
        }

        HashSet<string> disabledSet = new HashSet<string>(disabledModsByIdAndVersion);
        List<ModContainer> list = mods.Where((ModContainer mod) => mod?.Info == null || disabledSet.Contains(mod.Info.ModID + "@" + mod.Info.Version) || disabledSet.Contains(mod.Info.ModID ?? "")).ToList();
        foreach (ModContainer item in list)
        {
            item.Status = ModStatus.Disabled;
        }

        return list.Count();
    }

    private void CheckDuplicateModIDMods(IEnumerable<ModContainer> mods)
    {
        foreach (IGrouping<string, ModContainer> item in from mod in mods
                                                         where mod.Info?.ModID != null && mod.Enabled
                                                         group mod by mod.Info.ModID into @group
                                                         where @group.Skip(1).Any()
                                                         select @group)
        {
            IOrderedEnumerable<ModContainer> source = item.OrderBy((ModContainer mod) => mod.Info);
            logger.Warning("Multiple mods share the mod ID '{0}' ({1}). Will only load the highest version one - v{2}.", item.Key, string.Join(", ", item.Select((ModContainer m) => "'" + m.FileName + "'")), source.First().Info.Version);
            foreach (ModContainer item2 in source.Skip(1))
            {
                item2.SetError(ModError.Loading);
            }
        }
    }

    private List<ModContainer> CheckAndSortDependencies(IEnumerable<ModContainer> mods)
    {
        mods = mods.Where((ModContainer mod) => !mod.Error.HasValue && mod.Enabled).ToList();
        List<ModContainer> list = new List<ModContainer>();
        HashSet<ModContainer> hashSet = new HashSet<ModContainer>(mods);
        List<ModContainer> list2 = new List<ModContainer>();
        Dictionary<string, ModContainer> dictionary = mods.Where((ModContainer mod) => mod.Info?.ModID != null).ToDictionary((ModContainer mod) => mod.Info.ModID);
        do
        {
            list2.Clear();
            foreach (ModContainer item in hashSet)
            {
                bool flag = true;
                if (item.Info != null)
                {
                    foreach (ModDependency dependency in item.Info.Dependencies)
                    {
                        if (!dictionary.TryGetValue(dependency.ModID, out var value) || !SatisfiesVersion(dependency.Version, value.Info.Version) || !value.Enabled)
                        {
                            item.SetError(ModError.Dependency);
                        }
                        else if (hashSet.Contains(value))
                        {
                            flag = false;
                        }
                    }
                }

                if (flag)
                {
                    list2.Add(item);
                }
            }

            foreach (ModContainer item2 in list2)
            {
                hashSet.Remove(item2);
                list.Add(item2);
            }
        }
        while (list2.Count > 0);
        foreach (ModContainer mod in mods)
        {
            if (mod.Enabled || mod.Status == ModStatus.Disabled)
            {
                continue;
            }

            mod.Logger.Error("Could not resolve some dependencies:");
            foreach (ModDependency dependency2 in mod.Info.Dependencies)
            {
                if (!dictionary.TryGetValue(dependency2.ModID, out var value2))
                {
                    mod.Logger.Error("    {0} - Missing", dependency2);
                    MissingDependencies.Add(dependency2.ModID + "@" + dependency2.Version);
                    if (mod.MissingDependencies == null)
                    {
                        mod.MissingDependencies = new List<string>();
                    }

                    mod.MissingDependencies.Add(dependency2.ModID + "@" + dependency2.Version);
                }
                else if (!SatisfiesVersion(dependency2.Version, value2.Info.Version))
                {
                    mod.Logger.Error("    {0} - Version mismatch (has {1})", dependency2, value2.Info.Version);
                    MissingDependencies.Add(dependency2.ModID + "@" + dependency2.Version);
                    if (mod.MissingDependencies == null)
                    {
                        mod.MissingDependencies = new List<string>();
                    }

                    mod.MissingDependencies.Add(dependency2.ModID + "@" + dependency2.Version);
                }
                else if (value2.Error == ModError.Loading)
                {
                    mod.Logger.Error("    {0} - Dependency {1} failed loading", dependency2, value2);
                }
                else if (value2.Error.GetValueOrDefault() == ModError.Dependency)
                {
                    mod.Logger.Error("    {0} - Dependency {1} has dependency errors itself", dependency2, value2);
                }
                else if (!value2.Enabled)
                {
                    mod.Logger.Error("    {0} - Dependency {1} is not enabled", dependency2, value2);
                }
            }
        }

        if (hashSet.Count > 0)
        {
            logger.Warning("Possible cyclic dependencies between mods: " + string.Join(", ", hashSet));
            list.AddRange(hashSet);
        }

        return list;
    }

    private bool SatisfiesVersion(string requested, string provided)
    {
        if (string.IsNullOrEmpty(requested) || string.IsNullOrEmpty(provided) || requested == "*")
        {
            return true;
        }

        SemVer.TryParse(requested, out var result);
        SemVer.TryParse(provided, out var result2);
        return result2 >= result;
    }

    public void RunModPhase(ModRunPhase phase)
    {
        RunModPhase(ref enabledSystems, phase);
    }

    public void RunModPhase(ref List<ModSystem> enabledSystems, ModRunPhase phase)
    {
        if (phase != ModRunPhase.Normal)
        {
            foreach (ModSystem enabledSystem in enabledSystems)
            {
                if (enabledSystem != null && enabledSystem.ShouldLoad(api) && !TryRunModPhase(enabledSystem.Mod, enabledSystem, api, phase))
                {
                    logger.Error("Failed to run mod phase {0} for mod {1}", phase, enabledSystem);
                }
            }

            return;
        }

        List<ModSystem> list = new List<ModSystem>();
        foreach (ModSystem enabledSystem2 in enabledSystems)
        {
            if (enabledSystem2.ShouldLoad(api))
            {
                logger.VerboseDebug("Starting system: " + enabledSystem2.GetType().Name);
                if (TryRunModPhase(enabledSystem2.Mod, enabledSystem2, api, ModRunPhase.Normal))
                {
                    list.Add(enabledSystem2);
                    continue;
                }

                logger.Error("Failed to start system {0}", enabledSystem2);
            }
        }

        logger.Notification("Started {0} systems on {1}:", list.Count, api.Side);
        foreach (IGrouping<Mod, ModSystem> item in from system in list
                                                   group system by system.Mod)
        {
            logger.Notification("    Mod {0}:", item.Key);
            foreach (ModSystem item2 in item)
            {
                logger.Notification("        {0}", item2);
            }
        }

        enabledSystems = list;
    }

    private bool TryRunModPhase(Mod mod, ModSystem system, ICoreAPI api, ModRunPhase phase)
    {
        try
        {
            switch (phase)
            {
                case ModRunPhase.Pre:
                    system.StartPre(api);
                    break;
                case ModRunPhase.Start:
                    system.Start(api);
                    break;
                case ModRunPhase.AssetsLoaded:
                    system.AssetsLoaded(api);
                    break;
                case ModRunPhase.AssetsFinalize:
                    system.AssetsFinalize(api);
                    break;
                case ModRunPhase.Normal:
                    if (api.Side == EnumAppSide.Client)
                    {
                        system.StartClientSide(api as ICoreClientAPI);
                    }
                    else
                    {
                        system.StartServerSide(api as ICoreServerAPI);
                    }

                    break;
                case ModRunPhase.Dispose:
                    system.Dispose();
                    break;
            }

            return true;
        }
        catch (FormatException ex)
        {
            throw ex;
        }
        catch (Exception e)
        {
            mod.Logger.Error("An exception was thrown when trying to start the mod:");
            mod.Logger.Error(e);
        }

        return false;
    }

    public void Dispose()
    {
        RunModPhase(ModRunPhase.Dispose);
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
