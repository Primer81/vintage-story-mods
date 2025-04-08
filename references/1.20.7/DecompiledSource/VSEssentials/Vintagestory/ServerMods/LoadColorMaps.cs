using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace Vintagestory.ServerMods;

public class LoadColorMaps : ModSystem
{
	private ICoreAPI api;

	public override double ExecuteOrder()
	{
		return 0.3;
	}

	public override void Start(ICoreAPI api)
	{
		this.api = api;
	}

	public override void AssetsLoaded(ICoreAPI api)
	{
		if (api is ICoreClientAPI)
		{
			loadColorMaps();
		}
	}

	private void loadColorMaps()
	{
		try
		{
			IAsset asset = api.Assets.TryGet("config/colormaps.json");
			if (asset != null)
			{
				ColorMap[] array = asset.ToObject<ColorMap[]>();
				foreach (ColorMap val in array)
				{
					api.RegisterColorMap(val);
				}
			}
		}
		catch (Exception e)
		{
			api.World.Logger.Error("Failed loading config/colormap.json. Will skip");
			api.World.Logger.Error(e);
		}
	}
}
