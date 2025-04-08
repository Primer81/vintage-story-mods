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
		if (!loadedMods.TryGetValue(modID, out var mod))
		{
			return null;
		}
		if (!mod.Enabled)
		{
			return null;
		}
		return mod;
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
		List<ModContainer> mods = CollectMods();
		using ModAssemblyLoader loader = new ModAssemblyLoader(ModSearchPaths, mods);
		foreach (ModContainer item in mods)
		{
			item.LoadModInfo(compilationContext, loader);
		}
		return mods;
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
				foreach (ModContainer mod2 in mods)
				{
					mod2.LoadModInfo(compilationContext, loader);
				}
			}
			int disabledModCount = DisableMods(mods, disabledModsByIdAndVersion);
			logger.Notification("Found {0} mods ({1} disabled)", mods.Count, disabledModCount);
		}
		else
		{
			logger.Notification("Found {0} mods (0 disabled)", mods.Count);
		}
		mods = verifyMods(mods);
		logger.Notification("Mods, sorted by dependency: {0}", string.Join(", ", mods.Select((ModContainer m) => m.Info.ModID)));
		foreach (ModContainer mod in mods)
		{
			if (mod.Enabled)
			{
				mod.Unpack(UnpackPath);
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
		List<ModSystem> enabledSystems = new List<ModSystem>();
		mods = mods.OrderBy((ModContainer mod) => mod.RequiresCompilation).ToList();
		using (ModAssemblyLoader loader = new ModAssemblyLoader(ModSearchPaths, mods))
		{
			foreach (ModContainer mod4 in mods)
			{
				if (mod4.Enabled)
				{
					mod4.LoadAssembly(compilationContext, loader);
				}
			}
		}
		logger.VerboseDebug("{0} assemblies loaded", mods.Count);
		if (mods.Any((ModContainer mod) => mod.Error.HasValue && mod.RequiresCompilation))
		{
			logger.Warning("One or more source code mods failed to compile. Info to modders: In case you cannot find the problem, be aware that the game engine currently can only compile C# code until version 5.0. Any language features from C#6.0 or above will result in compile errors.");
		}
		foreach (ModContainer mod3 in mods)
		{
			if (mod3.Enabled)
			{
				logger.VerboseDebug("Instantiate mod systems for {0}", mod3.Info.ModID);
				mod3.InstantiateModSystems(side);
			}
		}
		contentAssetOrigins = new OrderedDictionary<string, IAssetOrigin>();
		themeAssetOrigins = new OrderedDictionary<string, IAssetOrigin>();
		OrderedDictionary<string, int> textureSizes = new OrderedDictionary<string, int>();
		foreach (ModContainer mod2 in mods.Where((ModContainer mod) => mod.Enabled))
		{
			loadedMods.Add(mod2.Info.ModID, mod2);
			enabledSystems.AddRange(mod2.Systems);
			if (mod2.FolderPath != null && Directory.Exists(Path.Combine(mod2.FolderPath, "assets")))
			{
				bool num = mod2.Info.Type == EnumModType.Theme;
				OrderedDictionary<string, IAssetOrigin> origins = (num ? themeAssetOrigins : contentAssetOrigins);
				FolderOrigin origin = (num ? new ThemeFolderOrigin(mod2.FolderPath, (api.Side == EnumAppSide.Client) ? "textures/" : null) : new FolderOrigin(mod2.FolderPath, (api.Side == EnumAppSide.Client) ? "textures/" : null));
				origins.Add(mod2.FileName, origin);
				textureSizes.Add(mod2.FileName, mod2.Info.TextureSize);
			}
		}
		if (textureSizes.Count > 0)
		{
			TextureSize = textureSizes.Values.Last();
		}
		enabledSystems = enabledSystems.OrderBy((ModSystem system) => system.ExecuteOrder()).ToList();
		logger.Notification("Instantiated {0} mod systems from {1} enabled mods", enabledSystems.Count, Mods.Count());
		return enabledSystems;
	}

	private void ClearCacheFolder(IEnumerable<ModContainer> mods)
	{
		if (!Directory.Exists(UnpackPath))
		{
			return;
		}
		foreach (string folder in Directory.GetDirectories(UnpackPath).Except<string>(from mod in mods
			where !mod.Error.HasValue
			select mod.FolderPath, StringComparer.InvariantCultureIgnoreCase))
		{
			try
			{
				string[] files = Directory.GetFiles(folder, "*.dll");
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
				Directory.Delete(folder, recursive: true);
			}
			catch (Exception ex)
			{
				logger.Error("There was an exception deleting the cached mod folder '{0}':");
				logger.Error(ex);
			}
		}
	}

	private List<ModContainer> CollectMods()
	{
		List<DirectoryInfo> dirInfos = (from path in ModSearchPaths
			select new DirectoryInfo(path) into dirInfo
			group dirInfo by dirInfo.FullName.ToLowerInvariant() into @group
			select @group.First()).ToList();
		logger.Notification("Will search the following paths for mods:");
		foreach (DirectoryInfo dirInfo2 in dirInfos)
		{
			if (dirInfo2.Exists)
			{
				logger.Notification("    {0}", dirInfo2.FullName);
			}
			else
			{
				logger.Notification("    {0} (Not found?)", dirInfo2.FullName);
			}
		}
		return (from fsInfo in dirInfos.Where((DirectoryInfo dirInfo) => dirInfo.Exists).SelectMany((DirectoryInfo dirInfo) => dirInfo.GetFileSystemInfos())
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
		List<ModContainer> disabledMods = mods.Where((ModContainer mod) => mod?.Info == null || disabledSet.Contains(mod.Info.ModID + "@" + mod.Info.Version) || disabledSet.Contains(mod.Info.ModID ?? "")).ToList();
		foreach (ModContainer item in disabledMods)
		{
			item.Status = ModStatus.Disabled;
		}
		return disabledMods.Count();
	}

	private void CheckDuplicateModIDMods(IEnumerable<ModContainer> mods)
	{
		foreach (IGrouping<string, ModContainer> duplicateMods in from mod in mods
			where mod.Info?.ModID != null && mod.Enabled
			group mod by mod.Info.ModID into @group
			where @group.Skip(1).Any()
			select @group)
		{
			IOrderedEnumerable<ModContainer> sortedMods = duplicateMods.OrderBy((ModContainer mod) => mod.Info);
			logger.Warning("Multiple mods share the mod ID '{0}' ({1}). Will only load the highest version one - v{2}.", duplicateMods.Key, string.Join(", ", duplicateMods.Select((ModContainer m) => "'" + m.FileName + "'")), sortedMods.First().Info.Version);
			foreach (ModContainer item in sortedMods.Skip(1))
			{
				item.SetError(ModError.Loading);
			}
		}
	}

	private List<ModContainer> CheckAndSortDependencies(IEnumerable<ModContainer> mods)
	{
		mods = mods.Where((ModContainer mod) => !mod.Error.HasValue && mod.Enabled).ToList();
		List<ModContainer> sorted = new List<ModContainer>();
		HashSet<ModContainer> toCheck = new HashSet<ModContainer>(mods);
		List<ModContainer> toRemove = new List<ModContainer>();
		Dictionary<string, ModContainer> lookup = mods.Where((ModContainer mod) => mod.Info?.ModID != null).ToDictionary((ModContainer mod) => mod.Info.ModID);
		do
		{
			toRemove.Clear();
			foreach (ModContainer mod4 in toCheck)
			{
				bool dependenciesMet = true;
				if (mod4.Info != null)
				{
					foreach (ModDependency dependency2 in mod4.Info.Dependencies)
					{
						if (!lookup.TryGetValue(dependency2.ModID, out var dependingMod2) || !SatisfiesVersion(dependency2.Version, dependingMod2.Info.Version) || !dependingMod2.Enabled)
						{
							mod4.SetError(ModError.Dependency);
						}
						else if (toCheck.Contains(dependingMod2))
						{
							dependenciesMet = false;
						}
					}
				}
				if (dependenciesMet)
				{
					toRemove.Add(mod4);
				}
			}
			foreach (ModContainer mod3 in toRemove)
			{
				toCheck.Remove(mod3);
				sorted.Add(mod3);
			}
		}
		while (toRemove.Count > 0);
		foreach (ModContainer mod2 in mods)
		{
			if (mod2.Enabled || mod2.Status == ModStatus.Disabled)
			{
				continue;
			}
			mod2.Logger.Error("Could not resolve some dependencies:");
			foreach (ModDependency dependency in mod2.Info.Dependencies)
			{
				if (!lookup.TryGetValue(dependency.ModID, out var dependingMod))
				{
					mod2.Logger.Error("    {0} - Missing", dependency);
					MissingDependencies.Add(dependency.ModID + "@" + dependency.Version);
					if (mod2.MissingDependencies == null)
					{
						mod2.MissingDependencies = new List<string>();
					}
					mod2.MissingDependencies.Add(dependency.ModID + "@" + dependency.Version);
				}
				else if (!SatisfiesVersion(dependency.Version, dependingMod.Info.Version))
				{
					mod2.Logger.Error("    {0} - Version mismatch (has {1})", dependency, dependingMod.Info.Version);
					MissingDependencies.Add(dependency.ModID + "@" + dependency.Version);
					if (mod2.MissingDependencies == null)
					{
						mod2.MissingDependencies = new List<string>();
					}
					mod2.MissingDependencies.Add(dependency.ModID + "@" + dependency.Version);
				}
				else if (dependingMod.Error == ModError.Loading)
				{
					mod2.Logger.Error("    {0} - Dependency {1} failed loading", dependency, dependingMod);
				}
				else if (dependingMod.Error.GetValueOrDefault() == ModError.Dependency)
				{
					mod2.Logger.Error("    {0} - Dependency {1} has dependency errors itself", dependency, dependingMod);
				}
				else if (!dependingMod.Enabled)
				{
					mod2.Logger.Error("    {0} - Dependency {1} is not enabled", dependency, dependingMod);
				}
			}
		}
		if (toCheck.Count > 0)
		{
			logger.Warning("Possible cyclic dependencies between mods: " + string.Join(", ", toCheck));
			sorted.AddRange(toCheck);
		}
		return sorted;
	}

	private bool SatisfiesVersion(string requested, string provided)
	{
		if (string.IsNullOrEmpty(requested) || string.IsNullOrEmpty(provided) || requested == "*")
		{
			return true;
		}
		SemVer.TryParse(requested, out var reqVersion);
		SemVer.TryParse(provided, out var provVersion);
		return provVersion >= reqVersion;
	}

	public void RunModPhase(ModRunPhase phase)
	{
		RunModPhase(ref enabledSystems, phase);
	}

	public void RunModPhase(ref List<ModSystem> enabledSystems, ModRunPhase phase)
	{
		if (phase != ModRunPhase.Normal)
		{
			foreach (ModSystem system2 in enabledSystems)
			{
				if (system2 != null && system2.ShouldLoad(api) && !TryRunModPhase(system2.Mod, system2, api, phase))
				{
					logger.Error("Failed to run mod phase {0} for mod {1}", phase, system2);
				}
			}
			return;
		}
		List<ModSystem> startedSystems = new List<ModSystem>();
		foreach (ModSystem system4 in enabledSystems)
		{
			if (system4.ShouldLoad(api))
			{
				logger.VerboseDebug("Starting system: " + system4.GetType().Name);
				if (TryRunModPhase(system4.Mod, system4, api, ModRunPhase.Normal))
				{
					startedSystems.Add(system4);
					continue;
				}
				logger.Error("Failed to start system {0}", system4);
			}
		}
		logger.Notification("Started {0} systems on {1}:", startedSystems.Count, api.Side);
		foreach (IGrouping<Mod, ModSystem> group in from system in startedSystems
			group system by system.Mod)
		{
			logger.Notification("    Mod {0}:", group.Key);
			foreach (ModSystem system3 in group)
			{
				logger.Notification("        {0}", system3);
			}
		}
		enabledSystems = startedSystems;
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
		catch (FormatException ex2)
		{
			throw ex2;
		}
		catch (Exception ex)
		{
			mod.Logger.Error("An exception was thrown when trying to start the mod:");
			mod.Logger.Error(ex);
		}
		return false;
	}

	public void Dispose()
	{
		RunModPhase(ModRunPhase.Dispose);
	}
}
