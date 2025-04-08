using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace Vintagestory.Common;

public class PathOrigin : IAssetOrigin
{
	protected string fullPath;

	protected string domain;

	public string OriginPath => fullPath;

	public string Domain => domain;

	public PathOrigin(string domain, string fullPath)
		: this(domain, fullPath, null)
	{
	}

	public PathOrigin(string domain, string fullPath, string pathForReservedCharsCheck)
	{
		this.domain = domain.ToLowerInvariant();
		this.fullPath = fullPath;
		if (!this.fullPath.EndsWith(Path.DirectorySeparatorChar))
		{
			ReadOnlySpan<char> readOnlySpan = this.fullPath;
			char reference = Path.DirectorySeparatorChar;
			this.fullPath = string.Concat(readOnlySpan, new ReadOnlySpan<char>(in reference));
		}
		if (pathForReservedCharsCheck != null)
		{
			CheckForReservedCharacters(domain, pathForReservedCharsCheck);
		}
	}

	public void LoadAsset(IAsset asset)
	{
		if (asset.Location.Domain != domain)
		{
			throw new Exception("Invalid LoadAsset call or invalid asset instance. Trying to load [" + asset?.ToString() + "] from domain " + domain + " is bound to fail.");
		}
		string path = fullPath + asset.Location.Path.Replace('/', Path.DirectorySeparatorChar);
		if (!File.Exists(path))
		{
			throw new Exception(string.Concat("Requested asset [", asset.Location, "] could not be found"));
		}
		asset.Data = File.ReadAllBytes(path);
	}

	public bool TryLoadAsset(IAsset asset)
	{
		if (asset.Location.Domain != domain)
		{
			return false;
		}
		string path = fullPath + (asset as Asset).FilePath.Replace('/', Path.DirectorySeparatorChar);
		if (!File.Exists(path))
		{
			return false;
		}
		asset.Data = File.ReadAllBytes(path);
		return true;
	}

	public List<IAsset> GetAssets(AssetCategory Category, bool shouldLoad = true)
	{
		List<IAsset> assets = new List<IAsset>();
		ScanAssetFolderRecursive(fullPath + Category.Code, assets, shouldLoad);
		return assets;
	}

	public List<IAsset> GetAssets(AssetLocation baseLocation, bool shouldLoad = true)
	{
		List<IAsset> assets = new List<IAsset>();
		ScanAssetFolderRecursive(fullPath + baseLocation.Path, assets, shouldLoad);
		return assets;
	}

	private void ScanAssetFolderRecursive(string currentPath, List<IAsset> list, bool shouldLoad)
	{
		if (!Directory.Exists(currentPath))
		{
			return;
		}
		string[] directories = Directory.GetDirectories(currentPath);
		foreach (string fullPath2 in directories)
		{
			ScanAssetFolderRecursive(fullPath2, list, shouldLoad);
		}
		directories = Directory.GetFiles(currentPath);
		foreach (string fullPath in directories)
		{
			FileInfo f = new FileInfo(fullPath);
			if (!f.Name.Equals("thumbs.db", StringComparison.InvariantCultureIgnoreCase) && !f.Name.EndsWithOrdinal(".psd") && !f.Name.StartsWith('.'))
			{
				string path = fullPath.Substring(this.fullPath.Length).Replace(Path.DirectorySeparatorChar, '/');
				AssetLocation location = new AssetLocation(domain, path.ToLowerInvariant());
				Asset asset = new Asset(shouldLoad ? File.ReadAllBytes(fullPath) : null, location, this);
				asset.FilePath = path;
				list.Add(asset);
			}
		}
	}

	public bool IsAllowedToAffectGameplay()
	{
		return true;
	}

	public string GetDefaultDomain()
	{
		return domain;
	}

	public virtual void CheckForReservedCharacters(string domain, string path)
	{
		path = ((path == null) ? OriginPath : Path.Combine(OriginPath, path));
		if (!Directory.Exists(path))
		{
			return;
		}
		DirectoryInfo directoryInfo = new DirectoryInfo(path);
		int dirPathLength = directoryInfo.FullName.Length - 9;
		foreach (FileInfo item in directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories))
		{
			string filepath = item.FullName.Substring(dirPathLength + 1);
			FolderOrigin.CheckForReservedCharacters(domain, filepath);
		}
	}
}
