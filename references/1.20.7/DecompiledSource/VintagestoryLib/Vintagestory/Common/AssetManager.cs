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
		int count = 0;
		foreach (AssetCategory category in AssetCategory.categories.Values)
		{
			if ((category.SideType & side) <= (EnumAppSide)0)
			{
				continue;
			}
			Dictionary<AssetLocation, IAsset> categoryassets = GetAssetsDontLoad(category, Origins);
			foreach (IAsset asset in categoryassets.Values)
			{
				Assets[asset.Location] = asset;
			}
			count += categoryassets.Count;
			assetsByCategory[category.Code] = categoryassets.Values.ToList();
			Logger?.Notification("Found {1} base assets in category {0}", category, categoryassets.Count);
		}
		return count;
	}

	public int AddExternalAssets(ILogger Logger, ModLoader modloader = null)
	{
		List<string> assetOriginsForLog = new List<string>();
		List<IAssetOrigin> externalOrigins = new List<IAssetOrigin>();
		foreach (IAssetOrigin origin2 in CustomAppOrigins)
		{
			Origins.Add(origin2);
			externalOrigins.Add(origin2);
			assetOriginsForLog.Add("arg@" + origin2.OriginPath);
		}
		foreach (IAssetOrigin origin in CustomModOrigins)
		{
			Origins.Add(origin);
			externalOrigins.Add(origin);
			assetOriginsForLog.Add("modorigin@" + origin.OriginPath);
		}
		if (modloader != null)
		{
			foreach (KeyValuePair<string, IAssetOrigin> val2 in modloader.GetContentArchives())
			{
				externalOrigins.Add(val2.Value);
				Origins.Add(val2.Value);
				assetOriginsForLog.Add("mod@" + val2.Key);
			}
			foreach (KeyValuePair<string, IAssetOrigin> val in modloader.GetThemeArchives())
			{
				externalOrigins.Add(val.Value);
				Origins.Add(val.Value);
				assetOriginsForLog.Add("themepack@" + val.Key);
			}
		}
		if (assetOriginsForLog.Count > 0)
		{
			Logger.Notification("External Origins in load order: {0}", string.Join(", ", assetOriginsForLog));
		}
		int categoryIndex = 0;
		int count = 0;
		foreach (AssetCategory category in AssetCategory.categories.Values)
		{
			if ((category.SideType & side) > (EnumAppSide)0)
			{
				Dictionary<AssetLocation, IAsset> categoryassets = GetAssetsDontLoad(category, externalOrigins);
				foreach (IAsset asset in categoryassets.Values)
				{
					Assets[asset.Location] = asset;
				}
				count += categoryassets.Count;
				if (!assetsByCategory.TryGetValue(category.Code, out var list))
				{
					list = (assetsByCategory[category.Code] = new List<IAsset>());
				}
				list.AddRange(categoryassets.Values);
				Logger.Notification("Found {1} external assets in category {0}", category, categoryassets.Count);
			}
			categoryIndex++;
		}
		allAssetsLoaded = true;
		return count;
	}

	public void UnloadExternalAssets(ILogger logger)
	{
		allAssetsLoaded = false;
		InitAndLoadBaseAssets(null);
	}

	public void UnloadAssets(AssetCategory category)
	{
		foreach (KeyValuePair<AssetLocation, IAsset> val in Assets)
		{
			if (val.Key.Category == category)
			{
				val.Value.Data = null;
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
		foreach (KeyValuePair<AssetLocation, IAsset> val in Assets)
		{
			if (!val.Value.IsPatched)
			{
				val.Value.Data = null;
			}
		}
	}

	public List<AssetLocation> GetLocations(string fullPathBeginsWith, string domain = null)
	{
		List<AssetLocation> locations = new List<AssetLocation>();
		foreach (IAsset asset in Assets.Values)
		{
			if (asset.Location.BeginsWith(domain, fullPathBeginsWith))
			{
				locations.Add(asset.Location);
			}
		}
		return locations;
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
		IAsset asset = null;
		if (!Assets.TryGetValue(Location, out asset))
		{
			return null;
		}
		if (!asset.IsLoaded() && loadAsset)
		{
			asset.Origin.TryLoadAsset(asset);
		}
		return asset;
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
		List<IAsset> foundassets = new List<IAsset>();
		if (assetsByCategory.TryGetValue(category.Code, out var categoryAssets))
		{
			foreach (IAsset asset in categoryAssets)
			{
				if (asset.Location.Category == category)
				{
					if (!asset.IsLoaded() && loadAsset)
					{
						asset.Origin.LoadAsset(asset);
					}
					foundassets.Add(asset);
				}
			}
		}
		return foundassets;
	}

	public List<IAsset> GetManyInCategory(string categoryCode, string pathBegins, string domain = null, bool loadAsset = true)
	{
		List<IAsset> foundassets = new List<IAsset>();
		if (assetsByCategory.TryGetValue(categoryCode, out var categoryAssets))
		{
			int offset = categoryCode.Length + 1;
			foreach (IAsset asset in categoryAssets)
			{
				if (asset.Location.BeginsWith(domain, pathBegins, offset))
				{
					if (loadAsset && !asset.IsLoaded())
					{
						asset.Origin.LoadAsset(asset);
					}
					foundassets.Add(asset);
				}
			}
		}
		return foundassets;
	}

	public List<IAsset> GetMany(string partialPath, string domain = null, bool loadAsset = true)
	{
		List<IAsset> foundassets = new List<IAsset>();
		foreach (KeyValuePair<AssetLocation, IAsset> val in Assets)
		{
			IAsset asset = val.Value;
			if (val.Key.BeginsWith(domain, partialPath))
			{
				if (loadAsset && !asset.IsLoaded())
				{
					asset.Origin.LoadAsset(asset);
				}
				foundassets.Add(asset);
			}
		}
		return foundassets;
	}

	public Dictionary<AssetLocation, T> GetMany<T>(ILogger logger, string fullPath, string domain = null)
	{
		Dictionary<AssetLocation, T> result = new Dictionary<AssetLocation, T>();
		foreach (Asset asset in GetMany(fullPath, domain))
		{
			try
			{
				result.Add(asset.Location, asset.ToObject<T>());
			}
			catch (JsonReaderException e)
			{
				logger.Error("Syntax error in json file '{0}': {1}", asset, e.Message);
			}
		}
		return result;
	}

	internal Dictionary<AssetLocation, IAsset> GetAssetsDontLoad(AssetCategory category, List<IAssetOrigin> fromOrigins)
	{
		Dictionary<AssetLocation, IAsset> assets = new Dictionary<AssetLocation, IAsset>();
		foreach (IAssetOrigin Origin in fromOrigins)
		{
			if (!Origin.IsAllowedToAffectGameplay() && category.AffectsGameplay)
			{
				continue;
			}
			foreach (IAsset asset in Origin.GetAssets(category, shouldLoad: false))
			{
				assets[asset.Location] = asset;
			}
		}
		return assets;
	}

	public int Reload(AssetLocation location)
	{
		Assets.RemoveAllByKey((AssetLocation x) => location == null || location.IsChild(x));
		int count = 0;
		List<IAsset> list = null;
		if (location != null)
		{
			int pathSep = location.Path.IndexOf('/');
			if (pathSep > 0)
			{
				string categoryCode = location.Path.Substring(0, pathSep);
				if (assetsByCategory.TryGetValue(categoryCode, out list))
				{
					list.RemoveAll((IAsset a) => location.IsChild(a.Location));
				}
			}
		}
		foreach (IAssetOrigin origin in Origins)
		{
			List<IAsset> locationAssets = origin.GetAssets(location);
			foreach (IAsset asset in locationAssets)
			{
				Assets[asset.Location] = asset;
				count++;
			}
			list?.AddRange(locationAssets);
		}
		return count;
	}

	public int Reload(AssetCategory category)
	{
		Assets.RemoveAllByKey((AssetLocation x) => category == null || x.Category == category);
		int count = 0;
		if (!assetsByCategory.TryGetValue(category.Code, out var list))
		{
			list = (assetsByCategory[category.Code] = new List<IAsset>());
		}
		else
		{
			list.Clear();
		}
		foreach (IAssetOrigin origin in Origins)
		{
			List<IAsset> categoryAssets = origin.GetAssets(category);
			foreach (IAsset asset in categoryAssets)
			{
				Assets[asset.Location] = asset;
				count++;
			}
			list.AddRange(categoryAssets);
		}
		foreach (KeyValuePair<AssetLocation, IAsset> val in RuntimeAssets)
		{
			if (val.Key.Category == category)
			{
				Add(val.Key, val.Value);
			}
		}
		return count;
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
			IAssetOrigin orig = CustomModOrigins[i];
			if ((orig as PathOrigin)?.OriginPath == fullPath && (orig as PathOrigin)?.Domain == domain)
			{
				return;
			}
		}
		CustomModOrigins.Add(new PathOrigin(domain, fullPath, pathForReservedCharsCheck));
	}
}
