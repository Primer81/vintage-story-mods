using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.Common;

namespace Vintagestory.Client.NoObf;

public class UnloadableShape : Shape
{
	public bool Loaded;

	public IDictionary<string, CompositeTexture> TexturesResolved;

	public void Unload()
	{
		Loaded = false;
		Textures = null;
		Elements = null;
		Animations = null;
		AnimationsByCrc32 = null;
		JointsById = null;
	}

	public bool Load(ClientMain game, AssetLocationAndSource srcandLoc)
	{
		Loaded = true;
		AssetLocation newLocation = srcandLoc.CopyWithPathPrefixAndAppendixOnce("shapes/", ".json");
		IAsset asset = ScreenManager.Platform.AssetManager.TryGet(newLocation);
		if (asset == null)
		{
			game.Platform.Logger.Warning("Did not find required shape {0} anywhere. (defined in {1})", newLocation, srcandLoc.Source);
			return false;
		}
		try
		{
			ShapeElement.locationForLogging = newLocation;
			JsonUtil.PopulateObject(this, Asset.BytesToString(asset.Data), asset.Location.Domain);
			return true;
		}
		catch (Exception e)
		{
			game.Platform.Logger.Warning("Failed parsing shape model {0}\n{1}", newLocation, e.Message);
			return false;
		}
	}

	public void ResolveTextures(Dictionary<AssetLocation, CompositeTexture> shapeTexturesCache)
	{
		FastSmallDictionary<string, CompositeTexture> dict = new FastSmallDictionary<string, CompositeTexture>(Textures.Count);
		foreach (KeyValuePair<string, AssetLocation> val in Textures)
		{
			AssetLocation textureLoc = val.Value;
			if (!shapeTexturesCache.TryGetValue(textureLoc, out var ct))
			{
				ct = (shapeTexturesCache[textureLoc] = new CompositeTexture
				{
					Base = textureLoc
				});
			}
			dict.Add(val.Key, ct);
		}
		TexturesResolved = dict;
	}
}
